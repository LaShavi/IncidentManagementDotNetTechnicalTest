using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tests.Fixtures;

namespace Tests.Infrastructure.Repositories;

/// <summary>
/// Test suite para TokenBlacklistRepository
/// Prueba todas las operaciones con tokens en blacklist (revocación de access tokens)
/// </summary>
public class TokenBlacklistRepositoryTests : TestBase
{
    private readonly TokenBlacklistRepository _repository;
    private readonly Mock<ILogger<TokenBlacklistRepository>> _mockLogger;

    /// <summary>
    /// Constructor: Inicializa el repositorio y el logger mock
    /// Se ejecuta ANTES de cada prueba [Fact]
    /// </summary>
    public TokenBlacklistRepositoryTests()
    {
        _mockLogger = new Mock<ILogger<TokenBlacklistRepository>>();
        _repository = new TokenBlacklistRepository(Context);
    }

    #region AddTokenAsync Tests

    /// <summary>
    /// ? PRUEBA 1: Agregar token a blacklist exitosamente
    /// 
    /// Objetivo: Verificar que se puede agregar un token hasheado a la blacklist
    /// Resultado esperado: Token guardado en BD con userId, hash, expiración y reason
    /// 
    /// Escenario real:
    /// Usuario hace logout
    /// Sistema hashea el JWT token (SHA256)
    /// Sistema extrae expiración del token
    /// Sistema guarda en TokenBlacklist
    /// Próxima request con ese token: middleware lo rechaza (401)
    /// </summary>
    [Fact]
    public async Task AddTokenAsync_WithValidToken_ShouldAddToDatabase()
    {
        // ARRANGE: Preparar usuario y datos del token
        var userId = Guid.NewGuid();
        var user = TestDataFixtures.CreateTestUser(id: userId);
        await UserRepository.AddAsync(user);

        var tokenHash = "hashedToken123ABC"; // Token ya hasheado con SHA256
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var reason = "logout_requested";

        // ACT: Agregar token a blacklist
        await _repository.AddTokenAsync(userId, tokenHash, expiresAt, reason);

        // ASSERT: Verificar que se guardó correctamente
        var blacklistedToken = await Context.TokenBlacklist
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        
        blacklistedToken.Should().NotBeNull();
        blacklistedToken?.UserId.Should().Be(userId);
        blacklistedToken?.TokenHash.Should().Be(tokenHash);
        blacklistedToken?.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
        blacklistedToken?.Reason.Should().Be(reason);
        blacklistedToken?.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// ? PRUEBA 2: Agregar token con reason por defecto
    /// 
    /// Objetivo: Verificar que se usa "Manual revocation" si no se especifica reason
    /// Resultado esperado: Token guardado con reason = "Manual revocation"
    /// </summary>
    [Fact]
    public async Task AddTokenAsync_WithoutReason_ShouldUseDefaultReason()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var user = TestDataFixtures.CreateTestUser(id: userId);
        await UserRepository.AddAsync(user);

        var tokenHash = "hashedTokenDefault";
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        // ACT: Agregar sin especificar reason
        await _repository.AddTokenAsync(userId, tokenHash, expiresAt);

        // ASSERT: Verificar reason por defecto
        var blacklistedToken = await Context.TokenBlacklist
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        
        blacklistedToken.Should().NotBeNull();
        blacklistedToken?.Reason.Should().Be("Manual revocation");
    }

    #endregion

    #region IsTokenBlacklistedAsync Tests

    /// <summary>
    /// ? PRUEBA 3: Verificar token en blacklist (existe y NO expiró)
    /// 
    /// Objetivo: Verificar que retorna true para token en blacklist válido
    /// Resultado esperado: true
    /// 
    /// Escenario real:
    /// Usuario hizo logout hace 5 minutos (token revocado)
    /// Usuario intenta usar el mismo token
    /// Middleware verifica: token está en blacklist y NO expiró
    /// Sistema rechaza: 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task IsTokenBlacklistedAsync_WithBlacklistedToken_ShouldReturnTrue()
    {
        // ARRANGE: Agregar token a blacklist con expiración futura
        var userId = Guid.NewGuid();
        var user = TestDataFixtures.CreateTestUser(id: userId);
        await UserRepository.AddAsync(user);

        var tokenHash = "hashedToken456";
        var expiresAt = DateTime.UtcNow.AddMinutes(10); // Expira en 10 min
        
        await _repository.AddTokenAsync(userId, tokenHash, expiresAt);

        // ACT: Verificar si está en blacklist
        var result = await _repository.IsTokenBlacklistedAsync(tokenHash);

        // ASSERT: Debe estar en blacklist
        result.Should().BeTrue();
    }

