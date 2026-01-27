// Root myDeserializedClass = JsonSerializer.Deserialize<List<Root>>(myJsonResponse);

using System.Text.Json.Serialization;

namespace SnapX.CommonUI.Types;

public record Asset(
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("id")] int? Id,
        [property: JsonPropertyName("node_id")] string NodeId,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("label")] string Label,
        [property: JsonPropertyName("uploader")] Uploader Uploader,
        [property: JsonPropertyName("content_type")] string ContentType,
        [property: JsonPropertyName("state")] string State,
        [property: JsonPropertyName("size")] int? Size,
        [property: JsonPropertyName("download_count")] int? DownloadCount,
        [property: JsonPropertyName("created_at")] DateTime? CreatedAt,
        [property: JsonPropertyName("updated_at")] DateTime? UpdatedAt,
        [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl
    );

public record Release(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("assets_url")] string AssetsUrl,
    [property: JsonPropertyName("upload_url")] string UploadUrl,
    [property: JsonPropertyName("html_url")] string HtmlUrl,
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("author")] Author Author,
    [property: JsonPropertyName("node_id")] string NodeId,
    [property: JsonPropertyName("tag_name")] string TagName,
    [property: JsonPropertyName("target_commitish")] string TargetCommitish,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("draft")] bool? Draft,
    [property: JsonPropertyName("prerelease")] bool? Prerelease,
    [property: JsonPropertyName("created_at")] DateTime? CreatedAt,
    [property: JsonPropertyName("published_at")] DateTime? PublishedAt,
    [property: JsonPropertyName("assets")] IReadOnlyList<Asset> Assets,
    [property: JsonPropertyName("tarball_url")] string TarballUrl,
    [property: JsonPropertyName("zipball_url")] string ZipballUrl,
    [property: JsonPropertyName("body")] string Body
);

public record Uploader(
    [property: JsonPropertyName("login")] string Login,
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("node_id")] string NodeId,
    [property: JsonPropertyName("avatar_url")] string AvatarUrl,
    [property: JsonPropertyName("gravatar_id")] string GravatarId,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("html_url")] string HtmlUrl,
    [property: JsonPropertyName("followers_url")] string FollowersUrl,
    [property: JsonPropertyName("following_url")] string FollowingUrl,
    [property: JsonPropertyName("gists_url")] string GistsUrl,
    [property: JsonPropertyName("starred_url")] string StarredUrl,
    [property: JsonPropertyName("subscriptions_url")] string SubscriptionsUrl,
    [property: JsonPropertyName("organizations_url")] string OrganizationsUrl,
    [property: JsonPropertyName("repos_url")] string ReposUrl,
    [property: JsonPropertyName("events_url")] string EventsUrl,
    [property: JsonPropertyName("received_events_url")] string ReceivedEventsUrl,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("user_view_type")] string UserViewType,
    [property: JsonPropertyName("site_admin")] bool? SiteAdmin
);

