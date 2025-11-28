using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        #region Modelos Generales    
        public DbSet<ClienteEntity> Clientes { get; set; }
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }
        public DbSet<PasswordResetTokenEntity> PasswordResetTokens { get; set; }
        public DbSet<TokenBlacklistEntity> TokenBlacklist { get; set; }
        #endregion

        #region DTOs 
        //public DbSet<ResponseAllTableOtherExample> ResponseAllTableOtherExample { get; set; }
        #endregion

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            var configuration = builder.Build();

            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    configuration.GetConnectionString("dbContext"),
                    sqlOptions => sqlOptions.EnableRetryOnFailure()
                );
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Desactivamos llave primaria para DTOs de SPs.
            //modelBuilder.Entity<ResponseAllTableOtherExample>().HasNoKey();

            // Definimos info de tablas
            #region Configuración Entidades
            
            // Cliente
            modelBuilder.Entity<ClienteEntity>(entity =>
            {
                entity.ToTable("Clientes", "dbo");
            });

            // User
            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.ToTable("Users", "dbo");
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username);
                entity.HasIndex(e => e.Email);
            });

            // RefreshToken
            modelBuilder.Entity<RefreshTokenEntity>(entity =>
            {
                entity.ToTable("RefreshTokens", "dbo");
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.UserId);
                
                // Relación con User
                entity.HasOne(rt => rt.User)
                      .WithMany(u => u.RefreshTokens)
                      .HasForeignKey(rt => rt.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // PasswordResetToken
            modelBuilder.Entity<PasswordResetTokenEntity>(entity =>
            {
                entity.ToTable("PasswordResetTokens", "dbo");
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // TokenBlacklist
            modelBuilder.Entity<TokenBlacklistEntity>(entity =>
            {
                entity.ToTable("TokenBlacklist", "dbo");
                entity.HasIndex(e => e.TokenHash).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt); // Para limpiar expirados
                
                // Relación con User
                entity.HasOne(tb => tb.User)
                      .WithMany()
                      .HasForeignKey(tb => tb.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            #endregion

            //OnModelCreatingPartial(modelBuilder);
        }
    }
}
