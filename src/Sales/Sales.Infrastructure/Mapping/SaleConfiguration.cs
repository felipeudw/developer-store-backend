using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sales.Domain.Entities;

namespace Sales.Infrastructure.Mapping
{
    public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
    {
        public void Configure(EntityTypeBuilder<Sale> builder)
        {
            builder.ToTable("sales");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.SaleNumber).IsRequired().HasMaxLength(64);
            builder.Property(s => s.SaleDate).IsRequired();

            builder.Property(s => s.CustomerId).IsRequired().HasMaxLength(64);
            builder.Property(s => s.CustomerName).IsRequired().HasMaxLength(256);

            builder.Property(s => s.BranchId).IsRequired().HasMaxLength(64);
            builder.Property(s => s.BranchName).IsRequired().HasMaxLength(256);

            builder.Property(s => s.TotalAmount).HasPrecision(18, 2);
            builder.Property(s => s.Cancelled).IsRequired();

            builder.Property(s => s.CreatedAt).IsRequired();
            builder.Property(s => s.UpdatedAt).IsRequired();

            builder.HasIndex(s => s.SaleNumber).IsUnique();

            // Backing field for items and relationship
            builder.Navigation(s => s.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasMany(s => s.Items)
                   .WithOne()
                   .HasForeignKey("SaleId")
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
    {
        public void Configure(EntityTypeBuilder<SaleItem> builder)
        {
            builder.ToTable("sale_items");

            builder.HasKey(i => i.Id);

            builder.Property<Guid>("SaleId").IsRequired();
            builder.HasIndex("SaleId");

            builder.Property(i => i.ProductId).IsRequired().HasMaxLength(64);
            builder.Property(i => i.ProductName).IsRequired().HasMaxLength(256);

            builder.Property(i => i.Quantity).IsRequired();

            builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
            builder.Property(i => i.DiscountPercent).HasPrecision(5, 2);
            builder.Property(i => i.TotalAmount).HasPrecision(18, 2);

            builder.Property(i => i.Cancelled).IsRequired();
        }
    }
}