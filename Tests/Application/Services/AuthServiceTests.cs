using Application.DTOs.Auth;
using Application.Services;
using Application.Ports;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tests.Fixtures;

namespace Tests.Application.Services;

/// <summary>
/// Test suite para AuthService
/// Prueba la lógica de autenticación: login, registro, refresh token, cambio de contraseña, reset de contraseña, etc.
/// Utiliza Moq para simular repositorios, email service, y password hasher
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly Mock<IPasswordResetTokenRepository> _mockPasswordResetTokenRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly Mock<ITokenBlacklistRepository> _mockTokenBlacklistRepository;
    private readonly AuthService _authService;

    /// <summary>
    /// Constructor: Inicializa los mocks y el AuthService
    /// Se ejecuta ANTES de cada prueba [Fact]
    /// </summary>
    public AuthServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _mockPasswordResetTokenRepository = new Mock<IPasswordResetTokenRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockEmailService = new Mock<IEmailService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AuthService>>();
        _mockTokenBlacklistRepository = new Mock<ITokenBlacklistRepository>();

        // Setup configuration defaults
        _mockConfiguration.Setup(x => x["Authentication:SecretKey"]).Returns("LLAVE-DE-SEGURIDAD402814678901278021745jabfheojagrvnbxudmaloskdkf091649451548174528874512361922210425100");
        _mockConfiguration.Setup(x => x["Authentication:Issuer"]).Returns("HexagonalArchitectureTemplate");
        _mockConfiguration.Setup(x => x["Authentication:Audience"]).Returns("HexagonalArchitectureTemplate-Users");
        _mockConfiguration.Setup(x => x["Authentication:AccessTokenExpiration"]).Returns("15");
        _mockConfiguration.Setup(x => x["Authentication:RefreshTokenExpiration"]).Returns("7");

        _authService = new AuthService(
            _mockUserRepository.Object,
            _mockRefreshTokenRepository.Object,
            _mockPasswordHasher.Object,
            _mockConfiguration.Object,
            _mockPasswordResetTokenRepository.Object,
            _mockEmailService.Object,
            _mockLogger.Object,
            _mockTokenBlacklistRepository.Object
        );
    }

    #region LoginAsync Tests

    /// <summary>
    /// ✅ PRUEBA 1: Login exitoso con credenciales válidas
    /// 
    /// Objetivo: Verificar que el servicio puede autenticar un usuario con credenciales correctas
    /// Resultado esperado: LoginResponseDTO con AccessToken, RefreshToken y datos del usuario
    /// 
    /// Escenario real:
    /// Usuario con username="testuser" existe en BD
    /// Contraseña es correcta (hash coincide)
    /// Sistema genera JWT + Refresh Token
    /// Sistema actualiza LastAccess del usuario
    /// </summary>
    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnLoginResponse()
    {
        // ARRANGE: Preparar usuario existente con credenciales válidas
        var loginRequest = new LoginRequestDTO { Username = "testuser", Password = "password123" };
        var user = TestDataFixtures.CreateTestUser(username: "testuser");
        
        // Mock: Repositorio retorna usuario existente
        _mockUserRepository.Setup(r => r.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        
        // Mock: Password hasher verifica que la contraseña es correcta
        _mockPasswordHasher.Setup(h => h.VerifyPassword("password123", user.PasswordHash))
            .Returns(true);
        
        // Mock: Repositorio actualiza el usuario (LastAccess)
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);
        
        // Mock: Repositorio guarda el nuevo refresh token
        _mockRefreshTokenRepository.Setup(r => r.AddAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);

        // ACT: Hacer login
        var result = await _authService.LoginAsync(loginRequest);

        // ASSERT: Verificar respuesta completa
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();  // JWT token generado
        result.RefreshToken.Should().NotBeNullOrEmpty(); // Refresh token generado
        result.User.Username.Should().Be("testuser");    // Usuario correcto
        
        // Verificar que se llamó a los métodos correctos
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        _mockRefreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
    }

    /// <summary>
    /// ❌ PRUEBA 2: Login fallido - Username no existe
    /// 
    /// Objetivo: Verificar que el login es rechazado si el usuario no existe
    /// Resultado esperado: UnauthorizedAccessException
    /// 
    /// Escenario real:
    /// Usuario intenta login con username="nonexistent" que NO existe en BD
    /// Sistema no encuentra el usuario
    /// Sistema rechaza con 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task LoginAsync_WithInvalidUsername_ShouldThrowUnauthorizedAccessException()
    {
        // ARRANGE: Usuario no existe en BD
        var loginRequest = new LoginRequestDTO { Username = "nonexistent", Password = "password123" };
        
        // Mock: Repositorio retorna null (usuario no existe)
        _mockUserRepository.Setup(r => r.GetByUsernameAsync("nonexistent"))
            .ReturnsAsync((User?)null);

        // ACT & ASSERT: Debe lanzar excepción
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(loginRequest));
        
        // Verificar que NO se creó refresh token
        _mockRefreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
    }

    /// <summary>
    /// ❌ PRUEBA 3: Login fallido - Contraseña incorrecta
    /// 
    /// Objetivo: Verificar que el login es rechazado con contraseña incorrecta
    /// Resultado esperado: UnauthorizedAccessException + se registra intento fallido
    /// 
    /// Escenario real:
    /// Usuario existe en BD
    /// Usuario intenta login con contraseña incorrecta
    /// Sistema verifica hash: NO coincide
    /// Sistema incrementa contador de intentos fallidos
    /// Si alcanza máximo (5), bloquea la cuenta por 15 minutos
    /// </summary>
    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowUnauthorizedAccessException()
    {
        // ARRANGE: Usuario existe pero contraseña es incorrecta
        var loginRequest = new LoginRequestDTO { Username = "testuser", Password = "wrongpassword" };
        var user = TestDataFixtures.CreateTestUser(username: "testuser");
        
        // Mock: Repositorio retorna usuario existente
        _mockUserRepository.Setup(r => r.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        
        // Mock: Password hasher indica que contraseña NO es correcta
        _mockPasswordHasher.Setup(h => h.VerifyPassword("wrongpassword", user.PasswordHash))
            .Returns(false);
        
        // Mock: Se actualiza el contador de intentos fallidos
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // ACT & ASSERT: Debe lanzar excepción
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(loginRequest));
        
        // Verificar que se registró el intento fallido
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    /// <summary>
    /// ❌ PRUEBA 4: Login fallido - Cuenta desactivada
    /// 
    /// Objetivo: Verificar que no se puede hacer login con cuenta inactiva/bloqueada
    /// Resultado esperado: UnauthorizedAccessException
    /// 
    /// Escenario real:
    /// Usuario existe en BD pero:
    /// - isActive = false (cuenta desactivada), O
    /// - IsLockedUntil > DateTime.UtcNow (cuenta bloqueada temporalmente)
    /// Sistema rechaza el login
    /// </summary>
    [Fact]
    public async Task LoginAsync_WithDeactivatedAccount_ShouldThrowUnauthorizedAccessException()
    {
        // ARRANGE: Usuario existe pero cuenta está desactivada
        var loginRequest = new LoginRequestDTO { Username = "testuser", Password = "password123" };
        var user = TestDataFixtures.CreateTestUser(username: "testuser", isActive: false);
        
        // Mock: Repositorio retorna usuario desactivado
        _mockUserRepository.Setup(r => r.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        // ACT & ASSERT: Debe lanzar excepción
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(loginRequest));
    }

    #endregion

    #region RegisterAsync Tests

    /// <summary>
    /// ✅ PRUEBA 5: Registro exitoso de nuevo usuario
    /// 
    /// Objetivo: Verificar que se puede registrar un nuevo usuario con datos válidos
    /// Resultado esperado: LoginResponseDTO con tokens + usuario creado en BD + email de bienvenida
    /// 
    /// Escenario real:
    /// Usuario proporciona: username, email, contraseña fuerte, nombre, apellido
    /// Sistema valida:
    /// - Username no existe (es único)
    /// - Email no existe (es único)
    /// - Contraseña cumple política (8+ chars, mayúsculas, números, símbolos)
    /// Sistema crea usuario en BD (hasheando contraseña)
    /// Sistema envía email de bienvenida
    /// Sistema genera JWT + Refresh Token
    /// Usuario puede hacer login inmediatamente
    /// </summary>
    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUserAndReturnLoginResponse()
    {
        // ARRANGE: Preparar datos de registro válidos
        var registerRequest = new RegisterRequestDTO
        {
            Username = "newuser",
            Email = "newuser@test.com",
            Password = "SecurePass@jkm2025",  // ✅ Contraseña válida (sin secuencias obvias)
            FirstName = "New",
            LastName = "User"
        };

        // Mock: Username NO existe
        _mockUserRepository.Setup(r => r.ExistsUsernameAsync("newuser"))
            .ReturnsAsync(false);
        
        // Mock: Email NO existe
        _mockUserRepository.Setup(r => r.ExistsEmailAsync("newuser@test.com"))
            .ReturnsAsync(false);
        
        // Mock: Password hasher genera hash
        _mockPasswordHasher.Setup(h => h.HashPassword("SecurePass@jkm2025"))
            .Returns("hashedPassword");
        
        // Mock: Usuario se guarda en BD
        _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);
        
        // Mock: Email de bienvenida se envía
        _mockEmailService.Setup(e => e.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        // Mock: Refresh token se guarda
        _mockRefreshTokenRepository.Setup(r => r.AddAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);

        // ACT: Hacer registro
        var result = await _authService.RegisterAsync(registerRequest);

        // ASSERT: Verificar respuesta completa
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();      // JWT generado
        result.RefreshToken.Should().NotBeNullOrEmpty();     // Refresh token generado
        result.User.Username.Should().Be("newuser");         // Usuario creado
        
        // Verificar que se llamó a los métodos correctos
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _mockEmailService.Verify(e => e.SendWelcomeEmailAsync("newuser@test.com", "newuser"), Times.Once);
    }

    /// <summary>
    /// ❌ PRUEBA 6: Registro fallido - Username ya existe
    /// 
    /// Objetivo: Verificar que no se puede registrar con username duplicado
    /// Resultado esperado: InvalidOperationException
    /// 
    /// Escenario real:
    /// Usuario intenta registrarse con username="existinguser" que ya existe
    /// Sistema valida: "Este username ya está en uso"
    /// Sistema rechaza el registro
    /// </summary>
    [Fact]
    public async Task RegisterAsync_WithExistingUsername_ShouldThrowInvalidOperationException()
    {
        // ARRANGE: Preparar registro con username duplicado
        var registerRequest = new RegisterRequestDTO
        {
            Username = "existinguser",
            Email = "newemail@test.com",
            Password = "SecurePass@bcd99",
            FirstName = "New",
            LastName = "User"
        };

        // Mock: Username YA existe
        _mockUserRepository.Setup(r => r.ExistsUsernameAsync("existinguser"))
            .ReturnsAsync(true);

        // ACT & ASSERT: Debe lanzar excepción
        await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(registerRequest));
        
        // Verificar que NO se creó el usuario
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    /// <summary>
    /// ❌ PRUEBA 7: Registro fallido - Email ya existe
    /// 
    /// Objetivo: Verificar que no se puede registrar con email duplicado
    /// Resultado esperado: InvalidOperationException
    /// 
    /// Escenario real:
    /// Usuario intenta registrarse con email="existing@test.com" que ya existe
    /// Sistema valida: "Este email ya está registrado"
    /// Sistema rechaza el registro
    /// </summary>
    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowInvalidOperationException()
    {
        // ARRANGE: Preparar registro con email duplicado
        var registerRequest = new RegisterRequestDTO
        {
            Username = "newuser",
            Email = "existing@test.com",
            Password = "SecurePass@bcd99",
            FirstName = "New",
            LastName = "User"
        };

        // Mock: Username NO existe
        _mockUserRepository.Setup(r => r.ExistsUsernameAsync("newuser"))
            .ReturnsAsync(false);
        
        // Mock: Email YA existe
        _mockUserRepository.Setup(r => r.ExistsEmailAsync("existing@test.com"))
            .ReturnsAsync(true);

        // ACT & ASSERT: Debe lanzar excepción
        await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(registerRequest));
    }

    #endregion

    #region RefreshTokenAsync Tests

    /// <summary>
    /// ✅ PRUEBA 8: Refrescar token válido
    /// 
    /// Objetivo: Verificar que se puede obtener nuevo access token usando refresh token válido
    /// Resultado esperado: LoginResponseDTO con NUEVOS tokens (token rotado)
    /// 
    /// Escenario real:
    /// Usuario tiene:
    /// - Access Token (próximo a expirar en 15 min)
    /// - Refresh Token válido (válido por 7 días)
    /// Usuario llama a /refresh-token
    /// Sistema valida refresh token: existe, no expiró, no revocado
    /// Sistema revoca el refresh token anterior
    /// Sistema genera NUEVO access token + NUEVO refresh token
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_WithValidRefreshToken_ShouldReturnNewTokens()
    {
        // ARRANGE: Preparar refresh token válido
        var refreshTokenRequest = new RefreshTokenRequestDTO { RefreshToken = "validToken123" };
        var refreshTokenEntity = TestDataFixtures.CreateTestRefreshToken(token: "validToken123");
        var user = TestDataFixtures.CreateTestUser(id: refreshTokenEntity.UserId);

        // Mock: Refresh token existe y es válido
        _mockRefreshTokenRepository.Setup(r => r.GetByTokenAsync("validToken123"))
            .ReturnsAsync(refreshTokenEntity);
        
        // Mock: Usuario asociado al token existe
        _mockUserRepository.Setup(r => r.GetByIdAsync(refreshTokenEntity.UserId))
            .ReturnsAsync(user);
        
        // Mock: Token anterior se revoca (actualiza)
        _mockRefreshTokenRepository.Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);
        
        // Mock: Nuevo refresh token se guarda
        _mockRefreshTokenRepository.Setup(r => r.AddAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);

        // ACT: Hacer refresh
        var result = await _authService.RefreshTokenAsync(refreshTokenRequest);

        // ASSERT: Verificar que retorna nuevos tokens
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();     // Nuevo JWT
        result.RefreshToken.Should().NotBeNullOrEmpty();    // Nuevo refresh token
        result.RefreshToken.Should().NotBe("validToken123"); // Token ROTADO (diferente)
        
        // Verificar que se revocó el anterior
        _mockRefreshTokenRepository.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>()), Times.Once);
    }

    /// <summary>
    /// ❌ PRUEBA 9: Refrescar con token inválido
    /// 
    /// Objetivo: Verificar que no se puede refrescar con token que NO existe
    /// Resultado esperado: UnauthorizedAccessException
    /// 
    /// Escenario real:
    /// Usuario intenta usar refresh token="invalidToken" que NO existe
    /// Sistema busca en BD: no encuentra nada
    /// Sistema rechaza con 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ShouldThrowUnauthorizedAccessException()
    {
        // ARRANGE: Preparar token inválido
        var refreshTokenRequest = new RefreshTokenRequestDTO { RefreshToken = "invalidToken" };
        
        // Mock: Token NO existe en BD
        _mockRefreshTokenRepository.Setup(r => r.GetByTokenAsync("invalidToken"))
            .ReturnsAsync((RefreshToken?)null);

        // ACT & ASSERT: Debe lanzar excepción
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.RefreshTokenAsync(refreshTokenRequest));
    }

    /// <summary>
    /// ❌ PRUEBA 10: Refrescar con token expirado
    /// 
    /// Objetivo: Verificar que no se puede refrescar con token que ya expiró
    /// Resultado esperado: UnauthorizedAccessException
    /// 
    /// Escenario real:
    /// Token fue creado hace 7 días + 1 hora (ExpiresAt = hace 1 hora)
    /// Sistema valida: DateTime.UtcNow > ExpiresAt
    /// Sistema rechaza: "Token expirado"
    /// Usuario debe hacer login de nuevo
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ShouldThrowUnauthorizedAccessException()
    {
        // ARRANGE: Preparar token expirado
        var refreshTokenRequest = new RefreshTokenRequestDTO { RefreshToken = "expiredToken" };
        var expiredToken = TestDataFixtures.CreateTestRefreshToken(
            token: "expiredToken",
            expiresAt: DateTime.UtcNow.AddHours(-1)  // Expiró hace 1 hora
        );

        // Mock: Token existe pero está expirado
        _mockRefreshTokenRepository.Setup(r => r.GetByTokenAsync("expiredToken"))
            .ReturnsAsync(expiredToken);

        // ACT & ASSERT: Debe lanzar excepción
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.RefreshTokenAsync(refreshTokenRequest));
    }

    #endregion

    #region ChangePasswordAsync Tests

    /// <summary>
    /// ✅ PRUEBA 11: Cambiar contraseña exitosamente
    /// 
    /// Objetivo: Verificar que se puede cambiar la contraseña verificando la actual
    /// Resultado esperado: Contraseña cambiada + email de notificación
    /// 
    /// Escenario real:
    /// Usuario autenticado proporciona:
    /// - Contraseña actual (correcta)
    /// - Nueva contraseña (fuerte, diferente a anterior)
    /// - Confirmación de nueva contraseña (igual a nueva)
    /// Sistema valida:
    /// - Contraseña actual es correcta (verifica hash)
    /// - Nueva contraseña = confirmación
    /// - Nueva contraseña cumple política
    /// Sistema hashea nueva contraseña
    /// Sistema actualiza usuario en BD
    /// Sistema envía email de notificación
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_WithValidCurrentPassword_ShouldChangePassword()
    {
        // ARRANGE: Preparar datos para cambiar contraseña
        var userId = Guid.NewGuid();
        var user = TestDataFixtures.CreateTestUser(id: userId);
        var changePasswordRequest = new ChangePasswordDTO
        {
            CurrentPassword = "currentPassword",
            NewPassword = "Secure!P9sw0Rd#2024",          // ✅ Contraseña válida (sin secuencias obvias)
            ConfirmNewPassword = "Secure!P9sw0Rd#2024"    // ✅ Confirmación coincide
        };

        // Mock: Usuario existe
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);
        
        // Mock: Contraseña actual es correcta
        _mockPasswordHasher.Setup(h => h.VerifyPassword("currentPassword", user.PasswordHash))
            .Returns(true);
        
        // Mock: Nueva contraseña se hashea
        _mockPasswordHasher.Setup(h => h.HashPassword("Secure!P9sw0Rd#2024"))
            .Returns("newHashedPassword");
        
        // Mock: Usuario se actualiza en BD
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);
        
        // Mock: Email de notificación se envía
        _mockEmailService.Setup(e => e.SendPasswordChangedNotificationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // ACT: Cambiar contraseña
        await _authService.ChangePasswordAsync(userId, changePasswordRequest);

        // ASSERT: Verificar que todo se ejecutó correctamente
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        _mockEmailService.Verify(e => e.SendPasswordChangedNotificationAsync(user.Email, user.Username), Times.Once);
    }

    /// <summary>
    /// ❌ PRUEBA 12: Cambiar contraseña con contraseña actual incorrecta
    /// 
    /// Objetivo: Verificar que no se puede cambiar contraseña sin verificar la actual
    /// Resultado esperado: UnauthorizedAccessException
    /// 
    /// Escenario real:
    /// Usuario proporciona contraseña actual INCORRECTA
    /// Sistema verifica hash: NO coincide
    /// Sistema rechaza el cambio (por seguridad)
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_WithWrongCurrentPassword_ShouldThrowUnauthorizedAccessException()
    {
        // ARRANGE: Preparar datos con contraseña actual INCORRECTA
        var userId = Guid.NewGuid();
        var user = TestDataFixtures.CreateTestUser(id: userId);
        var changePasswordRequest = new ChangePasswordDTO
        {
            CurrentPassword = "wrongPassword",             // ❌ INCORRECTA
            NewPassword = "NewSecurePass@jkl2025",
            ConfirmNewPassword = "NewSecurePass@jkl2025"
        };

        // Mock: Usuario existe
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);
        
        // Mock: Contraseña actual NO es correcta
        _mockPasswordHasher.Setup(h => h.VerifyPassword("wrongPassword", user.PasswordHash))
            .Returns(false);

        // ACT & ASSERT: Debe lanzar excepción
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.ChangePasswordAsync(userId, changePasswordRequest));
    }

    /// <summary>
    /// ❌ PRUEBA 13: Cambiar contraseña - nuevas contraseñas no coinciden
    /// 
    /// Objetivo: Verificar que las nuevas contraseñas deben ser iguales
    /// Resultado esperado: InvalidOperationException
    /// 
    /// Escenario real:
    /// Usuario ingresa:
    /// - Nueva contraseña: "NewSecurePass@xyz88"
    /// - Confirmación: "DifferentPassword@xyz88" (DIFERENTE)
    /// Sistema valida: "Las contraseñas no coinciden"
    /// Sistema rechaza el cambio
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_WithMismatchedNewPasswords_ShouldThrowInvalidOperationException()
    {
        // ARRANGE: Preparar datos con confirmación diferente
        var userId = Guid.NewGuid();
        var user = TestDataFixtures.CreateTestUser(id: userId);
        var changePasswordRequest = new ChangePasswordDTO
        {
            CurrentPassword = "currentPassword",
            NewPassword = "NewSecurePass@ijk2025",
            ConfirmNewPassword = "DifferentPassword@pqr77"  // ❌ NO coincide
        };

        // Mock: Usuario existe
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);
        
        // Mock: Contraseña actual es correcta
        _mockPasswordHasher.Setup(h => h.VerifyPassword("currentPassword", user.PasswordHash))
            .Returns(true);

        // ACT & ASSERT: Debe lanzar excepción
        await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.ChangePasswordAsync(userId, changePasswordRequest));
    }

    #endregion

    #region RevokeTokenAsync Tests

    /// <summary>
    /// ✅ PRUEBA 14: Revocar (invalidar) un refresh token específico
    /// 
    /// Objetivo: Verificar que se puede invalidar un refresh token específico
    /// Resultado esperado: Token revocado (no se puede usar más)
    /// 
    /// Escenario real:
    /// Usuario desconecta un dispositivo específico
    /// Sistema marca el refresh token como "revocado"
    /// Ese dispositivo no puede refrescar tokens más
    /// Los otros dispositivos siguen funcionando
    /// </summary>
    [Fact]
    public async Task RevokeTokenAsync_WithValidToken_ShouldRevokeToken()
    {
        // ARRANGE: Preparar token para revocar
        var token = "tokenToRevoke";
        
        // Mock: Token se revoca en BD
        _mockRefreshTokenRepository.Setup(r => r.RevokeTokenAsync(token))
            .Returns(Task.CompletedTask);

        // ACT: Revocar token
        await _authService.RevokeTokenAsync(token);

        // ASSERT: Verificar que se llamó al método de revocación
        _mockRefreshTokenRepository.Verify(r => r.RevokeTokenAsync(token), Times.Once);
    }

    #endregion

    #region RevokeAllTokensAsync Tests

    /// <summary>
    /// ✅ PRUEBA 15: Logout - Revocar TODOS los refresh tokens del usuario
    /// 
    /// Objetivo: Verificar que se pueden invalidar todos los tokens de un usuario (logout de todos lados)
    /// Resultado esperado: Todos los refresh tokens revocados
    /// 
    /// Escenario real:
    /// Usuario hace logout
    /// Sistema revoca TODOS los refresh tokens del usuario
    /// Usuario es desconectado de TODOS los dispositivos
    /// Resultado: Debe hacer login de nuevo en todos lados
    /// </summary>
    [Fact]
    public async Task RevokeAllTokensAsync_ShouldRevokeAllUserTokens()
    {
        // ARRANGE: Preparar usuario con múltiples tokens
        var userId = Guid.NewGuid();
        
        // Mock: Todos los tokens del usuario se revocan
        _mockRefreshTokenRepository.Setup(r => r.RevokeAllByUserIdAsync(userId))
            .Returns(Task.CompletedTask);

        // ACT: Revocar todos los tokens
        await _authService.RevokeAllTokensAsync(userId);

        // ASSERT: Verificar que se llamó al método de revocación
        _mockRefreshTokenRepository.Verify(r => r.RevokeAllByUserIdAsync(userId), Times.Once);
    }

    #endregion

    #region ValidateTokenAsync Tests

    /// <summary>
    /// ✅ PRUEBA 16: Validar token JWT válido
    /// 
    /// Objetivo: Verificar que se puede validar un token JWT que es válido
    /// Resultado esperado: True
    /// 
    /// Escenario real:
    /// Sistema recibe un JWT token
    /// Sistema valida:
    /// - Firma es correcta (hecho con la secret key)
    /// - No está expirado (exp claim es > DateTime.UtcNow)
    /// - Issuer es correcto
    /// - Audience es correcto
    /// Sistema retorna True
    /// </summary>
    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ShouldReturnTrue()
    {
        // ARRANGE: Preparar token JWT válido
        var validToken = _authService.GenerateJwtToken(TestDataFixtures.CreateTestUser());

        // ACT: Validar token
        var result = await _authService.ValidateTokenAsync(validToken);

        // ASSERT: Token debe ser válido
        result.Should().BeTrue();
    }

    /// <summary>
    /// ❌ PRUEBA 17: Validar token inválido
    /// 
    /// Objetivo: Verificar que se detecta cuando un token es inválido
    /// Resultado esperado: False
    /// 
    /// Escenario real:
    /// Sistema recibe string que NO es un JWT válido
    /// Sistema intenta validar pero falla (formato incorrecto, firma inválida, expirado, etc.)
    /// Sistema retorna False
    /// </summary>
    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // ARRANGE: Preparar token inválido (string random)
        var invalidToken = "invalidTokenString";

        // ACT: Intentar validar
        var result = await _authService.ValidateTokenAsync(invalidToken);

        // ASSERT: Token debe ser inválido
        result.Should().BeFalse();
    }

    #endregion

    #region GenerateJwtToken Tests

    /// <summary>
    /// ✅ PRUEBA 18: Generar JWT token válido
    /// 
    /// Objetivo: Verificar que se genera un JWT token válido con los claims correctos
    /// Resultado esperado: JWT válido con claims del usuario
    /// 
    /// Escenario real:
    /// Sistema recibe un usuario
    /// Sistema genera JWT con:
    /// - Firma (HS256 con SecretKey)
    /// - Claims: NameIdentifier (UserId), Name (Username), Email
    /// - Expiration: DateTime.UtcNow + 15 minutos
    /// - Issuer y Audience configurados
    /// Sistema retorna el token
    /// </summary>
    [Fact]
    public void GenerateJwtToken_ShouldGenerateValidToken()
    {
        // ARRANGE: Preparar usuario
        var user = TestDataFixtures.CreateTestUser();

        // ACT: Generar token JWT
        var token = _authService.GenerateJwtToken(user);

        // ASSERT: Verificar que es un JWT válido
        token.Should().NotBeNullOrEmpty();
        
        // Decodificar y validar claims
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
        jwtToken.Should().NotBeNull();
        
        // Verificar claims importantes
        jwtToken?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            .Should().Be(user.Id.ToString());
        jwtToken?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value
            .Should().Be(user.Username);
    }

    #endregion

    #region GenerateRefreshToken Tests

    /// <summary>
    /// ✅ PRUEBA 19: Generar refresh token único
    /// 
    /// Objetivo: Verificar que se generan tokens únicos cada vez (no repetidos)
    /// Resultado esperado: Dos tokens diferentes
    /// 
    /// Escenario real:
    /// Sistema genera refresh token #1
    /// Sistema genera refresh token #2
    /// Resultado: token1 ≠ token2 (son únicos)
    /// Razón: Usa RandomNumberGenerator para garantizar unicidad
    /// </summary>
    [Fact]
    public void GenerateRefreshToken_ShouldGenerateUniqueTokens()
    {
        // ACT: Generar dos tokens
        var token1 = _authService.GenerateRefreshToken();
        var token2 = _authService.GenerateRefreshToken();

        // ASSERT: Verificar que ambos son válidos y diferentes
        token1.Should().NotBeNullOrEmpty();
        token2.Should().NotBeNullOrEmpty();
        token1.Should().NotBe(token2);  // ← Deben ser DIFERENTES
    }

    #endregion

    #region RequestPasswordResetAsync Tests

    /// <summary>
    /// ✅ PRUEBA 20: Solicitar reset de contraseña con email válido
    /// 
    /// Objetivo: Verificar que se genera token de reset y se envía por email
    /// Resultado esperado: Email de reset enviado + token guardado en BD
    /// 
    /// Escenario real (Password Recovery):
    /// 1. Usuario dice "Olvidé mi contraseña"
    /// 2. Usuario ingresa email
    /// 3. Sistema busca usuario por email
    /// 4. Sistema genera token de reset (válido 1 hora)
    /// 5. Sistema guarda token en BD
    /// 6. Sistema envía email con link: https://miapp.com/reset?token=ABC123
    /// 7. Usuario recibe email y hace click
    /// 
    /// Esta prueba es pasos 3-6
    /// </summary>
    [Fact]
    public async Task RequestPasswordResetAsync_WithValidEmail_ShouldSendPasswordResetEmail()
    {
        // ARRANGE: Preparar email válido
        var email = "test@test.com";
        var user = TestDataFixtures.CreateTestUser(email: email);
        
        // Mock: Usuario existe con ese email
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(user);
        
        // Mock: Token de reset se guarda en BD
        _mockPasswordResetTokenRepository.Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>()))
            .Returns(Task.CompletedTask);
        
        // Mock: Email se envía
        _mockEmailService.Setup(e => e.SendPasswordResetEmailAsync(email, user.Username, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // ACT: Solicitar reset
        await _authService.RequestPasswordResetAsync(email);

        // ASSERT: Verificar que se guardó el token y se envió email
        _mockPasswordResetTokenRepository.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>()), Times.Once);
        _mockEmailService.Verify(e => e.SendPasswordResetEmailAsync(email, user.Username, It.IsAny<string>()), Times.Once);
    }

    /// <summary>
    /// ✅ PRUEBA 21: Solicitar reset con email no registrado
    /// 
    /// Objetivo: Verificar que NO se envía email si el usuario no existe (seguridad)
    /// Resultado esperado: No se envía email (por razones de seguridad)
    /// 
    /// Escenario real:
    /// Atacante intenta averiguar qué emails están registrados
    /// Intenta: forgot-password con email="nonexistent@test.com"
    /// Sistema busca: no encuentra usuario
    /// Sistema retorna: OK (pero NO envía email)
    /// Razón: No debes revelar qué emails están registrados (information disclosure)
    /// </summary>
    [Fact]
    public async Task RequestPasswordResetAsync_WithNonExistentEmail_ShouldNotSendEmail()
    {
        // ARRANGE: Preparar email que NO existe
        var email = "nonexistent@test.com";
        
        // Mock: Usuario NO existe
        _mockUserRepository.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync((User?)null);

        // ACT: Solicitar reset
        await _authService.RequestPasswordResetAsync(email);

        // ASSERT: Verificar que NO se envió email
        _mockEmailService.Verify(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region ResetPasswordAsync Tests

    /// <summary>
    /// ✅ PRUEBA 22: Reset de contraseña con token válido
    /// 
    /// Objetivo: Verificar que se cambia la contraseña usando token de reset válido
    /// Resultado esperado: Contraseña cambiada + token marcado como "usado"
    /// 
    /// Escenario real (continuación de Password Recovery):
    /// 1. Usuario recibió email con token (del RequestPasswordReset anterior)
    /// 2. Usuario hace click en link: /reset-password?token=ABC123
    /// 3. Usuario ve formulario de reset
    /// 4. Usuario ingresa nueva contraseña 2 veces
    /// 5. Sistema valida:
    ///    - Token existe
    ///    - Token NO está expirado
    ///    - Token NO fue usado antes
    ///    - Nueva contraseña cumple política
    ///    - Confirmación coincide
    /// 6. Sistema hashea y actualiza contraseña
    /// 7. Sistema marca token como "usado" (no se puede usar de nuevo)
    /// 8. Usuario puede hacer login con nueva contraseña
    /// 
    /// Esta prueba es pasos 5-8
    /// </summary>
    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ShouldResetPassword()
    {
        // ARRANGE: Preparar datos para reset con token VÁLIDO
        var userId = Guid.NewGuid();
        var resetToken = TestDataFixtures.CreateTestPasswordResetToken(userId: userId, token: "validResetToken");
        var user = TestDataFixtures.CreateTestUser(id: userId);
        var resetPasswordRequest = new ResetPasswordDTO
        {
            Token = "validResetToken",
            NewPassword = "MyP@ssw0rd#Secure2024",           // ✅ Válida
            ConfirmNewPassword = "MyP@ssw0rd#Secure2024"     // ✅ Coincide
        };

        // Mock: Token existe y es válido
        _mockPasswordResetTokenRepository.Setup(r => r.GetByTokenAsync("validResetToken"))
            .ReturnsAsync(resetToken);
        
        // Mock: Usuario asociado al token existe
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);
        
        // Mock: Nueva contraseña se hashea
        _mockPasswordHasher.Setup(h => h.HashPassword("MyP@ssw0rd#Secure2024"))
            .Returns("newHashedPassword");
        
        // Mock: Usuario se actualiza en BD
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);
        
        // Mock: Token se marca como usado
        _mockPasswordResetTokenRepository.Setup(r => r.MarkAsUsedAsync(resetToken.Id))
            .Returns(Task.CompletedTask);

        // ACT: Hacer reset
        await _authService.ResetPasswordAsync(resetPasswordRequest);

        // ASSERT: Verificar que se actualizó y token se marcó
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        _mockPasswordResetTokenRepository.Verify(r => r.MarkAsUsedAsync(resetToken.Id), Times.Once);
    }

    /// <summary>
    /// ❌ PRUEBA 23: Reset con token expirado
    /// 
    /// Objetivo: Verificar que no se puede usar token expirado
    /// Resultado esperado: InvalidOperationException
    /// 
    /// Escenario real:
    /// Usuario recibió email con token hace 2 horas
    /// Token era válido por 1 hora
    /// Usuario intenta usar el token (ya expiró)
    /// Sistema valida: DateTime.UtcNow > ExpiresAt
    /// Sistema rechaza: "Token expirado"
    /// Usuario debe solicitar nuevo reset
    /// </summary>
    [Fact]
    public async Task ResetPasswordAsync_WithExpiredToken_ShouldThrowInvalidOperationException()
    {
        // ARRANGE: Preparar token EXPIRADO
        var resetToken = TestDataFixtures.CreateTestPasswordResetToken(
            token: "expiredToken",
            expiresAt: DateTime.UtcNow.AddHours(-1)  // Expiró hace 1 hora
        );
        var resetPasswordRequest = new ResetPasswordDTO
        {
            Token = "expiredToken",
            NewPassword = "NewSecurePass@xyz2025",
            ConfirmNewPassword = "NewSecurePass@xyz2025"
        };

        // Mock: Token existe pero está expirado
        _mockPasswordResetTokenRepository.Setup(r => r.GetByTokenAsync("expiredToken"))
            .ReturnsAsync(resetToken);

        // ACT & ASSERT: Debe lanzar excepción
        await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.ResetPasswordAsync(resetPasswordRequest));
    }

    /// <summary>
    /// ❌ PRUEBA 24: Reset con token ya usado
    /// 
    /// Objetivo: Verificar que no se puede usar un token dos veces
    /// Resultado esperado: InvalidOperationException
    /// 
    /// Escenario real:
    /// Usuario recibió email con token
    /// Usuario lo usó una vez y cambió su contraseña
    /// Token se marcó como "isUsed = true"
    /// Atacante intenta usar la MISMA token para cambiar la contraseña de nuevo
    /// Sistema valida: "Este token ya fue usado"
    /// Sistema rechaza el cambio
    /// </summary>
    [Fact]
    public async Task ResetPasswordAsync_WithAlreadyUsedToken_ShouldThrowInvalidOperationException()
    {
        // ARRANGE: Preparar token YA USADO
        var resetToken = TestDataFixtures.CreateTestPasswordResetToken(token: "usedToken", isUsed: true);
        var resetPasswordRequest = new ResetPasswordDTO
        {
            Token = "usedToken",
            NewPassword = "NewSecurePass@pqr2025",
            ConfirmNewPassword = "NewSecurePass@pqr2025"
        };

        // Mock: Token existe pero ya fue usado
        _mockPasswordResetTokenRepository.Setup(r => r.GetByTokenAsync("usedToken"))
            .ReturnsAsync(resetToken);

        // ACT & ASSERT: Debe lanzar excepción
        await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.ResetPasswordAsync(resetPasswordRequest));
    }

    #endregion

    #region GetUserFromTokenAsync Tests

    /// <summary>
    /// ✅ PRUEBA 25: Obtener usuario desde token JWT válido
    /// 
    /// Objetivo: Verificar que se puede extraer el usuario desde un JWT válido
    /// Resultado esperado: Usuario encontrado con datos correctos
    /// 
    /// Escenario real:
    /// Sistema recibe JWT token en header Authorization
    /// Sistema decodifica el token y extrae UserId
    /// Sistema busca usuario por ID en BD
    /// Sistema retorna el usuario
    /// </summary>
    [Fact]
    public async Task GetUserFromTokenAsync_WithValidToken_ShouldReturnUser()
    {
        // ARRANGE: Preparar usuario y generar token válido
        var user = TestDataFixtures.CreateTestUser();
        var validToken = _authService.GenerateJwtToken(user);

        // Mock: Usuario existe en BD
        _mockUserRepository.Setup(r => r.GetByIdAsync(user.Id))
            .ReturnsAsync(user);

        // ACT: Obtener usuario desde token
        var result = await _authService.GetUserFromTokenAsync(validToken);

        // ASSERT: Verificar que se obtuvo el usuario correcto
        result.Should().NotBeNull();
        result?.Id.Should().Be(user.Id);
        result?.Username.Should().Be(user.Username);
        result?.Email.Should().Be(user.Email);
    }

    /// <summary>
    /// ❌ PRUEBA 26: Obtener usuario desde token inválido
    /// 
    /// Objetivo: Verificar que retorna null con token inválido
    /// Resultado esperado: null
    /// 
    /// Escenario real:
    /// Sistema recibe token corrupto/inválido
    /// Sistema intenta decodificar pero falla
    /// Sistema retorna null (rechaza request)
    /// </summary>
    [Fact]
    public async Task GetUserFromTokenAsync_WithInvalidToken_ShouldReturnNull()
    {
        // ARRANGE: Preparar token inválido
        var invalidToken = "invalidTokenString123";

        // ACT: Intentar obtener usuario
        var result = await _authService.GetUserFromTokenAsync(invalidToken);

        // ASSERT: Debe retornar null
        result.Should().BeNull();
    }

    #endregion

    #region UpdateUserProfileAsync Tests

    /// <summary>
    /// ✅ PRUEBA 27: Actualizar perfil de usuario exitosamente
    /// 
    /// Objetivo: Verificar que se puede actualizar email, nombre y apellido
    /// Resultado esperado: Usuario actualizado + email de notificación
    /// 
    /// Escenario real:
    /// Usuario autenticado cambia su email/nombre
    /// Sistema valida los datos
    /// Sistema actualiza en BD
    /// Sistema envía email de notificación al nuevo email
    /// </summary>
    [Fact]
    public async Task UpdateUserProfileAsync_WithValidData_ShouldUpdateUser()
    {
        // ARRANGE: Preparar datos de actualización
        var userId = Guid.NewGuid();
        var user = TestDataFixtures.CreateTestUser(id: userId);
        var updateDto = new UpdateUserProfileDTO
        {
            Email = "newemail@test.com",
            FirstName = "UpdatedFirst",
            LastName = "UpdatedLast"
        };

        // Mock: Usuario existe
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);
        
        // Mock: Usuario se actualiza
        _mockUserRepository.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);
        
        // Mock: Email de notificación se envía
        _mockEmailService.Setup(e => e.SendProfileUpdatedNotificationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // ACT: Actualizar perfil
        await _authService.UpdateUserProfileAsync(userId, updateDto);

        // ASSERT: Verificar que se actualizó y se envió email
        _mockUserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        _mockEmailService.Verify(e => e.SendProfileUpdatedNotificationAsync(updateDto.Email, user.Username), Times.Once);
    }

    /// <summary>
    /// ❌ PRUEBA 28: Actualizar perfil de usuario no existente
    /// 
    /// Objetivo: Verificar que se rechaza actualización si usuario no existe
    /// Resultado esperado: InvalidOperationException
    /// 
    /// Escenario real:
    /// Sistema intenta actualizar usuario que fue eliminado
    /// Sistema no encuentra el usuario en BD
    /// Sistema lanza excepción
    /// </summary>
    [Fact]
    public async Task UpdateUserProfileAsync_WithNonExistentUser_ShouldThrowException()
    {
        // ARRANGE: Preparar datos para usuario inexistente
        var userId = Guid.NewGuid();
        var updateDto = new UpdateUserProfileDTO
        {
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "User"
        };

        // Mock: Usuario NO existe
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // ACT & ASSERT: Debe lanzar excepción
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _authService.UpdateUserProfileAsync(userId, updateDto));
    }

    #endregion

    #region DeleteUserAsync Tests

    /// <summary>
    /// ✅ PRUEBA 29: Eliminar usuario exitosamente
    /// 
    /// Objetivo: Verificar que se puede eliminar cuenta de usuario
    /// Resultado esperado: Usuario eliminado + email de notificación
    /// 
    /// Escenario real:
    /// Usuario solicita eliminar su cuenta (GDPR compliance)
    /// Sistema elimina todos sus datos de BD
    /// Sistema envía email de confirmación
    /// Usuario ya no puede hacer login
    /// </summary>
    [Fact]
    public async Task DeleteUserAsync_WithValidUser_ShouldDeleteUser()
    {
        // ARRANGE: Preparar usuario a eliminar
        var userId = Guid.NewGuid();
        var user = TestDataFixtures.CreateTestUser(id: userId);

        // Mock: Usuario existe
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);
        
        // Mock: Usuario se elimina
        _mockUserRepository.Setup(r => r.DeleteAsync(userId))
            .Returns(Task.CompletedTask);
        
        // Mock: Email de confirmación se envía
        _mockEmailService.Setup(e => e.SendAccountDeletedNotificationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // ACT: Eliminar usuario
        await _authService.DeleteUserAsync(userId);

        // ASSERT: Verificar que se eliminó y se envió email
        _mockUserRepository.Verify(r => r.DeleteAsync(userId), Times.Once);
        _mockEmailService.Verify(e => e.SendAccountDeletedNotificationAsync(user.Email, user.Username), Times.Once);
    }

    /// <summary>
    /// ❌ PRUEBA 30: Eliminar usuario no existente
    /// 
    /// Objetivo: Verificar que se rechaza eliminación si usuario no existe
    /// Resultado esperado: InvalidOperationException
    /// 
    /// Escenario real:
    /// Sistema intenta eliminar usuario que ya fue eliminado
    /// Sistema no encuentra el usuario en BD
    /// Sistema lanza excepción
    /// </summary>
    [Fact]
    public async Task DeleteUserAsync_WithNonExistentUser_ShouldThrowException()
    {
        // ARRANGE: Preparar ID de usuario inexistente
        var userId = Guid.NewGuid();

        // Mock: Usuario NO existe
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // ACT & ASSERT: Debe lanzar excepción
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _authService.DeleteUserAsync(userId));
    }

    #endregion

    #region RevokeAccessTokenAsync Tests

    /// <summary>
    /// ✅ PRUEBA 31: Revocar access token exitosamente (Logout con blacklist)
    /// 
    /// Objetivo: Verificar que se puede revocar un access token agregándolo a blacklist
    /// Resultado esperado: Token agregado a blacklist + no se puede usar más
    /// 
    /// Escenario real:
    /// Usuario hace logout
    /// Sistema toma el JWT del header Authorization
    /// Sistema hashea el token (SHA256)
    /// Sistema extrae la expiración del JWT
    /// Sistema guarda en TokenBlacklist (userId, tokenHash, expiresAt, reason)
    /// Próxima request con ese token: middleware lo rechaza (401)
    /// </summary>
    [Fact]
    public async Task RevokeAccessTokenAsync_WithValidToken_ShouldAddToBlacklist()
    {
        // ARRANGE: Preparar usuario y token válido
        var userId = Guid.NewGuid();
        var user = TestDataFixtures.CreateTestUser(id: userId);
        var token = _authService.GenerateJwtToken(user);

        // Mock: Token se agrega a blacklist
        _mockTokenBlacklistRepository.Setup(r => r.AddTokenAsync(
            It.IsAny<Guid>(), 
            It.IsAny<string>(), 
            It.IsAny<DateTime>(), 
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // ACT: Revocar el access token
        await _authService.RevokeAccessTokenAsync(userId, token);

        // ASSERT: Verificar que se agregó a blacklist con los datos correctos
        _mockTokenBlacklistRepository.Verify(r => r.AddTokenAsync(
            userId, 
            It.IsAny<string>(),  // tokenHash (hasheado con SHA256)
            It.IsAny<DateTime>(), // expiresAt (extraído del JWT)
            "access_token_revocation"), Times.Once);
    }

    #endregion
}
