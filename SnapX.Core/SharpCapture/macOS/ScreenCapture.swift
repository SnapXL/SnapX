import ScreenCaptureKit
import CoreGraphics
import ImageIO
import AVFoundation
import UniformTypeIdentifiers
import AppKit

@objc public class ScreenCaptureManager: NSObject, SCStreamDelegate, SCStreamOutput {
    private var stream: SCStream?
    private var latestFrame: CGImage?
    private var isCapturingContinuously: Bool = false
    private var continuousCaptureCompletion: ((Data?) -> Void)?
    private var singleCaptureCompletion: ((Data?) -> Void)?

    override public init() {
        super.init()
    }

    // MARK: - SCStreamDelegate

    public func stream(_ stream: SCStream, didOutputSampleBuffer sampleBuffer: CMSampleBuffer, of type: SCStreamOutputType) {
        guard type == .screen, CMSampleBufferGetNumSamples(sampleBuffer) == 1, CMSampleBufferIsValid(sampleBuffer), CMSampleBufferDataIsReady(sampleBuffer) else { return }
        guard let imageBuffer = CMSampleBufferGetImageBuffer(sampleBuffer) else { return }

        latestFrame = convertImageBufferToCGImage(imageBuffer)

        if isCapturingContinuously, let frame = latestFrame, let completion = continuousCaptureCompletion {
            completion(convertCGImageToPNG(frame))
        } else if let frame = latestFrame, let completion = singleCaptureCompletion {
            singleCaptureCompletion = nil // Clear before calling to prevent re-entrancy issues if stopCapture is slow
            stopCapture() // Stop after processing this single frame
            completion(convertCGImageToPNG(frame))
        }
    }

    public func stream(_ stream: SCStream, didStopWithError error: Error) {
        print("Stream stopped with error: \(error.localizedDescription)")
        DispatchQueue.main.async {
            self.isCapturingContinuously = false
            self.continuousCaptureCompletion?(nil) // Notify continuous capture might have failed
            self.continuousCaptureCompletion = nil
            self.singleCaptureCompletion?(nil) // Notify single capture might have failed
            self.singleCaptureCompletion = nil
            self.stream = nil // Ensure stream is nil'd out
        }
    }

    // MARK: - SCStreamOutput (Required for SCStreamDelegate)
    // This specific method signature is part of SCStreamOutput.
    // SCStreamDelegate inherits SCStreamOutput, and stream(_:didOutputSampleBuffer:ofType:) is the
    // primary callback for delegate pattern. This can be left empty if not directly used
    // or if all handling is in the delegate's specific sample buffer method.
    public func stream(_ output: SCStreamOutput, didOutputSampleBuffer sampleBuffer: CMSampleBuffer, of type: SCStreamOutputType) {
        // This is the SCStreamOutput protocol requirement.
        // If self is added as a direct output via addStreamOutput, and also as a delegate,
        // ensure there's no double processing. Typically, stream(_:didOutputSampleBuffer:ofType:) from
        // SCStreamDelegate is sufficient when self is the delegate.
        // For safety and to adhere to typical usage where delegate handles it:
        if output as? ScreenCaptureManager === self && stream != nil { // Check if it's this specific instance and relates to our stream
             // Already handled by SCStreamDelegate's stream(_:didOutputSampleBuffer:ofType:)
        }
    }


    // MARK: - Capture Methods

    @objc public func captureFullscreen(completion: @escaping (NSData?) -> Void) {
        capture(with: nil, windows: nil, completion: completion)
    }

    @objc public func captureScreen(bounds: NSRect, completion: @escaping (NSData?) -> Void) {
        capture(with: bounds, windows: nil, completion: completion)
    }

    @objc public func captureRectangle(rect: NSRect, completion: @escaping (NSData?) -> Void) {
        capture(with: rect, windows: nil, completion: completion)
    }

    // MARK: - Continuous Screen Capture

    @objc public func startContinuousCapture(completion: @escaping (NSData?) -> Void) {
        guard !isCapturingContinuously else {
            print("Continuous capture is already in progress.")
            return
        }
        isCapturingContinuously = true
        continuousCaptureCompletion = { data in // Wrap to NSData for Obj-C compatibility
            completion(data as NSData?)
        }
        startCapture(contentRect: nil, windows: nil, forContinuous: true)
    }

