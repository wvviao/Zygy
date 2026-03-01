using Scalar.AspNetCore;
using Zygy.Api;
using Zygy.Api.Endpoints;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddServices(builder.Configuration, builder.Environment);

var app = builder.Build();

app.MapOpenApi();

app.MapScalarApiReference("/", options => SetupScalarOptions(options)
    .WithOpenApiRoutePattern("/openapi/v1.json"));

var apiRoute = app.MapGroup("/api");

foreach (var endpoint in app.Services.GetServices<IEndpoint>())
{
    endpoint.AddRoutes(apiRoute);
}

app.Run();
return;

static ScalarOptions SetupScalarOptions(ScalarOptions options)
    => options.AddPreferredSecuritySchemes("Bearer")
        .EnablePersistentAuthentication()
        .WithTitle("Zygy API")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch);
