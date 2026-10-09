using Microsoft.EntityFrameworkCore;

namespace StockSystemBack.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<ProductMaster> ProductMaster { get; set; }
        public DbSet<ProductStock> ProductStock { get; set; }
        public DbSet<ShoppingCart> ShoppingCart { get; set; }
        public DbSet<ShoppingOrder> ShoppingOrder { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductMaster>().HasNoKey();
            modelBuilder.Entity<ProductStock>().HasNoKey();
            modelBuilder.Entity<ProductStock>(entity =>
            {
                entity.ToTable("product_stock");
                entity.HasKey(e => e.stock_id);
                entity.Property(e => e.stock_id).ValueGeneratedNever();
            });
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ShoppingCart>().HasKey(x => x.cart_id);
            modelBuilder.Entity<ShoppingCart>().Property(x => x.cart_id).ValueGeneratedOnAdd();
            modelBuilder.Entity<ShoppingOrder>().HasKey(x => x.order_id);
            modelBuilder.Entity<ShoppingOrder>().Property(x => x.order_id).ValueGeneratedOnAdd();
        }
    }
}