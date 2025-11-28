using Api.Controllers;
using Application.DTOs.Cliente;
using Application.Ports;
using AutoMapper;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Tests.Fixtures;

namespace Tests.Api.Controllers;

/// <summary>
/// Test suite para ClienteController
/// Prueba todos los endpoints HTTP para gestión de clientes
/// Utiliza Moq para simular el servicio y mapper
/// </summary>
public class ClienteControllerTests
{
    private readonly Mock<IClienteService> _mockClienteService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<ClienteController>> _mockLogger;
    private readonly ClienteController _controller;

    /// <summary>
    /// Constructor: Inicializa los mocks y el controller
    /// Se ejecuta ANTES de cada prueba [Fact]
    /// </summary>
    public ClienteControllerTests()
    {
        _mockClienteService = new Mock<IClienteService>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<ClienteController>>();
        
        _controller = new ClienteController(
            _mockClienteService.Object,
            _mockMapper.Object,
            _mockLogger.Object);

        // Setup HttpContext for the controller
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    /// <summary>
    /// ? PRUEBA 1: Obtener lista de todos los clientes
    /// 
    /// Objetivo: Verificar que el endpoint retorna HTTP 200 con lista de clientes
    /// Resultado esperado: OkObjectResult + Lista con 2 clientes
    /// 
    /// Escenario real:
    /// GET /api/clientes
    /// Sistema obtiene lista de la BD
    /// Sistema mapea a DTOs
    /// Sistema retorna HTTP 200 + JSON con clientes
    /// </summary>
    [Fact]
    public async Task GetAll_ShouldReturnOkWithClientes()
    {
        // Arrange: Preparar 2 clientes y sus DTOs
        var clientes = TestDataFixtures.CreateTestClientes(2);
        var clienteDtos = new List<ClienteResponseDTO>
        {
            new() { Id = clientes[0].Id, Cedula = clientes[0].Cedula, Email = clientes[0].Email, 
                    Telefono = clientes[0].Telefono, Nombre = clientes[0].Nombre, Apellido = clientes[0].Apellido },
            new() { Id = clientes[1].Id, Cedula = clientes[1].Cedula, Email = clientes[1].Email, 
                    Telefono = clientes[1].Telefono, Nombre = clientes[1].Nombre, Apellido = clientes[1].Apellido }
        };

        // Mock: Servicio retorna 2 clientes
        _mockClienteService.Setup(s => s.GetAllAsync())
                          .ReturnsAsync(clientes);
        
        // Mock: Mapper convierte a DTOs
        _mockMapper.Setup(m => m.Map<IEnumerable<ClienteResponseDTO>>(clientes))
                   .Returns(clienteDtos);

        // Act: Llamar GET /api/clientes
        var result = await _controller.GetAll();

        // Assert: Verificar HTTP 200 + datos
        var actionResult = result.Result;
        actionResult.Should().BeOfType<OkObjectResult>();
        
        var okResult = actionResult as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    /// <summary>
    /// ? PRUEBA 2: Obtener cliente por ID existente
    /// 
    /// Objetivo: Verificar que el endpoint retorna cliente específico
    /// Resultado esperado: HTTP 200 + Datos del cliente
    /// 
    /// Escenario real:
    /// GET /api/clientes/{id}
    /// Sistema busca cliente en BD
    /// Sistema mapea a DTO
    /// Sistema retorna HTTP 200 + datos del cliente
    /// </summary>
    [Fact]
    public async Task GetById_WithExistingId_ShouldReturnOkWithCliente()
    {
        // Arrange: Preparar un cliente y su DTO
        var cliente = TestDataFixtures.CreateTestCliente();
        var clienteDto = new ClienteResponseDTO 
        { 
            Id = cliente.Id, 
            Cedula = cliente.Cedula, 
            Email = cliente.Email, 
            Telefono = cliente.Telefono, 
            Nombre = cliente.Nombre, 
            Apellido = cliente.Apellido 
        };

        // Mock: Servicio retorna el cliente
        _mockClienteService.Setup(s => s.GetByIdAsync(cliente.Id))
                          .ReturnsAsync(cliente);
        
        // Mock: Mapper convierte a DTO
        _mockMapper.Setup(m => m.Map<ClienteResponseDTO>(cliente))
                   .Returns(clienteDto);

        // Act: Llamar GET /api/clientes/{id}
        var result = await _controller.GetById(cliente.Id);

        // Assert: Verificar HTTP 200
        var actionResult = result.Result;
        actionResult.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// ? PRUEBA 3: Obtener cliente por ID inexistente
    /// 
    /// Objetivo: Verificar que el endpoint rechaza con error si cliente no existe
    /// Resultado esperado: KeyNotFoundException
    /// 
    /// Escenario real:
    /// GET /api/clientes/{idInexistente}
    /// Sistema busca en BD
    /// Cliente no encontrado
    /// Sistema lanza excepción (es convertida a HTTP 404 por el middleware)
    /// </summary>
    [Fact]
    public async Task GetById_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange: Preparar un ID que no existe
        var nonExistingId = Guid.NewGuid();
        
        // Mock: Servicio retorna null (no encontrado)
        _mockClienteService.Setup(s => s.GetByIdAsync(nonExistingId))
                          .ReturnsAsync((Cliente?)null);

        // Act & Assert: Debe lanzar excepción KeyNotFoundException
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _controller.GetById(nonExistingId));
    }

    /// <summary>
    /// ? PRUEBA 4: Crear nuevo cliente
    /// 
    /// Objetivo: Verificar que se puede crear un nuevo cliente
    /// Resultado esperado: HTTP 200 + Cliente creado
    /// 
    /// Escenario real:
    /// POST /api/clientes
    /// Usuario envía datos del nuevo cliente
    /// Sistema valida los datos
    /// Sistema guarda en BD
    /// Sistema retorna HTTP 200
    /// </summary>
    [Fact]
    public async Task Create_WithValidDto_ShouldReturnCreated()
    {
        // Arrange: Preparar DTO con datos válidos
        var createDto = new CreateClienteDTO
        {
            Cedula = "12345678",
            Email = "test@test.com",
            Telefono = "555-1234",
            Nombre = "Juan",
            Apellido = "Perez"
        };

        // Mock: Servicio acepta el cliente
        _mockClienteService.Setup(s => s.AddAsync(It.IsAny<Cliente>()))
                          .Returns(Task.CompletedTask);

        // Act: Llamar POST /api/clientes
        var result = await _controller.Create(createDto);

        // Assert: Verificar HTTP 200 + que se guardó
        var actionResult = result.Result;
        actionResult.Should().BeOfType<OkObjectResult>();
        
        // Verificar que el servicio fue llamado
        _mockClienteService.Verify(s => s.AddAsync(It.IsAny<Cliente>()), Times.Once);
    }

    /// <summary>
    /// ? PRUEBA 5: Crear cliente con datos inválidos
    /// 
    /// Objetivo: Verificar que el endpoint rechaza datos incompletos
    /// Resultado esperado: HTTP 400 (Bad Request)
    /// 
    /// Escenario real:
    /// POST /api/clientes
    /// Usuario envía DTO vacío (sin llenar campos)
    /// Sistema valida ModelState
    /// Sistema retorna HTTP 400 (Bad Request)
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidModelState_ShouldReturnBadRequest()
    {
        // Arrange: Preparar DTO vacío (inválido)
        var createDto = new CreateClienteDTO(); // Empty DTO
        
        // Simular error de validación
        _controller.ModelState.AddModelError("Nombre", "Nombre es requerido");

        // Act: Llamar POST /api/clientes con DTO inválido
        var result = await _controller.Create(createDto);

        // Assert: Verificar HTTP 400
        var actionResult = result.Result;
        actionResult.Should().BeOfType<BadRequestObjectResult>();
    }
}