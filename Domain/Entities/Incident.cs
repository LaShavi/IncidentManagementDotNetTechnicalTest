using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class Incident
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
        public int StatusId { get; set; } = 1;

        [Required]
        [Range(1, 5)]
        public int Priority { get; set; } = 3;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ClosedAt { get; set; }

        // Navigation Properties
        public User User { get; set; } = null!;
        public IncidentCategory Category { get; set; } = null!;
        public IncidentStatus Status { get; set; } = null!;
        public ICollection<IncidentUpdate> Updates { get; set; } = new List<IncidentUpdate>();
        public ICollection<IncidentAttachment> Attachments { get; set; } = new List<IncidentAttachment>();
        public IncidentMetric? Metrics { get; set; }

        // Domain Methods
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Title) &&
                   !string.IsNullOrWhiteSpace(Description) &&
                   UserId != Guid.Empty &&
                   CategoryId != Guid.Empty &&
                   Priority >= 1 && Priority <= 5;
        }

        public bool IsClosed()
        {
            return StatusId == 3 && ClosedAt.HasValue;
        }

        public void CloseIncident()
        {
            StatusId = 3;
            ClosedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateStatus(int newStatusId)
        {
            if (StatusId != newStatusId)
            {
                StatusId = newStatusId;
                UpdatedAt = DateTime.UtcNow;

                if (newStatusId == 3)
                {
                    ClosedAt = DateTime.UtcNow;
                }
            }
        }

        public void UpdateBasicInfo(string? title, string? description, int? priority)
        {
            if (!string.IsNullOrWhiteSpace(title))
                Title = title;

            if (!string.IsNullOrWhiteSpace(description))
                Description = description;

            if (priority.HasValue && priority >= 1 && priority <= 5)
                Priority = priority.Value;

            UpdatedAt = DateTime.UtcNow;
        }
    }
}