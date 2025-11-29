using Application.DTOs.Incident;
using Domain.Enums;

namespace Application.Helpers
{
    public static class PriorityHelper
    {
        public static List<PriorityDTO> GetAllPriorities()
        {
            //foreach (IncidentPriority value in Enum.GetValues(typeof(IncidentPriority)))
            //{
            //    Console.WriteLine($"Prioridad actual: {value}");
            //}

            return new List<PriorityDTO>
            {
                new PriorityDTO 
                { 
                    Value = (int)IncidentPriority.VeryLow,
                    Name = nameof(IncidentPriority.VeryLow),
                    DisplayName = "Muy Baja",
                    Color = "#808080"
                },
                new PriorityDTO 
                { 
                    Value = (int)IncidentPriority.Low,
                    Name = nameof(IncidentPriority.Low),
                    DisplayName = "Baja",
                    Color = "#4CAF50"
                },
                new PriorityDTO 
                { 
                    Value = (int)IncidentPriority.Medium,
                    Name = nameof(IncidentPriority.Medium),
                    DisplayName = "Media",
                    Color = "#FFC107"
                },
                new PriorityDTO 
                { 
                    Value = (int)IncidentPriority.High,
                    Name = nameof(IncidentPriority.High),
                    DisplayName = "Alta",
                    Color = "#FF9800"
                },
                new PriorityDTO 
                { 
                    Value = (int)IncidentPriority.Critical,
                    Name = nameof(IncidentPriority.Critical),
                    DisplayName = "Crítica",
                    Color = "#F44336"
                }
            };
        }

        public static string GetDisplayName(int priority)
        {
            return priority switch
            {
                1 => "Muy Baja",
                2 => "Baja",
                3 => "Media",
                4 => "Alta",
                5 => "Crítica",
                _ => "Desconocida"
            };
        }

        public static string GetColor(int priority)
        {
            return priority switch
            {
                1 => "#808080",
                2 => "#4CAF50",
                3 => "#FFC107",
                4 => "#FF9800",
                5 => "#F44336",
                _ => "#000000"
            };
        }
    }
}