using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;
using Zygy.Api;
using Zygy.Api.Endpoints;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddServices(builder.Configuration, builder.Environment);

var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.All
});

// **Must** place `UseAuthentication` before UseAuthorization
app.UseAuthentication();
app.UseAuthorization();
//

app.MapOpenApi();
app.MapScalarApiReference("/docs",
        options => SetupScalarOptions(options))
    .RequireAuthorization(BasicAuthPolicy);

app.MapGet("/", () => "✔️");
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
        .WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl);
