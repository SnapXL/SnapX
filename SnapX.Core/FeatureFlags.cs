namespace SnapX.Core;

// These constants are defined at build time.
// Thanks to NativeAOT trimming, once a feature flag is disabled, the code for it is gone poof.

public static class FeatureFlags
{
#if DISABLE_TELEMETRY
    public static readonly bool DisableTelemetry = true;
#else
    public static readonly bool DisableTelemetry = false;
#endif
#if DISABLE_OCR
    public static readonly bool DisableOCR = true;
#else
    public static readonly bool DisableOCR = false;
#endif
#if DISABLE_AUTO_UPDATES
    public static readonly bool DisableAutoUpdates = true;
#else
    public static readonly bool DisableAutoUpdates = false;
#endif
#if DISABLE_UPLOADS
    public static readonly bool DisableUploads = true;
#else
    public static readonly bool DisableUploads = false;
#endif
}
