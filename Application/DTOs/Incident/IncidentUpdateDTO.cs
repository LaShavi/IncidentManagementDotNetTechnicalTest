namespace Application.DTOs.Incident
{
    public class IncidentUpdateDTO
    {
        public Guid Id { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string UpdateType { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}