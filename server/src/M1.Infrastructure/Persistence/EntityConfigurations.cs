using M1.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M1.Infrastructure.Persistence;

public class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasIndex(u => u.Email).IsUnique();
        b.Property(u => u.Email).HasMaxLength(320);
        b.Property(u => u.FullName).HasMaxLength(200);
        b.HasIndex(u => u.GoogleId);
    }
}

public class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.HasIndex(t => t.TokenHash);
        b.HasOne(t => t.User).WithMany().OnDelete(DeleteBehavior.Cascade);
    }
}

public class EmailTokenConfig : IEntityTypeConfiguration<EmailToken>
{
    public void Configure(EntityTypeBuilder<EmailToken> b)
    {
        b.HasIndex(t => t.TokenHash);
        b.HasOne(t => t.User).WithMany().OnDelete(DeleteBehavior.Cascade);
    }
}

public class CategoryConfig : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.HasIndex(c => c.Slug).IsUnique();
        b.Property(c => c.Name).HasMaxLength(120);
        b.HasOne(c => c.Parent).WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BrandConfig : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> b)
    {
        b.HasIndex(x => x.Slug).IsUnique();
        b.Property(x => x.Name).HasMaxLength(120);
    }
}

public class ProductConfig : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.HasIndex(p => p.Slug).IsUnique();
        b.Property(p => p.Name).HasMaxLength(200);
        b.Property(p => p.Currency).HasMaxLength(3);
        b.HasOne(p => p.Category).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.Brand).WithMany().OnDelete(DeleteBehavior.SetNull);
    }
}

public class ProductVariantConfig : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> b)
    {
        b.HasIndex(v => v.Sku).IsUnique();
        b.HasOne(v => v.Product).WithMany(p => p.Variants)
            .HasForeignKey(v => v.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CartConfig : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> b)
    {
        b.HasIndex(c => c.UserId).IsUnique(); // one cart per user
    }
}

public class CartItemConfig : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> b)
    {
        b.HasIndex(i => new { i.CartId, i.VariantId }).IsUnique();
        b.HasOne(i => i.Variant).WithMany().OnDelete(DeleteBehavior.Cascade);
    }
}

public class WishlistItemConfig : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> b)
    {
        b.HasIndex(w => new { w.UserId, w.ProductId }).IsUnique();
    }
}

public class CouponConfig : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> b)
    {
        b.HasIndex(c => c.Code).IsUnique();
        b.Property(c => c.Code).HasMaxLength(40);
    }
}

public class OrderConfig : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.HasIndex(o => o.OrderNumber).IsUnique();
        b.Property(o => o.OrderNumber).HasMaxLength(24);
        b.OwnsOne(o => o.ShippingAddress);
        b.HasOne(o => o.User).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.HasOne(o => o.Coupon).WithMany().OnDelete(DeleteBehavior.SetNull);
    }
}

public class OrderItemConfig : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.HasOne(i => i.Order).WithMany(o => o.Items)
            .HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        // Keep sold variants referenced: catalog cleanup must not delete history.
        b.HasOne(i => i.Variant).WithMany().OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReviewConfig : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> b)
    {
        b.HasIndex(r => new { r.ProductId, r.UserId }).IsUnique();
        b.HasOne(r => r.Product).WithMany().OnDelete(DeleteBehavior.Cascade);
        b.HasOne(r => r.User).WithMany().OnDelete(DeleteBehavior.Cascade);
    }
}
