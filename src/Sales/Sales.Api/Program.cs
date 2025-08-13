using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Shared.Common.Security;
using Sales.Infrastructure.Persistence;
using Sales.Domain.Repositories;
using Sales.Infrastructure.Repositories;
using Sales.Domain.Entities;
using Sales.Api.Middleware;
using Rebus.ServiceProvider;
using Rebus.Config;
using Sales.Application.Abstractions;
using Sales.Infrastructure.Messaging;
using FluentValidation;
using FluentValidation.AspNetCore;
using Sales.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

// Services
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateSaleRequestValidator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAutoMapper(typeof(Sales.Api.Features.Sales.SalesMappingProfile).Assembly);
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sales API", Version = "v1" });
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

  // HealthChecks: DB
  builder.Services
      .AddHealthChecks()
      .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty);

// EF Core - PostgreSQL
builder.Services.AddDbContext<SalesDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(cs);
});

// Repositories
builder.Services.AddScoped<ISaleRepository, SaleRepository>();

// Integration Events (Rebus + RabbitMQ as one-way client)
var rabbitMqConnectionString = builder.Configuration.GetSection("RabbitMq:ConnectionString").Value;
builder.Services.AddScoped<IIntegrationEventPublisher, IntegrationEventPublisher>();
builder.Services.AddRebus(configure => configure
    .Transport(t => t.UseRabbitMqAsOneWayClient(rabbitMqConnectionString))
);

// JWT Auth
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

// Apply migrations and seed demo data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
    db.Database.EnsureCreated();

    if (!db.Sales.Any())
    {
        var demo = Sale.Create(
            saleNumber: "S-0001",
            saleDate: DateTime.UtcNow,
            customerId: "cust-1",
            customerName: "Demo Customer",
            branchId: "br-1",
            branchName: "Main Branch",
            items: new[]
            {
                new Sale.NewItem("p-1","Demo Product A", 3, 10.00m),
                new Sale.NewItem("p-2","Demo Product B", 5, 20.00m)
            }
        );
        db.Sales.Add(demo);
        db.SaveChanges();
    }
}

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

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

namespace Sales.Api { public class Program { } }
