using System.Diagnostics;
using System.Runtime.InteropServices;
using Zygy.Api.Models.Responses;
using Zygy.Api.Utilities;

namespace Zygy.Api.Endpoints;

[RegisterSingleton<IEndpoint, AppEndpoint>(Duplicate = DuplicateStrategy.Append)]
public class AppEndpoint : IEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder self)
    {
        var g = self.MapGroup("").AllowAnonymous().WithGroupName("public").WithTags("应用");
        g.MapGet("app", AppInfo);
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
}
