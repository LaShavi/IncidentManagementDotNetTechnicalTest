using Domain.Entities;
using Tests.Fixtures;

namespace Tests.Domain.Entities;

/// <summary>
/// Test suite para la entidad Cliente
/// Prueba la lógica de dominio: validaciones, métodos y propiedades
/// </summary>
public class ClienteTests
{
    /// <summary>
    /// ? PRUEBA 1: Obtener nombre completo del cliente
    /// 
    /// Objetivo: Verificar que el método ObtenerNombreCompleto() concatena correctamente
    /// Resultado esperado: "Juan Carlos Pérez González"
    /// 
    /// Escenario real:
    /// Sistema necesita mostrar nombre completo en pantalla
    /// Sistema combina nombre + apellido
    /// Sistema retorna "Nombre Apellido"
    /// </summary>
    [Fact]
    public void Cliente_ObtenerNombreCompleto_ShouldReturnCorrectFormat()
    {
        // Arrange: Crear cliente con nombre y apellido específicos
        var cliente = TestDataFixtures.CreateTestCliente(
            nombre: "Juan Carlos",
            apellido: "Pérez González"
        );

        // Act: Obtener el nombre completo
        var nombreCompleto = cliente.ObtenerNombreCompleto();

        // Assert: Verificar que se concatenó correctamente
        nombreCompleto.Should().Be("Juan Carlos Pérez González");
    }

    /// <summary>
    /// ? PRUEBA 2: Validar cliente con todos los datos requeridos
    /// 
    /// Objetivo: Verificar que EsValido() retorna true con datos válidos
    /// Resultado esperado: true
    /// 
    /// Escenario real:
    /// Usuario completa todos los campos del formulario
    /// Sistema valida que todos los datos sean correctos
    /// Sistema permite guardar al cliente
    /// </summary>
    [Fact]
    public void Cliente_EsValido_WithValidData_ShouldReturnTrue()
    {
        // Arrange: Crear un cliente con datos válidos
        var cliente = TestDataFixtures.CreateTestCliente();

        // Act: Validar el cliente
        var esValido = cliente.EsValido();

        // Assert: Verificar que está válido
        esValido.Should().BeTrue();
    }

    /// <summary>
    /// ? PRUEBA 3: Validar cliente con campos vacíos
    /// 
    /// Objetivo: Verificar que EsValido() retorna false si faltan campos requeridos
    /// Resultado esperado: false (para cada combinación de campo vacío)
    /// 
    /// Escenario real:
    /// Usuario intenta guardar sin llenar todos los campos
    /// Sistema valida cada campo obligatorio
    /// Si alguno está vacío: rechaza
    /// 
    /// Casos probados:
    /// - Cédula vacía
    /// - Email vacío
    /// - Teléfono vacío
    /// - Nombre vacío
    /// - Apellido vacío
    /// </summary>
    [Theory]
    [InlineData("", "test@test.com", "555-1234", "Juan", "Perez")]
    [InlineData("12345678", "", "555-1234", "Juan", "Perez")]
    [InlineData("12345678", "test@test.com", "", "Juan", "Perez")]
    [InlineData("12345678", "test@test.com", "555-1234", "", "Perez")]
    [InlineData("12345678", "test@test.com", "555-1234", "Juan", "")]
    public void Cliente_EsValido_WithMissingRequiredFields_ShouldReturnFalse(
        string cedula, string email, string telefono, string nombre, string apellido)
    {
        // Arrange: Crear cliente con un campo vacío (según parámetro)
        var cliente = TestDataFixtures.CreateTestCliente(
            cedula: cedula,
            email: email,
            telefono: telefono,
            nombre: nombre,
            apellido: apellido
        );

        // Act: Validar el cliente
        var esValido = cliente.EsValido();

        // Assert: Verificar que es inválido (por campo vacío)
        esValido.Should().BeFalse();
    }

    /// <summary>
    /// ? PRUEBA 4: Validar cliente con email inválido
    /// 
    /// Objetivo: Verificar que EsValido() rechaza emails con formato incorrecto
    /// Resultado esperado: false (para cada formato inválido)
    /// 
    /// Escenario real:
    /// Usuario ingresa email con formato incorrecto
    /// Sistema valida el formato del email
    /// Si no tiene @, dominio, etc.: rechaza
    /// 
    /// Casos probados:
    /// - "invalid-email" (sin @)
    /// - "@test.com" (sin usuario)
    /// - "test@" (sin dominio)
    /// - "test" (sin @ ni dominio)
    /// </summary>
    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@test.com")]
    [InlineData("test@")]
    [InlineData("test")]
    public void Cliente_EsValido_WithInvalidEmail_ShouldReturnFalse(string invalidEmail)
    {
        // Arrange: Crear cliente con email inválido
        var cliente = TestDataFixtures.CreateTestCliente(email: invalidEmail);

        // Act: Validar el cliente
        var esValido = cliente.EsValido();

        // Assert: Verificar que es inválido por email
        esValido.Should().BeFalse();
    }

    /// <summary>
    /// ? PRUEBA 5: Asignar y verificar propiedades del cliente
    /// 
    /// Objetivo: Verificar que todas las propiedades se asignan y retornan correctamente
    /// Resultado esperado: Todas las propiedades con valores correctos
    /// 
    /// Escenario real:
    /// Sistema crea un cliente con valores específicos
    /// Sistema asigna cada propiedad
    /// Sistema valida que se guardaron correctamente
    /// </summary>
    [Fact]
    public void Cliente_Properties_ShouldBeSetCorrectly()
    {
        // Arrange: Preparar valores para el cliente
        var id = Guid.NewGuid();
        var cedula = "12345678";
        var email = "test@test.com";
        var telefono = "555-1234";
        var nombre = "Juan";
        var apellido = "Perez";

        // Act: Crear cliente con esos valores
        var cliente = new Cliente
        {
            Id = id,
            Cedula = cedula,
            Email = email,
            Telefono = telefono,
            Nombre = nombre,
            Apellido = apellido
        };

        // Assert: Verificar que cada propiedad tiene el valor correcto
        cliente.Id.Should().Be(id);
        cliente.Cedula.Should().Be(cedula);
        cliente.Email.Should().Be(email);
        cliente.Telefono.Should().Be(telefono);
        cliente.Nombre.Should().Be(nombre);
        cliente.Apellido.Should().Be(apellido);
    }
}