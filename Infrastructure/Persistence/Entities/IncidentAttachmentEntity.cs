using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Persistence.Entities
{
    public class IncidentAttachmentEntity
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

        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public IncidentEntity Incident { get; set; } = null!;
        public UserEntity UploadedByUser { get; set; } = null!;
    }
}