using Microsoft.EntityFrameworkCore;
using Tests.Fixtures;

namespace Tests.Infrastructure.Repositories;

/// <summary>
/// Test suite para UserRepository
/// Prueba todas las operaciones CRUD y búsquedas de usuarios en la base de datos
/// </summary>
public class UserRepositoryTests : TestBase
{
    /// <summary>
    /// ? PRUEBA 1: Agregar usuario válido a la BD
    /// 
    /// Objetivo: Verificar que se puede agregar un usuario nuevo a la base de datos
    /// Resultado esperado: HTTP 200 + Usuario guardado en BD con todos los datos
    /// 
    /// Escenario real:
    /// Nuevo usuario se registra en el sistema
    /// Sistema valida los datos
    /// Sistema guarda el usuario en BD
    /// Sistema retorna el usuario guardado
    /// </summary>
    [Fact]
    public async Task AddAsync_WithValidUser_ShouldAddToDatabase()
    {
        // Arrange: Preparar un usuario válido
        var user = TestDataFixtures.CreateTestUser();

        // Act: Guardar el usuario en BD
        await UserRepository.AddAsync(user);

        // Assert: Verificar que se guardó correctamente
        var addedUser = await Context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        addedUser.Should().NotBeNull();
        addedUser?.Username.Should().Be(user.Username);
    }

    /// <summary>
    /// ? PRUEBA 2: Obtener usuario por ID existente
    /// 
    /// Objetivo: Verificar que se puede recuperar un usuario de la BD por su ID
    /// Resultado esperado: Usuario encontrado con todos sus datos
    /// 
    /// Escenario real:
    /// Usuario se registró previamente
    /// Sistema busca al usuario por ID
    /// Sistema retorna los datos del usuario
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnUser()
    {
        // Arrange: Crear y guardar un usuario
        var user = TestDataFixtures.CreateTestUser();
        await UserRepository.AddAsync(user);

        // Act: Obtener el usuario por su ID
        var result = await UserRepository.GetByIdAsync(user.Id);

        // Assert: Verificar que se obtuvieron los datos correctos
        result.Should().NotBeNull();
        result?.Username.Should().Be(user.Username);
    }

    /// <summary>
    /// ? PRUEBA 3: Obtener usuario por ID no existente
    /// 
    /// Objetivo: Verificar que retorna null cuando el usuario no existe
    /// Resultado esperado: null (sin error)
    /// 
    /// Escenario real:
    /// Usuario intenta buscar un usuario con ID inexistente
    /// Sistema busca en BD
    /// Sistema no encuentra nada
    /// Sistema retorna null sin error
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Act: Intentar obtener usuario con ID inexistente
        var result = await UserRepository.GetByIdAsync(Guid.NewGuid());

