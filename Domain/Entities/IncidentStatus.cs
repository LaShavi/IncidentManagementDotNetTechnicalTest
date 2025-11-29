using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class IncidentStatus
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

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<Incident> Incidents { get; set; } = new List<Incident>();

        // Domain Methods
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(DisplayName) &&
                   OrderSequence > 0;
        }
    }
}