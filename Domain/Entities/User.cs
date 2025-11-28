using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "User";
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastAccess { get; set; }
        
        public int FailedAttempts { get; set; } = 0;
        
        public DateTime? LockedUntil { get; set; }

        // Domain methods
        public string GetFullName()
        {
            return $"{FirstName} {LastName}".Trim();
        }

        public bool IsLocked()
        {
            return LockedUntil.HasValue && LockedUntil > DateTime.UtcNow;
        }

        public void LockTemporarily(int minutes = 15)
        {
            LockedUntil = DateTime.UtcNow.AddMinutes(minutes);
        }

        public void UnlockAccount()
        {
            LockedUntil = null;
            FailedAttempts = 0;
        }

        public void RegisterFailedAttempt()
        {
            FailedAttempts++;
            if (FailedAttempts >= 5)
            {
                LockTemporarily();
            }
        }

        public void RegisterSuccessfulAccess()
        {
            LastAccess = DateTime.UtcNow;
            FailedAttempts = 0;
            LockedUntil = null;
        }
    }
}
