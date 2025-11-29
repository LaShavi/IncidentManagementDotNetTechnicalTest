using Application.DTOs.Incident;
using Application.Ports;
using Application.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.DTOs.Common;
using System.Security.Claims;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class IncidentController : BaseApiController
    {
        private readonly IIncidentService _incidentService;
        private readonly ILogger<IncidentController> _logger;
        private readonly IIncidentCategoryRepository _incidentCategoryRepository;
        private readonly IIncidentStatusRepository _incidentStatusRepository;
        private readonly IMapper _mapper;

        public IncidentController(
            IIncidentService incidentService,
            IIncidentCategoryRepository incidentCategoryRepository,
            IIncidentStatusRepository incidentStatusRepository,
            IMapper mapper,
            ILogger<IncidentController> logger) : base(logger)
        {
            _incidentService = incidentService;
            _incidentCategoryRepository = incidentCategoryRepository;
            _incidentStatusRepository = incidentStatusRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Obtener todos los incidentes
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<IEnumerable<IncidentResponseDTO>>>> GetAll()
        {
            return await ExecuteAsync(async () =>
            {
                var incidents = await _incidentService.GetAllAsync();
                return incidents;
            }, "Incidentes obtenidos exitosamente");
        }

        /// <summary>
        /// Obtener incidente por ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<IncidentResponseDTO>>> GetById(Guid id)
        {
            return await ExecuteAsync(async () =>
            {
                var incident = await _incidentService.GetByIdAsync(id);
                if (incident == null)
                    throw new KeyNotFoundException($"Incidente {id} no encontrado");

                return incident;
            }, "Incidente obtenido exitosamente");
        }

        /// <summary>
        /// Obtener incidentes del usuario actual
        /// </summary>
        [HttpGet("user/mine")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<IEnumerable<IncidentResponseDTO>>>> GetMyIncidents()
        {
            return await ExecuteAsync(async () =>
            {
                var userId = GetCurrentUserId();
                var incidents = await _incidentService.GetByUserIdAsync(userId);
                return incidents;
            }, "Incidentes del usuario obtenidos exitosamente");
        }

        /// <summary>
        /// Obtener incidentes por categoria
        /// </summary>
        [HttpGet("category/{categoryId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<IEnumerable<IncidentResponseDTO>>>> GetByCategory(Guid categoryId)
        {
            return await ExecuteAsync(async () =>
            {
                var incidents = await _incidentService.GetByCategoryIdAsync(categoryId);
                return incidents;
            }, "Incidentes por categoria obtenidos exitosamente");
        }

        /// <summary>
        /// Obtener incidentes por estado
        /// </summary>
        [HttpGet("status/{statusId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<IEnumerable<IncidentResponseDTO>>>> GetByStatus(int statusId)
        {
            return await ExecuteAsync(async () =>
            {
                var incidents = await _incidentService.GetByStatusIdAsync(statusId);
                return incidents;
            }, "Incidentes por estado obtenidos exitosamente");
        }

        /// <summary>
        /// Crear nuevo incidente
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse>> Create([FromBody] CreateIncidentRequestDTO dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse();

            return await ExecuteAsync(async () =>
            {
                var userId = GetCurrentUserId();
                var incident = await _incidentService.CreateAsync(dto, userId);
                // No retornar, solo ejecutar. ExecuteAsync maneja ApiResponse sin <T>
            }, "Incidente creado exitosamente");
        }

        /// <summary>
        /// Actualizar incidente existente
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse>> Update(Guid id, [FromBody] UpdateIncidentRequestDTO dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse();

            return await ExecuteAsync(async () =>
            {
                await _incidentService.UpdateAsync(id, dto);
            }, "Incidente actualizado exitosamente");
        }

        /// <summary>
        /// Reasignar incidente a otro usuario
        /// </summary>
        [HttpPut("{id}/assign")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse>> AssignToUser(Guid id, [FromBody] AssignUserRequestDTO dto)
        {
            return await ExecuteAsync(async () =>
            {
                var currentUserId = GetCurrentUserId();
                await _incidentService.AssignToUserAsync(id, dto.NewUserId, currentUserId);
            }, "Incidente reasignado exitosamente");
        }

        /// <summary>
        /// Eliminar incidente
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            return await ExecuteAsync(async () =>
            {
                await _incidentService.DeleteAsync(id);
            }, "Incidente eliminado exitosamente");
        }

        /// <summary>
        /// Agregar comentario a un incidente
        /// </summary>
        [HttpPost("{id}/comments")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse>> AddComment(Guid id, [FromBody] AddCommentRequestDTO dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse();

            return await ExecuteAsync(async () =>
            {
                var authorId = GetCurrentUserId();
                await _incidentService.AddCommentAsync(id, dto.Comment, authorId);
            }, "Comentario agregado exitosamente");
        }

        /// <summary>
        /// Obtener actualizaciones/comentarios de un incidente
        /// </summary>
        [HttpGet("{id}/updates")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<IEnumerable<IncidentUpdateDTO>>>> GetUpdates(Guid id)
        {
            return await ExecuteAsync(async () =>
            {
                var updates = await _incidentService.GetUpdatesAsync(id);
                return updates;
            }, "Actualizaciones obtenidas exitosamente");
        }

        /// <summary>
        /// Obtiene el ID del usuario actual del token JWT
        /// </summary>
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Unable to extract user ID from JWT token");
                throw new UnauthorizedAccessException("No se pudo obtener el ID del usuario del token");
            }

            return userId;
        }

        /// <summary>
        /// Obtener todas las categorias disponibles
        /// </summary>
        [HttpGet("categories")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IEnumerable<IncidentCategoryDTO>>>> GetCategories()
        {
            return await ExecuteAsync(async () =>
            {
                var categories = await _incidentCategoryRepository.GetActiveAsync();
                return _mapper.Map<IEnumerable<IncidentCategoryDTO>>(categories);
            }, "Categorias obtenidas exitosamente");
        }

        /// <summary>
        /// Obtener todos los estados disponibles
        /// </summary>
        [HttpGet("statuses")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IEnumerable<IncidentStatusDTO>>>> GetStatuses()
        {
            return await ExecuteAsync(async () =>
            {
                var statuses = await _incidentStatusRepository.GetAllAsync();
                return _mapper.Map<IEnumerable<IncidentStatusDTO>>(statuses);
            }, "Estados obtenidos exitosamente");
        }

        /// <summary>
        /// Obtener todas las prioridades disponibles
        /// </summary>
        [HttpGet("priorities")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IEnumerable<PriorityDTO>>>> GetPriorities()
        {
            return await ExecuteAsync(async () =>
            {
                IEnumerable<PriorityDTO> priorities = PriorityHelper.GetAllPriorities();
                return await Task.FromResult(priorities);
            }, "Prioridades obtenidas exitosamente");
        }
    }
}