        // Assert: Verificar que retorna null
        result.Should().BeNull();
    }

    /// <summary>
    /// ? PRUEBA 4: Obtener usuario por username existente
    /// 
    /// Objetivo: Verificar que se puede buscar usuario por su username
    /// Resultado esperado: Usuario encontrado con datos correctos
    /// 
    /// Escenario real:
    /// Usuario se registró con username único
    /// Sistema busca por username
    /// Sistema encuentra y retorna el usuario
    /// </summary>
    [Fact]
    public async Task GetByUsernameAsync_WithExistingUsername_ShouldReturnUser()
    {
        // Arrange: Crear usuario con username único
        var user = TestDataFixtures.CreateTestUser(username: "uniqueuser");
        await UserRepository.AddAsync(user);

        // Act: Buscar por username
        var result = await UserRepository.GetByUsernameAsync("uniqueuser");

        // Assert: Verificar que encontró el usuario correcto
        result.Should().NotBeNull();
        result?.Username.Should().Be("uniqueuser");
    }

    /// <summary>
    /// ? PRUEBA 5: Obtener usuario por username no existente
    /// 
    /// Objetivo: Verificar que retorna null para username inexistente
    /// Resultado esperado: null
    /// 
    /// Escenario real:
    /// Sistema busca username que no existe
    /// Sistema no encuentra coincidencia
    /// Sistema retorna null
    /// </summary>
    [Fact]
    public async Task GetByUsernameAsync_WithNonExistingUsername_ShouldReturnNull()
    {
        // Act: Intentar buscar username inexistente
        var result = await UserRepository.GetByUsernameAsync("nonexistent");

        // Assert: Verificar que retorna null
        result.Should().BeNull();
    }

    /// <summary>
    /// ? PRUEBA 6: Obtener usuario por email existente
    /// 
    /// Objetivo: Verificar búsqueda de usuario por email
    /// Resultado esperado: Usuario encontrado
    /// 
    /// Escenario real:
    /// Usuario se registró con email único
    /// Sistema busca por email
    /// Sistema retorna el usuario
    /// </summary>
    [Fact]
    public async Task GetByEmailAsync_WithExistingEmail_ShouldReturnUser()
    {
        // Arrange: Crear usuario con email único
        var user = TestDataFixtures.CreateTestUser(email: "unique@test.com");
        await UserRepository.AddAsync(user);

        // Act: Buscar por email
        var result = await UserRepository.GetByEmailAsync("unique@test.com");

        // Assert: Verificar que encontró el usuario
        result.Should().NotBeNull();
        result?.Email.Should().Be("unique@test.com");
    }

    /// <summary>
    /// ? PRUEBA 7: Obtener usuario por email no existente
    /// 
    /// Objetivo: Verificar que retorna null para email inexistente
    /// Resultado esperado: null
    /// 
    /// Escenario real:
    /// Sistema busca email que no existe
    /// Sistema no encuentra coincidencia
    /// Sistema retorna null
    /// </summary>
    [Fact]
    public async Task GetByEmailAsync_WithNonExistingEmail_ShouldReturnNull()
    {
        // Act: Intentar buscar email inexistente
        var result = await UserRepository.GetByEmailAsync("nonexistent@test.com");

        // Assert: Verificar que retorna null
        result.Should().BeNull();
    }

    /// <summary>
    /// ? PRUEBA 8: Validar que username existe
    /// 
    /// Objetivo: Verificar método ExistsUsernameAsync retorna true para username existente
    /// Resultado esperado: true
    /// 
    /// Escenario real:
    /// Sistema valida si username está disponible al registrar
    /// Username ya existe (tomado)
    /// Sistema retorna true
    /// Sistema impide que se registre con ese username
    /// </summary>
    [Fact]
    public async Task ExistsUsernameAsync_WithExistingUsername_ShouldReturnTrue()
    {
        // Arrange: Crear usuario con username específico
        var user = TestDataFixtures.CreateTestUser(username: "existinguser");
        await UserRepository.AddAsync(user);

        // Act: Verificar si username existe
        var result = await UserRepository.ExistsUsernameAsync("existinguser");

        // Assert: Verificar que retorna true
        result.Should().BeTrue();
    }

    /// <summary>
    /// ? PRUEBA 9: Validar que username NO existe
    /// 
    /// Objetivo: Verificar que retorna false para username disponible
    /// Resultado esperado: false
    /// 
    /// Escenario real:
    /// Nuevo usuario intenta registrarse
    /// Sistema valida si username está disponible
    /// Username no existe (está libre)
    /// Sistema retorna false (permitir usar este username)
    /// </summary>
    [Fact]
    public async Task ExistsUsernameAsync_WithNonExistingUsername_ShouldReturnFalse()
    {
        // Act: Verificar si username inexistente existe
        var result = await UserRepository.ExistsUsernameAsync("nonexistent");

        // Assert: Verificar que retorna false (no existe)
        result.Should().BeFalse();
    }

    /// <summary>
    /// ? PRUEBA 10: Validar que email existe
    /// 
    /// Objetivo: Verificar que retorna true para email registrado
    /// Resultado esperado: true
    /// 
    /// Escenario real:
    /// Sistema valida si email está disponible al registrar
    /// Email ya existe (tomado)
    /// Sistema retorna true
    /// Sistema impide duplicados de email
    /// </summary>
    [Fact]
    public async Task ExistsEmailAsync_WithExistingEmail_ShouldReturnTrue()
    {
        // Arrange: Crear usuario con email específico
        var user = TestDataFixtures.CreateTestUser(email: "existing@test.com");
        await UserRepository.AddAsync(user);

        // Act: Verificar si email existe
        var result = await UserRepository.ExistsEmailAsync("existing@test.com");

        // Assert: Verificar que retorna true
        result.Should().BeTrue();
    }

    /// <summary>
    /// ? PRUEBA 11: Validar que email NO existe
    /// 
    /// Objetivo: Verificar que retorna false para email disponible
    /// Resultado esperado: false
    /// 
    /// Escenario real:
    /// Nuevo usuario intenta registrarse
    /// Sistema valida si email está disponible
    /// Email no existe (está libre)
    /// Sistema retorna false (permitir usar este email)
    /// </summary>
    [Fact]
    public async Task ExistsEmailAsync_WithNonExistingEmail_ShouldReturnFalse()
    {
        // Act: Verificar si email inexistente existe
        var result = await UserRepository.ExistsEmailAsync("nonexistent@test.com");

        // Assert: Verificar que retorna false (no existe)
        result.Should().BeFalse();
    }

    /// <summary>
    /// ? PRUEBA 12: Actualizar usuario en BD
    /// 
    /// Objetivo: Verificar que se pueden actualizar datos del usuario
    /// Resultado esperado: Datos actualizados en BD
    /// 
    /// Escenario real:
    /// Usuario cambia su nombre o email
    /// Sistema valida los nuevos datos
    /// Sistema actualiza en BD
    /// Sistema verifica que los cambios se guardaron
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WithValidUser_ShouldUpdateDatabase()
    {
        // Arrange: Crear y guardar usuario
        var user = TestDataFixtures.CreateTestUser();
        await UserRepository.AddAsync(user);
        
        // Modificar datos del usuario
        user.FirstName = "UpdatedName";
        user.Email = "updated@test.com";

        // Act: Actualizar el usuario en BD
        await UserRepository.UpdateAsync(user);

        // Assert: Verificar que los datos se actualizaron
        var updatedUser = await UserRepository.GetByIdAsync(user.Id);
        updatedUser?.FirstName.Should().Be("UpdatedName");
        updatedUser?.Email.Should().Be("updated@test.com");
    }

    /// <summary>
    /// ? PRUEBA 13: Eliminar usuario de BD
    /// 
    /// Objetivo: Verificar que se puede eliminar un usuario
    /// Resultado esperado: Usuario eliminado (no existe más)
    /// 
    /// Escenario real:
    /// Usuario solicita eliminar su cuenta
    /// Sistema valida la solicitud
    /// Sistema elimina todos sus datos de BD
    /// Sistema verifica que ya no existe
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WithExistingId_ShouldRemoveFromDatabase()
    {
        // Arrange: Crear y guardar usuario
        var user = TestDataFixtures.CreateTestUser();
        await UserRepository.AddAsync(user);

        // Act: Eliminar el usuario
        await UserRepository.DeleteAsync(user.Id);

        // Assert: Verificar que fue eliminado
        var deletedUser = await UserRepository.GetByIdAsync(user.Id);
        deletedUser.Should().BeNull();
    }

    /// <summary>
    /// ? PRUEBA 14: Obtener todos los usuarios
    /// 
    /// Objetivo: Verificar que se pueden obtener todos los usuarios registrados
    /// Resultado esperado: Lista con todos los usuarios (al menos los 3 que creamos)
    /// 
    /// Escenario real:
    /// Admin quiere ver lista de todos los usuarios
    /// Sistema busca todos los usuarios en BD
    /// Sistema retorna la lista completa
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ShouldReturnAllUsers()
    {
        // Arrange: Crear y guardar 3 usuarios
        var users = TestDataFixtures.CreateTestUsers(3);
        foreach (var user in users)
        {
            await UserRepository.AddAsync(user);
        }

        // Act: Obtener todos los usuarios
        var result = await UserRepository.GetAllAsync();

        // Assert: Verificar que se obtuvieron al menos los 3 usuarios
        result.Should().HaveCountGreaterThanOrEqualTo(3);
    }
}

