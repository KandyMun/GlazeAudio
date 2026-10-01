namespace GlazeAudio.Api.Infrastructure;

/// <summary>One page of a list, with paging metadata and navigation links.</summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages) : Resource;

public static class Paging
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;

    /// <summary>Applies defaults and checks the range. Returns a 400 problem if page/pageSize are invalid.</summary>
    public static IResult? Validate(int? page, int? pageSize, out int validPage, out int validPageSize)
    {
        validPage = page ?? 1;
        validPageSize = pageSize ?? DefaultPageSize;

        var errors = new Dictionary<string, string[]>();
        if (validPage < 1)
            errors["page"] = ["page must be 1 or greater."];
        if (validPageSize is < 1 or > MaxPageSize)
            errors["pageSize"] = [$"pageSize must be between 1 and {MaxPageSize}."];

        return errors.Count == 0
            ? null
            : Results.ValidationProblem(errors, statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid query parameters.");
    }

    public static PagedResult<T> Create<T>(HttpRequest request, IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<T>(items, page, pageSize, totalCount, totalPages)
        {
            Links = ApiLinks.Page(request, page, pageSize, totalPages)
        };
    }
}
