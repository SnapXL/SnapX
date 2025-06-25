
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Job;
using SnapX.Core.Utils.Parsers;

namespace SnapX.Core.Upload;

public class UploadInfoParser : NameParser
{
    public const string? HTMLLink = "<a href=\"$url\">$url</a>";
    public const string? HTMLImage = "<img src=\"$url\">";
    public const string? HTMLLinkedImage = "<a href=\"$url\"><img src=\"$thumbnailurl\"></a>";
    public const string? ForumLink = "[url]$url[/url]";
    public const string? ForumImage = "[img]$url[/img]";
    public const string? ForumLinkedImage = "[url=$url][img]$thumbnailurl[/img][/url]";
    public const string WikiImage = "[$url]";
    public const string WikiLinkedImage = "[$url $thumbnailurl]";
    public const string? MarkdownLink = "[$url]($url)";
    public const string? MarkdownImage = "![]($url)";
    public const string? MarkdownLinkedImage = "[![]($thumbnailurl)]($url)";

    public string? Parse(TaskInfo info, string? pattern)
    {
        if (info != null && !string.IsNullOrEmpty(pattern))
        {
            pattern = Parse(pattern);

            if (info.Result != null)
            {
                string? result = info.Result.ToString();

                if (string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(info.FilePath))
                {
                    result = info.FilePath;
                }

                pattern = pattern.Replace("$result", result ?? "");
                pattern = pattern.Replace("$url", info.Result.URL ?? "");
                pattern = pattern.Replace("$shorturl", info.Result.ShortenedURL ?? "");
                pattern = pattern.Replace("$thumbnailurl", info.Result.ThumbnailURL ?? "");
                pattern = pattern.Replace("$deletionurl", info.Result.DeletionURL ?? "");
            }

            pattern = pattern.Replace("$filenamenoext", !string.IsNullOrEmpty(info.FileName) ? Path.GetFileNameWithoutExtension(info.FileName) : "");
            pattern = pattern.Replace("$filename", info.FileName ?? "");
            pattern = pattern.Replace("$filepath", info.FilePath ?? "");
            pattern = pattern.Replace("$folderpath", !string.IsNullOrEmpty(info.FilePath) ? Path.GetDirectoryName(info.FilePath) : "");
            pattern = pattern.Replace("$foldername", !string.IsNullOrEmpty(info.FilePath) ? Path.GetFileName(Path.GetDirectoryName(info.FilePath)) : "");
            pattern = pattern.Replace("$thumbnailfilenamenoext", !string.IsNullOrEmpty(info.ThumbnailFilePath) ? Path.GetFileNameWithoutExtension(info.ThumbnailFilePath) : "");
            pattern = pattern.Replace("$thumbnailfilename", !string.IsNullOrEmpty(info.ThumbnailFilePath) ? Path.GetFileName(info.ThumbnailFilePath) : "");

            if (info.UploadDuration != null)
            {
                pattern = pattern.Replace("$uploadtime", info.UploadDuration.ElapsedMilliseconds.ToString());
            }
        }

        return pattern;
    }
}

