// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload;

public static class UploadersConfigValidator
{
    public static bool Validate<T>(int index, UploadersConfig config)
    {
        var destination = (Enum)Enum.ToObject(typeof(T), index);

        return destination switch
        {
            ImageDestination imageDestination => Validate(imageDestination, config),
            TextDestination textDestination => Validate(textDestination, config),
            FileDestination fileDestination => Validate(fileDestination, config),
            UrlShortenerType urlShortenerType => Validate(urlShortenerType, config),
            URLSharingServices urlSharingServices => Validate(urlSharingServices, config),
            _ => true
        };
    }

    public static bool Validate(ImageDestination destination, UploadersConfig config) =>
        destination == ImageDestination.FileUploader ||
        UploaderFactory.ImageUploaderServices[destination].CheckConfig(config);

    public static bool Validate(TextDestination destination, UploadersConfig config) =>
        destination == TextDestination.FileUploader ||
        UploaderFactory.TextUploaderServices[destination].CheckConfig(config);


    public static bool Validate(FileDestination destination, UploadersConfig config) =>
        UploaderFactory.FileUploaderServices[destination].CheckConfig(config);

    public static bool Validate(UrlShortenerType destination, UploadersConfig config) =>
        UploaderFactory.URLShortenerServices[destination].CheckConfig(config);

    public static bool Validate(URLSharingServices destination, UploadersConfig config) =>
        UploaderFactory.URLSharingServices[destination].CheckConfig(config);

}

