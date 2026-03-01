using Zygy.Api.Models.Responses;

namespace Zygy.Api.Utilities;

public static class Extensions
{
    extension(ApiResponse)
    {
        public static Ok<ApiResponse<TData>> Success<TData>(TData? data) =>
            TypedResults.Ok(new ApiResponse<TData> { Code = 200, Message = "ok", Data = data });

        public static Ok<ApiResponse> Success() => TypedResults.Ok(new ApiResponse { Code = 200, Message = "ok" });

        public static BadRequest<ApiResponse> InvalidParam() =>
            TypedResults.BadRequest(new ApiResponse { Code = 400, Message = "Invalid param" });

        public static InternalServerError<ApiResponse> InternalServerError(string message) =>
            TypedResults.InternalServerError(new ApiResponse { Code = 500, Message = message });
    }

    extension<TSource>(IQueryable<TSource> self)
    {
        public IQueryable<TSource> Pagination(int page, int perPage)
            => self.Skip((page - 1) * perPage).Take(perPage);
    }

    extension(IConfiguration self)
    {
        public string GetRequiredValue(string key)
        {
            var value = self[key];
            return string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentNullException(key, $"Value for {key} is missing.")
                : value;
        }
    }
}
