namespace Application.DTOs.Incident
{
    public class CreateIncidentRequestDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public int Priority { get; set; } = 3;
    }
}