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
    new MySqlServerVersion(new Version(8, 0, 40))));

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
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
		ValidIssuer = builder.Configuration["Jwt:Issuer"],
		ValidAudience = builder.Configuration["Jwt:Audience"],
		NameClaimType = ClaimTypes.Name,
		RoleClaimType = ClaimTypes.Role,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };

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

            foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
            {
                operation.Value.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme } }] = Array.Empty<string>()
                });
            }
        }
    }
}