/// <summary>
/// Test suite para RefreshTokenRepository
/// Prueba todas las operaciones con refresh tokens en la BD
/// </summary>
public class RefreshTokenRepositoryTests : TestBase
{
    /// <summary>
    /// ? PRUEBA 1: Agregar refresh token válido
    /// 
    /// Objetivo: Verificar que se puede guardar un refresh token en BD
    /// Resultado esperado: Token guardado con todos los datos
    /// 
    /// Escenario real:
    /// Usuario hace login exitosamente
    /// Sistema genera refresh token
    /// Sistema guarda el token en BD
    /// Sistema retorna el token al usuario
    /// </summary>
    [Fact]
    public async Task AddAsync_WithValidToken_ShouldAddToDatabase()
    {
        // Arrange
        // ?? Primero crear un usuario válido (la clave foránea lo requiere)
        var user = TestDataFixtures.CreateTestUser();
        await UserRepository.AddAsync(user);
        
        var token = TestDataFixtures.CreateTestRefreshToken(userId: user.Id);

        // Act: Guardar el refresh token
        await RefreshTokenRepository.AddAsync(token);

        // Assert: Verificar que se guardó
        var addedToken = await Context.RefreshTokens.FirstOrDefaultAsync(t => t.Id == token.Id);
        addedToken.Should().NotBeNull();
        addedToken?.Token.Should().Be(token.Token);
    }

