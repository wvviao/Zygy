using System.Text.Json;
using LinqToDB;
using LinqToDB.Extensions.DependencyInjection;
using LinqToDB.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;
using Zygy.Api.Repositories;
using Zygy.Api.Utilities;

namespace Zygy.Api;

internal static class ConfigureServices
{
    extension(IServiceCollection self)
    {
        internal void AddServices(IConfiguration config, IWebHostEnvironment env)
        {
            self.ConfigureCache(config);
            self.ConfigureJson();
            self.AddValidation();
            self.ConfigureOpenApi(env);
            self.ConfigureDbContext(config);
            self.AddAppServices();
        }

        private void ConfigureJson() =>
            self.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                options.SerializerOptions.DictionaryKeyPolicy = null;
            });

        private void ConfigureOpenApi(IWebHostEnvironment env)
        {
            self.AddOpenApi("v1", options =>
            {
                options.ShouldInclude = desc => env.IsDevelopment() || desc.GroupName == "public";
                options.AddDocumentTransformer((doc, _, _) =>
                {
                    doc.Servers = [];
                    return Task.CompletedTask;
                });
            });
        }

        private void ConfigureDbContext(IConfiguration config) =>
            self.AddLinqToDBContext<AppDbContext>((provider, options)
                => options
                    .UsePostgreSQL(config.GetRequiredValue("ConnectionStrings:Default"))
                    .UseDefaultLogging(provider)
            );

        private void ConfigureCache(IConfiguration config)
        {
            var connStr = config.GetRequiredValue("Redis:ConnectionString");
            self.AddMemoryCache()
                .AddStackExchangeRedisCache(options => { options.Configuration = connStr; })
                .AddFusionCacheSystemTextJsonSerializer()
                .AddFusionCache()
                .WithRegisteredDistributedCache();
        }
    }
}
