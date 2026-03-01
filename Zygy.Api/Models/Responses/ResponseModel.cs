using System.Text.Json;

namespace Zygy.Api.Models.Responses;

public record TimestampResponse(long Seconds, long Milliseconds);

public record AppInfoResponse(
    long Uptime,
    long StartTime,
    long? BuildTime,
    string? GitHash,
    string Runtime);

public record RequestInfoResponse(
    IDictionary<string, object> Headers,
    string Url,
    JsonDocument? Json,
    IDictionary<string, object>? Form,
    string? Data);
