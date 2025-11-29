using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class IncidentMetric
    {
        public Guid Id { get; set; }

        [Required]
        public Guid IncidentId { get; set; }

        public long? TimeToClose { get; set; }

        public int CommentCount { get; set; } = 0;

        public int AttachmentCount { get; set; } = 0;

        public int? AverageResolutionTime { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public Incident Incident { get; set; } = null!;
    }
}