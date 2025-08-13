using Microsoft.OpenApi.Models;
using Serilog;
using Shared.Common.Security;

var builder = WebApplication.CreateBuilder(args);

// Serilog basic configuration from appsettings if present
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth API", Version = "v1" });
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT Bearer token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, new List<string>() }
    });
});

// Health checks (basic)
builder.Services.AddHealthChecks();

// Auth + JWT
builder.Services.AddJwtAuthentication(builder.Configuration);

 // Local auth dependencies
builder.Services.AddSingleton<Auth.Api.Infrastructure.Users.IUserStore, Auth.Api.Infrastructure.Users.InMemoryUserStore>();
builder.Services.AddSingleton<Shared.Common.Security.IPasswordHasher, Auth.Api.Infrastructure.Security.BCryptPasswordHasher>();

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

namespace Auth.Api { public class Program { } }
