namespace Application.DTOs.Incident
{
    public class UpdateIncidentRequestDTO
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? StatusId { get; set; }
        public int? Priority { get; set; }
    }
}