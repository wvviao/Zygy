using System.Diagnostics;
using System.Runtime.InteropServices;
using LinqToDB.Data;
using Microsoft.AspNetCore.Mvc;
using ZiggyCreatures.Caching.Fusion;
using Zygy.Api.Models.Responses;
using Zygy.Api.Repositories;
using Zygy.Api.Utilities;

namespace Zygy.Api.Endpoints;

[RegisterSingleton<IEndpoint, AppEndpoint>(Duplicate = DuplicateStrategy.Append)]
public class AppEndpoint(IFusionCache cache) : IEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder self)
    {
        var builder = self.MapGroup("app").AllowAnonymous().WithGroupName("public").WithTags("应用");
        builder.MapGet("info", AppInfo);
        builder.MapGet("/test", Test);
    }

    [EndpointSummary("获取应用基本信息")]
    public Ok<ApiResponse<AppInfoResponse>> AppInfo()
    {
        var startTimeUtc =
            new DateTimeOffset(
                DateTime.SpecifyKind(Process.GetCurrentProcess().StartTime.ToUniversalTime(), DateTimeKind.Utc));
        var uptime = DateTimeOffset.UtcNow - startTimeUtc;
        var gitHash = AssemblyHelpers.GetGitHash();
        return ApiResponse.Success(new AppInfoResponse(
            Uptime: Convert.ToInt32(uptime.TotalSeconds),
            StartTime: startTimeUtc.ToUnixTimeSeconds(),
            BuildTime: AssemblyHelpers.GetBuildTime(),
            GitHash: gitHash,
            Runtime: RuntimeInformation.FrameworkDescription
        ));
    }

    [EndpointSummary("测试")]
    public async ValueTask<Ok<ApiResponse<string>>> Test([FromServices] AppDbContext db, CancellationToken ct)
    {
        var values = await db.QueryToListAsync<int>("select 1", ct);
        await cache.SetAsync("test", values, TimeSpan.FromMinutes(5), token: ct);
        return ApiResponse.Success(values.FirstOrDefault(2) + "");
    }
}
