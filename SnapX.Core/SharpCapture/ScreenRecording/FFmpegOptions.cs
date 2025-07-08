// SPDX-License-Identifier: GPL-3.0-or-later

using SnapX.Core.Utils;

namespace SnapX.Core.ScreenCapture.ScreenRecording;

public class FFmpegOptions
{
    // General
    public bool OverrideCLIPath { get; set; } = false;
    public string? CLIPath { get; set; } = "";
    public string VideoSource { get; set; } = FFmpegCaptureDevice.GDIGrab.Value;
    public string AudioSource { get; set; } = FFmpegCaptureDevice.None.Value;
    public FFmpegVideoCodec VideoCodec { get; set; } = FFmpegVideoCodec.libx264;
    public FFmpegAudioCodec AudioCodec { get; set; } = FFmpegAudioCodec.libvoaacenc;
    public string UserArgs { get; set; } = "";
    public bool UseCustomCommands { get; set; } = false;
    public string CustomCommands { get; set; } = "";

    // Video
    public FFmpegPreset x264_Preset { get; set; } = FFmpegPreset.ultrafast;
    public int x264_CRF { get; set; } = 28;
    public bool x264_Use_Bitrate { get; set; } = false;
    public int x264_Bitrate { get; set; } = 3000; // kbps
    public int VPx_Bitrate { get; set; } = 3000; // kbps
    public int XviD_QScale { get; set; } = 10;
    public FFmpegNVENCPreset NVENC_Preset { get; set; } = FFmpegNVENCPreset.p4;
    public FFmpegNVENCTune NVENC_Tune { get; set; } = FFmpegNVENCTune.ll;
    public int NVENC_Bitrate { get; set; } = 3000; // kbps
    public FFmpegPaletteGenStatsMode GIFStatsMode { get; set; } = FFmpegPaletteGenStatsMode.full;
    public FFmpegPaletteUseDither GIFDither { get; set; } = FFmpegPaletteUseDither.sierra2_4a;
    public int GIFBayerScale { get; set; } = 2;
    public FFmpegAMFUsage AMF_Usage { get; set; } = FFmpegAMFUsage.lowlatency;
    public FFmpegAMFQuality AMF_Quality { get; set; } = FFmpegAMFQuality.speed;
    public int AMF_Bitrate { get; set; } = 3000; // kbps
    public FFmpegQSVPreset QSV_Preset { get; set; } = FFmpegQSVPreset.fast;
    public int QSV_Bitrate { get; set; } = 3000; // kbps

    // Audio
    public int AAC_Bitrate { get; set; } = 128; // kbps
    public int Opus_Bitrate { get; set; } = 128; // kbps
    public int Vorbis_QScale { get; set; } = 3;
    public int MP3_QScale { get; set; } = 4;

    public string? FFmpegPath
    {
        get
        {
            if (OverrideCLIPath && !string.IsNullOrEmpty(CLIPath))
            {
                return FileHelpers.GetAbsolutePath(CLIPath);
            }

            return FileHelpers.GetAbsolutePath("ffmpeg.exe");
        }
    }

    public string Extension
    {
        get
        {
            if (!string.IsNullOrEmpty(VideoSource))
            {
                switch (VideoCodec)
                {
                    case FFmpegVideoCodec.libx264:
                    case FFmpegVideoCodec.libx265:
                    case FFmpegVideoCodec.h264_nvenc:
                    case FFmpegVideoCodec.hevc_nvenc:
                    case FFmpegVideoCodec.h264_amf:
                    case FFmpegVideoCodec.hevc_amf:
                    case FFmpegVideoCodec.h264_qsv:
                    case FFmpegVideoCodec.hevc_qsv:
                        return "mp4";
                    case FFmpegVideoCodec.libvpx:
                    case FFmpegVideoCodec.libvpx_vp9:
                        return "webm";
                    case FFmpegVideoCodec.libxvid:
                        return "avi";
                    case FFmpegVideoCodec.gif:
                        return "gif";
                    case FFmpegVideoCodec.libwebp:
                        return "webp";
                    case FFmpegVideoCodec.apng:
                        return "apng";
                }
            }
            else if (!string.IsNullOrEmpty(AudioSource))
            {
                switch (AudioCodec)
                {
                    case FFmpegAudioCodec.libvoaacenc:
                        return "m4a";
                    case FFmpegAudioCodec.libopus:
                        return "opus";
                    case FFmpegAudioCodec.libvorbis:
                        return "ogg";
                    case FFmpegAudioCodec.libmp3lame:
                        return "mp3";
                }
            }

            return "mp4";
        }
    }

    public bool IsSourceSelected => IsVideoSourceSelected || IsAudioSourceSelected;

    public bool IsVideoSourceSelected => !string.IsNullOrEmpty(VideoSource);

    public bool IsAudioSourceSelected => !string.IsNullOrEmpty(AudioSource) && (!IsVideoSourceSelected || !IsAnimatedImage);

    public bool IsAnimatedImage => VideoCodec == FFmpegVideoCodec.gif || VideoCodec == FFmpegVideoCodec.libwebp || VideoCodec == FFmpegVideoCodec.apng;

    public bool IsEvenSizeRequired => !IsAnimatedImage;

    // TEMP: For backward compatibility
    public void FixSources()
    {
        if (VideoSource.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            VideoSource = FFmpegCaptureDevice.None.Value;
        }
        else if (VideoSource.Equals("GDI grab", StringComparison.OrdinalIgnoreCase))
        {
            VideoSource = FFmpegCaptureDevice.GDIGrab.Value;
        }

        if (AudioSource.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            AudioSource = FFmpegCaptureDevice.None.Value;
        }
    }
}
