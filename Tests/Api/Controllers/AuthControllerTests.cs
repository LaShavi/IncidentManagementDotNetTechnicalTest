using Api.Controllers;
using Api.DTOs.Common;
using Application.DTOs.Auth;
using Application.Ports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Tests.Api.Controllers;

/// <summary>
/// Test suite para AuthController
/// Prueba todos los endpoints de autenticación: login, register, refresh token, logout, etc.
/// Utiliza Moq para simular el AuthService y validar que el controller retorna las respuestas HTTP correctas
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly AuthController _controller;

    /// <summary>
    /// Constructor: Inicializa los mocks y el controller
    /// Se ejecuta ANTES de cada prueba [Fact]
    /// </summary>
    public AuthControllerTests()
    {
        // Crear mocks (simuladores) del AuthService y Logger
        _mockAuthService = new Mock<IAuthService>();
        _mockLogger = new Mock<ILogger<AuthController>>();
        
        // Crear instancia del controller con el mock del servicio
        _controller = new AuthController(_mockAuthService.Object)
        {
            // Configurar el contexto HTTP (simula una request HTTP real)
            ControllerContext = new ControllerContext()
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            }
        };
    }

    #region Login Tests

    /// <summary>
    /// ✅ PRUEBA 1: Login exitoso con credenciales válidas
    /// 
    /// Objetivo: Verificar que un usuario puede hacer login con username y contraseña correctos
    /// Resultado esperado: HTTP 200 (OK) + JWT token + datos del usuario
    /// 
    /// Escenario real:
    /// Usuario ingresa: username="testuser", password="password123"
    /// Sistema retorna: AccessToken + RefreshToken + UserInfo
    /// </summary>
    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOkWithLoginResponse()
    {
        // ARRANGE (Preparación): Configurar los datos de entrada y el comportamiento esperado
        var loginRequest = new LoginRequestDTO { Username = "testuser", Password = "password123" };
        var loginResponse = new LoginResponseDTO
        {
            AccessToken = "accessToken123",
            RefreshToken = "refreshToken123",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            User = new UserInfoDTO
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@test.com",
                FirstName = "Test",
                LastName = "User",
                Role = "User"
            }
        };

        // Configurar el mock: "Si se llama a LoginAsync con este request, retorna esta respuesta"
        _mockAuthService.Setup(s => s.LoginAsync(loginRequest))
            .ReturnsAsync(loginResponse);

        // ACT (Ejecución): Llamar al método que queremos probar
        var result = await _controller.Login(loginRequest);

        // ASSERT (Validación): Verificar que el resultado es lo que esperábamos
        // 1. El result.Result debe ser OkObjectResult (HTTP 200)
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        okResult.StatusCode.Should().Be(200);
        
        // 2. El valor retornado debe ser una ApiResponse<LoginResponseDTO>
        var response = okResult.Value as ApiResponse<LoginResponseDTO>;
        response.Should().NotBeNull();
        response?.Data.Should().Be(loginResponse);
        
        // 3. Verificar que se llamó exactamente 1 vez al LoginAsync
        _mockAuthService.Verify(s => s.LoginAsync(loginRequest), Times.Once);
    }

    /// <summary>
    /// ❌ PRUEBA 2: Login fallido con credenciales inválidas
    /// 
    /// Objetivo: Verificar que el login es rechazado con contraseña incorrecta
    /// Resultado esperado: HTTP 401 (Unauthorized) + Sin token
    /// 
    /// Escenario real:
    /// Usuario ingresa: username="testuser", password="wrongpassword" (INCORRECTO)
    /// Sistema retorna: Error 401 Unauthorized
    /// Usuario NO recibe token
    /// </summary>
    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // ARRANGE: Preparar un request con contraseña incorrecta
        var loginRequest = new LoginRequestDTO { Username = "testuser", Password = "wrongpassword" };

        // Configurar el mock para lanzar excepción de unauthorized
        _mockAuthService.Setup(s => s.LoginAsync(loginRequest))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

        // ACT: Intentar hacer login
        var result = await _controller.Login(loginRequest);

        // ASSERT: Verificar que rechaza la request
        // El result.Result debe ser UnauthorizedObjectResult (HTTP 401)
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        unauthorizedResult.StatusCode.Should().Be(401);
        // No se retorna token
    }

    #endregion

    #region Register Tests

    /// <summary>
    /// ✅ PRUEBA 3: Registro exitoso de nuevo usuario
    /// 
    /// Objetivo: Verificar que un nuevo usuario puede registrarse exitosamente
    /// Resultado esperado: HTTP 201 (Created) + JWT token + datos del nuevo usuario
    /// 
    /// Escenario real:
    /// Usuario completa el formulario de registro con todos los datos
    /// Sistema valida los datos
    /// Sistema crea el usuario en BD
    /// Sistema retorna tokens para que inicie sesión automáticamente
    /// </summary>
    [Fact]
    public async Task Register_WithValidData_ShouldReturnCreatedWithLoginResponse()
    {
        // ARRANGE: Preparar datos de registro válidos
        var registerRequest = new RegisterRequestDTO
        {
            Username = "newuser",
            Email = "newuser@test.com",
            Password = "SecurePassword123!",
            FirstName = "New",
            LastName = "User"
        };

        // Simular la respuesta exitosa del servidor
        var loginResponse = new LoginResponseDTO
        {
            AccessToken = "accessToken123",
            RefreshToken = "refreshToken123",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            User = new UserInfoDTO
            {
                Id = Guid.NewGuid(),
                Username = "newuser",
                Email = "newuser@test.com",
                FirstName = "New",
                LastName = "User",
                Role = "User"
            }
        };

        // Configurar el mock para registro exitoso
        _mockAuthService.Setup(s => s.RegisterAsync(registerRequest))
            .ReturnsAsync(loginResponse);

        // ACT: Hacer el registro
        var result = await _controller.Register(registerRequest);

        // ASSERT: Verificar que se creó correctamente
        // HTTP 201 (Created) - Recurso creado exitosamente
        var createdResult = Assert.IsType<ObjectResult>(result.Result);
        createdResult.StatusCode.Should().Be(201);
        
        // Verificar que se llamó al RegisterAsync
        _mockAuthService.Verify(s => s.RegisterAsync(registerRequest), Times.Once);
    }

    /// <summary>
    /// ❌ PRUEBA 4: Registro fallido - Username ya existe
    /// 
    /// Objetivo: Verificar que no se pueden registrar usuarios con username duplicado
    /// Resultado esperado: HTTP 400 (Bad Request) + Error
    /// 
    /// Escenario real:
    /// Usuario intenta registrarse con username="existinguser" que ya existe
    /// Sistema valida en BD: "Este username ya está usado"
    /// Sistema rechaza el registro
    /// </summary>
    [Fact]
    public async Task Register_WithExistingUsername_ShouldReturnBadRequest()
    {
        // ARRANGE: Preparar datos de registro con username que ya existe
        var registerRequest = new RegisterRequestDTO
        {
            Username = "existinguser",  // ← Este username ya existe en la BD
            Email = "newemail@test.com",
            Password = "SecurePassword123!",
            FirstName = "New",
            LastName = "User"
        };

        // Configurar el mock para rechazar por username duplicado
        _mockAuthService.Setup(s => s.RegisterAsync(registerRequest))
            .ThrowsAsync(new InvalidOperationException("Username already exists"));

        // ACT: Intentar hacer el registro
        var result = await _controller.Register(registerRequest);

        // ASSERT: Verificar que rechaza la request
        // HTTP 400 (Bad Request) - La solicitud es inválida
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        badRequestResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region RefreshToken Tests

    /// <summary>
    /// ✅ PRUEBA 5: Refrescar token válido
    /// 
    /// Objetivo: Verificar que se puede obtener un nuevo access token usando el refresh token
    /// Resultado esperado: HTTP 200 (OK) + Nuevo AccessToken + Nuevo RefreshToken
    /// 
    /// Escenario real:
    /// Usuario tiene: AccessToken (expirado en 15 min) + RefreshToken (válido por 7 días)
    /// AccessToken se acerca a expirar
    /// Usuario llama a /refresh-token con su RefreshToken
    /// Sistema valida el RefreshToken
    /// Sistema genera nuevo AccessToken + nuevo RefreshToken
    /// El antiguo RefreshToken se revoca automáticamente
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithValidRefreshToken_ShouldReturnOkWithNewTokens()
    {
        // ARRANGE: Preparar un refresh token válido
        var refreshRequest = new RefreshTokenRequestDTO { RefreshToken = "validToken123" };
        
        // Simular la respuesta: Nuevos tokens
        var loginResponse = new LoginResponseDTO
        {
            AccessToken = "newAccessToken123",     // ← NUEVO token de acceso
            RefreshToken = "newRefreshToken123",   // ← NUEVO refresh token
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            User = new UserInfoDTO
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@test.com",
                FirstName = "Test",
                LastName = "User",
                Role = "User"
            }
        };

        // Configurar el mock para refresh exitoso
        _mockAuthService.Setup(s => s.RefreshTokenAsync(refreshRequest))
            .ReturnsAsync(loginResponse);

        // ACT: Hacer el refresh
        var result = await _controller.RefreshToken(refreshRequest);

        // ASSERT: Verificar que retorna nuevos tokens
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        okResult.StatusCode.Should().Be(200);
        
        // Verificar que se llamó al RefreshTokenAsync
        _mockAuthService.Verify(s => s.RefreshTokenAsync(refreshRequest), Times.Once);
    }

    /// <summary>
    /// ❌ PRUEBA 6: Refrescar con token inválido o expirado
    /// 
    /// Objetivo: Verificar que no se puede refrescar con un token inválido/expirado
    /// Resultado esperado: HTTP 401 (Unauthorized)
    /// 
    /// Escenario real:
    /// Usuario intenta usar un RefreshToken expirado o inválido
    /// Sistema valida: "Este refresh token no es válido o ya expiró"
    /// Sistema rechaza y retorna 401
    /// Usuario debe hacer login de nuevo
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // ARRANGE: Preparar un refresh token inválido
        var refreshRequest = new RefreshTokenRequestDTO { RefreshToken = "invalidToken" };

        // Configurar el mock para rechazar el token inválido
        _mockAuthService.Setup(s => s.RefreshTokenAsync(refreshRequest))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid or expired token"));

        // ACT: Intentar hacer refresh con token inválido
        var result = await _controller.RefreshToken(refreshRequest);

        // ASSERT: Verificar que rechaza
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    #endregion

    #region RevokeToken Tests

    /// <summary>
    /// ✅ PRUEBA 7: Revocar (invalidar) un refresh token específico
    /// 
    /// Objetivo: Verificar que se puede invalidar un refresh token específico
    /// Resultado esperado: HTTP 200 (OK)
    /// 
    /// Escenario real:
    /// Usuario tiene múltiples dispositivos con diferentes refresh tokens
    /// Usuario desconecta un dispositivo específico
    /// Sistema revoca solo ese refresh token
    /// Ese dispositivo no puede usar más ese token
    /// Los otros dispositivos siguen siendo válidos
    /// </summary>
    [Fact]
    public async Task RevokeToken_WithValidToken_ShouldReturnOk()
    {
        // ARRANGE: Preparar un token para revocar
        var revokeRequest = new RefreshTokenRequestDTO { RefreshToken = "tokenToRevoke" };

        // Configurar el mock para revocación exitosa
        _mockAuthService.Setup(s => s.RevokeTokenAsync("tokenToRevoke"))
            .Returns(Task.CompletedTask);

        // ACT: Revocar el token
        var result = await _controller.RevokeRefreshToken(revokeRequest);

        // ASSERT: Verificar que se revocó exitosamente
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        okResult.StatusCode.Should().Be(200);
        
        // Verificar que se llamó al RevokeTokenAsync
        _mockAuthService.Verify(s => s.RevokeTokenAsync("tokenToRevoke"), Times.Once);
    }

    #endregion

    #region RevokeAllRefreshToken Tests

    /// <summary>
    /// ✅ PRUEBA 8: RevokeAllRefreshToken - Revocar TODOS los refresh tokens del usuario
    /// 
    /// Objetivo: Verificar que RevokeAllRefreshToken invalida todos los dispositivos del usuario
    /// Resultado esperado: HTTP 200 (OK)
    /// 
    /// Escenario real:
    /// Usuario está autenticado en: PC, Celular, Tablet
    /// Usuario hace RevokeAllRefreshToken
    /// Sistema revoca TODOS los refresh tokens
    /// Resultado: Usuario debe hacer login en TODOS los dispositivos
    /// </summary>
    [Fact]
    public async Task RevokeAllRefreshToken_WithValidUser_ShouldRevokeAllTokens()
    {
        // ARRANGE: Preparar el usuario autenticado
        var userId = Guid.NewGuid();
        
        // Crear claims (datos del usuario autenticado) - simula un JWT decodificado
        var claims = new List<Claim>
        {
            // El ClaimTypes.NameIdentifier contiene el userId del JWT
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var claimsIdentity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Asignar el usuario autenticado al controller
        _controller.ControllerContext.HttpContext.User = claimsPrincipal;

        // Configurar el mock para revocación de todos los tokens
        _mockAuthService.Setup(s => s.RevokeAllTokensAsync(userId))
            .Returns(Task.CompletedTask);

        // ACT: Hacer RevokeAllRefreshToken
        var result = await _controller.RevokeAllRefreshToken();

        // ASSERT: Verificar que se revocaron todos los tokens
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        okResult.StatusCode.Should().Be(200);
        
        // Verificar que se pasó el userId correcto
        _mockAuthService.Verify(s => s.RevokeAllTokensAsync(userId), Times.Once);
    }

    #endregion

    #region ChangePassword Tests

    /// <summary>
    /// ✅ PRUEBA 9: Cambiar contraseña exitosamente
    /// 
    /// Objetivo: Verificar que un usuario puede cambiar su contraseña
    /// Resultado esperado: HTTP 200 (OK)
    /// 
    /// Escenario real:
    /// Usuario autenticado ingresa:
    /// - Contraseña actual (correcta)
    /// - Nueva contraseña (fuerte)
    /// - Confirmación de nueva contraseña
    /// Sistema valida:
    /// - Contraseña actual es correcta
    /// - Nueva contraseña = confirmación
    /// - Nueva contraseña cumple política
    /// Sistema cambia la contraseña
    /// Sistema envía email de notificación
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithValidData_ShouldReturnOk()
    {
        // ARRANGE: Preparar datos para cambiar contraseña
        var userId = Guid.NewGuid();
        var changePasswordRequest = new ChangePasswordDTO
        {
            CurrentPassword = "currentPassword",           // ← Contraseña actual (correcta)
            NewPassword = "NewSecurePassword123!",         // ← Nueva contraseña fuerte
            ConfirmNewPassword = "NewSecurePassword123!"   // ← Confirmación (igual a nueva)
        };

        // Crear claims del usuario autenticado
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var claimsIdentity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        _controller.ControllerContext.HttpContext.User = claimsPrincipal;

        // Configurar el mock para cambio de contraseña exitoso
        _mockAuthService.Setup(s => s.ChangePasswordAsync(userId, changePasswordRequest))
            .Returns(Task.CompletedTask);

        // ACT: Cambiar la contraseña
        var result = await _controller.ChangePassword(changePasswordRequest);

        // ASSERT: Verificar que se cambió exitosamente
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        okResult.StatusCode.Should().Be(200);
        
        // Verificar que se llamó con los parámetros correctos
        _mockAuthService.Verify(s => s.ChangePasswordAsync(userId, changePasswordRequest), Times.Once);
    }

    /// <summary>
    /// ❌ PRUEBA 10: Cambiar contraseña con contraseña actual incorrecta
    /// 
    /// Objetivo: Verificar que no se puede cambiar contraseña sin verificar la actual
    /// Resultado esperado: Error (401 o 400)
    /// 
    /// Escenario real:
    /// Usuario intenta cambiar contraseña pero ingresa contraseña actual incorrecta
    /// Sistema valida: "La contraseña actual que ingresaste es incorrecta"
    /// Sistema rechaza el cambio
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ShouldReturnBadRequest()
    {
        // ARRANGE: Preparar datos con contraseña actual INCORRECTA
        var userId = Guid.NewGuid();
        var changePasswordRequest = new ChangePasswordDTO
        {
            CurrentPassword = "wrongPassword",             // ← INCORRECTA
            NewPassword = "NewSecurePassword123!",
            ConfirmNewPassword = "NewSecurePassword123!"
        };

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var claimsIdentity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        _controller.ControllerContext.HttpContext.User = claimsPrincipal;

        // Configurar el mock para rechazar por contraseña actual incorrecta
        _mockAuthService.Setup(s => s.ChangePasswordAsync(userId, changePasswordRequest))
            .ThrowsAsync(new UnauthorizedAccessException("Current password incorrect"));

        // ACT: Intentar cambiar con contraseña incorrecta
        var result = await _controller.ChangePassword(changePasswordRequest);

        // ASSERT: Verificar que rechaza la solicitud
        // Debe retornar BadRequest (HTTP 400) porque la contraseña actual es incorrecta
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        badRequestResult.StatusCode.Should().Be(400);
        
        // Verificar que se intentó llamar al ChangePasswordAsync
        _mockAuthService.Verify(s => s.ChangePasswordAsync(userId, changePasswordRequest), Times.Once);
    }

    #endregion

    #region UpdateProfile Tests

    /// <summary>
    /// ✅ PRUEBA 11: Actualizar perfil del usuario
    /// 
    /// Objetivo: Verificar que se puede actualizar información del perfil (no contraseña)
    /// Resultado esperado: HTTP 200 (OK)
    /// 
    /// Escenario real:
    /// Usuario autenticado actualiza:
    /// - Email
    /// - Nombre
    /// - Apellido
    /// Sistema valida los datos
    /// Sistema actualiza en BD
    /// Sistema envía email de notificación
    /// </summary>
    [Fact]
    public async Task UpdateProfile_WithValidData_ShouldReturnOk()
    {
        // ARRANGE: Preparar datos para actualizar perfil
        var userId = Guid.NewGuid();
        var updateProfileRequest = new UpdateUserProfileDTO
        {
            Email = "newemail@test.com",
            FirstName = "UpdatedName",
            LastName = "UpdatedLastName"
        };

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var claimsIdentity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        _controller.ControllerContext.HttpContext.User = claimsPrincipal;

        // Configurar el mock para actualización exitosa
        _mockAuthService.Setup(s => s.UpdateUserProfileAsync(userId, updateProfileRequest))
            .Returns(Task.CompletedTask);

        // ACT: Actualizar el perfil
        var result = await _controller.UpdateProfile(updateProfileRequest);

        // ASSERT: Verificar que se actualizó exitosamente
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        okResult.StatusCode.Should().Be(200);
        
        // Verificar que se llamó con los datos correctos
        _mockAuthService.Verify(s => s.UpdateUserProfileAsync(userId, updateProfileRequest), Times.Once);
    }

    #endregion

    #region ForgotPassword Tests

    /// <summary>
    /// ✅ PRUEBA 12: Solicitar reset de contraseña (Forgot Password)
    /// 
    /// Objetivo: Verificar que se puede solicitar un reset de contraseña por email
    /// Resultado esperado: HTTP 200 (OK) + Email de reset enviado
    /// 
    /// Escenario real (Password Recovery Flow):
    /// 1. Usuario dice "Olvidé mi contraseña"
    /// 2. Usuario ingresa su email
    /// 3. Sistema busca al usuario por email
    /// 4. Sistema genera un token de reset (válido 1 hora)
    /// 5. Sistema envía email con link: https://miapp.com/reset?token=ABC123
    /// 6. Usuario hace click en el link
    /// 7. Usuario ingresa nueva contraseña
    /// 8. Sistema valida el token y cambia la contraseña
    /// 
    /// Esta prueba es el PASO 2-5
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WithValidEmail_ShouldReturnOk()
    {
        // ARRANGE: Preparar el email válido
        var email = "test@test.com";

        // Configurar el mock para envío de email exitoso
        _mockAuthService.Setup(s => s.RequestPasswordResetAsync(email))
            .Returns(Task.CompletedTask);

        // ACT: Solicitar reset de contraseña
        var result = await _controller.ForgotPassword(email);

        // ASSERT: Verificar que se envió el email
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        okResult.StatusCode.Should().Be(200);
        
        // Verificar que se llamó al RequestPasswordResetAsync
        _mockAuthService.Verify(s => s.RequestPasswordResetAsync(email), Times.Once);
        // Email fue enviado al usuario
    }

    /// <summary>
    /// ❌ PRUEBA 13: Forgot Password con email vacío
    /// 
    /// Objetivo: Verificar que no se puede solicitar reset sin ingrescar email
    /// Resultado esperado: HTTP 400 (Bad Request)
    /// 
    /// Escenario real:
    /// Usuario deja el campo de email vacío
    /// Sistema valida: "Email es requerido"
    /// Sistema rechaza la solicitud
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WithEmptyEmail_ShouldReturnBadRequest()
    {
        // ARRANGE: Preparar un email vacío (INVÁLIDO)
        var email = "";  // ← Email vacío

        // ACT: Intentar solicitar reset sin email
        var result = await _controller.ForgotPassword(email);

        // ASSERT: Verificar que rechaza
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        badRequestResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region ResetPassword Tests

    /// <summary>
    /// ✅ PRUEBA 14: Reset de contraseña con token válido
    /// 
    /// Objetivo: Verificar que se puede cambiar la contraseña usando el token de reset
    /// Resultado esperado: HTTP 200 (OK)
    /// 
    /// Escenario real (continuación del Password Recovery Flow):
    /// 1. Usuario recibió email con link: https://miapp.com/reset?token=ABC123
    /// 2. Usuario hace click en el link
    /// 3. Sistema valida el token (existe, no expiró, no fue usado)
    /// 4. Usuario ve formulario para ingresar nueva contraseña
    /// 5. Usuario ingresa nueva contraseña 2 veces (confirmación)
    /// 6. Usuario presiona "Cambiar contraseña"
    /// 7. Sistema valida: token válido, contraseña válida, coinciden
    /// 8. Sistema cambia la contraseña
    /// 9. Sistema marca el token como "usado"
    /// 10. Usuario puede hacer login con nueva contraseña
    /// 
    /// Esta prueba es el PASO 5-10
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithValidToken_ShouldReturnOk()
    {
        // ARRANGE: Preparar datos para reset con token VÁLIDO
        var resetPasswordRequest = new ResetPasswordDTO
        {
            Token = "validResetToken",                    // ← Token válido (no expirado, no usado)
            NewPassword = "NewSecurePassword123!",        // ← Nueva contraseña fuerte
            ConfirmNewPassword = "NewSecurePassword123!"  // ← Confirmación
        };

        // Configurar el mock para reset exitoso
        _mockAuthService.Setup(s => s.ResetPasswordAsync(resetPasswordRequest))
            .Returns(Task.CompletedTask);

        // ACT: Hacer el reset
        var result = await _controller.ResetPassword(resetPasswordRequest);

        // ASSERT: Verificar que se reseteó exitosamente
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        okResult.StatusCode.Should().Be(200);
        
        // Verificar que se llamó al ResetPasswordAsync
        _mockAuthService.Verify(s => s.ResetPasswordAsync(resetPasswordRequest), Times.Once);
        // Token ahora está marcado como "usado" (no se puede usar de nuevo)
    }

    /// <summary>
    /// ❌ PRUEBA 15: Reset de contraseña con token expirado
    /// 
    /// Objetivo: Verificar que no se puede usar un token de reset expirado
    /// Resultado esperado: HTTP 400 (Bad Request)
    /// 
    /// Escenario real:
    /// Usuario recibió email con reset token
    /// Token es válido por 1 hora (timestamp: 14:00)
    /// Usuario intenta usar el token después de 1 hora (timestamp: 15:05) - EXPIRADO
    /// Sistema valida: "El token de reset ha expirado"
    /// Sistema rechaza el cambio
    /// Usuario debe solicitar nuevo reset (Forgot Password de nuevo)
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithExpiredToken_ShouldReturnBadRequest()
    {
        // ARRANGE: Preparar datos con token EXPIRADO
        var resetPasswordRequest = new ResetPasswordDTO
        {
            Token = "expiredToken",                       // ← Token expirado (pasó 1 hora)
            NewPassword = "NewSecurePassword123!",
            ConfirmNewPassword = "NewSecurePassword123!"
        };

        // Configurar el mock para rechazar por token expirado
        _mockAuthService.Setup(s => s.ResetPasswordAsync(resetPasswordRequest))
            .ThrowsAsync(new InvalidOperationException("Invalid or expired reset token"));

        // ACT: Intentar reset con token expirado
        var result = await _controller.ResetPassword(resetPasswordRequest);

        // ASSERT: Verificar que rechaza
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        badRequestResult.StatusCode.Should().Be(400);
        // Usuario debe solicitar nuevo reset
    }

    #endregion
}
