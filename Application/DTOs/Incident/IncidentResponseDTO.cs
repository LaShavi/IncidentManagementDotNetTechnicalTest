namespace Application.DTOs.Incident
{
    public class IncidentResponseDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string PriorityName { get; set; } = string.Empty;  // "Alta", "Crítica", etc.
        public string PriorityColor { get; set; } = string.Empty; // "#FF9800"
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public int CommentCount { get; set; }
        public int AttachmentCount { get; set; }
    }
}