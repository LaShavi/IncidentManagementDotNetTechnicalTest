using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Token { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime ExpiresAt { get; set; }
        
        public bool IsRevoked { get; set; } = false;
        
        public DateTime? RevokedAt { get; set; }
        
        [MaxLength(500)]
        public string? ReplacedBy { get; set; }

        // Navigation
        public User User { get; set; } = null!;

        // Domain methods
        public bool IsExpired()
        {
            return DateTime.UtcNow >= ExpiresAt;
        }

        public bool IsActive()
        {
            return !IsRevoked && !IsExpired();
        }

        public void Revoke(string? replacedBy = null)
        {
            IsRevoked = true;
            RevokedAt = DateTime.UtcNow;
            ReplacedBy = replacedBy;
        }
    }
}