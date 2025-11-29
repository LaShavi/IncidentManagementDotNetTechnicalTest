using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Persistence.Entities
{
    public class IncidentMetricEntity
    {
        public Guid Id { get; set; }

        [Required]
        public Guid IncidentId { get; set; }

        public long? TimeToClose { get; set; }

        public int CommentCount { get; set; }

        public int AttachmentCount { get; set; }

        public int? AverageResolutionTime { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public IncidentEntity Incident { get; set; } = null!;
    }
}