using System;
using System.Collections.Generic;

namespace Sales.Api.Features.Sales
{
    public sealed class SaleItemRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public sealed class CreateSaleRequest
    {
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime? SaleDate { get; set; }

        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;

        public string BranchId { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;

        public List<SaleItemRequest> Items { get; set; } = new();
    }

    public sealed class UpdateSaleRequest
    {
        public string? SaleNumber { get; set; }
        public DateTime? SaleDate { get; set; }

        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }

        public string? BranchId { get; set; }
        public string? BranchName { get; set; }

        public List<SaleItemRequest> Items { get; set; } = new();
    }

    public sealed class SaleItemResponse
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

    public sealed class SaleResponse
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

        public List<SaleItemResponse> Items { get; set; } = new();
    }

}