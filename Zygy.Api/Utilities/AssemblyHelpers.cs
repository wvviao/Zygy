using System.Reflection;

namespace Zygy.Api.Utilities;

public static class AssemblyHelpers
{
    private static readonly IEnumerable<AssemblyMetadataAttribute>? AssemblyMetadataAttributes;
    private const string GitHashAssemblyKey = "GitHash";
    private static string? _gitHash;
    private static long? _buildTime;

    static AssemblyHelpers()
    {
        AssemblyMetadataAttributes = Assembly.GetEntryAssembly()?.GetCustomAttributes<AssemblyMetadataAttribute>();
    }

    public static string? GetGitHash()
    {
        if (!string.IsNullOrWhiteSpace(_gitHash))
        {
            return _gitHash;
        }

        _gitHash = AssemblyMetadataAttributes?
            .Where(i => i.Key == GitHashAssemblyKey)
            .FirstOrDefault()?.Value;
        return _gitHash;
    }

    public static long? GetBuildTime()
    {
        if (_buildTime != null)
        {
            return _buildTime;
        }

        var buildTimeStr = AssemblyMetadataAttributes?
            .Where(i => i.Key == "BuildTime")
            .FirstOrDefault()?
            .Value;
        if (string.IsNullOrEmpty(buildTimeStr))
        {
            return null;
        }

        if (!long.TryParse(buildTimeStr, out var buildTime))
        {
            return null;
        }

        _buildTime = buildTime;
        return buildTime;
    }
}
