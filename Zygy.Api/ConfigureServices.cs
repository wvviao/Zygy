using System.Text.Json;

namespace Zygy.Api;

internal static class ConfigureServices
{
    extension(IServiceCollection self)
    {
        internal void AddServices(IConfiguration config, IWebHostEnvironment env)
        {
            self.ConfigureJson();
            self.AddValidation();
            self.ConfigureOpenApi(env);
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
    }
}
