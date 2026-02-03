namespace Api.DTOs.Common
{
    /// <summary>
    /// Información de paginación para respuestas paginadas
    /// </summary>
    public class PaginationMetaDTO
    {
        /// <summary>
        /// Página actual
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Registros por página
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total de registros encontrados
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// Total de páginas
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Indica si hay página anterior
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        /// Indica si hay página siguiente
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;
    }
}