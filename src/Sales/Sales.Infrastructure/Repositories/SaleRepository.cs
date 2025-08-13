using Microsoft.EntityFrameworkCore;
using Sales.Domain.Entities;
using Sales.Domain.Repositories;
using Sales.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sales.Infrastructure.Repositories
{
    public sealed class SaleRepository : ISaleRepository
    {
        private readonly SalesDbContext _db;

        public SaleRepository(SalesDbContext db)
        {
            _db = db;
        }

        public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Sales
                .AsNoTracking()
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }

        public async Task<IReadOnlyList<Sale>> ListAsync(int page, int pageSize, CancellationToken ct = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 20;

            return await _db.Sales
                .AsNoTracking()
                .Include(s => s.Items)
                .OrderByDescending(s => s.SaleDate)
                .ThenByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
        }

        public async Task AddAsync(Sale sale, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sale);
            await _db.Sales.AddAsync(sale, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Sale sale, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(sale);

            // Attach and mark aggregate for update. Items are owned by aggregate and cascade rules handle changes.
            var tracked = await _db.Sales
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == sale.Id, ct);

            if (tracked is null)
            {
                // Not tracked - just update whole graph
                _db.Sales.Update(sale);
            }
            else
            {
                // EF will compute differences when we set current values and handle collection
                _db.Entry(tracked).CurrentValues.SetValues(sale);

                // Sync items (remove deleted, add new, update existing)
                // Build lookup for existing items
                var existingItems = tracked.Items.ToDictionary(i => i.Id, i => i);

                // Remove items not present anymore
                foreach (var ei in tracked.Items.ToList())
                {
                    if (!sale.Items.Any(i => i.Id == ei.Id))
                    {
                        _db.Entry(ei).State = EntityState.Deleted;
                    }
                }

                // Add or update items
                foreach (var item in sale.Items)
                {
                    if (existingItems.TryGetValue(item.Id, out var existing))
                    {
                        _db.Entry(existing).CurrentValues.SetValues(item);
                    }
                    else
                    {
                        // Ensure the relationship is set for newly added items
                        var entry = _db.Entry(item);
                        entry.Property("SaleId").CurrentValue = tracked.Id; // set shadow FK
                        entry.State = EntityState.Added;
                    }
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Sales.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (entity is null) return;

            _db.Sales.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}