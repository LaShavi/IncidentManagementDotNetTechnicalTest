using Application.Services;
using Domain.Entities;
using Application.Ports;
using Tests.Fixtures;
using Microsoft.Extensions.Logging;

namespace Tests.Application.Services;

/// <summary>
/// Test suite para ClienteService
/// Prueba la lógica de negocio de clientes
/// Utiliza Moq para simular el repositorio
/// </summary>
public class ClienteServiceTests
{
    private readonly Mock<IClienteRepository> _mockRepository;
    private readonly Mock<ILogger<ClienteService>> _mockLogger;
    private readonly IClienteService _service;

    /// <summary>
    /// Constructor: Inicializa los mocks y el servicio
    /// Se ejecuta ANTES de cada prueba [Fact]
    /// </summary>
    public ClienteServiceTests()
    {
        _mockRepository = new Mock<IClienteRepository>();
        _mockLogger = new Mock<ILogger<ClienteService>>();
        _service = new ClienteService(_mockRepository.Object, _mockLogger.Object);
    }

    /// <summary>
    /// ? PRUEBA 1: Obtener todos los clientes
    /// 
    /// Objetivo: Verificar que el servicio obtiene todos los clientes del repositorio
    /// Resultado esperado: Lista con 3 clientes
    /// 
    /// Escenario real:
    /// Admin solicita lista completa de clientes
    /// Servicio consulta el repositorio
    /// Servicio retorna la lista
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ShouldReturnAllClientes()
    {
        // Arrange: Preparar 3 clientes de prueba
        var expectedClientes = TestDataFixtures.CreateTestClientes(3);
        
        // Mock: Configurar que el repositorio retorne estos 3 clientes
        _mockRepository.Setup(repo => repo.GetAllAsync())
                      .ReturnsAsync(expectedClientes);

        // Act: Obtener todos los clientes
        var result = await _service.GetAllAsync();

        // Assert: Verificar que el servicio retornó los 3 clientes
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(expectedClientes);
        
        // Verificar que se llamó al repositorio exactamente 1 vez
        _mockRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    /// <summary>
    /// ? PRUEBA 2: Obtener cliente por ID existente
    /// 
    /// Objetivo: Verificar que el servicio obtiene un cliente específico
    /// Resultado esperado: Cliente encontrado con datos correctos
    /// 
    /// Escenario real:
    /// Usuario selecciona un cliente específico
    /// Servicio busca ese cliente por ID
    /// Servicio retorna los datos
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnCliente()
    {
        // Arrange: Preparar un cliente con ID específico
        var clienteId = Guid.NewGuid();
        var expectedCliente = TestDataFixtures.CreateTestCliente(id: clienteId);
        
        // Mock: Configurar que el repositorio retorne este cliente
        _mockRepository.Setup(repo => repo.GetByIdAsync(clienteId))
                      .ReturnsAsync(expectedCliente);

        // Act: Obtener el cliente por su ID
        var result = await _service.GetByIdAsync(clienteId);

        // Assert: Verificar que el servicio retornó el cliente correcto
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedCliente);
        
        // Verificar que se llamó al repositorio con el ID correcto
        _mockRepository.Verify(repo => repo.GetByIdAsync(clienteId), Times.Once);
    }

    /// <summary>
    /// ? PRUEBA 3: Obtener cliente por ID inexistente
    /// 
    /// Objetivo: Verificar que retorna null si el cliente no existe
    /// Resultado esperado: null
    /// 
    /// Escenario real:
    /// Usuario intenta acceder a cliente con ID inexistente
    /// Servicio busca en repositorio
    /// Repositorio no encuentra nada
    /// Servicio retorna null
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange: Preparar un ID que no existe
        var clienteId = Guid.NewGuid();
        
        // Mock: Configurar que el repositorio retorne null (no encontrado)
        _mockRepository.Setup(repo => repo.GetByIdAsync(clienteId))
                      .ReturnsAsync((Cliente?)null);

        // Act: Intentar obtener cliente inexistente
        var result = await _service.GetByIdAsync(clienteId);

        // Assert: Verificar que retorna null
        result.Should().BeNull();
        
        // Verificar que se intentó buscar
        _mockRepository.Verify(repo => repo.GetByIdAsync(clienteId), Times.Once);
    }

    /// <summary>
    /// ? PRUEBA 4: Agregar nuevo cliente
    /// 
    /// Objetivo: Verificar que el servicio delega a repositorio para agregar cliente
    /// Resultado esperado: Repositorio llamado exactamente 1 vez
    /// 
    /// Escenario real:
    /// Usuario completa formulario de nuevo cliente
    /// Sistema valida los datos
    /// Sistema llama al repositorio para guardar
    /// Repositorio persiste en BD
    /// </summary>
    [Fact]
    public async Task AddAsync_WithValidCliente_ShouldCallRepository()
    {
        // Arrange: Preparar un cliente válido
        var cliente = TestDataFixtures.CreateTestCliente();
        
        // Mock: Configurar que el repositorio acepta el cliente
        _mockRepository.Setup(repo => repo.AddAsync(It.IsAny<Cliente>()))
                      .Returns(Task.CompletedTask);

        // Act: Agregar el cliente
        await _service.AddAsync(cliente);

        // Assert: Verificar que se llamó al repositorio con este cliente
        _mockRepository.Verify(repo => repo.AddAsync(cliente), Times.Once);
    }

    /// <summary>
    /// ? PRUEBA 5: Actualizar cliente existente
    /// 
    /// Objetivo: Verificar que el servicio delega a repositorio para actualizar
    /// Resultado esperado: Repositorio llamado exactamente 1 vez
    /// 
    /// Escenario real:
    /// Usuario modifica datos del cliente
    /// Sistema valida los cambios
    /// Sistema llama al repositorio para guardar cambios
    /// Repositorio actualiza en BD
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WithValidCliente_ShouldCallRepository()
    {
        // Arrange: Preparar un cliente modificado
        var cliente = TestDataFixtures.CreateTestCliente();
        
        // Mock: Configurar que el repositorio acepta la actualización
        _mockRepository.Setup(repo => repo.UpdateAsync(It.IsAny<Cliente>()))
                      .Returns(Task.CompletedTask);

        // Act: Actualizar el cliente
        await _service.UpdateAsync(cliente);

        // Assert: Verificar que se llamó al repositorio con este cliente
        _mockRepository.Verify(repo => repo.UpdateAsync(cliente), Times.Once);
    }

    /// <summary>
    /// ? PRUEBA 6: Eliminar cliente
    /// 
    /// Objetivo: Verificar que el servicio delega a repositorio para eliminar
    /// Resultado esperado: Repositorio llamado exactamente 1 vez
    /// 
    /// Escenario real:
    /// Usuario solicita eliminar un cliente
    /// Sistema valida la solicitud
    /// Sistema llama al repositorio para eliminar
    /// Repositorio elimina de BD
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldCallRepository()
    {
        // Arrange: Preparar un ID para eliminar
        var clienteId = Guid.NewGuid();
        
        // Mock: Configurar que el repositorio puede eliminar
        _mockRepository.Setup(repo => repo.DeleteAsync(clienteId))
                      .Returns(Task.CompletedTask);

        // Act: Eliminar el cliente
        await _service.DeleteAsync(clienteId);

        // Assert: Verificar que se llamó al repositorio con este ID
        _mockRepository.Verify(repo => repo.DeleteAsync(clienteId), Times.Once);
    }
}