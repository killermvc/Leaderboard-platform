using Scalar.AspNetCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OpenApi;
using StackExchange.Redis;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

using Leaderboard.Repositories;
using Leaderboard.Models;
using Leaderboard.Services;
using Leaderboard.Middleware;
using Leaderboard.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
    new MySqlServerVersion(new Version(8, 0, 40)),
    mySqlOptions => mySqlOptions.EnableRetryOnFailure()));

string redisConnectionString = builder.Configuration.GetConnectionString("Redis")!;
ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services.AddSingleton(provider => redis);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IScoreRepository, ScoreRepository>();
builder.Services.AddScoped<IGameModeratorRepository, GameModeratorRepository>();
builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<IApiKeyAuthorizationService, ApiKeyAuthorizationService>();

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});

if(builder.Configuration["Cors:Origins"] is not null)
{
	builder.Services.AddCors(options =>
	{
		options.AddDefaultPolicy(policy =>
		{
			policy.WithOrigins(builder.Configuration["Cors:Origins"]!.Split(","))
				.AllowAnyHeader()
				.AllowAnyMethod();
		});
	});
}


builder.Services.AddControllers();
builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection("RateLimiting"));
builder.Services.Configure<ClerkOptions>(builder.Configuration.GetSection(ClerkOptions.SectionName));


builder.Services.AddOpenApi("v1", options => {
	//Allow for scalar ui to add a field for jwt
	options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var clerkIssuer = builder.Configuration["Clerk:Issuer"];
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = !string.IsNullOrWhiteSpace(clerkIssuer),
        ValidateAudience = string.IsNullOrWhiteSpace(clerkIssuer),
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = clerkIssuer ?? builder.Configuration["Jwt:Issuer"],
        ValidAudience = string.IsNullOrWhiteSpace(clerkIssuer) ? builder.Configuration["Jwt:Audience"] : null,
		NameClaimType = ClaimTypes.Name,
		RoleClaimType = ClaimTypes.Role,
        IssuerSigningKey = string.IsNullOrWhiteSpace(clerkIssuer)
            ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
            : null
    };
    if (!string.IsNullOrWhiteSpace(clerkIssuer))
    {
        options.Authority = clerkIssuer;
        options.RequireHttpsMetadata = true;
    }

	if(builder.Environment.IsDevelopment())
	{
		options.Events = new JwtBearerEvents
		{
			OnAuthenticationFailed = context =>
			{
				Console.WriteLine($"Authentication failed: {context.Exception.Message}");
				return Task.CompletedTask;
			},
			OnTokenValidated = context =>
			{
				Console.WriteLine("Token validated successfully.");
				return Task.CompletedTask;
			}
		};
	}
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
	app.MapScalarApiReference();
}

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<ClerkUserMiddleware>();
app.UseMiddleware<GameClientRateLimitingMiddleware>();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.UseAuthorization();

app.UseCors();

app.MapControllers();

app.Run();

//Allow for scalar ui to add a field for jwt
internal sealed class BearerSecuritySchemeTransformer(Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
        {
            var requirements = new Dictionary<string, OpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "Json Web Token"
                }
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = requirements;
            document.Components.SecuritySchemes[ApiKeyAuthenticationMiddleware.HeaderName] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Name = ApiKeyAuthenticationMiddleware.HeaderName,
                In = ParameterLocation.Header,
                Description = "Game-scoped API key. The key may also be supplied as the apiKey or api_key query parameter for clients that cannot set headers."
            };

            foreach (var path in document.Paths)
            {
                foreach (var operation in path.Value.Operations)
                {
                    bool isGameClientEndpoint = path.Key.StartsWith("/api/v1/", StringComparison.OrdinalIgnoreCase);
                    operation.Value.Security.Clear();
                    operation.Value.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = isGameClientEndpoint ? ApiKeyAuthenticationMiddleware.HeaderName : "Bearer",
                                Type = ReferenceType.SecurityScheme
                            }
                        }] = Array.Empty<string>()
                    });
                }
            }
        }
    }
}
