using Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging;
using Tests.Fixtures;

namespace Tests.Infrastructure.Repositories;

/// <summary>
/// Test suite para ClienteRepository
/// Prueba todas las operaciones CRUD de clientes en la base de datos
/// </summary>
public class ClienteRepositoryTests : TestBase
{
    private readonly ClienteRepository _repository;
    private readonly Mock<ILogger<ClienteRepository>> _mockLogger;

    /// <summary>
    /// Constructor: Inicializa el repositorio y el logger mock
    /// Se ejecuta ANTES de cada prueba [Fact]
    /// </summary>
    public ClienteRepositoryTests()
    {
        _mockLogger = new Mock<ILogger<ClienteRepository>>();
        _repository = new ClienteRepository(Context, _mockLogger.Object);
    }

    /// <summary>
    /// ? PRUEBA 1: Obtener lista de clientes vacía
    /// 
    /// Objetivo: Verificar que retorna lista vacía cuando no hay clientes
    /// Resultado esperado: Lista vacía (no null)
    /// 
    /// Escenario real:
    /// Sistema inicia sin datos (BD nueva)
    /// Admin solicita lista de clientes
    /// Sistema retorna lista vacía
    /// </summary>
    [Fact]
    public async Task GetAllAsync_WithNoData_ShouldReturnEmptyList()
    {
        // Act: Obtener todos los clientes (cuando no hay ninguno)
        var result = await _repository.GetAllAsync();

        // Assert: Verificar que retorna lista vacía (no null)
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>
    /// ? PRUEBA 2: Obtener todos los clientes
    /// 
    /// Objetivo: Verificar que retorna lista completa de clientes
    /// Resultado esperado: Lista con los 3 clientes guardados
    /// 
    /// Escenario real:
    /// Se guardaron 3 clientes en BD
    /// Admin solicita lista de clientes
    /// Sistema retorna lista completa
    /// </summary>
    [Fact]
    public async Task GetAllAsync_WithData_ShouldReturnAllClientes()
    {
        // Arrange: Crear y guardar 3 clientes
        var testClientes = TestDataFixtures.CreateTestClientes(3);
        
        foreach (var cliente in testClientes)
        {
            await _repository.AddAsync(cliente);
        }

        // Act: Obtener todos los clientes
        var result = await _repository.GetAllAsync();

        // Assert: Verificar que se obtuvieron los 3 clientes
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    /// <summary>
    /// ? PRUEBA 3: Obtener cliente por ID existente
    /// 
    /// Objetivo: Verificar que se recupera cliente específico por ID
    /// Resultado esperado: Cliente con todos sus datos
    /// 
    /// Escenario real:
    /// Usuario selecciona un cliente de la lista
    /// Sistema busca el cliente por su ID
    /// Sistema retorna todos los datos del cliente
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnCliente()
    {
        // Arrange: Crear y guardar un cliente
        var cliente = TestDataFixtures.CreateTestCliente();
        await _repository.AddAsync(cliente);

        // Act: Obtener el cliente por su ID
        var result = await _repository.GetByIdAsync(cliente.Id);

        // Assert: Verificar que se obtuvieron todos los datos correctamente
        result.Should().NotBeNull();
        result!.Id.Should().Be(cliente.Id);
        result.Cedula.Should().Be(cliente.Cedula);
        result.Email.Should().Be(cliente.Email);
        result.Nombre.Should().Be(cliente.Nombre);
        result.Apellido.Should().Be(cliente.Apellido);
    }

    /// <summary>
    /// ? PRUEBA 4: Obtener cliente por ID inexistente
    /// 
    /// Objetivo: Verificar que retorna null para ID que no existe
    /// Resultado esperado: null
    /// 
    /// Escenario real:
    /// Usuario intenta acceder a cliente que no existe (ID inválido)
    /// Sistema busca en BD
    /// Sistema no encuentra nada
    /// Sistema retorna null (cliente no encontrado)
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange: Preparar un ID que no existe
        var nonExistingId = Guid.NewGuid();

        // Act: Intentar obtener cliente con ID inexistente
        var result = await _repository.GetByIdAsync(nonExistingId);

        // Assert: Verificar que retorna null
        result.Should().BeNull();
    }

    /// <summary>
    /// ? PRUEBA 5: Agregar nuevo cliente a BD
    /// 
    /// Objetivo: Verificar que se guarda un nuevo cliente correctamente
    /// Resultado esperado: Cliente guardado con todos sus datos
    /// 
    /// Escenario real:
    /// Usuario completa formulario de nuevo cliente
    /// Sistema valida los datos
    /// Sistema guarda el cliente en BD
    /// Sistema retorna confirmación
    /// </summary>
    [Fact]
    public async Task AddAsync_WithValidCliente_ShouldPersistToDatabase()
    {
        // Arrange: Crear un cliente nuevo
        var cliente = TestDataFixtures.CreateTestCliente();

        // Act: Guardar el cliente en BD
        await _repository.AddAsync(cliente);

        // Assert: Verificar que se guardó correctamente
        var savedCliente = await _repository.GetByIdAsync(cliente.Id);
        savedCliente.Should().NotBeNull();
        savedCliente!.Cedula.Should().Be(cliente.Cedula);
        savedCliente.Email.Should().Be(cliente.Email);
    }

    /// <summary>
    /// ? PRUEBA 6: Actualizar datos de cliente existente
    /// 
    /// Objetivo: Verificar que se pueden modificar datos del cliente
    /// Resultado esperado: Datos actualizados en BD
    /// 
    /// Escenario real:
    /// Usuario abre el perfil del cliente
    /// Usuario modifica nombre y email
    /// Sistema guarda los cambios en BD
    /// Sistema verifica que se actualizó
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WithExistingCliente_ShouldUpdateDatabase()
    {
        // Arrange: Crear y guardar un cliente
        var cliente = TestDataFixtures.CreateTestCliente();
        await _repository.AddAsync(cliente);

        // Modificar los datos del cliente
        cliente.Nombre = "Updated Name";
        cliente.Email = "updated@test.com";

        // Act: Guardar los cambios en BD
        await _repository.UpdateAsync(cliente);

        // Assert: Verificar que se actualizaron los datos
        var updatedCliente = await _repository.GetByIdAsync(cliente.Id);
        updatedCliente.Should().NotBeNull();
        updatedCliente!.Nombre.Should().Be("Updated Name");
        updatedCliente.Email.Should().Be("updated@test.com");
    }

    /// <summary>
    /// ? PRUEBA 7: Eliminar cliente de BD
    /// 
    /// Objetivo: Verificar que se puede eliminar un cliente
    /// Resultado esperado: Cliente eliminado (no existe más)
    /// 
    /// Escenario real:
    /// Usuario solicita eliminar un cliente
    /// Sistema valida la solicitud
    /// Sistema elimina el cliente de BD
    /// Sistema verifica que fue eliminado
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WithExistingId_ShouldRemoveFromDatabase()
    {
        // Arrange: Crear y guardar un cliente
        var cliente = TestDataFixtures.CreateTestCliente();
        await _repository.AddAsync(cliente);

        // Verificar que existe
        var existingCliente = await _repository.GetByIdAsync(cliente.Id);
        existingCliente.Should().NotBeNull();

        // Act: Eliminar el cliente
        await _repository.DeleteAsync(cliente.Id);

        // Assert: Verificar que fue eliminado
        var deletedCliente = await _repository.GetByIdAsync(cliente.Id);
        deletedCliente.Should().BeNull();
    }
}