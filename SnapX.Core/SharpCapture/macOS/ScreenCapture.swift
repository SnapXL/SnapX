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
    public func stream(_ output: SCStreamOutput, didOutputSampleBuffer sampleBuffer: CMSampleBuffer, of type: SCStreamOutputType) {
        if output as? ScreenCaptureManager === self && stream != nil {
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
                continuousCompletion(nil)
            }
            if error != nil && self.isCapturingContinuously {
                self.isCapturingContinuously = false
                self.continuousCaptureCompletion = nil
            }
            self.stream = nil
        }
    }

    private func startCapture(contentRect: CGRect?, windows: [SCWindow]?, forContinuous: Bool) {
        Task {
            do {
                let sharableContent = try await SCShareableContent.current
                let availableDisplays = sharableContent.displays

                let config = SCStreamConfiguration()
                config.pixelFormat = kCVPixelFormatType_32BGRA
                config.showsCursor = true
                config.queueDepth = 5

                var filter: SCContentFilter? // Initialize filter as optional

                if let regionRectPoints = contentRect { // Regional capture
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
                    let localRectOriginX = (regionRectPoints.origin.x - displayFramePoints.origin.x) * scaleFactor
                    let localRectOriginYFromBottom = (regionRectPoints.origin.y - displayFramePoints.origin.y) * scaleFactor
                    let pixelWidth = regionRectPoints.width * scaleFactor
                    let pixelHeight = regionRectPoints.height * scaleFactor
                    let localRectOriginYFromTop = CGFloat(displayForRect.height) - localRectOriginYFromBottom - pixelHeight

                    config.sourceRect = CGRect(x: localRectOriginX, y: localRectOriginYFromTop, width: pixelWidth, height: pixelHeight)
                    config.width = Int(pixelWidth)
                    config.height = Int(pixelHeight)

                    filter = SCContentFilter(display: displayForRect, excludingWindows: [])

                } else if let windowsToCapture = windows, !windowsToCapture.isEmpty { // Window capture
                    // Initialize filter for window capture
                    filter = SCContentFilter()

                    if windowsToCapture.count == 1, let window = windowsToCapture.first {
                        var scaleFactor: CGFloat = 1.0
                        if let screen = NSScreen.screens.first(where: { $0.frame.intersects(window.frame) }) {
                            scaleFactor = screen.backingScaleFactor
                        } else if let mainScreen = NSScreen.main {
                            scaleFactor = mainScreen.backingScaleFactor
                        }
                        config.width = Int(window.frame.width * scaleFactor)
                        config.height = Int(window.frame.height * scaleFactor)
                    }
                } else { // Fullscreen capture
                    guard let firstDisplay = availableDisplays.first else {
                        print("Error: No displays available for fullscreen capture.")
                        completeRequestWithError(error: NSError(domain: "ScreenCaptureError", code: 1004, userInfo: [NSLocalizedDescriptionKey: "No displays available."]))
                        return
                    }
                    filter = SCContentFilter(display: firstDisplay, excludingWindows: [])
                    config.width = Int(firstDisplay.width)
                    config.height = Int(firstDisplay.height)
                }

                guard let finalFilter = filter else {
                    print("Error: Content filter could not be initialized.")
                    completeRequestWithError(error: NSError(domain: "ScreenCaptureError", code: 1005, userInfo: [NSLocalizedDescriptionKey: "Content filter initialization failed."]))
                    return
                }

                self.stream = SCStream(filter: finalFilter, configuration: config, delegate: self)
                try self.stream?.addStreamOutput(self, type: .screen, sampleHandlerQueue: .main)
                try await self.stream?.startCapture()

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
                if !isCapturingContinuously {
                    DispatchQueue.main.async {
                        self.singleCaptureCompletion = nil
                    }
                }
                DispatchQueue.main.async {
                    self.stream = nil
                }
            } catch {
                print("Error stopping capture stream: \(error.localizedDescription)")
            }
        }
    }

    private func convertImageBufferToCGImage(_ buffer: CVImageBuffer) -> CGImage? {
        let ciImage = CIImage(cvImageBuffer: buffer)
        let context = CIContext(options: nil)
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
