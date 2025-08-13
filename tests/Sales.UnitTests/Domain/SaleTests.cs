using System;
using System.Linq;
using FluentAssertions;
using Sales.Domain.Entities;

namespace Sales.UnitTests.Domain;

public class SaleTests
{
    private static Sale.NewItem NI(string id, string name, int qty, decimal price)
        => new Sale.NewItem(id, name, qty, price);

    [Fact]
    public void Create_ShouldInitializeAggregate_AndCalculateTotalsAndDiscounts()
    {
        // Arrange
        var items = new[]
        {
            NI("p-1", "Prod 1", 3, 10.00m), // no discount: 3 * 10.00 = 30.00
            NI("p-2", "Prod 2", 5, 20.00m), // 10% discount: gross 100.00 - 10.00 = 90.00
        };

        // Act
        var sale = Sale.Create(
            saleNumber: "S-0001",
            saleDate: DateTime.UtcNow.AddMinutes(-5),
            customerId: "cust-1",
            customerName: "John",
            branchId: "br-1",
            branchName: "Main",
            items: items
        );

        // Assert
        sale.Should().NotBeNull();
        sale.Id.Should().NotBe(Guid.Empty);
        sale.SaleNumber.Should().Be("S-0001");
        sale.CustomerId.Should().Be("cust-1");
        sale.CustomerName.Should().Be("John");
        sale.BranchId.Should().Be("br-1");
        sale.BranchName.Should().Be("Main");
        sale.Cancelled.Should().BeFalse();

        sale.Items.Should().HaveCount(2);
        sale.Items.Any(i => i.Id == Guid.Empty).Should().BeFalse();

        var i1 = sale.Items.First(i => i.ProductId == "p-1");
        var i2 = sale.Items.First(i => i.ProductId == "p-2");

        i1.DiscountPercent.Should().Be(0.00m);
        i1.TotalAmount.Should().Be(30.00m);

        i2.DiscountPercent.Should().Be(0.10m);
        i2.TotalAmount.Should().Be(90.00m);

        sale.TotalAmount.Should().Be(120.00m);
    }

    [Theory]
    [InlineData(1, 0.00)]
    [InlineData(3, 0.00)]
    [InlineData(4, 0.10)]
    [InlineData(9, 0.10)]
    [InlineData(10, 0.20)]
    [InlineData(20, 0.20)]
    public void CalculateDiscountPercent_ShouldFollowThresholds(int qty, decimal expected)
    {
        var discount = Sale.CalculateDiscountPercent(qty);
        discount.Should().Be(expected);
    }

    [Fact]
    public void CalculateDiscountPercent_ShouldThrow_WhenAboveLimit()
    {
        Action act = () => Sale.CalculateDiscountPercent(21);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*above 20 identical items*");
    }

    [Fact]
    public void Create_ShouldThrow_OnInvalidArguments()
    {
        // Empty ids or names are invalid
        Action act1 = () => Sale.Create("", DateTime.UtcNow, "c", "n", "b", "bn", new[] { NI("p", "pn", 1, 1m) });
        Action act2 = () => Sale.Create("s", DateTime.UtcNow, "", "n", "b", "bn", new[] { NI("p", "pn", 1, 1m) });
        Action act3 = () => Sale.Create("s", DateTime.UtcNow, "c", "", "b", "bn", new[] { NI("p", "pn", 1, 1m) });
        Action act4 = () => Sale.Create("s", DateTime.UtcNow, "c", "n", "", "bn", new[] { NI("p", "pn", 1, 1m) });
        Action act5 = () => Sale.Create("s", DateTime.UtcNow, "c", "n", "b", "", new[] { NI("p", "pn", 1, 1m) });

        act1.Should().Throw<ArgumentException>();
        act2.Should().Throw<ArgumentException>();
        act3.Should().Throw<ArgumentException>();
        act4.Should().Throw<ArgumentException>();
        act5.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrow_OnInvalidItemData()
    {
        // invalid quantity
        Action actQty = () => Sale.Create("s", DateTime.UtcNow, "c", "n", "b", "bn",
            new[] { NI("p", "pn", 0, 1m) });

        // invalid price
        Action actPrice = () => Sale.Create("s", DateTime.UtcNow, "c", "n", "b", "bn",
            new[] { NI("p", "pn", 1, 0m) });

        // above quantity limit
        Action actLimit = () => Sale.Create("s", DateTime.UtcNow, "c", "n", "b", "bn",
            new[] { NI("p", "pn", 21, 1m) });

        actQty.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("quantity");
        actPrice.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("unitPrice");
        actLimit.Should().Throw<InvalidOperationException>()
            .WithMessage("*above 20 identical items*");
    }

    [Fact]
    public void Update_ShouldModifyFieldsAndRecalculateTotals()
    {
        var sale = Sale.Create("S-1", DateTime.UtcNow, "c1", "cust", "b1", "branch", new[]
        {
            NI("p1", "Prod 1", 2, 10m)
        });

        sale.Update(
            saleNumber: "S-2",
            saleDate: DateTime.UtcNow.AddDays(1),
            customerId: "c2",
            customerName: "cust 2",
            branchId: "b2",
            branchName: "branch 2",
            items: new[] { NI("p2", "Prod 2", 4, 20m) } // 10% discount on 4 items: 4*20 - 10% = 72.00
        );

        sale.SaleNumber.Should().Be("S-2");
        sale.CustomerId.Should().Be("c2");
        sale.CustomerName.Should().Be("cust 2");
        sale.BranchId.Should().Be("b2");
        sale.BranchName.Should().Be("branch 2");

        sale.Items.Should().HaveCount(1);
        sale.Items.First().ProductId.Should().Be("p2");
        sale.TotalAmount.Should().Be(72.00m);
    }

    [Fact]
    public void CancelSale_ShouldCancelAllItems_AndZeroTotals()
    {
        var sale = Sale.Create("S-1", DateTime.UtcNow, "c1", "cust", "b1", "branch", new[]
        {
            NI("p1", "Prod 1", 2, 10m), // 20
            NI("p2", "Prod 2", 10, 5m)  // 10 * 5 = 50 - 20% = 40
        });

        sale.TotalAmount.Should().Be(60.00m);

        sale.CancelSale();

        sale.Cancelled.Should().BeTrue();
        sale.Items.All(i => i.Cancelled).Should().BeTrue();
        sale.Items.Sum(i => i.TotalAmount).Should().Be(0.00m);
        sale.TotalAmount.Should().Be(0.00m);
    }

    [Fact]
    public void CancelItem_ShouldCancelOnlySelectedItem_AndRecalculateTotals()
    {
        var sale = Sale.Create("S-1", DateTime.UtcNow, "c1", "cust", "b1", "branch", new[]
        {
            NI("p1", "Prod 1", 2, 10m), // 20
            NI("p2", "Prod 2", 4, 10m)  // 4 * 10 = 40 - 10% = 36
        });
        var itemToCancel = sale.Items.First(i => i.ProductId == "p2");

        sale.TotalAmount.Should().Be(56.00m);

        sale.CancelItem(itemToCancel.Id);

        itemToCancel.Cancelled.Should().BeTrue();
        // Remaining total equals the non-cancelled item's total
        sale.TotalAmount.Should().Be(20.00m);
    }

    [Fact]
    public void CancelItem_ShouldThrow_WhenItemNotFound()
    {
        var sale = Sale.Create("S-1", DateTime.UtcNow, "c1", "cust", "b1", "branch", new[]
        {
            NI("p1", "Prod 1", 2, 10m)
        });

        Action act = () => sale.CancelItem(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("Item not found");
    }
}