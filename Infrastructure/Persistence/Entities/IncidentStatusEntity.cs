using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Persistence.Entities
{
    public class IncidentStatusEntity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        public int OrderSequence { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public ICollection<IncidentEntity> Incidents { get; set; } = new List<IncidentEntity>();
    }
}