    /// <summary>
    /// ? PRUEBA 4: Verificar token NO en blacklist
    /// 
    /// Objetivo: Verificar que retorna false para token no revocado
    /// Resultado esperado: false
    /// 
    /// Escenario real:
    /// Usuario tiene token válido (nunca revocado)
    /// Middleware verifica: token NO está en blacklist
    /// Sistema permite el acceso
    /// </summary>
    [Fact]
    public async Task IsTokenBlacklistedAsync_WithNonBlacklistedToken_ShouldReturnFalse()
    {
        // ACT: Verificar token que nunca fue agregado
        var result = await _repository.IsTokenBlacklistedAsync("nonExistentHash");

        // ASSERT: No debe estar en blacklist
        result.Should().BeFalse();
    }

    /// <summary>
    /// ? PRUEBA 5: Verificar token expirado en blacklist
    /// 
    /// Objetivo: Verificar que retorna false para token que ya expiró
    /// Resultado esperado: false (token expirado = no importa si está en blacklist)
    /// 
    /// Escenario real:
    /// Token fue revocado hace 1 hora (agregado a blacklist)
    /// Token tenía expiración de 15 min (ya expiró hace 45 min)
    /// Usuario intenta usarlo
    /// Sistema verifica: token está en blacklist pero YA EXPIRÓ
    /// Sistema retorna false (no necesita rechazar, ya es inválido por expiración)
    /// </summary>
    [Fact]
    public async Task IsTokenBlacklistedAsync_WithExpiredToken_ShouldReturnFalse()
    {
        // ARRANGE: Agregar token con expiración pasada
        var userId = Guid.NewGuid();
        var user = TestDataFixtures.CreateTestUser(id: userId);
        await UserRepository.AddAsync(user);

        var tokenHash = "expiredTokenHash";
        var expiresAt = DateTime.UtcNow.AddHours(-1); // Expiró hace 1 hora
        
        await _repository.AddTokenAsync(userId, tokenHash, expiresAt);

        // ACT: Verificar token expirado
        var result = await _repository.IsTokenBlacklistedAsync(tokenHash);

        // ASSERT: No debe estar en blacklist (ya expiró)
        result.Should().BeFalse();
    }

    #endregion

    #region CleanExpiredTokensAsync Tests

    /// <summary>
    /// ? PRUEBA 6: Limpiar tokens expirados de blacklist
    /// 
    /// Objetivo: Verificar que se eliminan tokens expirados pero se mantienen los válidos
    /// Resultado esperado: Tokens expirados eliminados, tokens válidos conservados
    /// 
    /// Escenario real:
    /// Sistema ejecuta tarea programada (background job) cada noche
    /// Tarea busca tokens en blacklist que ya expiraron
    /// Tarea elimina esos tokens (ya no son necesarios)
    /// Razón: Optimizar BD y liberar espacio
    /// </summary>
    [Fact]
    public async Task CleanExpiredTokensAsync_ShouldRemoveExpiredTokens()
    {
        // ARRANGE: Agregar 2 tokens (1 expirado, 1 válido)
        var userId = Guid.NewGuid();
        var user = TestDataFixtures.CreateTestUser(id: userId);
        await UserRepository.AddAsync(user);

        // Token expirado (hace 1 hora)
        await _repository.AddTokenAsync(
            userId, "expiredHash", DateTime.UtcNow.AddHours(-1), "expired_token");
        
        // Token válido (expira en 1 hora)
        await _repository.AddTokenAsync(
            userId, "validHash", DateTime.UtcNow.AddHours(1), "valid_token");

        // ACT: Limpiar tokens expirados
        await _repository.CleanExpiredTokensAsync();

        // ASSERT: Verificar que el expirado fue eliminado y el válido se mantiene
        var expiredExists = await _repository.IsTokenBlacklistedAsync("expiredHash");
        var validExists = await _repository.IsTokenBlacklistedAsync("validHash");
        
        expiredExists.Should().BeFalse(); // Fue eliminado
        validExists.Should().BeTrue();    // Se mantiene
    }