    /// <summary>
    /// ? PRUEBA 2: Obtener refresh token por token string
    /// 
    /// Objetivo: Verificar que se puede buscar un token por su valor string
    /// Resultado esperado: Token encontrado con todos sus datos
    /// 
    /// Escenario real:
    /// Usuario intenta refrescar su access token
    /// Usuario envía su refresh token
    /// Sistema busca el token en BD
    /// Sistema valida que existe y es válido
    /// </summary>
    [Fact]
    public async Task GetByTokenAsync_WithExistingToken_ShouldReturnToken()
    {
        // Arrange
        // ?? Primero crear un usuario válido (la clave foránea lo requiere)
        var user = TestDataFixtures.CreateTestUser();
        await UserRepository.AddAsync(user);
        
        var token = TestDataFixtures.CreateTestRefreshToken(userId: user.Id, token: "uniqueToken123");
        await RefreshTokenRepository.AddAsync(token);

        // Act: Buscar el token por su valor
        var result = await RefreshTokenRepository.GetByTokenAsync("uniqueToken123");

        // Assert: Verificar que encontró el token
        result.Should().NotBeNull();
        result?.Token.Should().Be("uniqueToken123");
    }

    /// <summary>
    /// ? PRUEBA 3: Obtener refresh token inexistente
    /// 
    /// Objetivo: Verificar que retorna null para token no encontrado
    /// Resultado esperado: null
    /// 
    /// Escenario real:
    /// Usuario intenta usar token falso/inválido
    /// Sistema busca en BD
    /// Sistema no encuentra el token
    /// Sistema retorna null (solicita nuevo login)
    /// </summary>
    [Fact]
    public async Task GetByTokenAsync_WithNonExistingToken_ShouldReturnNull()
    {
        // Act: Intentar obtener token inexistente
        var result = await RefreshTokenRepository.GetByTokenAsync("nonexistentToken");

        // Assert: Verificar que retorna null
        result.Should().BeNull();
    }