    @objc public func stopContinuousCapture() {
        stopCapture() // This will also trigger cleanup via didStopWithError or successful stop
        isCapturingContinuously = false
        continuousCaptureCompletion = nil
    }

    @objc public func getLatestFrame(completion: @escaping (NSData?) -> Void) {
        if let frame = latestFrame {
            completion(convertCGImageToPNG(frame) as NSData?)
        } else {
            completion(nil)
        }
    }

    // MARK: - Helper Functions

    private func capture(with contentRect: CGRect?, windows: [SCWindow]?, completion: @escaping (NSData?) -> Void) {
        singleCaptureCompletion = { data in // Wrap to NSData for Obj-C compatibility
            completion(data as NSData?)
        }
        startCapture(contentRect: contentRect, windows: windows, forContinuous: false)
    }

    private func completeRequestWithError(error: Error? = nil) {
        DispatchQueue.main.async {
            if let singleCompletion = self.singleCaptureCompletion {
                singleCompletion(nil)
                self.singleCaptureCompletion = nil
            }
            if let continuousCompletion = self.continuousCaptureCompletion {
                // For continuous, we might not want to nil out the completion here,
                // as it's an ongoing process. didStopWithError will handle it.
                // However, if setup fails, we call it with nil.
                continuousCompletion(nil)
            }
            // Reset flags if necessary
            if error != nil && self.isCapturingContinuously {
                 self.isCapturingContinuously = false
                 self.continuousCaptureCompletion = nil // Critical if setup failed for continuous
            }
            self.stream = nil // Ensure stream is nil if setup failed
        }
    }

