using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Persistence.Entities
{
    public class IncidentEntity
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid CategoryId { get; set; }

        [Required]
        public int StatusId { get; set; }

        [Required]
        public int Priority { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ClosedAt { get; set; }

        // Navigation Properties
        public UserEntity User { get; set; } = null!;
        public IncidentCategoryEntity Category { get; set; } = null!;
        public IncidentStatusEntity Status { get; set; } = null!;
        public ICollection<IncidentUpdateEntity> Updates { get; set; } = new List<IncidentUpdateEntity>();
    }
}