    /// <summary>
    /// ? PRUEBA 4: Revocar (invalidar) un refresh token específico
    /// 
    /// Objetivo: Verificar que se puede marcar un token como revocado
    /// Resultado esperado: Token con RevokedAt establecido
    /// 
    /// Escenario real:
    /// Usuario cierra sesión en un dispositivo específico
    /// Sistema marca el refresh token de ese dispositivo como revocado
    /// Ese dispositivo no puede refrescar tokens más
    /// Los otros dispositivos siguen funcionando
    /// </summary>
    [Fact]
    public async Task RevokeTokenAsync_WithValidToken_ShouldRevokeToken()
    {
        // Arrange
        // ?? Primero crear un usuario válido (la clave foránea lo requiere)
        var user = TestDataFixtures.CreateTestUser();
        await UserRepository.AddAsync(user);
        
        var token = TestDataFixtures.CreateTestRefreshToken(userId: user.Id, token: "tokenToRevoke");
        await RefreshTokenRepository.AddAsync(token);

        // Act: Revocar el token específico
        await RefreshTokenRepository.RevokeTokenAsync("tokenToRevoke");

        // Assert: Verificar que fue revocado (tiene RevokedAt)
        var revokedToken = await RefreshTokenRepository.GetByTokenAsync("tokenToRevoke");
        revokedToken?.RevokedAt.Should().NotBeNull();
    }

    /// <summary>
    /// ? PRUEBA 5: Revocar TODOS los tokens de un usuario
    /// 
    /// Objetivo: Verificar que se pueden invalidar todos los tokens simultáneamente
    /// Resultado esperado: Todos los tokens del usuario marcados como revocados
    /// 
    /// Escenario real:
    /// Usuario hace logout global (de todos los dispositivos)
    /// Sistema revoca TODOS sus refresh tokens
    /// Usuario es desconectado de TODOS los dispositivos
    /// Usuario debe hacer login de nuevo en cada dispositivo
    /// </summary>
    [Fact]
    public async Task RevokeAllByUserIdAsync_ShouldRevokeAllUserTokens()
    {
        // Arrange
        // ?? Primero crear un usuario válido (la clave foránea lo requiere)
        var user = TestDataFixtures.CreateTestUser();
        await UserRepository.AddAsync(user);
        
        // Crear 2 tokens para el mismo usuario (multidispositivo)
        var token1 = TestDataFixtures.CreateTestRefreshToken(userId: user.Id, token: "token1");
        var token2 = TestDataFixtures.CreateTestRefreshToken(userId: user.Id, token: "token2");
        
        await RefreshTokenRepository.AddAsync(token1);
        await RefreshTokenRepository.AddAsync(token2);

        // Act: Revocar TODOS los tokens del usuario
        await RefreshTokenRepository.RevokeAllByUserIdAsync(user.Id);

        // Assert: Verificar que ambos tokens fueron revocados
        var tokens = await Context.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync();
        tokens.Should().AllSatisfy(t => t.RevokedAt.Should().NotBeNull());
    }

    /// <summary>
    /// ? PRUEBA 6: Actualizar refresh token
    /// 
    /// Objetivo: Verificar que se pueden actualizar datos del token
    /// Resultado esperado: Token actualizado en BD
    /// 
    /// Escenario real:
    /// Sistema necesita marcar token como revocado (actualizar)
    /// Sistema cambia la propiedad RevokedAt
    /// Sistema guarda los cambios
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WithValidToken_ShouldUpdateDatabase()
    {
        // Arrange
        // ?? Primero crear un usuario válido (la clave foránea lo requiere)
        var user = TestDataFixtures.CreateTestUser();
        await UserRepository.AddAsync(user);
        
        var token = TestDataFixtures.CreateTestRefreshToken(userId: user.Id);
        await RefreshTokenRepository.AddAsync(token);

        // Modificar el token (marcar como revocado)
        token.RevokedAt = DateTime.UtcNow;

        // Act: Actualizar el token en BD
        await RefreshTokenRepository.UpdateAsync(token);

        // Assert: Verificar que se actualizó
        var updatedToken = await RefreshTokenRepository.GetByTokenAsync(token.Token);
        updatedToken?.RevokedAt.Should().NotBeNull();
    }
}

