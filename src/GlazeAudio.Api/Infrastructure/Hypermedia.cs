using System.Text.Json.Serialization;

namespace GlazeAudio.Api.Infrastructure;

/// <summary>A hypermedia link: where the client can go next and which HTTP method to use.</summary>
public record Link(string Href, string Method = "GET");

/// <summary>Base for every response that carries hypermedia links (HAL-style "_links" object).</summary>
public abstract record Resource
{
    [JsonPropertyName("_links")]
    [JsonPropertyOrder(int.MaxValue)]
    public Dictionary<string, Link> Links { get; init; } = [];
}

/// <summary>Builds absolute links for every resource, so a client can navigate the API without hard-coding URLs.</summary>
public static class ApiLinks
{
    private static string Url(HttpRequest request, string path) =>
        $"{request.Scheme}://{request.Host}{request.PathBase}{path}";

    public static Dictionary<string, Link> Root(HttpRequest r) => new()
    {
        ["self"] = new(Url(r, "/api")),
        ["albums"] = new(Url(r, "/api/albums")),
        ["createAlbum"] = new(Url(r, "/api/albums"), "POST"),
        ["users"] = new(Url(r, "/api/users")),
        ["register"] = new(Url(r, "/api/users"), "POST"),
        ["docs"] = new(Url(r, "/openapi/v1.json"))
    };

    public static Dictionary<string, Link> Album(HttpRequest r, int albumId)
    {
        var self = $"/api/albums/{albumId}";
        return new()
        {
            ["self"] = new(Url(r, self)),
            ["update"] = new(Url(r, self), "PUT"),
            ["delete"] = new(Url(r, self), "DELETE"),
            ["songs"] = new(Url(r, $"{self}/songs")),
            ["createSong"] = new(Url(r, $"{self}/songs"), "POST"),
            ["overview"] = new(Url(r, $"{self}/overview")),
            ["albums"] = new(Url(r, "/api/albums"))
        };
    }

    public static Dictionary<string, Link> Song(HttpRequest r, int albumId, int songId)
    {
        var self = $"/api/albums/{albumId}/songs/{songId}";
        return new()
        {
            ["self"] = new(Url(r, self)),
            ["update"] = new(Url(r, self), "PUT"),
            ["delete"] = new(Url(r, self), "DELETE"),
            ["reviews"] = new(Url(r, $"{self}/reviews")),
            ["createReview"] = new(Url(r, $"{self}/reviews"), "POST"),
            ["album"] = new(Url(r, $"/api/albums/{albumId}"))
        };
    }

    public static Dictionary<string, Link> Review(HttpRequest r, int albumId, int songId, int reviewId, int userId)
    {
        var song = $"/api/albums/{albumId}/songs/{songId}";
        var self = $"{song}/reviews/{reviewId}";
        return new()
        {
            ["self"] = new(Url(r, self)),
            ["update"] = new(Url(r, self), "PUT"),
            ["delete"] = new(Url(r, self), "DELETE"),
            ["song"] = new(Url(r, song)),
            ["album"] = new(Url(r, $"/api/albums/{albumId}")),
            ["author"] = new(Url(r, $"/api/users/{userId}"))
        };
    }

    public static Dictionary<string, Link> User(HttpRequest r, int userId)
    {
        var self = $"/api/users/{userId}";
        return new()
        {
            ["self"] = new(Url(r, self)),
            ["update"] = new(Url(r, self), "PUT"),
            ["delete"] = new(Url(r, self), "DELETE"),
            ["reviews"] = new(Url(r, $"{self}/reviews")),
            ["users"] = new(Url(r, "/api/users"))
        };
    }

    public static Dictionary<string, Link> Overview(HttpRequest r, int albumId) => new()
    {
        ["self"] = new(Url(r, $"/api/albums/{albumId}/overview")),
        ["album"] = new(Url(r, $"/api/albums/{albumId}")),
        ["songs"] = new(Url(r, $"/api/albums/{albumId}/songs"))
    };

    /// <summary>self / first / prev / next / last links for a page, keeping the current filters in the query string.</summary>
    public static Dictionary<string, Link> Page(HttpRequest r, int page, int pageSize, int totalPages)
    {
        string PageUrl(int p)
        {
            var query = r.Query
                .Where(q => !q.Key.Equals("page", StringComparison.OrdinalIgnoreCase)
                         && !q.Key.Equals("pageSize", StringComparison.OrdinalIgnoreCase))
                .SelectMany(q => q.Value.Select(v => $"{Uri.EscapeDataString(q.Key)}={Uri.EscapeDataString(v ?? "")}"))
                .Append($"page={p}")
                .Append($"pageSize={pageSize}");
            return Url(r, $"{r.Path}?{string.Join("&", query)}");
        }

        var lastPage = Math.Max(totalPages, 1);
        var links = new Dictionary<string, Link>
        {
            ["self"] = new(PageUrl(page)),
            ["first"] = new(PageUrl(1)),
            ["last"] = new(PageUrl(lastPage))
        };
        if (page > 1) links["prev"] = new(PageUrl(Math.Min(page - 1, lastPage)));
        if (page < totalPages) links["next"] = new(PageUrl(page + 1));
        return links;
    }
}
