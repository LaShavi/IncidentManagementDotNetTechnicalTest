using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Incident
{
    /// <summary>
    /// DTO para filtrar incidentes con paginación
    /// </summary>
    public class IncidentFilterRequestDTO
    {
        /// <summary>
        /// Búsqueda por título (contiene)
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Filtrar por estado (StatusId)
        /// </summary>
        public int? StatusId { get; set; }

        /// <summary>
        /// Filtrar por prioridad (1-5)
        /// </summary>
        public int? Priority { get; set; }

        /// <summary>
        /// Filtrar por categoría
        /// </summary>
        public Guid? CategoryId { get; set; }

        /// <summary>
        /// Número de página (comienza en 1)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "PageNumber debe ser mayor a 0")]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Cantidad de registros por página
        /// </summary>
        [Range(1, 100, ErrorMessage = "PageSize debe estar entre 1 y 100")]
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Campo para ordenar (createdAt, priority, title)
        /// </summary>
        public string SortBy { get; set; } = "createdAt";

        /// <summary>
        /// Dirección de orden (asc, desc)
        /// </summary>
        public string SortOrder { get; set; } = "desc";
    }
}