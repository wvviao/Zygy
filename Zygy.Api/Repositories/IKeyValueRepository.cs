namespace Zygy.Api.Repositories;

public interface IKeyValueRepository
{
    string? GetValue(string group, string key);

    ValueTask<string?> GetValueAsync(string group, string key, CancellationToken ct = default);
}
