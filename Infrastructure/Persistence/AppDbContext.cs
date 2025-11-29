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

        #region Incident Management
        public DbSet<IncidentEntity> Incidents { get; set; }
        public DbSet<IncidentCategoryEntity> IncidentCategories { get; set; }
        public DbSet<IncidentStatusEntity> IncidentStatuses { get; set; }
        public DbSet<IncidentUpdateEntity> IncidentUpdates { get; set; }
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
            #region Configuracion Entidades

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
                entity.HasIndex(e => e.ExpiresAt);
                
                entity.HasOne(tb => tb.User)
                      .WithMany()
                      .HasForeignKey(tb => tb.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // IncidentCategory
            modelBuilder.Entity<IncidentCategoryEntity>(entity =>
            {
                entity.ToTable("IncidentCategories", "dbo");
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.IsActive);
            });

            // IncidentStatus
            modelBuilder.Entity<IncidentStatusEntity>(entity =>
            {
                entity.ToTable("IncidentStatuses", "dbo");
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.OrderSequence);
            });

            // Incident
            modelBuilder.Entity<IncidentEntity>(entity =>
            {
                entity.ToTable("Incidents", "dbo");
                
                entity.HasIndex(e => new { e.UserId, e.StatusId }).IsClustered();
                entity.HasIndex(e => e.CategoryId);
                entity.HasIndex(e => e.StatusId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.Priority);

                entity.HasOne(i => i.User)
                      .WithMany()
                      .HasForeignKey(i => i.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(i => i.Category)
                      .WithMany(c => c.Incidents)
                      .HasForeignKey(i => i.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.Status)
                      .WithMany()
                      .HasForeignKey(i => i.StatusId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(i => i.Updates)
                      .WithOne(u => u.Incident)
                      .HasForeignKey(u => u.IncidentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // IncidentUpdate
            modelBuilder.Entity<IncidentUpdateEntity>(entity =>
            {
                entity.ToTable("IncidentUpdates", "dbo");
                
                entity.HasIndex(e => new { e.IncidentId, e.CreatedAt }).IsClustered();
                entity.HasIndex(e => e.AuthorId);
                entity.HasIndex(e => e.UpdateType);

                entity.HasOne(u => u.Incident)
                      .WithMany(i => i.Updates)
                      .HasForeignKey(u => u.IncidentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(u => u.Author)
                      .WithMany()
                      .HasForeignKey(u => u.AuthorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion

            //OnModelCreatingPartial(modelBuilder);
        }
    }
}
