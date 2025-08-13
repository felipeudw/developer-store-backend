using System;

namespace Sales.Application.IntegrationEvents
{
    // Integration Events for Sales bounded context

    public sealed record SaleCreatedEvent(
        Guid SaleId,
        string SaleNumber,
        DateTime SaleDate,
        string CustomerId,
        string CustomerName,
        string BranchId,
        string BranchName,
        decimal TotalAmount
    );

    public sealed record SaleModifiedEvent(
        Guid SaleId,
        string SaleNumber,
        DateTime SaleDate,
        string CustomerId,
        string CustomerName,
        string BranchId,
        string BranchName,
        decimal TotalAmount
    );

    public sealed record SaleCancelledEvent(
        Guid SaleId,
        DateTime CancelledAt,
        string SaleNumber,
        string CustomerId,
        string CustomerName,
        string BranchId,
        string BranchName,
        decimal TotalAmount
    );

    public sealed record ItemCancelledEvent(
        Guid SaleId,
        Guid ItemId,
        string ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        decimal DiscountPercent
    );
}