using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sales.Api;
using Sales.Application.Abstractions;
using Sales.Application.IntegrationEvents;
using Sales.Infrastructure.Persistence;
using Xunit;

namespace Sales.FunctionalTests;

internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-user"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Test User"),
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public sealed class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var appSettings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=dummy;Username=dummy;Password=dummy",
                ["RabbitMq:ConnectionString"] = "amqp://guest:guest@localhost:5672",
                ["Jwt:SecretKey"] = "test_secret_key_for_functional_tests"
            };
            config.AddInMemoryCollection(appSettings);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(config =>
        {
            var appSettings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=dummy;Username=dummy;Password=dummy",
                ["RabbitMq:ConnectionString"] = "amqp://guest:guest@localhost:5672"
            };
            config.AddInMemoryCollection(appSettings);
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SecretKey"] = "test_secret_key_for_functional_tests" });
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Testing:UseInMemoryDatabase"] = "true" });
        });

        builder.ConfigureServices(services =>
        {
            // Use Program.cs provider selection (in-memory for tests) - no explicit re-registration here

            // Replace IntegrationEventPublisher with No-op
            services.RemoveAll<IIntegrationEventPublisher>();
            services.AddSingleton<IIntegrationEventPublisher>(new NoopIntegrationEventPublisher());

            // Replace authentication with Test scheme
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            // Remove health checks that may have been registered in Program and add a no-op builder
            services.RemoveAll<IHealthCheck>();
            services.AddHealthChecks();
        });

        return base.CreateHost(builder);
    }

    private sealed class NoopIntegrationEventPublisher : IIntegrationEventPublisher
    {
        public Task PublishAsync(SaleCreatedEvent evt, System.Threading.CancellationToken ct = default) => Task.CompletedTask;
        public Task PublishAsync(SaleModifiedEvent evt, System.Threading.CancellationToken ct = default) => Task.CompletedTask;
        public Task PublishAsync(SaleCancelledEvent evt, System.Threading.CancellationToken ct = default) => Task.CompletedTask;
        public Task PublishAsync(ItemCancelledEvent evt, System.Threading.CancellationToken ct = default) => Task.CompletedTask;
    }
}

public sealed class SalesEndpointsTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public SalesEndpointsTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test");
        return client;
    }

    [Fact]
    public async Task List_ShouldReturn200_WithSeededOrEmptyResult()
    {
        var client = CreateClient();

        var resp = await client.GetAsync("/sales?page=1&pageSize=10");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resp.Content.ReadAsStringAsync();
        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("\"items\"");
    }

    [Fact]
    public async Task Create_Get_Update_CancelItem_CancelSale_Flow_ShouldSucceed()
    {
        var client = CreateClient();

        // CREATE
        var createPayload = new
        {
            saleNumber = "FT-1001",
            saleDate = (DateTime?)null,
            customerId = "cust-100",
            customerName = "Functional Tester",
            branchId = "br-100",
            branchName = "FT Branch",
            items = new[]
            {
                new { productId = "p-1", productName = "Prod 1", quantity = 4, unitPrice = 10.00m } // 4*10=40 - 10% = 36
            }
        };

        var createResp = await client.PostAsJsonAsync("/sales", createPayload);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = JsonSerializer.Deserialize<SaleResponseDto>(await createResp.Content.ReadAsStringAsync(), _jsonOptions)!;
        created.Should().NotBeNull();
        created.TotalAmount.Should().Be(36.00m);
        created.Items.Should().HaveCount(1);
        var saleId = created.Id;
        var itemId = created.Items[0].Id;

        // GET BY ID
        var getResp = await client.GetAsync($"/sales/{saleId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var got = JsonSerializer.Deserialize<SaleResponseDto>(await getResp.Content.ReadAsStringAsync(), _jsonOptions)!;
        got.Id.Should().Be(saleId);
        got.TotalAmount.Should().Be(36.00m);

        // UPDATE: replace items and fields
        var updatePayload = new
        {
            saleNumber = "FT-1002",
            saleDate = DateTime.UtcNow,
            customerId = "cust-200",
            customerName = "Functional Tester 2",
            branchId = "br-200",
            branchName = "FT Branch 2",
            items = new[]
            {
                new { productId = "p-2", productName = "Prod 2", quantity = 10, unitPrice = 5.00m } // 50 - 20% = 40
            }
        };
        var updateResp = await client.PutAsJsonAsync($"/sales/{saleId}", updatePayload);
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = JsonSerializer.Deserialize<SaleResponseDto>(await updateResp.Content.ReadAsStringAsync(), _jsonOptions)!;
        updated.SaleNumber.Should().Be("FT-1002");
        updated.CustomerId.Should().Be("cust-200");
        updated.BranchId.Should().Be("br-200");
        updated.TotalAmount.Should().Be(40.00m);
        updated.Items.Should().ContainSingle(i => i.ProductId == "p-2");
        itemId = updated.Items[0].Id;

        // CANCEL ITEM
        var cancelItemResp = await client.PostAsync($"/sales/{saleId}/items/{itemId}/cancel", content: null);
        cancelItemResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterCancelItem = JsonSerializer.Deserialize<SaleResponseDto>(await cancelItemResp.Content.ReadAsStringAsync(), _jsonOptions)!;
        afterCancelItem.Items.First(i => i.Id == itemId).Cancelled.Should().BeTrue();
        afterCancelItem.TotalAmount.Should().Be(0.00m);

        // CANCEL SALE
        var cancelSaleResp = await client.PostAsync($"/sales/{saleId}/cancel", content: null);
        cancelSaleResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterCancelSale = JsonSerializer.Deserialize<SaleResponseDto>(await cancelSaleResp.Content.ReadAsStringAsync(), _jsonOptions)!;
        afterCancelSale.Cancelled.Should().BeTrue();
        afterCancelSale.Items.All(i => i.Cancelled).Should().BeTrue();
        afterCancelSale.TotalAmount.Should().Be(0.00m);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenValidationFails()
    {
        var client = CreateClient();

        var invalidPayload = new
        {
            saleNumber = "", // invalid
            saleDate = (DateTime?)null,
            customerId = "", // invalid
            customerName = "",
            branchId = "",
            branchName = "",
            items = Array.Empty<object>() // invalid (NotEmpty)
        };

        var resp = await client.PostAsJsonAsync("/sales", invalidPayload);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await resp.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();
    }

    // DTOs for deserialization in tests (mirror of API responses)
    private sealed class SaleItemResponseDto
    {
        public Guid Id { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal TotalAmount { get; set; }
        public bool Cancelled { get; set; }
    }

    private sealed class SaleResponseDto
    {
        public Guid Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string BranchId { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public bool Cancelled { get; set; }
        public List<SaleItemResponseDto> Items { get; set; } = new();
    }
}