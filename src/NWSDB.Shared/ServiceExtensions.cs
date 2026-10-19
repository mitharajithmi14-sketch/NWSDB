using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace NWSDB.Shared;

/// <summary>
/// Shared start-up code used by every NWSDB service, so controllers, Swagger,
/// JWT security and CORS are configured the same way everywhere (reusability).
/// </summary>
public static class ServiceExtensions
{
    public const string AdminPolicy = "Admin";

    public static WebApplicationBuilder AddNwsdbCommon(this WebApplicationBuilder builder, string serviceTitle)
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = serviceTitle, Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the token returned by POST /api/auth/login"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwt["Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });

        builder.Services.AddAuthorization(options =>
            options.AddPolicy(AdminPolicy, policy => policy.RequireRole("Admin")));

        builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

        return builder;
    }

    public static WebApplication UseNwsdbCommon(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }

    /// <summary>A customer may only see their own account; an admin may see every account.</summary>
    public static bool CanAccess(this ClaimsPrincipal user, string accountNo) =>
        user.IsInRole("Admin") || user.FindFirst("accountNo")?.Value == accountNo;

    /// <summary>Returns the "Bearer ..." header so a service can call another service as the same user.</summary>
    public static string? BearerToken(this Microsoft.AspNetCore.Http.HttpRequest request) =>
        request.Headers.Authorization.FirstOrDefault();
}
