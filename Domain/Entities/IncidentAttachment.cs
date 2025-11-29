using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class IncidentAttachment
    {
        public Guid Id { get; set; }

        [Required]
        public Guid IncidentId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        public long FileSize { get; set; }

        [Required]
        [MaxLength(50)]
        public string FileType { get; set; } = string.Empty;

        [Required]
        public Guid UploadedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public Incident Incident { get; set; } = null!;
        public User UploadedByUser { get; set; } = null!;
    }
}