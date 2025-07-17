// var myDeserializedClass = JsonSerializer.Deserialize<List<Root>>(myJsonResponse);
#pragma warning disable

using System.Text.Json.Serialization;

namespace SnapX.CommonUI.Types;


public record Author(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("date")] DateTime? Date,
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

public record Commit(
    [property: JsonPropertyName("author")] Author? Author,
    [property: JsonPropertyName("committer")] Committer? Committer,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("tree")] Tree? Tree,
    [property: JsonPropertyName("html_url")] string Url,
    [property: JsonPropertyName("comment_count")] int? CommentCount,
    [property: JsonPropertyName("verification")] Verification? Verification
);

public record Committer(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("date")] DateTime? Date,
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

public record Parent(
    [property: JsonPropertyName("sha")] string Sha,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("html_url")] string HtmlUrl
);

public record Root(
    [property: JsonPropertyName("sha")] string Sha,
    [property: JsonPropertyName("node_id")] string NodeId,
    [property: JsonPropertyName("commit")] Commit? Commit,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("html_url")] string HtmlUrl,
    [property: JsonPropertyName("comments_url")] string CommentsUrl,
    [property: JsonPropertyName("author")] Author? Author,
    [property: JsonPropertyName("committer")] Committer? Committer,
    [property: JsonPropertyName("parents")] IReadOnlyList<Parent>? Parents

);

public record Tree(
    [property: JsonPropertyName("sha")] string Sha,
    [property: JsonPropertyName("url")] string Url
);

public record Verification(
    [property: JsonPropertyName("verified")] bool? Verified,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("signature")] string Signature,
    [property: JsonPropertyName("payload")] string Payload,
    [property: JsonPropertyName("verified_at")] DateTime? VerifiedAt
);

