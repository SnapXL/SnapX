import ScreenCaptureKit
import CoreGraphics
import ImageIO

@objc public class ScreenCaptureManager: NSObject, SCStreamDelegate, SCStreamOutput {
    private var stream: SCStream?
    private var latestFrame: CGImage?
    private var isCapturingContinuously: Bool = false
    private var continuousCaptureCompletion: ((Data?) -> Void)?

    override public init() {
        super.init()
    }

    // MARK: - SCStreamDelegate

    public func stream(_ stream: SCStream, didOutputSampleBuffer sampleBuffer: CMSampleBuffer, of type: SCSampleBufferType) {
        if type == .video {
            if let imageBuffer = CMSampleBufferGetImageBuffer(sampleBuffer) {
                latestFrame = imageBuffer
                if isCapturingContinuously, let completion = continuousCaptureCompletion {
                    if let pngData = convertCGImageToPNG(latestFrame!) {
                        completion(pngData)
                    } else {
                        completion(nil)
                    }
                }
            }
        }
    }

    public func stream(_ stream: SCStream, didStopWithError error: Error) {
        print("Stream stopped with error: \(error.localizedDescription)")
        isCapturingContinuously = false
        continuousCaptureCompletion = nil
    }

    // MARK: - SCStreamOutput (Not directly used in this implementation but required)

    public func didOutput(sampleBuffer: CMSampleBuffer, of type: SCSampleBufferType) {
        // Delegate method from SCStreamOutput, handled in stream(_:didOutputSampleBuffer:of:)
    }

    // MARK: - Capture Methods

    @objc public func captureFullscreen(completion: @escaping (NSData?) -> Void) {
        capture(contentRect: nil, completion: completion)
    }

    @objc public func captureScreen(bounds: NSRect, completion: @escaping (NSData?) -> Void) {
        capture(contentRect: bounds, completion: completion)
    }

    @objc public func captureScreen(posX: CGFloat, posY: CGFloat, completion: @escaping (NSData?) -> Void) {
        let rect = CGRect(x: posX - 5, y: posY - 5, width: 10, height: 10) // Capture a small area around the point
        capture(contentRect: rect, completion: completion)
    }

    @objc public func captureWindow(posX: CGFloat, posY: CGFloat, completion: @escaping (NSData?) -> Void) {
        guard let window = getWindowAt(point: CGPoint(x: posX, y: posY)) else {
            completion(nil)
            return
        }
        capture(windows: [window], completion: completion)
    }

    @objc public func captureRectangle(rect: NSRect, completion: @escaping (NSData?) -> Void) {
        capture(contentRect: rect, completion: completion)
    }

    // MARK: - Continuous Screen Capture
    // It is only a PoC

    @objc public func startContinuousCapture(completion: @escaping (NSData?) -> Void) {
        guard !isCapturingContinuously else { return }
        isCapturingContinuously = true
        continuousCaptureCompletion = completion

        Task {
            do {
                let filter = SCContentFilter(desktopIndependentWindow: .exclude, screen: .main)
                let config = SCStreamConfiguration()
                config.width = 1920
                config.height = 1080
                config.pixelFormat = kCVPixelFormatType_32BGRA
                config.showsCursor = true

                stream = SCStream(filter: filter, configuration: config, delegate: self)
                try stream?.start()
            } catch {
                print("Error starting continuous capture stream: \(error.localizedDescription)")
                isCapturingContinuously = false
                continuousCaptureCompletion = nil
                completion(nil)
            }
        }
    }

    @objc public func stopContinuousCapture() {
        stream?.stop()
        isCapturingContinuously = false
        continuousCaptureCompletion = nil
    }

    @objc public func getLatestFrame(completion: @escaping (NSData?) -> Void) {
        if let frame = latestFrame {
            if let pngData = convertCGImageToPNG(frame) {
                completion(pngData as NSData)
            } else {
                completion(nil)
            }
        } else {
            completion(nil)
        }
    }

    // MARK: - Helper Functions

    private func capture(contentRect: CGRect? = nil, windows: [SCWindow]? = nil, completion: @escaping (NSData?) -> Void) {
        Task {
            do {
                var filter: SCContentFilter
                if let rect = contentRect {
                    filter = SCContentFilter(desktopIndependentWindow: .exclude, screen: .some(rect))
                } else if let windowsToCapture = windows {
                    filter = SCContentFilter(desktopIndependentWindows: windowsToCapture, screen: nil)
                } else {
                    filter = SCContentFilter(desktopIndependentWindow: .exclude, screen: .main)
                }

                let config = SCStreamConfiguration()
                if let rect = contentRect {
                    config.width = Int(rect.width)
                    config.height = Int(rect.height)
                } else {
                    config.width = 0 // Capture full screen dimensions
                    config.height = 0
                }
                config.pixelFormat = kCVPixelFormatType_32BGRA
                config.showsCursor = true

                let stream = SCStream(filter: filter, configuration: config, delegate: nil)
                var image: CGImage?
                let semaphore = DispatchSemaphore(value: 0)

                let outputHandler: (CMSampleBuffer?, SCSampleBufferType, Error?) -> Void = { (sampleBuffer, type, error) in
                    if type == .video, let buffer = sampleBuffer, let imageBuffer = CMSampleBufferGetImageBuffer(buffer) {
                        image = imageBuffer
                    }
                    semaphore.signal()
                }

                try stream.start()
                stream.output = outputHandler

                _ = semaphore.wait(timeout: .now() + 5) // Wait for a maximum of 5 seconds for the first frame

                try stream.stop()

                if let capturedImage = image, let pngData = convertCGImageToPNG(capturedImage) {
                    completion(pngData as NSData)
                } else {
                    completion(nil)
                }

            } catch {
                print("Error capturing screen: \(error.localizedDescription)")
                completion(nil)
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

    private func convertCGImageToPNG(_ image: CGImage) -> Data? {
        guard let mutableData = CFDataCreateMutable(nil, 0),
              let destination = CGImageDestinationCreateWithData(mutableData, kUTTypePNG, 1, nil) else {
            return nil
        }
        CGImageDestinationAddImage(destination, image, nil)
        if CGImageDestinationFinalize(destination) {
            return mutableData as Data
        }
        return nil
    }
}
