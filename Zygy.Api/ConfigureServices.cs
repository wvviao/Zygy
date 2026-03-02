using System.Security.Claims;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.S3;
using idunno.Authentication.Basic;
using LinqToDB;
using LinqToDB.Extensions.DependencyInjection;
using LinqToDB.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
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
            self.ConfigureAuth(config, env);
            self.ConfigureOpenApi(env);
            self.ConfigureDbContext(config);
            self.ConfigureS3(config);
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
                options.ShouldInclude = _ => true;
                options.AddDocumentTransformer((doc, _, _) =>
                {
                    doc.Servers = [];
                    doc.Components ??= new OpenApiComponents();
                    doc.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                    doc.Components.SecuritySchemes.Add(JwtBearerDefaults.AuthenticationScheme,
                        new OpenApiSecurityScheme
                        {
                            Type = SecuritySchemeType.Http,
                            Scheme = "bearer",
                            BearerFormat = "JWT",
                            In = ParameterLocation.Header,
                            Description = "请输入 JWT Token"
                        });
                    doc.Components.SecuritySchemes.Add(BasicAuthenticationDefaults.AuthenticationScheme,
                        new OpenApiSecurityScheme
                        {
                            Type = SecuritySchemeType.Http,
                            Scheme = "basic",
                            In = ParameterLocation.Header,
                            Description = "请输入用户名和密码"
                        });
                    return Task.CompletedTask;
                });
                options.AddOperationTransformer((op, ctx, _) =>
                {
                    var metadata = ctx.Description.ActionDescriptor.EndpointMetadata;
                    op.Security = metadata.OfType<AuthorizeAttribute>()
                        .Select(i => i.AuthenticationSchemes ?? JwtBearerDefaults.AuthenticationScheme)
                        .Distinct()
                        .Select(i => new OpenApiSecurityRequirement
                        {
                            {
                                new OpenApiSecuritySchemeReference(i, ctx.Document),
                                []
                            }
                        })
                        .ToList();
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

        private void ConfigureS3(IConfiguration config)
        {
            var options = config.GetAWSOptions();
            options.Credentials = new BasicAWSCredentials(
                accessKey: config.GetRequiredValue("S3:AccessKey"),
                secretKey: config.GetRequiredValue("S3:SecretKey"));
            self.AddDefaultAWSOptions(options);
            self.AddAWSService<IAmazonS3>();
        }

        private void ConfigureAuth(IConfiguration config, IWebHostEnvironment env)
        {
            var builder = self.AddAuthorization(options =>
            {
                options.AddPolicy(ReadSysPolicy, policy => policy.RequireClaim("permissions", ReadSysPolicy));
                options.AddPolicy(WriteSysPolicy, policy => policy.RequireClaim("permissions", WriteSysPolicy));
                options.AddPolicy(BasicAuthPolicy,
                    policy => policy.AddAuthenticationSchemes(BasicAuthenticationDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                );
            }).AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });
            if (env.IsDevelopment())
            {
                builder.AddJwtBearer();
            }
            else
            {
                builder.AddJwtBearer(options =>
                {
                    options.IncludeErrorDetails = false;
                    options.Authority = config.GetRequiredValue("Auth0:Authority");
                    options.Audience = config.GetRequiredValue("Auth0:Audience");
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = ClaimTypes.NameIdentifier
                    };
                });
            }

            var username = config.GetRequiredValue("BasicAuth:Username");
            var password = config.GetRequiredValue("BasicAuth:Password");
            self.AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme).AddBasic(options =>
            {
                options.AllowInsecureProtocol = true;
                options.Realm = "Zygy API";
                options.Events = new BasicAuthenticationEvents
                {
                    OnValidateCredentials = ctx =>
                    {
                        if (ctx.Username != username || ctx.Password != password)
                        {
                            ctx.Fail("Invalid username or password.");
                            return Task.CompletedTask;
                        }

                        var claims = new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, ctx.Username, ClaimValueTypes.String,
                                ctx.Options.ClaimsIssuer),
                            new Claim(ClaimTypes.Name, ctx.Username, ClaimValueTypes.String,
                                ctx.Options.ClaimsIssuer)
                        };
                        ctx.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ctx.Scheme.Name));
                        ctx.Success();
                        return Task.CompletedTask;
                    }
                };
            });
        }
    }
}
