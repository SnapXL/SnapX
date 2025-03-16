import ScreenCaptureKit
import CoreGraphics
import ImageIO
import AVFoundation
import UniformTypeIdentifiers

@objc public class ScreenCaptureManager: NSObject, SCStreamDelegate {
    private var stream: SCStream?
    private var latestFrame: CGImage?
    private var isCapturingContinuously: Bool = false
    private var continuousCaptureCompletion: ((Data?) -> Void)?

    override public init() {
        super.init()
    }

    // MARK: - SCStreamDelegate

    public func stream(_ stream: SCStream, didOutputSampleBuffer sampleBuffer: CMSampleBuffer, of type: SCStreamOutputType) {
        guard type == .screen, let imageBuffer = CMSampleBufferGetImageBuffer(sampleBuffer) else { return }

        latestFrame = convertImageBufferToCGImage(imageBuffer)

        if isCapturingContinuously, let completion = continuousCaptureCompletion {
            completion(convertCGImageToPNG(latestFrame!))
        }
    }

    public func stream(_ stream: SCStream, didStopWithError error: Error) {
        print("Stream stopped with error: \(error.localizedDescription)")
        isCapturingContinuously = false
        continuousCaptureCompletion = nil
    }

    // MARK: - Capture Methods

    @objc public func captureFullscreen(completion: @escaping (NSData?) -> Void) {
        capture(contentRect: nil, completion: completion)
    }

    @objc public func captureScreen(bounds: NSRect, completion: @escaping (NSData?) -> Void) {
        capture(contentRect: bounds, completion: completion)
    }

    @objc public func captureRectangle(rect: NSRect, completion: @escaping (NSData?) -> Void) {
        capture(contentRect: rect, completion: completion)
    }

    // MARK: - Continuous Screen Capture

    @objc public func startContinuousCapture(completion: @escaping (NSData?) -> Void) {
        guard !isCapturingContinuously else { return }
        isCapturingContinuously = true
        continuousCaptureCompletion = completion

        Task {
            do {
                guard let mainScreen = NSScreen.main else { return }
                let screenSize = mainScreen.frame.size

                let filter = SCContentFilter(display: mainScreen, excluding: [])
                let config = SCStreamConfiguration()
                config.width = Int(screenSize.width)
                config.height = Int(screenSize.height)
                config.pixelFormat = kCVPixelFormatType_32BGRA
                config.showsCursor = true

                stream = SCStream(filter: filter, configuration: config, delegate: self)
                try stream?.startCapture()
            } catch {
                print("Error starting continuous capture stream: \(error.localizedDescription)")
                isCapturingContinuously = false
                continuousCaptureCompletion = nil
                completion(nil)
            }
        }
    }

    @objc public func stopContinuousCapture() {
        stream?.stopCapture()
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

    private func capture(contentRect: CGRect?, completion: @escaping (NSData?) -> Void) {
        Task {
            do {
                guard let mainScreen = NSScreen.main else { return }
                let screenSize = mainScreen.frame.size

                let filter = SCContentFilter(display: mainScreen, excluding: [])
                let config = SCStreamConfiguration()

                if let rect = contentRect {
                    config.width = Int(rect.width)
                    config.height = Int(rect.height)
                } else {
                    config.width = Int(screenSize.width)
                    config.height = Int(screenSize.height)
                }

                config.pixelFormat = kCVPixelFormatType_32BGRA
                config.showsCursor = true

                let stream = SCStream(filter: filter, configuration: config, delegate: nil)
                try stream.startCapture()

                DispatchQueue.global().asyncAfter(deadline: .now() + 1) {
                    stream.stopCapture()
                }

                if let frame = latestFrame, let pngData = convertCGImageToPNG(frame) {
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

    private func convertImageBufferToCGImage(_ buffer: CVImageBuffer) -> CGImage? {
        let ciImage = CIImage(cvImageBuffer: buffer)
        let context = CIContext()
        return context.createCGImage(ciImage, from: ciImage.extent)
    }

    private func convertCGImageToPNG(_ image: CGImage) -> Data? {
        guard let mutableData = CFDataCreateMutable(nil, 0),
              let destination = CGImageDestinationCreateWithData(mutableData, UTTypePNG, 1, nil) else {
            return nil
        }
        CGImageDestinationAddImage(destination, image, nil)
        if CGImageDestinationFinalize(destination) {
            return mutableData as Data
        }
        return nil
    }
}
