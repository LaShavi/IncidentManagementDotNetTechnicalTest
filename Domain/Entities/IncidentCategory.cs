using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class IncidentCategory
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(7)]
        public string? Color { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public ICollection<Incident> Incidents { get; set; } = new List<Incident>();

        // Domain Methods
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Name) && Name.Length >= 2 && Name.Length <= 100;
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}