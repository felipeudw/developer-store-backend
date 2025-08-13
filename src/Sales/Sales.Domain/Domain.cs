using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sales.Domain.Entities
{
    /// <summary>
    /// Aggregate root representing a Sale.
    /// </summary>
    public class Sale
    {
// Domain length constraints (kept in sync with EF configuration)
        private const int MaxSaleNumberLength = 64;
        private const int MaxCustomerIdLength = 64;
        private const int MaxCustomerNameLength = 256;
        private const int MaxBranchIdLength = 64;
        private const int MaxBranchNameLength = 256;
        private const int MaxProductIdLength = 64;
        private const int MaxProductNameLength = 256;
        private readonly List<SaleItem> _items = new();

        public Guid Id { get; private set; } = Guid.NewGuid();
        public string SaleNumber { get; private set; } = string.Empty;
        public DateTime SaleDate { get; private set; } = DateTime.UtcNow;

        // External identities (denormalized)
        public string CustomerId { get; private set; } = string.Empty;
        public string CustomerName { get; private set; } = string.Empty;

        public string BranchId { get; private set; } = string.Empty;
        public string BranchName { get; private set; } = string.Empty;

        public decimal TotalAmount { get; private set; }
        public bool Cancelled { get; private set; }

        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

        public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();

        public static Sale Create(string saleNumber,
                                  DateTime saleDate,
                                  string customerId,
                                  string customerName,
                                  string branchId,
                                  string branchName,
                                  IEnumerable<NewItem> items)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(saleNumber);
            ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
            ArgumentException.ThrowIfNullOrWhiteSpace(customerName);
            ArgumentException.ThrowIfNullOrWhiteSpace(branchId);
            ArgumentException.ThrowIfNullOrWhiteSpace(branchName);

            ValidateMaxLength(saleNumber, MaxSaleNumberLength, nameof(saleNumber));
            ValidateMaxLength(customerId, MaxCustomerIdLength, nameof(customerId));
            ValidateMaxLength(customerName, MaxCustomerNameLength, nameof(customerName));
            ValidateMaxLength(branchId, MaxBranchIdLength, nameof(branchId));
            ValidateMaxLength(branchName, MaxBranchNameLength, nameof(branchName));

            var sale = new Sale
            {
                SaleNumber = saleNumber.Trim(),
                SaleDate = saleDate == default ? DateTime.UtcNow : saleDate,
                CustomerId = customerId.Trim(),
                CustomerName = customerName.Trim(),
                BranchId = branchId.Trim(),
                BranchName = branchName.Trim()
            };

            sale.ReplaceItems(items);
            return sale;
        }

        public void Update(string saleNumber,
                           DateTime saleDate,
                           string customerId,
                           string customerName,
                           string branchId,
                           string branchName,
                           IEnumerable<NewItem> items)
        {
            if (!string.IsNullOrWhiteSpace(saleNumber))
            {
                ValidateMaxLength(saleNumber, MaxSaleNumberLength, nameof(saleNumber));
                SaleNumber = saleNumber.Trim();
            }
            if (!string.IsNullOrWhiteSpace(customerId))
            {
                ValidateMaxLength(customerId, MaxCustomerIdLength, nameof(customerId));
                CustomerId = customerId.Trim();
            }
            if (!string.IsNullOrWhiteSpace(customerName))
            {
                ValidateMaxLength(customerName, MaxCustomerNameLength, nameof(customerName));
                CustomerName = customerName.Trim();
            }
            if (!string.IsNullOrWhiteSpace(branchId))
            {
                ValidateMaxLength(branchId, MaxBranchIdLength, nameof(branchId));
                BranchId = branchId.Trim();
            }
            if (!string.IsNullOrWhiteSpace(branchName))
            {
                ValidateMaxLength(branchName, MaxBranchNameLength, nameof(branchName));
                BranchName = branchName.Trim();
            }
            SaleDate = saleDate == default ? SaleDate : saleDate;

            ReplaceItems(items);
        }

        public void CancelSale()
        {
            if (Cancelled) return;
            Cancelled = true;
            foreach (var it in _items)
            {
                it.Cancel();
            }
            RecalculateTotals();
        }

        public void CancelItem(Guid itemId)
        {
            var it = _items.FirstOrDefault(i => i.Id == itemId);
            if (it is null) throw new InvalidOperationException("Item not found");
            it.Cancel();
            RecalculateTotals();
        }

        private void ReplaceItems(IEnumerable<NewItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            _items.Clear();

            foreach (var ni in items)
            {
                ValidateItemInput(ni.ProductId, ni.ProductName, ni.Quantity, ni.UnitPrice);

                var discountPercent = CalculateDiscountPercent(ni.Quantity);
                var item = new SaleItem(Guid.NewGuid(), ni.ProductId.Trim(), ni.ProductName.Trim(),
                                        ni.Quantity, EnsureTwoDecimals(ni.UnitPrice), discountPercent);
                _items.Add(item);
            }

            RecalculateTotals();
        }

        private static void ValidateItemInput(string productId, string productName, int quantity, decimal unitPrice)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(productId);
            ArgumentException.ThrowIfNullOrWhiteSpace(productName);

            ValidateMaxLength(productId, MaxProductIdLength, nameof(productId));
            ValidateMaxLength(productName, MaxProductNameLength, nameof(productName));

            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero");
            if (unitPrice <= 0) throw new ArgumentOutOfRangeException(nameof(unitPrice), "UnitPrice must be greater than zero");
            if (quantity > 20) throw new InvalidOperationException("It's not possible to sell above 20 identical items");
        }

        public static decimal CalculateDiscountPercent(int quantity)
        {
            if (quantity > 20) throw new InvalidOperationException("It's not possible to sell above 20 identical items");
            if (quantity >= 10) return 0.20m;
            if (quantity >= 4) return 0.10m;
            return 0.00m; // below 4 items cannot have discount
        }

        private void RecalculateTotals()
        {
            foreach (var item in _items)
            {
                if (!item.Cancelled)
                {
                    item.RecalculateTotals();
                }
                else
                {
                    item.SetTotals(0m);
                }
            }

            TotalAmount = EnsureTwoDecimals(_items.Where(i => !i.Cancelled).Sum(i => i.TotalAmount));
            UpdatedAt = DateTime.UtcNow;
        }

        private static decimal EnsureTwoDecimals(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private static void ValidateMaxLength(string value, int max, string paramName)
        {
            var len = (value ?? string.Empty).Trim().Length;
            if (len > max) throw new ArgumentOutOfRangeException(paramName, $"Maximum length is {max} characters");
        }

        public readonly record struct NewItem(string ProductId, string ProductName, int Quantity, decimal UnitPrice);
    }

    /// <summary>
    /// Sale item (entity)
    /// </summary>
    public class SaleItem
    {
        internal SaleItem() { } // EF
        internal SaleItem(Guid id, string productId, string productName, int quantity, decimal unitPrice, decimal discountPercent)
        {
            Id = id;
            ProductId = productId;
            ProductName = productName;
            Quantity = quantity;
            UnitPrice = unitPrice;
            DiscountPercent = discountPercent;
            RecalculateTotals();
        }

        public Guid Id { get; private set; }
        public string ProductId { get; private set; } = string.Empty;
        public string ProductName { get; private set; } = string.Empty;

        public int Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }

        /// <summary>0.00 - 1.00 range (e.g., 0.10 for 10%)</summary>
        public decimal DiscountPercent { get; private set; }

        public decimal TotalAmount { get; private set; }
        public bool Cancelled { get; private set; }

        internal void Cancel() => Cancelled = true;

        internal void RecalculateTotals()
        {
            var gross = UnitPrice * Quantity;
            var discount = gross * DiscountPercent;
            var net = gross - discount;
            TotalAmount = Math.Round(net, 2, MidpointRounding.AwayFromZero);
        }

        internal void SetTotals(decimal total) => TotalAmount = Math.Round(total, 2, MidpointRounding.AwayFromZero);
    }
}

namespace Sales.Domain.Repositories
{
    using Sales.Domain.Entities;
    using System.Threading;

    public interface ISaleRepository
    {
        Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<Sale>> ListAsync(int page, int pageSize, CancellationToken ct = default);
        Task AddAsync(Sale sale, CancellationToken ct = default);
        Task UpdateAsync(Sale sale, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}