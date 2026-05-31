using ProductManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ProductManagementSystem.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<SysUser> SysUser { get; set; }
        public DbSet<UserToken> UserToken { get; set; }
        public DbSet<Product> Product { get; set; }
        public DbSet<Item> Item { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(UserEntity).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .HasOne(typeof(SysUser), "CreatedByUser")
                        .WithMany()
                        .HasForeignKey("CreatedBy")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName($"{entityType.GetTableName()}_createdby_fkey");

                    modelBuilder.Entity(entityType.ClrType)
                        .HasOne(typeof(SysUser), "UpdatedByUser")
                        .WithMany()
                        .HasForeignKey("UpdatedBy")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName($"{entityType.GetTableName()}_updatedby_fkey");
                }

                // Optional: Set all tables & columns to lowercase for PostgreSQL
                entityType.SetTableName(entityType.GetTableName()?.ToLower());

                foreach (var property in entityType.GetProperties())
                {
                    property.SetColumnName(property.Name.ToLower());
                }

                foreach (var foreignKey in entityType.GetForeignKeys())
                {
                    foreach (var property in foreignKey.Properties)
                    {
                        property.SetColumnName(property.Name.ToLower());
                    }
                    foreignKey.SetConstraintName(foreignKey?.GetConstraintName()?.ToLower());
                }

                foreach (var key in entityType.GetKeys())
                {
                    key.SetName(key.GetName()?.ToLower());
                }

                foreach (var index in entityType.GetIndexes())
                {
                    index.SetDatabaseName(index.GetDatabaseName()?.ToLower());
                }
            }
        }
    }
}

