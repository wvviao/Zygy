namespace Zygy.Api.Models.Responses;

public class ApiResponse
{
    public required int Code { get; set; }
    public required string Message { get; set; }
}

public class ApiResponse<TData> : ApiResponse
{
    public TData? Data { get; set; }
}

public class PageResponse<TData> : ApiResponse<IEnumerable<TData>>
{
    public int Total { get; set; }
}