/// <summary>
/// Test suite para PasswordResetTokenRepository
/// Prueba todas las operaciones con tokens de reset de contraseña
/// </summary>
public class PasswordResetTokenRepositoryTests : TestBase
{
    /// <summary>
    /// ? PRUEBA 1: Agregar token de reset de contraseña
    /// 
    /// Objetivo: Verificar que se guarda el token de reset en BD
    /// Resultado esperado: Token guardado con todos los datos
    /// 
    /// Escenario real:
    /// Usuario solicitó "Olvidé contraseña"
    /// Sistema genera token de reset
    /// Sistema guarda el token en BD (válido por 1 hora)
    /// Sistema envía email con link
    /// </summary>
    [Fact]
    public async Task AddAsync_WithValidToken_ShouldAddToDatabase()
    {
        // Arrange: Crear un token de reset
        var token = TestDataFixtures.CreateTestPasswordResetToken();

        // Act: Guardar el token en BD
        await PasswordResetTokenRepository.AddAsync(token);

        // Assert: Verificar que se guardó
        var addedToken = await Context.PasswordResetTokens.FirstOrDefaultAsync(t => t.Id == token.Id);
        addedToken.Should().NotBeNull();
        addedToken?.Token.Should().Be(token.Token);
    }

    /// <summary>
    /// ? PRUEBA 2: Obtener token de reset por su valor
    /// 
    /// Objetivo: Verificar búsqueda del token por su string
    /// Resultado esperado: Token encontrado
    /// 
    /// Escenario real:
    /// Usuario hace click en link del email
    /// El link contiene: /reset-password?token=resetToken123
    /// Sistema extrae el token del URL
    /// Sistema busca el token en BD
    /// Sistema valida que existe y no expiró
    /// </summary>
    [Fact]
    public async Task GetByTokenAsync_WithExistingToken_ShouldReturnToken()
    {
        // Arrange: Crear y guardar token de reset
        var token = TestDataFixtures.CreateTestPasswordResetToken(token: "resetToken123");
        await PasswordResetTokenRepository.AddAsync(token);

        // Act: Buscar el token por su valor
        var result = await PasswordResetTokenRepository.GetByTokenAsync("resetToken123");

        // Assert: Verificar que encontró el token
        result.Should().NotBeNull();
        result?.Token.Should().Be("resetToken123");
    }

    /// <summary>
    /// ? PRUEBA 3: Obtener token de reset inexistente
    /// 
    /// Objetivo: Verificar que retorna null para token no encontrado
    /// Resultado esperado: null
    /// 
    /// Escenario real:
    /// Atacante intenta usar un token falso
    /// Sistema busca en BD
    /// Sistema no encuentra el token
    /// Sistema rechaza la solicitud
    /// </summary>
    [Fact]
    public async Task GetByTokenAsync_WithNonExistingToken_ShouldReturnNull()
    {
        // Act: Intentar obtener token inexistente
        var result = await PasswordResetTokenRepository.GetByTokenAsync("nonexistentToken");

        // Assert: Verificar que retorna null
        result.Should().BeNull();
    }

    /// <summary>
    /// ? PRUEBA 4: Marcar token de reset como utilizado
    /// 
    /// Objetivo: Verificar que se marca el token como usado (para no reutilizarlo)
    /// Resultado esperado: Token con IsUsed = true
    /// 
    /// Escenario real:
    /// Usuario completa el reset de contraseña
    /// Sistema guarda la nueva contraseña
    /// Sistema marca el token como IsUsed = true
    /// Si alguien intenta usarlo de nuevo: rechaza (ya fue usado)
    /// </summary>
    [Fact]
    public async Task MarkAsUsedAsync_WithValidToken_ShouldMarkAsUsed()
    {
        // Arrange: Crear y guardar token de reset
        var token = TestDataFixtures.CreateTestPasswordResetToken(token: "tokenToUse");
        await PasswordResetTokenRepository.AddAsync(token);

        // Act: Marcar el token como usado
        await PasswordResetTokenRepository.MarkAsUsedAsync(token.Id);

        // Assert: Verificar que está marcado como usado
        var usedToken = await PasswordResetTokenRepository.GetByTokenAsync("tokenToUse");
        usedToken?.IsUsed.Should().BeTrue();
    }
}
