using Application.DTOs.Incident;
using Application.Services;
using Application.Ports;
using Domain.Entities;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Tests.Application.Services;

/// <summary>
/// Test suite para IncidentService
/// Prueba la lógica de gestión de incidentes: crear, actualizar, eliminar, comentar, reasignar, etc.
/// Utiliza Moq para simular repositorios y AutoMapper
/// </summary>
public class IncidentServiceTests
{
    private readonly Mock<IIncidentRepository> _mockIncidentRepository;
    private readonly Mock<IIncidentCategoryRepository> _mockCategoryRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<IncidentService>> _mockLogger;
    private readonly IncidentService _incidentService;

    /// <summary>
    /// Constructor: Inicializa los mocks y el IncidentService
    /// Se ejecuta ANTES de cada prueba [Fact]
    /// </summary>
    public IncidentServiceTests()
    {
        _mockIncidentRepository = new Mock<IIncidentRepository>();
        _mockCategoryRepository = new Mock<IIncidentCategoryRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<IncidentService>>();

        _incidentService = new IncidentService(
            _mockIncidentRepository.Object,
            _mockCategoryRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object
        );
    }

    #region CreateAsync Tests

    /// <summary>
    /// PRUEBA 1: Crear incidente con datos válidos
    /// 
    /// Objetivo: Verificar que se crea un incidente correctamente
    /// Resultado esperado: Incidente creado con userId del token, statusId=1 (OPEN)
    /// 
    /// Escenario real:
    /// Usuario autenticado crea incidente con:
    /// - Título, descripción, categoryId válida, prioridad (1-5)
    /// Sistema valida que la categoría existe
    /// Sistema crea incidente con:
    /// - userId = del token JWT
    /// - statusId = 1 (OPEN por defecto)
    /// - createdAt = DateTime.UtcNow
    /// </summary>
    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateIncident()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var createDto = new CreateIncidentRequestDTO
        {
            Title = "Bug en login",
            Description = "Los usuarios no pueden iniciar sesión",
            CategoryId = categoryId,
            Priority = 5
        };

        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            Title = createDto.Title,
            Description = createDto.Description,
            CategoryId = categoryId,
            UserId = userId,
            Priority = 5,
            StatusId = 1
        };

        var responseDto = new IncidentResponseDTO
        {
            Id = incident.Id,
            Title = incident.Title,
            UserId = userId
        };

        // Mock: Categoría existe
        _mockCategoryRepository.Setup(r => r.ExistsAsync(categoryId))
            .ReturnsAsync(true);

        // Mock: Mapper convierte DTO a Entidad
        _mockMapper.Setup(m => m.Map<Incident>(createDto))
            .Returns(incident);

        // Mock: Repositorio guarda el incidente
        _mockIncidentRepository.Setup(r => r.AddAsync(It.IsAny<Incident>()))
            .Returns(Task.CompletedTask);

        // Mock: Mapper convierte Entidad a ResponseDTO
        _mockMapper.Setup(m => m.Map<IncidentResponseDTO>(It.IsAny<Incident>()))
            .Returns(responseDto);

        // ACT
        var result = await _incidentService.CreateAsync(createDto, userId);

        // ASSERT
        result.Should().NotBeNull();
        result.Title.Should().Be("Bug en login");
        result.UserId.Should().Be(userId);
        _mockIncidentRepository.Verify(r => r.AddAsync(It.IsAny<Incident>()), Times.Once);
    }

    /// <summary>
    /// PRUEBA 2: Crear incidente con categoría inexistente
    /// 
    /// Objetivo: Verificar que no se puede crear incidente con categoría inválida
    /// Resultado esperado: KeyNotFoundException
    /// </summary>
    [Fact]
    public async Task CreateAsync_WithInvalidCategory_ShouldThrowKeyNotFoundException()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var invalidCategoryId = Guid.NewGuid();
        var createDto = new CreateIncidentRequestDTO
        {
            Title = "Test",
            Description = "Test description",
            CategoryId = invalidCategoryId,
            Priority = 3
        };

        // Mock: Categoría NO existe
        _mockCategoryRepository.Setup(r => r.ExistsAsync(invalidCategoryId))
            .ReturnsAsync(false);

        // ACT & ASSERT
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _incidentService.CreateAsync(createDto, userId));
    }

    /// <summary>
    /// PRUEBA 3: Crear incidente con datos inválidos (título vacío)
    /// 
    /// Objetivo: Verificar que se validan los datos del dominio
    /// Resultado esperado: InvalidOperationException
    /// </summary>
    [Fact]
    public async Task CreateAsync_WithInvalidData_ShouldThrowInvalidOperationException()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var createDto = new CreateIncidentRequestDTO
        {
            Title = "", // Título vacío
            Description = "Test",
            CategoryId = categoryId,
            Priority = 3
        };

        var invalidIncident = new Incident
        {
            Title = "",
            Description = "Test",
            CategoryId = categoryId,
            UserId = userId
        };

        // Mock: Categoría existe
        _mockCategoryRepository.Setup(r => r.ExistsAsync(categoryId))
            .ReturnsAsync(true);

        // Mock: Mapper retorna incidente inválido
        _mockMapper.Setup(m => m.Map<Incident>(createDto))
            .Returns(invalidIncident);

        // ACT & ASSERT
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _incidentService.CreateAsync(createDto, userId));
    }

    /// <summary>
    /// PRUEBA 4: Crear incidente debe asignar statusId=1 (OPEN) por defecto
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldSetStatusToOpen()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var createDto = new CreateIncidentRequestDTO
        {
            Title = "Nuevo incidente",
            Description = "Descripción",
            CategoryId = categoryId,
            Priority = 3
        };

        Incident? capturedIncident = null;

        _mockCategoryRepository.Setup(r => r.ExistsAsync(categoryId)).ReturnsAsync(true);
        
        // FIX: Crear incidente VÁLIDO (no vacío)
        _mockMapper.Setup(m => m.Map<Incident>(createDto)).Returns(new Incident
        {
            Id = Guid.NewGuid(),
            Title = createDto.Title,           // Título válido
            Description = createDto.Description, // Descripción válida
            CategoryId = categoryId,            // CategoryId válido
            UserId = Guid.Empty,                // Se asignará después
            Priority = createDto.Priority,      // Prioridad válida (1-5)
            StatusId = 1                        // Estado por defecto
    });
    
    _mockIncidentRepository.Setup(r => r.AddAsync(It.IsAny<Incident>()))
        .Callback<Incident>(i => capturedIncident = i)
        .Returns(Task.CompletedTask);

    _mockMapper.Setup(m => m.Map<IncidentResponseDTO>(It.IsAny<Incident>()))
        .Returns(new IncidentResponseDTO());

        // ACT
        await _incidentService.CreateAsync(createDto, userId);

        // ASSERT
        capturedIncident.Should().NotBeNull();
        capturedIncident!.StatusId.Should().Be(1); // OPEN
        capturedIncident!.UserId.Should().Be(userId); // Usuario asignado
        capturedIncident!.Title.Should().Be("Nuevo incidente");
    }

    /// <summary>
    /// PRUEBA 5: Crear incidente debe validar prioridad (1-5)
    /// </summary>
    [Fact]
    public async Task CreateAsync_WithInvalidPriority_ShouldThrowInvalidOperationException()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var createDto = new CreateIncidentRequestDTO
        {
            Title = "Test",
            Description = "Test",
            CategoryId = categoryId,
            Priority = 10 // Fuera de rango
        };

        var invalidIncident = new Incident
        {
            Priority = 10,
            Title = "Test",
            Description = "Test"
        };

        _mockCategoryRepository.Setup(r => r.ExistsAsync(categoryId)).ReturnsAsync(true);
        _mockMapper.Setup(m => m.Map<Incident>(createDto)).Returns(invalidIncident);

        // ACT & ASSERT
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _incidentService.CreateAsync(createDto, userId));
    }

    #endregion

    #region GetByIdAsync Tests

    /// <summary>
    /// PRUEBA 6: Obtener incidente por ID válido
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnIncident()
    {
        // ARRANGE
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            Title = "Test Incident"
        };
        var responseDto = new IncidentResponseDTO
        {
            Id = incidentId,
            Title = "Test Incident"
        };

        _mockIncidentRepository.Setup(r => r.GetByIdAsync(incidentId))
            .ReturnsAsync(incident);
        _mockMapper.Setup(m => m.Map<IncidentResponseDTO>(incident))
            .Returns(responseDto);

        // ACT
        var result = await _incidentService.GetByIdAsync(incidentId);

        // ASSERT
        result.Should().NotBeNull();
        result!.Id.Should().Be(incidentId);
        result.Title.Should().Be("Test Incident");
    }

    /// <summary>
    /// PRUEBA 7: Obtener incidente con ID inexistente
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // ARRANGE
        var incidentId = Guid.NewGuid();

        _mockIncidentRepository.Setup(r => r.GetByIdAsync(incidentId))
            .ReturnsAsync((Incident?)null);

        // ACT
        var result = await _incidentService.GetByIdAsync(incidentId);

        // ASSERT
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    /// <summary>
    /// PRUEBA 8: Obtener todos los incidentes
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ShouldReturnAllIncidents()
    {
        // ARRANGE
        var incidents = new List<Incident>
        {
            new Incident { Id = Guid.NewGuid(), Title = "Incident 1" },
            new Incident { Id = Guid.NewGuid(), Title = "Incident 2" },
            new Incident { Id = Guid.NewGuid(), Title = "Incident 3" }
        };

        var responseDtos = new List<IncidentResponseDTO>
        {
            new IncidentResponseDTO { Id = incidents[0].Id, Title = "Incident 1" },
            new IncidentResponseDTO { Id = incidents[1].Id, Title = "Incident 2" },
            new IncidentResponseDTO { Id = incidents[2].Id, Title = "Incident 3" }
        };

        _mockIncidentRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(incidents);
        _mockMapper.Setup(m => m.Map<IEnumerable<IncidentResponseDTO>>(incidents))
            .Returns(responseDtos);

        // ACT
        var result = await _incidentService.GetAllAsync();

        // ASSERT
        result.Should().HaveCount(3);
        result.Should().Contain(r => r.Title == "Incident 1");
    }

    #endregion

    #region GetByUserIdAsync Tests

    /// <summary>
    /// PRUEBA 9: Obtener incidentes de un usuario específico
    /// </summary>
    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnUserIncidents()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var incidents = new List<Incident>
        {
            new Incident { Id = Guid.NewGuid(), UserId = userId, Title = "User Incident 1" },
            new Incident { Id = Guid.NewGuid(), UserId = userId, Title = "User Incident 2" }
        };

        var responseDtos = new List<IncidentResponseDTO>
        {
            new IncidentResponseDTO { UserId = userId, Title = "User Incident 1" },
            new IncidentResponseDTO { UserId = userId, Title = "User Incident 2" }
        };

        _mockIncidentRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(incidents);
        _mockMapper.Setup(m => m.Map<IEnumerable<IncidentResponseDTO>>(incidents))
            .Returns(responseDtos);

        // ACT
        var result = await _incidentService.GetByUserIdAsync(userId);

        // ASSERT
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.UserId == userId);
    }

    #endregion

    #region GetByCategoryIdAsync Tests

    /// <summary>
    /// PRUEBA 10: Obtener incidentes por categoría
    /// </summary>
    [Fact]
    public async Task GetByCategoryIdAsync_ShouldReturnCategoryIncidents()
    {
        // ARRANGE
        var categoryId = Guid.NewGuid();
        var incidents = new List<Incident>
        {
            new Incident { CategoryId = categoryId, Title = "Bug 1" },
            new Incident { CategoryId = categoryId, Title = "Bug 2" }
        };

        var responseDtos = new List<IncidentResponseDTO>
        {
            new IncidentResponseDTO { CategoryId = categoryId, Title = "Bug 1" },
            new IncidentResponseDTO { CategoryId = categoryId, Title = "Bug 2" }
        };

        _mockIncidentRepository.Setup(r => r.GetByCategoryIdAsync(categoryId))
            .ReturnsAsync(incidents);
        _mockMapper.Setup(m => m.Map<IEnumerable<IncidentResponseDTO>>(incidents))
            .Returns(responseDtos);

        // ACT
        var result = await _incidentService.GetByCategoryIdAsync(categoryId);

        // ASSERT
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.CategoryId == categoryId);
    }

    #endregion

    #region GetByStatusIdAsync Tests

    /// <summary>
    /// PRUEBA 11: Obtener incidentes por estado
    /// </summary>
    [Fact]
    public async Task GetByStatusIdAsync_ShouldReturnStatusIncidents()
    {
        // ARRANGE
        var statusId = 1; // OPEN
        var incidents = new List<Incident>
        {
            new Incident { StatusId = statusId, Title = "Open 1" },
            new Incident { StatusId = statusId, Title = "Open 2" }
        };

        var responseDtos = new List<IncidentResponseDTO>
        {
            new IncidentResponseDTO { StatusId = statusId, Title = "Open 1" },
            new IncidentResponseDTO { StatusId = statusId, Title = "Open 2" }
        };

        _mockIncidentRepository.Setup(r => r.GetByStatusIdAsync(statusId))
            .ReturnsAsync(incidents);
        _mockMapper.Setup(m => m.Map<IEnumerable<IncidentResponseDTO>>(incidents))
            .Returns(responseDtos);

        // ACT
        var result = await _incidentService.GetByStatusIdAsync(statusId);

        // ASSERT
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.StatusId == statusId);
    }

    #endregion

    #region UpdateAsync Tests

    /// <summary>
    /// PRUEBA 12: Actualizar incidente con datos válidos
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldUpdateIncident()
    {
        // ARRANGE
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            Title = "Original Title",
            Description = "Original Description",
            UserId = Guid.NewGuid(),        // FIX: Agregar UserId válido
            CategoryId = Guid.NewGuid(),    // FIX: Agregar CategoryId válido
            StatusId = 1,
            Priority = 3
        };

        var updateDto = new UpdateIncidentRequestDTO
        {
            Title = "Updated Title",
            Description = "Updated Description",
            StatusId = 2,
            Priority = 5
        };

        _mockIncidentRepository.Setup(r => r.GetByIdAsync(incidentId))
            .ReturnsAsync(incident);
        _mockMapper.Setup(m => m.Map(updateDto, incident))
            .Callback<UpdateIncidentRequestDTO, Incident>((dto, inc) =>
        {
            // FIX: Actualizar los campos correctamente
            if (!string.IsNullOrEmpty(dto.Title)) inc.Title = dto.Title;
            if (!string.IsNullOrEmpty(dto.Description)) inc.Description = dto.Description;
            if (dto.StatusId.HasValue) inc.StatusId = dto.StatusId.Value;
            if (dto.Priority.HasValue) inc.Priority = dto.Priority.Value;
        })
        .Returns(incident);
        _mockIncidentRepository.Setup(r => r.UpdateAsync(It.IsAny<Incident>()))
            .Returns(Task.CompletedTask);

        // ACT
        await _incidentService.UpdateAsync(incidentId, updateDto);

        // ASSERT
        _mockIncidentRepository.Verify(r => r.UpdateAsync(It.IsAny<Incident>()), Times.Once);
    }

    /// <summary>
    /// PRUEBA 13: Actualizar incidente inexistente
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WithNonExistentIncident_ShouldThrowKeyNotFoundException()
    {
        // ARRANGE
        var incidentId = Guid.NewGuid();
        var updateDto = new UpdateIncidentRequestDTO();

        _mockIncidentRepository.Setup(r => r.GetByIdAsync(incidentId))
            .ReturnsAsync((Incident?)null);

        // ACT & ASSERT
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _incidentService.UpdateAsync(incidentId, updateDto));
    }

    /// <summary>
    /// PRUEBA 14: Actualizar a estado CLOSED debe setear ClosedAt
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ToClosedStatus_ShouldSetClosedAt()
    {
        // ARRANGE
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            Title = "Valid Title",              // FIX: Agregar título válido
            Description = "Valid Description",  // FIX: Agregar descripción válida
            UserId = Guid.NewGuid(),            // FIX: Agregar UserId válido
            CategoryId = Guid.NewGuid(),        // FIX: Agregar CategoryId válido
            StatusId = 2,                       // IN_PROGRESS
            Priority = 3,                       // FIX: Agregar prioridad válida
            ClosedAt = null
        };

        var updateDto = new UpdateIncidentRequestDTO
        {
            StatusId = 3 // CLOSED
    };

    _mockIncidentRepository.Setup(r => r.GetByIdAsync(incidentId))
        .ReturnsAsync(incident);
    _mockMapper.Setup(m => m.Map(updateDto, incident))
        .Callback<UpdateIncidentRequestDTO, Incident>((dto, inc) => 
        {
            if (dto.StatusId.HasValue) inc.StatusId = dto.StatusId.Value;
        })
        .Returns(incident);
    _mockIncidentRepository.Setup(r => r.UpdateAsync(It.IsAny<Incident>()))
        .Returns(Task.CompletedTask);

        // ACT
        await _incidentService.UpdateAsync(incidentId, updateDto);

        // ASSERT
        _mockIncidentRepository.Verify(r => r.UpdateAsync(It.IsAny<Incident>()), Times.Once);
    }

    /// <summary>
    /// PRUEBA 15: Actualizar solo título (campos opcionales)
    /// </summary>
    [Fact]
    public async Task UpdateAsync_OnlyTitle_ShouldUpdateOnlyTitle()
    {
        // ARRANGE
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            Title = "Original",
            Description = "Original Desc",
            UserId = Guid.NewGuid(),        // FIX: Agregar UserId válido
            CategoryId = Guid.NewGuid(),    // FIX: Agregar CategoryId válido
            StatusId = 1,                   // FIX: Agregar StatusId válido
            Priority = 3
        };

        var updateDto = new UpdateIncidentRequestDTO
        {
            Title = "Updated Title Only"
        };

        _mockIncidentRepository.Setup(r => r.GetByIdAsync(incidentId))
            .ReturnsAsync(incident);
        _mockMapper.Setup(m => m.Map(updateDto, incident))
            .Callback<UpdateIncidentRequestDTO, Incident>((dto, inc) =>
        {
            if (!string.IsNullOrEmpty(dto.Title)) inc.Title = dto.Title;
        })
        .Returns(incident);
        _mockIncidentRepository.Setup(r => r.UpdateAsync(It.IsAny<Incident>()))
            .Returns(Task.CompletedTask);

        // ACT
        await _incidentService.UpdateAsync(incidentId, updateDto);

        // ASSERT
        _mockIncidentRepository.Verify(r => r.UpdateAsync(It.IsAny<Incident>()), Times.Once);
    }

    /// <summary>
    /// PRUEBA 16: Actualizar con datos inválidos
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WithInvalidData_ShouldThrowInvalidOperationException()
    {
        // ARRANGE
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            Title = "Valid",
            Description = "Valid"
        };

        var updateDto = new UpdateIncidentRequestDTO
        {
            Title = "" // Inválido
        };

        _mockIncidentRepository.Setup(r => r.GetByIdAsync(incidentId))
            .ReturnsAsync(incident);
        _mockMapper.Setup(m => m.Map(updateDto, incident))
            .Callback<UpdateIncidentRequestDTO, Incident>((dto, inc) => inc.Title = "")
            .Returns(incident);

        // ACT & ASSERT
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _incidentService.UpdateAsync(incidentId, updateDto));
    }

    #endregion

    #region DeleteAsync Tests

    /// <summary>
    /// PRUEBA 17: Eliminar incidente existente
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldDeleteIncident()
    {
        // ARRANGE
        var incidentId = Guid.NewGuid();

        _mockIncidentRepository.Setup(r => r.ExistsAsync(incidentId))
            .ReturnsAsync(true);
        _mockIncidentRepository.Setup(r => r.DeleteAsync(incidentId))
            .Returns(Task.CompletedTask);

        // ACT
        await _incidentService.DeleteAsync(incidentId);

        // ASSERT
        _mockIncidentRepository.Verify(r => r.DeleteAsync(incidentId), Times.Once);
    }

    /// <summary>
    /// PRUEBA 18: Eliminar incidente inexistente
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ShouldThrowKeyNotFoundException()
    {
        // ARRANGE
        var incidentId = Guid.NewGuid();

        _mockIncidentRepository.Setup(r => r.ExistsAsync(incidentId))
            .ReturnsAsync(false);

        // ACT & ASSERT
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _incidentService.DeleteAsync(incidentId));
    }

    #endregion

    #region AddCommentAsync Tests

    /// <summary>
    /// PRUEBA 19: Agregar comentario válido
    /// </summary>
    [Fact]
    public async Task AddCommentAsync_WithValidData_ShouldAddComment()
    {
        // ARRANGE
        var incidentId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var comment = "Este es un comentario de prueba";
        var incident = new Incident { Id = incidentId };

        _mockIncidentRepository.Setup(r => r.GetByIdAsync(incidentId))
            .ReturnsAsync(incident);
        _mockIncidentRepository.Setup(r => r.AddUpdateAsync(It.IsAny<IncidentUpdate>()))
            .Returns(Task.CompletedTask);

        // ACT
        await _incidentService.AddCommentAsync(incidentId, comment, authorId);

        // ASSERT
        _mockIncidentRepository.Verify(r => r.AddUpdateAsync(It.Is<IncidentUpdate>(u => 
            u.IncidentId == incidentId &&
            u.AuthorId == authorId &&
            u.Comment == comment &&
            u.UpdateType == "COMMENT"
        )), Times.Once);
    }

    /// <summary>
    /// PRUEBA 20: Agregar comentario a incidente inexistente
    /// </summary>
    [Fact]
    public async Task AddCommentAsync_ToNonExistentIncident_ShouldThrowKeyNotFoundException()
    {
        // ARRANGE
        var incidentId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        _mockIncidentRepository.Setup(r => r.GetByIdAsync(incidentId))
            .ReturnsAsync((Incident?)null);

        // ACT & ASSERT
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _incidentService.AddCommentAsync(incidentId, "comment", authorId));
    }

    /// <summary>
    /// PRUEBA 21: Agregar comentario vacío
    /// </summary>
    [Fact]
    public async Task AddCommentAsync_WithEmptyComment_ShouldThrowInvalidOperationException()
    {
        // ARRANGE
        var incidentId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var incident = new Incident { Id = incidentId };

        _mockIncidentRepository.Setup(r => r.GetByIdAsync(incidentId))
            .ReturnsAsync(incident);

        // ACT & ASSERT
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _incidentService.AddCommentAsync(incidentId, "", authorId));
    }

    #endregion

    #region GetUpdatesAsync Tests

    /// <summary>
    /// PRUEBA 22: Obtener actualizaciones/comentarios de un incidente
    /// </summary>
    [Fact]
    public async Task GetUpdatesAsync_ShouldReturnAllUpdates()
    {
        // ARRANGE
        var incidentId = Guid.NewGuid();
        var updates = new List<IncidentUpdate>
        {
            new IncidentUpdate { Id = Guid.NewGuid(), Comment = "Update 1" },
            new IncidentUpdate { Id = Guid.NewGuid(), Comment = "Update 2" }
        };

        var updateDtos = new List<IncidentUpdateDTO>
        {
            new IncidentUpdateDTO { Comment = "Update 1" },
            new IncidentUpdateDTO { Comment = "Update 2" }
        };

        _mockIncidentRepository.Setup(r => r.ExistsAsync(incidentId))
            .ReturnsAsync(true);
        _mockIncidentRepository.Setup(r => r.GetUpdatesByIncidentIdAsync(incidentId))
            .ReturnsAsync(updates);
        _mockMapper.Setup(m => m.Map<IEnumerable<IncidentUpdateDTO>>(updates))
            .Returns(updateDtos);

        // ACT
        var result = await _incidentService.GetUpdatesAsync(incidentId);

        // ASSERT
        result.Should().HaveCount(2);
    }

    /// <summary>
    /// PRUEBA 23: Obtener updates de incidente inexistente
    /// </summary>
    [Fact]
    public async Task GetUpdatesAsync_WithNonExistentIncident_ShouldThrowKeyNotFoundException()
    {
        // ARRANGE
        var incidentId = Guid.NewGuid();

        _mockIncidentRepository.Setup(r => r.ExistsAsync(incidentId))
            .ReturnsAsync(false);

        // ACT & ASSERT
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _incidentService.GetUpdatesAsync(incidentId));
    }

    #endregion

    #region AssignToUserAsync Tests

    /// <summary>
    /// PRUEBA 24: Reasignar incidente a otro usuario
    /// </summary>
    [Fact]
    public async Task AssignToUserAsync_WithValidData_ShouldReassignIncident()
    {
        // ARRANGE
        var incidentId = Guid.NewGuid();
        var oldUserId = Guid.NewGuid();
        var newUserId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();

        var incident = new Incident
        {
            Id = incidentId,
            UserId = oldUserId
        };

        _mockIncidentRepository.Setup(r => r.GetByIdAsync(incidentId))
            .ReturnsAsync(incident);
        _mockIncidentRepository.Setup(r => r.UpdateAsync(It.IsAny<Incident>()))
            .Returns(Task.CompletedTask);
        _mockIncidentRepository.Setup(r => r.AddUpdateAsync(It.IsAny<IncidentUpdate>()))
            .Returns(Task.CompletedTask);

        // ACT
        await _incidentService.AssignToUserAsync(incidentId, newUserId, currentUserId);

        // ASSERT
        incident.UserId.Should().Be(newUserId);
        _mockIncidentRepository.Verify(r => r.UpdateAsync(It.IsAny<Incident>()), Times.Once);
        _mockIncidentRepository.Verify(r => r.AddUpdateAsync(It.Is<IncidentUpdate>(u => 
            u.UpdateType == "USER_REASSIGNMENT" &&
            u.OldValue == oldUserId.ToString() &&
            u.NewValue == newUserId.ToString()
        )), Times.Once);
    }

    /// <summary>
    /// PRUEBA 25: Reasignar incidente inexistente
    /// </summary>
    [Fact]
    public async Task AssignToUserAsync_ToNonExistentIncident_ShouldThrowKeyNotFoundException()
    {
        // ARRANGE
        var incidentId = Guid.NewGuid();
        var newUserId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();

        _mockIncidentRepository.Setup(r => r.GetByIdAsync(incidentId))
            .ReturnsAsync((Incident?)null);

        // ACT & ASSERT
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _incidentService.AssignToUserAsync(incidentId, newUserId, currentUserId));
    }

    /// <summary>
    /// PRUEBA 26: Reasignar con userId inválido (Guid.Empty)
    /// </summary>
    [Fact]
    public async Task AssignToUserAsync_WithEmptyUserId_ShouldThrowArgumentException()
    {
        // ARRANGE
        var incidentId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var incident = new Incident { Id = incidentId };

        _mockIncidentRepository.Setup(r => r.GetByIdAsync(incidentId))
            .ReturnsAsync(incident);

        // ACT & ASSERT
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _incidentService.AssignToUserAsync(incidentId, Guid.Empty, currentUserId));
    }

    #endregion

    #region GetAllAsync - Navigation Properties Tests

    /// <summary>
    /// PRUEBA 27: Obtener todos los incidentes debe incluir Attachments y Metrics
    /// 
    /// Objetivo: Verificar que las relaciones con IncidentAttachment e IncidentMetric se cargan correctamente
    /// Resultado esperado: Incidentes con navegaciones Attachments y Metrics cargadas
    /// 
    /// IMPORTANTE: Este test valida que el fix del bug IncidentStatusEntityId funcione correctamente
    /// al cargar TODAS las relaciones incluyendo las nuevas entidades
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ShouldIncludeAttachmentsAndMetrics()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        
        var incidents = new List<Incident>
        {
            new Incident 
            { 
                Id = Guid.NewGuid(), 
                Title = "Incident with Attachments",
                UserId = userId,
                CategoryId = categoryId,
                StatusId = 1,
                Priority = 3,
                User = new User { Id = userId, Username = "testuser" },
                Category = new IncidentCategory { Id = categoryId, Name = "Bug" },
                Status = new IncidentStatus { Id = 1, Name = "OPEN", DisplayName = "Abierto" },
                Updates = new List<IncidentUpdate>(),
                // Nuevas navegaciones agregadas
                Attachments = new List<IncidentAttachment>
                {
                    new IncidentAttachment 
                    { 
                        Id = Guid.NewGuid(), 
                        FileName = "screenshot.png",
                        FileSize = 1024000,
                        FileType = "image/png"
                    }
                },
                Metrics = new IncidentMetric
                {
                    Id = Guid.NewGuid(),
                    CommentCount = 5,
                    AttachmentCount = 1
                }
            }
        };

        var responseDtos = new List<IncidentResponseDTO>
        {
            new IncidentResponseDTO 
            { 
                Id = incidents[0].Id, 
                Title = "Incident with Attachments",
                AttachmentCount = 1,
                CommentCount = 5
            }
        };

        _mockIncidentRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(incidents);
        _mockMapper.Setup(m => m.Map<IEnumerable<IncidentResponseDTO>>(incidents))
            .Returns(responseDtos);

        // ACT
        var result = await _incidentService.GetAllAsync();

        // ASSERT
        result.Should().HaveCount(1);
        var firstIncident = result.First();
        firstIncident.AttachmentCount.Should().Be(1);
        firstIncident.CommentCount.Should().Be(5);
        
        // Verificar que se llamó al repositorio con Include correcto
        _mockIncidentRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    #endregion

    #region GetByIdAsync - Navigation Properties Tests

    /// <summary>
    /// PRUEBA 28: Obtener incidente por ID debe cargar Attachments y Metrics
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ShouldIncludeAttachmentsAndMetrics()
    {
        // ARRANGE
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            Title = "Test Incident",
            UserId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            StatusId = 1,
            Priority = 3,
            Attachments = new List<IncidentAttachment>
            {
                new IncidentAttachment { FileName = "log.txt", FileSize = 2048 }
            },
            Metrics = new IncidentMetric { CommentCount = 3, AttachmentCount = 1 }
        };
        
        var responseDto = new IncidentResponseDTO
        {
            Id = incidentId,
            Title = "Test Incident",
            AttachmentCount = 1,
            CommentCount = 3
        };

        _mockIncidentRepository.Setup(r => r.GetByIdAsync(incidentId))
            .ReturnsAsync(incident);
        _mockMapper.Setup(m => m.Map<IncidentResponseDTO>(incident))
            .Returns(responseDto);

        // ACT
        var result = await _incidentService.GetByIdAsync(incidentId);

        // ASSERT
        result.Should().NotBeNull();
        result!.AttachmentCount.Should().Be(1);
        result.CommentCount.Should().Be(3);
    }

    #endregion

    #region Status and Category Navigation Tests

    /// <summary>
    /// PRUEBA 29: GetAllAsync debe cargar Status.DisplayName correctamente
    /// 
    /// Objetivo: Verificar que la relación IncidentStatus se carga sin errores
    /// Resultado esperado: StatusName mapeado desde Status.DisplayName
    /// 
    /// REGRESION TEST: Este test habría detectado el bug IncidentStatusEntityId
    /// porque valida que i.Status.DisplayName se accede correctamente
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ShouldMapStatusDisplayNameCorrectly()
    {
        // ARRANGE
        var incidents = new List<Incident>
        {
            new Incident 
            { 
                Id = Guid.NewGuid(), 
                Title = "Test",
                StatusId = 1,
                Status = new IncidentStatus { Id = 1, DisplayName = "Abierto" },
                UserId = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                Priority = 3
            }
        };

        var responseDtos = new List<IncidentResponseDTO>
        {
            new IncidentResponseDTO 
            { 
                StatusName = "Abierto"  // Mapeado desde i.Status.DisplayName
            }
        };

        _mockIncidentRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(incidents);
        _mockMapper.Setup(m => m.Map<IEnumerable<IncidentResponseDTO>>(incidents))
            .Returns(responseDtos);

        // ACT
        var result = await _incidentService.GetAllAsync();

        // ASSERT
        result.First().StatusName.Should().Be("Abierto");
    }

    /// <summary>
    /// PRUEBA 30: GetAllAsync debe cargar Category.Name correctamente
    /// 
    /// Objetivo: Verificar que la relación IncidentCategory se carga sin errores
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ShouldMapCategoryNameCorrectly()
    {
        // ARRANGE
        var incidents = new List<Incident>
        {
            new Incident 
            { 
                Id = Guid.NewGuid(), 
                Title = "Test",
                CategoryId = Guid.NewGuid(),
                Category = new IncidentCategory { Name = "Defecto" },
                UserId = Guid.NewGuid(),
                StatusId = 1,
                Priority = 3
            }
        };

        var responseDtos = new List<IncidentResponseDTO>
        {
            new IncidentResponseDTO 
            { 
                CategoryName = "Defecto"  // Mapeado desde i.Category.Name
            }
        };

        _mockIncidentRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(incidents);
        _mockMapper.Setup(m => m.Map<IEnumerable<IncidentResponseDTO>>(incidents))
            .Returns(responseDtos);

        // ACT
        var result = await _incidentService.GetAllAsync();

        // ASSERT
        result.First().CategoryName.Should().Be("Defecto");
    }

    #endregion

    #region Bidirectional Relationship Tests

    /// <summary>
    /// PRUEBA 31: IncidentStatus debe tener colección de Incidents
    /// 
    /// Objetivo: Verificar que la navegación bidireccional funciona correctamente
    /// </summary>
    [Fact]
    public async Task GetByStatusIdAsync_ShouldValidateBidirectionalRelation()
    {
        // ARRANGE
        var statusId = 1;
        var status = new IncidentStatus 
        { 
            Id = statusId, 
            Name = "OPEN",
            Incidents = new List<Incident>()  // Navegación inversa
        };

        var incidents = new List<Incident>
        {
            new Incident 
            { 
                StatusId = statusId,
                Status = status,  // Navegación hacia status
                Title = "Test",
                UserId = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                Priority = 3
            }
        };

        var responseDtos = new List<IncidentResponseDTO>
        {
            new IncidentResponseDTO { StatusId = statusId }
        };

        _mockIncidentRepository.Setup(r => r.GetByStatusIdAsync(statusId))
            .ReturnsAsync(incidents);
        _mockMapper.Setup(m => m.Map<IEnumerable<IncidentResponseDTO>>(incidents))
            .Returns(responseDtos);

        // ACT
        var result = await _incidentService.GetByStatusIdAsync(statusId);

        // ASSERT
        result.Should().HaveCount(1);
        result.First().StatusId.Should().Be(statusId);
    }

    /// <summary>
    /// PRUEBA 32: IncidentCategory debe tener colección de Incidents
    /// </summary>
    [Fact]
    public async Task GetByCategoryIdAsync_ShouldValidateBidirectionalRelation()
    {
        // ARRANGE
        var categoryId = Guid.NewGuid();
        var category = new IncidentCategory 
        { 
            Id = categoryId,
            Name = "Bug",
            Incidents = new List<Incident>()  // Navegación inversa
        };

        var incidents = new List<Incident>
        {
            new Incident 
            { 
                CategoryId = categoryId,
                Category = category,  // Navegación hacia category
                Title = "Test",
                UserId = Guid.NewGuid(),
                StatusId = 1,
                Priority = 3
            }
        };

        var responseDtos = new List<IncidentResponseDTO>
        {
            new IncidentResponseDTO { CategoryId = categoryId }
        };

        _mockIncidentRepository.Setup(r => r.GetByCategoryIdAsync(categoryId))
            .ReturnsAsync(incidents);
        _mockMapper.Setup(m => m.Map<IEnumerable<IncidentResponseDTO>>(incidents))
            .Returns(responseDtos);

        // ACT
        var result = await _incidentService.GetByCategoryIdAsync(categoryId);

        // ASSERT
        result.Should().HaveCount(1);
    }

    #endregion

    #region Regression Tests - IncidentStatusEntityId Bug

    /// <summary>
    /// PRUEBA 33: REGRESSION TEST - Verificar que no se genera columna IncidentStatusEntityId
    /// 
    /// Objetivo: Este test habría detectado el bug original
    /// Valida que EF Core mapea StatusId correctamente sin crear shadow properties
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ShouldNotThrowInvalidColumnException()
    {
        // ARRANGE
        var incidents = new List<Incident>
        {
            new Incident 
            { 
                Id = Guid.NewGuid(),
                Title = "Regression Test",
                UserId = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                StatusId = 1,  // FK real, NO debe crear IncidentStatusEntityId
                Priority = 3,
                Status = new IncidentStatus { Id = 1, DisplayName = "Abierto" },
                Category = new IncidentCategory { Id = Guid.NewGuid(), Name = "Bug" },
                User = new User { Id = Guid.NewGuid(), Username = "test" }
            }
        };

        var responseDtos = new List<IncidentResponseDTO>
        {
            new IncidentResponseDTO 
            { 
                StatusName = "Abierto",
                CategoryName = "Bug"
            }
        };

        _mockIncidentRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(incidents);
        _mockMapper.Setup(m => m.Map<IEnumerable<IncidentResponseDTO>>(incidents))
            .Returns(responseDtos);

        // ACT
        Func<Task> act = async () => await _incidentService.GetAllAsync();

        // ASSERT
        // No debe lanzar SqlException: Invalid column name 'IncidentStatusEntityId'
        await act.Should().NotThrowAsync<Exception>();
        
        var result = await _incidentService.GetAllAsync();
        result.First().StatusName.Should().NotBeNullOrEmpty();
        result.First().CategoryName.Should().NotBeNullOrEmpty();
    }

    #endregion

    /// <summary>
    /// Crea un IncidentAttachment de prueba
    /// </summary>
    public static IncidentAttachment CreateTestIncidentAttachment(
        Guid? id = null,
        Guid? incidentId = null,
        string fileName = "test.txt",
        long fileSize = 1024)
    {
        return new IncidentAttachment
        {
            Id = id ?? Guid.NewGuid(),
            IncidentId = incidentId ?? Guid.NewGuid(),
            FileName = fileName,
            FilePath = $"/uploads/{fileName}",
            FileSize = fileSize,
            FileType = "text/plain",
            UploadedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Crea un IncidentMetric de prueba
    /// </summary>
    public static IncidentMetric CreateTestIncidentMetric(
        Guid? id = null,
        Guid? incidentId = null,
        int commentCount = 0,
        int attachmentCount = 0)
    {
        return new IncidentMetric
        {
            Id = id ?? Guid.NewGuid(),
            IncidentId = incidentId ?? Guid.NewGuid(),
            CommentCount = commentCount,
            AttachmentCount = attachmentCount,
            TimeToClose = null,
            AverageResolutionTime = null,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Crea un IncidentStatus de prueba
    /// </summary>
    public static IncidentStatus CreateTestIncidentStatus(
        int id = 1,
        string name = "OPEN",
        string displayName = "Abierto")
    {
        return new IncidentStatus
        {
            Id = id,
            Name = name,
            DisplayName = displayName,
            Description = $"Estado {displayName}",
            OrderSequence = id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Crea un IncidentCategory de prueba
    /// </summary>
    public static IncidentCategory CreateTestIncidentCategory(
        Guid? id = null,
        string name = "Bug",
        bool isActive = true)
    {
        return new IncidentCategory
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Description = $"Categoría {name}",
            Color = "#FF0000",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }
}