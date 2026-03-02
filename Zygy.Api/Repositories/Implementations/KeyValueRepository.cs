using LinqToDB.Async;
using Zygy.Api.Services;

namespace Zygy.Api.Repositories.Implementations;

[RegisterSingleton(ServiceType = typeof(IKeyValueRepository))]
public class KeyValueRepository(IServiceScopeFactory ssf, IEncryptionService es) : IKeyValueRepository
{
    public string? GetValue(string group, string key)
    {
        using var scope = ssf.CreateScope();
        var value = scope
            .ServiceProvider
            .GetRequiredService<AppDbContext>()
            .KeyValue
            .Where(kv => kv.Group == group && kv.Key == key && kv.Enabled)
            .Select(kv => kv.Value)
            .FirstOrDefault();
        return es.DecryptFromBase64String(value);
    }

    public async ValueTask<string?> GetValueAsync(string group, string key, CancellationToken token)
    {
        await using var scope = ssf.CreateAsyncScope();
        var value = await scope
            .ServiceProvider
            .GetRequiredService<AppDbContext>()
            .KeyValue
            .Where(kv => kv.Group == group && kv.Key == key && kv.Enabled)
            .Select(kv => kv.Value)
            .FirstOrDefaultAsync(token);
        return es.DecryptFromBase64String(value);
    }
}