    private func startCapture(contentRect: CGRect?, windows: [SCWindow]?, forContinuous: Bool) {
        Task {
            do {
                let sharableContent = try await SCShareableContent.current
                let availableDisplays = sharableContent.displays
                // let availableWindows = sharableContent.windows // If needed for advanced window selection

                let filter: SCContentFilter
                let config = SCStreamConfiguration()
                config.pixelFormat = kCVPixelFormatType_32BGRA
                config.showsCursor = true
                config.queueDepth = 5 // A reasonable queue depth

                if let regionRectPoints = contentRect { // Regional capture (regionRectPoints is NSRect, in points)
                    guard let displayForRect = availableDisplays.first(where: { $0.frame.intersects(regionRectPoints) }) else {
                        print("Error: No display found containing the specified rectangle for regional capture.")
                        completeRequestWithError(error: NSError(domain: "ScreenCaptureError", code: 1003, userInfo: [NSLocalizedDescriptionKey: "No display found for specified region."]))
                        return
                    }

                    var scaleFactor: CGFloat = 1.0
                    if let screen = NSScreen.screens.first(where: { ($0.deviceDescription[NSDeviceDescriptionKey("NSScreenNumber")] as? CGDirectDisplayID) == displayForRect.displayID }) {
                        scaleFactor = screen.backingScaleFactor
                    }

                    let displayFramePoints = displayForRect.frame

                    // Convert global regionRectPoints to local coordinates relative to the display's origin.
                    // SCDisplay.frame origin is bottom-left in global coordinates.
                    // sourceRect is in pixels, relative to the display's origin (typically top-left for image data).
                    let localRectOriginX = (regionRectPoints.origin.x - displayFramePoints.origin.x) * scaleFactor
                    let localRectOriginYFromBottom = (regionRectPoints.origin.y - displayFramePoints.origin.y) * scaleFactor

                    let pixelWidth = regionRectPoints.width * scaleFactor
                    let pixelHeight = regionRectPoints.height * scaleFactor

                    // Adjust Y for top-left origin if sourceRect expects it.
                    // displayForRect.height is in pixels.
                    // The y-coordinate for sourceRect is often from the top.
                    // For ScreenCaptureKit, sourceRect is typically relative to the display's top-left origin.
                    let localRectOriginYFromTop = CGFloat(displayForRect.height) - localRectOriginYFromBottom - pixelHeight

                    config.sourceRect = CGRect(x: localRectOriginX, y: localRectOriginYFromTop, width: pixelWidth, height: pixelHeight)
                    config.width = Int(pixelWidth)
                    config.height = Int(pixelHeight)

                    // Filter for the specific display, the sourceRect will crop it.
                    filter = SCContentFilter(display: displayForRect, excludingWindows: [])

                } else if let windowsToCapture = windows, !windowsToCapture.isEmpty { // Window capture
                    let contentFilter = SCContentFilter()
                    contentFilter.desktopIndependentWindows = windowsToCapture
                    // For window capture, width/height should ideally be derived from the window(s) or let the stream decide.
                    // If capturing a single window, you might set config dimensions to match it (in pixels).
                    if windowsToCapture.count == 1, let window = windowsToCapture.first {
                        var scaleFactor: CGFloat = 1.0
                        // Find the screen the window is on to get the correct scale factor
                        if let screen = NSScreen.screens.first(where: { $0.frame.intersects(window.frame) }) {
                            scaleFactor = screen.backingScaleFactor
                        } else if let mainScreen = NSScreen.main { // Fallback to main screen's scale factor
                            scaleFactor = mainScreen.backingScaleFactor
                        }
                        config.width = Int(window.frame.width * scaleFactor)
                        config.height = Int(window.frame.height * scaleFactor)
                    }
                    // If multiple windows, it's complex; not setting width/height lets the stream create a composite.
                } else { // Fullscreen capture (of the first available display or a "main" display)
                    guard let firstDisplay = availableDisplays.first else {
                        print("Error: No displays available for fullscreen capture.")
                        completeRequestWithError(error: NSError(domain: "ScreenCaptureError", code: 1004, userInfo: [NSLocalizedDescriptionKey: "No displays available."]))
                        return
                    }
                    filter = SCContentFilter(display: firstDisplay, excludingWindows: [])
                    config.width = Int(firstDisplay.width)   // SCDisplay.width is in pixels
                    config.height = Int(firstDisplay.height) // SCDisplay.height is in pixels
                }

                self.stream = SCStream(filter: filter, configuration: config, delegate: self)
                // Add self as an output handler for the stream.
                try self.stream?.addStreamOutput(self, type: .screen, sampleHandlerQueue: .main) // Use a background queue if processing is heavy
                try self.stream?.startCapture()

            } catch {
                print("Error starting capture stream: \(error.localizedDescription)")
                completeRequestWithError(error: error)
            }
        }
    }

    private func stopCapture() {
        Task {
            do {
                try await stream?.stopCapture()
                // The SCStreamDelegate method stream(_:didStopWithError:) will handle cleanup.
                // If it stops without error, that delegate method is NOT called.
                // So, some cleanup might be needed here if no error.
                if !isCapturingContinuously { // For single frame, ensure completion is cleared
                    DispatchQueue.main.async {
                        self.singleCaptureCompletion = nil
                    }
                }
                // For continuous capture, didStopWithError handles it, or explicit stopContinuousCapture call.
                // Setting stream to nil here could be premature if didStopWithError is pending.
                // It's generally safer to nil it out in the delegate callback or when explicitly stopping.
                 DispatchQueue.main.async {
                     self.stream = nil
                 }

            } catch {
                print("Error stopping capture stream: \(error.localizedDescription)")
                // stream(_:didStopWithError:) should still be called by the system, which handles completions.
            }
        }
    }

    private func convertImageBufferToCGImage(_ buffer: CVImageBuffer) -> CGImage? {
        let ciImage = CIImage(cvImageBuffer: buffer)
        let context = CIContext(options: nil) // Create a new context each time or reuse one
        return context.createCGImage(ciImage, from: ciImage.extent)
    }

    private func convertCGImageToPNG(_ image: CGImage) -> Data? {
        guard let mutableData = CFDataCreateMutable(nil, 0),
              let destination = CGImageDestinationCreateWithData(mutableData, UTType.png.identifier as CFString, 1, nil) else {
            return nil
        }
        CGImageDestinationAddImage(destination, image, nil)
        if CGImageDestinationFinalize(destination) {
            return mutableData as Data
        }
        return nil
    }
}
