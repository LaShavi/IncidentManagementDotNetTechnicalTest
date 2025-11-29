using Domain.Enums;

namespace Application.DTOs.Incident
{
    public class PriorityDTO
    {
        public int Value { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }
}