using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Persistence.Entities
{
    public class IncidentUpdateEntity
    {
        public Guid Id { get; set; }

        [Required]
        public Guid IncidentId { get; set; }

        [Required]
        public Guid AuthorId { get; set; }

        [Required]
        public string Comment { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string UpdateType { get; set; } = string.Empty;

        [MaxLength(int.MaxValue)]
        public string? OldValue { get; set; }

        [MaxLength(int.MaxValue)]
        public string? NewValue { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public IncidentEntity Incident { get; set; } = null!;
        public UserEntity Author { get; set; } = null!;
    }
}