// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;

namespace SnapX.Core.Media;
public enum ThumbnailLocationType
{
    [Description("Default folder")]
    DefaultFolder,
    [Description("Parent folder of the media file")]
    ParentFolder,
    [Description("Custom folder")]
    CustomFolder
}

public enum ConverterVideoCodecs
{
    [Description("H.264 / x264")]
    x264,
    [Description("H.265 / x265")]
    x265,
    [Description("H.264 / NVENC")]
    h264_nvenc,
    [Description("HEVC / NVENC")]
    hevc_nvenc,
    [Description("H.264 / AMF")]
    h264_amf,
    [Description("HEVC / AMF")]
    hevc_amf,
    [Description("H.264 / Quick Sync")]
    h264_qsv,
    [Description("HEVC / Quick Sync")]
    hevc_qsv,
    [Description("VP8")]
    vp8,
    [Description("VP9")]
    vp9,
    [Description("AV1")]
    av1,
    [Description("MPEG-4 / Xvid")]
    xvid,
    [Description("GIF")]
    gif,
    [Description("WebP")]
    webp,
    [Description("APNG")]
    apng
}

public enum ImageBeautifierBackgroundType
{
    Gradient,
    Color,
    Image,
    Desktop,
    Transparent
}

