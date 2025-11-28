using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.Persistence.Entities
{
    /// <summary>
    /// Entidad para almacenar tokens de acceso revocados (Blacklist)
    /// Permite revocar tokens de forma inmediata sin esperar expiración
    /// </summary>
    [Table("TokenBlacklist", Schema = "dbo")]
    public class TokenBlacklistEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// El token JWT revocado (almacenado en hash por seguridad)
        /// </summary>
        [Required]
        [MaxLength(512)]
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>
        /// ID del usuario propietario del token
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Fecha y hora cuando fue revocado
        /// </summary>
        public DateTime RevokedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Fecha y hora de expiración del token (para limpieza automática)
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Razón de revocación (logout, logout-all, etc.)
        /// </summary>
        [MaxLength(50)]
        public string Reason { get; set; } = "Manual revocation";

        // Foreign Key
        public UserEntity? User { get; set; }
    }
}