using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Persistence;

namespace Tests.Integration;

/// <summary>
/// Test suite para integración de Cliente (end-to-end)
/// Prueba toda la stack: API ? Controller ? Service ? Repository ? BD
/// Utiliza WebApplicationFactory para levantar un servidor de prueba real
/// y una BD en memoria para simular la BD real sin necesidad de infraestructura
/// </summary>
public class ClienteIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    /// <summary>
    /// Constructor: Configura la aplicación web para testing
    /// Se ejecuta UNA SOLA VEZ para todos los tests de esta clase (por IClassFixture)
    /// 
    /// Pasos:
    /// 1. Crea instancia de WebApplicationFactory
    /// 2. Reemplaza BD real por BD en memoria
    /// 3. Crea HttpClient para hacer requests HTTP
    /// </summary>
    public ClienteIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Eliminar la BD real configurada
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Agregar BD en memoria para testing
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        });

        // Crear cliente HTTP que apunta al servidor de prueba
        _client = _factory.CreateClient();
    }

    /// <summary>
    /// ? PRUEBA 1: Acceder a endpoint sin autenticación
    /// 
    /// Objetivo: Verificar que el endpoint rechaza requests sin JWT token
    /// Resultado esperado: HTTP 401 (Unauthorized)
    /// 
    /// Escenario real (end-to-end):
    /// 1. Cliente HTTP hace GET /api/cliente (sin bearer token)
    /// 2. Llega a ClienteController
    /// 3. Middleware de autenticación valida JWT
    /// 4. No hay JWT válido: rechaza con HTTP 401
    /// 5. La respuesta nunca llega al servicio de negocio
    /// 
    /// Este test valida SEGURIDAD: confirma que los endpoints
    /// están protegidos y no permiten acceso anónimo
    /// </summary>
    [Fact]
    public async Task GetClientes_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act: Hacer GET sin token JWT
        var response = await _client.GetAsync("/api/cliente");

        // Assert: Verificar que retorna HTTP 401
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    // Note: Para hacer tests completos de integración, necesitarías configurar
    // la autenticación JWT o usar un mecanismo de bypass para testing
    // Esto se puede hacer con políticas de autorización condicionales en el entorno de testing
    //
    // Ejemplo de cómo hacerlo:
    // 1. Crear un AuthenticationHandler personalizado para testing
    // 2. Registrarlo en Program.cs solo si estamos en ambiente de testing
    // 3. Así los tests pueden hacer requests autenticados sin necesidad de JWT real
}