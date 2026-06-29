using Microsoft.EntityFrameworkCore;

namespace StockSystemBack.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<ProductMaster> ProductMaster { get; set; }
        public DbSet<ProductStock> ProductStock { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductMaster>().HasNoKey();
            modelBuilder.Entity<ProductStock>().HasNoKey();
            modelBuilder.Entity<ProductStock>(entity =>
            {
                entity.ToTable("product_stock");
                entity.HasKey(e => e.stock_id);  // ✅ กำหนด PK ตรงนี้
                entity.Property(e => e.stock_id).ValueGeneratedNever();
            });
        }
    }
}