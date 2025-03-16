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
        guard type == .screen, let imageBuffer = CMSampleBufferGetImageBuffer(sampleBuffer) else { return }

        latestFrame = convertImageBufferToCGImage(imageBuffer)

        if isCapturingContinuously, let frame = latestFrame, let completion = continuousCaptureCompletion {
            completion(convertCGImageToPNG(frame))
        } else if let frame = latestFrame, let completion = singleCaptureCompletion {
            singleCaptureCompletion = nil
            stopCapture()
            completion(convertCGImageToPNG(frame))
        }
    }

    public func stream(_ stream: SCStream, didStopWithError error: Error) {
        print("Stream stopped with error: \(error.localizedDescription)")
        isCapturingContinuously = false
        continuousCaptureCompletion = nil
        singleCaptureCompletion = nil
    }

    // MARK: - SCStreamOutput (Required for SCStream)

    public func didOutput(sampleBuffer: CMSampleBuffer, of type: SCStreamOutputType) {
        // Delegate method from SCStreamOutput, handled in stream(_:didOutputSampleBuffer:of:)
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

    @objc public func captureScreen(posX: CGFloat, posY: CGFloat, completion: @escaping (NSData?) -> Void) {
        let rect = CGRect(x: posX - 5, y: posY - 5, width: 10, height: 10) // Capture a small area around the point
        capture(with: rect, windows: nil, completion: completion)
    }

    @objc public func captureWindow(posX: CGFloat, posY: CGFloat, completion: @escaping (NSData?) -> Void) {
        guard let window = getWindowAt(point: CGPoint(x: posX, y: posY)) else {
            completion(nil)
            return
        }
        capture(with: nil, windows: [window], completion: completion)
    }

    // MARK: - Continuous Screen Capture

    @objc public func startContinuousCapture(completion: @escaping (NSData?) -> Void) {
        guard !isCapturingContinuously else { return }
        isCapturingContinuously = true
        continuousCaptureCompletion = completion

        startCapture(forContinuous: true)
    }

    @objc public func stopContinuousCapture() {
        stopCapture()
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
        singleCaptureCompletion = completion
        startCapture(contentRect: contentRect, windows: windows, forContinuous: false)
    }

    private func startCapture(contentRect: CGRect?, windows: [SCWindow]?, forContinuous: Bool) {
        Task {
            do {
                let filter: SCContentFilter
                if let rect = contentRect {
                    filter = SCContentFilter(desktopIndependentWindows: [], screenFilter: .some(.init(rect)))
                } else if let windowsToCapture = windows {
                    filter = SCContentFilter(desktopIndependentWindows: windowsToCapture, screen: nil)
                } else {
                    guard let mainScreen = NSScreen.main else { return }
                    filter = SCContentFilter(desktop: mainScreen.screen, excludingApplications: nil) // Use mainScreen.screen
                }

                let config = SCStreamConfiguration()
                if let rect = contentRect {
                    config.width = Int(rect.width)
                    config.height = Int(rect.height)
                } else {
                    guard let mainScreen = NSScreen.main else { return }
                    let screenSize: CGSize = mainScreen.frame.size
                    config.width = Int(screenSize.width)
                    config.height = Int(screenSize.height)
                }
                config.pixelFormat = kCVPixelFormatType_32BGRA
                config.showsCursor = true

                stream = SCStream(filter: filter, configuration: config, delegate: self) // Set delegate

                // Request access if needed
                switch await SCShareableContent.current.authorizationStatus {
                case .notDetermined:
                    do {
                        try await SCShareableContent.current.requestAuthorization()
                    } catch {
                        print("Authorization request failed: \(error)")
                        singleCaptureCompletion?(nil)
                        continuousCaptureCompletion?(nil)
                        return
                    }
                case .denied, .restricted:
                    print("Screen recording permission denied or restricted.")
                    singleCaptureCompletion?(nil)
                    continuousCaptureCompletion?(nil)
                    return
                case .allowed:
                    break
                @unknown default:
                    print("Unknown authorization status.")
                    singleCaptureCompletion?(nil)
                    continuousCaptureCompletion?(nil)
                    return
                }

                try stream?.startCapture()
            } catch {
                print("Error starting capture stream: \(error.localizedDescription)")
                singleCaptureCompletion?(nil)
                continuousCaptureCompletion?(nil)
            }
        }
    }

    private func stopCapture() {
        Task {
            do {
                try await stream?.stopCapture()
            } catch {
                print("Error stopping capture stream: \(error.localizedDescription)")
            }
        }
    }

    private func getWindowAt(point: CGPoint) -> SCWindow? {
        guard let windows = try? SCShareableContent.current.windows(onScreenOnly: true) else {
            return nil
        }
        for window in windows {
            if window.frame.contains(point) {
                return window
            }
        }
        return nil
    }

    private func convertImageBufferToCGImage(_ buffer: CVImageBuffer) -> CGImage? {
        let ciImage = CIImage(cvImageBuffer: buffer)
        let context = CIContext()
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
