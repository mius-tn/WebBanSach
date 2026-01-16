using Microsoft.EntityFrameworkCore;
using WedBanSach.Models;

namespace WedBanSach.Data;

public class BookStoreDbContext : DbContext
{
    public BookStoreDbContext(DbContextOptions<BookStoreDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserAddress> UserAddresses { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Publisher> Publishers { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<BookAuthor> BookAuthors { get; set; }
    public DbSet<BookCategory> BookCategories { get; set; }
    public DbSet<BookImage> BookImages { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<Shipping> Shippings { get; set; }
    public DbSet<ShippingMethod> ShippingMethods { get; set; }
    public DbSet<InventoryLog> InventoryLogs { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<PaymentSetting> PaymentSettings { get; set; }
    public DbSet<ChatRoom> ChatRooms { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure composite keys
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserID, ur.RoleID });

        modelBuilder.Entity<BookAuthor>()
            .HasKey(ba => new { ba.BookID, ba.AuthorID });

        modelBuilder.Entity<BookCategory>()
            .HasKey(bc => new { bc.BookID, bc.CategoryID });

        // Configure relationships
        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleID);

        modelBuilder.Entity<BookAuthor>()
            .HasOne(ba => ba.Book)
            .WithMany(b => b.BookAuthors)
            .HasForeignKey(ba => ba.BookID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BookAuthor>()
            .HasOne(ba => ba.Author)
            .WithMany(a => a.BookAuthors)
            .HasForeignKey(ba => ba.AuthorID);

        modelBuilder.Entity<BookCategory>()
            .HasOne(bc => bc.Book)
            .WithMany(b => b.BookCategories)
            .HasForeignKey(bc => bc.BookID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BookCategory>()
            .HasOne(bc => bc.Category)
            .WithMany(c => c.BookCategories)
            .HasForeignKey(bc => bc.CategoryID);

        modelBuilder.Entity<Category>()
            .HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryID)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure indexes
        modelBuilder.Entity<Book>()
            .HasIndex(b => b.ISBN)
            .IsUnique();

        modelBuilder.Entity<Book>()
            .HasIndex(b => b.Title);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Coupon>()
            .HasIndex(c => c.Code)
            .IsUnique();

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.OrderDate);

        // Configure constraints
        modelBuilder.Entity<Book>()
            .ToTable(t => t.HasCheckConstraint("CK_Book_StockQuantity", "StockQuantity >= 0"));

        modelBuilder.Entity<CartItem>()
            .ToTable(t => t.HasCheckConstraint("CK_CartItem_Quantity", "Quantity > 0"));

        modelBuilder.Entity<OrderDetail>()
            .ToTable(t => {
                t.HasCheckConstraint("CK_OrderDetail_Quantity", "Quantity > 0");
                t.HasTrigger("trg_UpdateStock");
            });

        modelBuilder.Entity<Review>()
            .ToTable(t => t.HasCheckConstraint("CK_Review_Rating", "Rating BETWEEN 1 AND 5"));
    }
}