    /// <summary>
    /// ? PRUEBA 7: Limpiar cuando no hay tokens expirados
    /// 
    /// Objetivo: Verificar que no falla si no hay tokens para eliminar
    /// Resultado esperado: Operación exitosa sin errores
    /// </summary>
    [Fact]
    public async Task CleanExpiredTokensAsync_WithNoExpiredTokens_ShouldNotFail()
    {
        // ARRANGE: Agregar solo token válido
        var userId = Guid.NewGuid();
        var user = TestDataFixtures.CreateTestUser(id: userId);
        await UserRepository.AddAsync(user);

        await _repository.AddTokenAsync(
            userId, "validHash", DateTime.UtcNow.AddHours(1));

        // ACT: Limpiar (no debería eliminar nada)
        await _repository.CleanExpiredTokensAsync();

        // ASSERT: Token válido sigue existiendo
        var exists = await _repository.IsTokenBlacklistedAsync("validHash");
        exists.Should().BeTrue();
    }

    #endregion

    #region RemoveUserTokensAsync Tests

    /// <summary>
    /// ? PRUEBA 8: Eliminar todos los tokens de un usuario
    /// 
    /// Objetivo: Verificar que se eliminan todos los tokens de un usuario específico
    /// Resultado esperado: Todos los tokens del usuario eliminados
    /// 
    /// Escenario real:
    /// Usuario elimina su cuenta (GDPR compliance)
    /// Sistema debe eliminar TODOS los datos del usuario
    /// Sistema elimina todos los tokens en blacklist del usuario
    /// Resultado: BD limpia de datos del usuario
    /// </summary>
    [Fact]
    public async Task RemoveUserTokensAsync_ShouldRemoveAllUserTokens()
    {
        // ARRANGE: Crear 2 usuarios con tokens cada uno
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        
        var user1 = TestDataFixtures.CreateTestUser(id: userId1);
        var user2 = TestDataFixtures.CreateTestUser(id: userId2);
        
        await UserRepository.AddAsync(user1);
        await UserRepository.AddAsync(user2);

        // Usuario 1: 2 tokens
        await _repository.AddTokenAsync(userId1, "user1Token1", DateTime.UtcNow.AddHours(1));
        await _repository.AddTokenAsync(userId1, "user1Token2", DateTime.UtcNow.AddHours(1));

        // Usuario 2: 1 token
        await _repository.AddTokenAsync(userId2, "user2Token1", DateTime.UtcNow.AddHours(1));

        // ACT: Eliminar tokens del usuario 1
        await _repository.RemoveUserTokensAsync(userId1);

        // ASSERT: Tokens de usuario 1 eliminados, tokens de usuario 2 intactos
        var user1Token1Exists = await _repository.IsTokenBlacklistedAsync("user1Token1");
        var user1Token2Exists = await _repository.IsTokenBlacklistedAsync("user1Token2");
        var user2Token1Exists = await _repository.IsTokenBlacklistedAsync("user2Token1");
        
        user1Token1Exists.Should().BeFalse(); // Eliminado
        user1Token2Exists.Should().BeFalse(); // Eliminado
        user2Token1Exists.Should().BeTrue();  // Intacto
    }

    /// <summary>
    /// ? PRUEBA 9: Eliminar tokens de usuario sin tokens
    /// 
    /// Objetivo: Verificar que no falla si usuario no tiene tokens
    /// Resultado esperado: Operación exitosa sin errores
    /// </summary>
    [Fact]
    public async Task RemoveUserTokensAsync_WithNoTokens_ShouldNotFail()
    {
        // ARRANGE: Usuario sin tokens
        var userId = Guid.NewGuid();
        var user = TestDataFixtures.CreateTestUser(id: userId);
        await UserRepository.AddAsync(user);

        // ACT: Intentar eliminar (no debería fallar)
        await _repository.RemoveUserTokensAsync(userId);

        // ASSERT: No hay error (operación exitosa)
        var tokensCount = await Context.TokenBlacklist
            .Where(t => t.UserId == userId)
            .CountAsync();
        
        tokensCount.Should().Be(0);
    }

    #endregion
}
