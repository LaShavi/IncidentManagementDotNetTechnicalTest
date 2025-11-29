using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class IncidentUpdate
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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public Incident Incident { get; set; } = null!;
        public User Author { get; set; } = null!;

        // Domain Methods
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Comment) &&
                   !string.IsNullOrWhiteSpace(UpdateType) &&
                   IncidentId != Guid.Empty &&
                   AuthorId != Guid.Empty;
        }

        public bool IsCommentUpdate()
        {
            return UpdateType == "COMMENT";
        }

        public bool IsStatusUpdate()
        {
            return UpdateType == "STATUS_CHANGE";
        }

        public bool IsFieldUpdate()
        {
            return UpdateType == "FIELD_UPDATE";
        }
    }
}