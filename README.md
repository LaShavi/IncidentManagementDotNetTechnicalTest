# 🏗️ Hexagonal Architecture Template

Una **plantilla profesional de API REST** desarrollada con **.NET 8** que implementa **Arquitectura Hexagonal (Puertos y Adaptadores)** con seguridad de nivel empresarial, pruebas unitarias completas y buenas prácticas de desarrollo.

---

## 📑 Tabla de Contenidos

- [Características](#características)
- [Arquitectura](#arquitectura)
- [Requisitos](#requisitos)
- [Instalación](#instalación)
- [Configuración](#configuración)
- [Uso](#uso)
- [API Endpoints](#api-endpoints)
- [Seguridad](#seguridad)
- [Pruebas](#pruebas)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Documentación](#documentación)
- [Producción](#producción)
- [Soporte](#soporte)

---

## ✨ Características

### 🔒 Seguridad
- ✅ **Autenticación JWT** con refresh tokens y rotación automática
- ✅ **Headers de seguridad HTTP** completos (HSTS, CSP, X-Frame-Options, Referrer-Policy)
- ✅ **Validación de entrada** robusta (Email, Username, Password, Token)
- ✅ **Política de contraseñas fuerte** (8+ caracteres, mayúsculas, números, símbolos)
- ✅ **Rate limiting** (100 requests/minuto)
- ✅ **CORS configurable** por origen
- ✅ **Auditoría de seguridad** completa (intentos fallidos, actividades sospechosas)
- ✅ **Detección de patrones maliciosos** (SQL injection, XSS, path traversal, bots)
- ✅ **Bloqueo de cuenta** por intentos fallidos (5 intentos = 15 min)
- ✅ **Encriptación BCrypt** para contraseñas

### ⚙️ Características Técnicas
- 🔷 **Arquitectura Hexagonal** (Puertos y Adaptadores)
- 🔷 **.NET 8** con C# 12
- 🔷 **SQL Server** como base de datos
- 🔷 **Entity Framework Core** para ORM
- 🔷 **AutoMapper** para mapeo de objetos
- 🔷 **Serilog** para logging estructurado (Console + File diario)
- 🔷 **JWT Bearer** para autenticación
- 🔷 **Swagger/OpenAPI** para documentación interactiva
- 🔷 **xUnit + FluentAssertions + Moq** para pruebas (58+)
- 🔷 **RazorLight** para plantillas de email

### 👥 Gestión de Usuarios
- ✅ Registro y login con JWT
- ✅ Cambio de contraseña con verificación
- ✅ Reset de contraseña por email
- ✅ Actualización de perfil
- ✅ Notificaciones por email (bienvenida, cambios, etc.)
- ✅ Bloqueo automático de cuenta
- ✅ Logout (revocación de tokens)
- ✅ Soporte para múltiples dispositivos

### 🌍 Internacionalización
- 🇪🇸 Español (es)
- 🇬🇧 Inglés (en)
- ✅ 35+ textos traducidos
- ✅ Textos de validación centralizados
- ✅ Configuración por entorno

---

## 🏛️ Arquitectura

**Hexagonal Architecture - 5 Proyectos Organizados en Capas:**

```
???????????????????????????????????????????
?  API Layer (Controllers, Middleware)     ?
?         Api.csproj                      ?
???????????????????????????????????????????
               ?
???????????????????????????????????????????
?  Application Layer (Servicios, Puertos)  ?
?      Application.csproj                 ?
???????????????????????????????????????????
               ?
???????????????????????????????????????????
?   Domain Layer (Entidades)               ?
?        Domain.csproj                    ?
???????????????????????????????????????????
               ?
???????????????????????????????????????????
? Infrastructure Layer (BD, Email, etc.)   ?
?     Infrastructure.csproj               ?
???????????????????????????????????????????

Tests.csproj ? Pruebas automatizadas (58+)
```

### **Responsabilidades por Capa:**

| Capa | Proyecto | Responsabilidad | Ejemplos |
|------|----------|---|---|
| **Domain** | Domain.csproj | Entidades y reglas de negocio | User, Cliente, RefreshToken |
| **Application** | Application.csproj | Lógica de aplicación | AuthService, ClienteService |
| **Infrastructure** | Infrastructure.csproj | Adaptadores externos | Repositorios, EmailService, BD |
| **API** | Api.csproj | Presentación HTTP | Controllers, Middleware, Swagger |
| **Tests** | Tests.csproj | Pruebas automatizadas | 58+ tests (unitarios e interacción) |

---

## 💻 Requisitos

- **.NET 8 SDK** o superior ([descargar](https://dotnet.microsoft.com/download))
- **SQL Server 2019** o superior / SQL Server Express
- **Visual Studio 2022** (recomendado) o **VS Code**

---

## 🔧 Instalación

### 1. Clonar el repositorio
```bash
git clone <repository-url>
cd HexagonalArchitectureTemplate
```

### 2. Restaurar dependencias
```bash
dotnet restore
```

### 3. Configurar appsettings.json

Edita `Api/appsettings.json` con tus valores:

```json
{
  "ConnectionStrings": {
    "dbContext": "Server=localhost;Database=BdHexagonalArchitectureTemplate;User Id=sa;Password=admin;"
  },
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "tu-email@gmail.com",
    "SmtpPassword": "tu-app-password"
  }
}
```

### 4. Crear base de datos

#### Opción A: Entity Framework Migrations (Recomendado)
```bash
dotnet ef database update --project Infrastructure --startup-project Api
```

#### Opción B: Script SQL
```sql
CREATE DATABASE BdHexagonalArchitectureTemplate;
```

### 5. Compilar
```bash
dotnet build
```

### 6. Ejecutar la API
```bash
cd Api
dotnet run
```

? **API disponible en:** `https://localhost:7085`  
? **Swagger UI disponible en:** `https://localhost:7085/swagger`

---

## ⚙️ Configuración

### Autenticación JWT

```json
"Authentication": {
  "SecretKey": "LLAVE-DE-SEGURIDAD-MUY-LARGA-AQUI-MINIMO-64-CARACTERES",
  "Issuer": "HexagonalArchitectureTemplate",
  "Audience": "HexagonalArchitectureTemplate-Users",
  "AccessTokenExpiration": "15",           // Minutos
  "RefreshTokenExpiration": "7",           // Días
  "AllowMultipleDevices": true,
  "MaxFailedAttempts": 5,
  "LockoutDurationMinutes": 15
}
```

### CORS y AllowedHosts

```json
"AllowedHosts": "localhost,api.miapp.com",

"CorsPolicies": [
  {
    "Origin": "https://www.miapp.com",
    "Methods": [ "GET", "POST", "PUT", "DELETE" ],
    "Headers": [ "Content-Type", "Authorization" ]
  },
  {
    "Origin": "https://app.miapp.com",
    "Methods": [ "GET", "POST" ],
    "Headers": [ "Content-Type", "Authorization" ]
  }
]
```

### Logging con Serilog

```json
"Serilog": {
  "MinimumLevel": { "Default": "Information" },
  "WriteTo": [
    { "Name": "Console" },
    { "Name": "File", "Args": { "path": "logs/log-.txt", "rollingInterval": "Day" } }
  ]
}
```

Los logs se guardan automáticamente en: `logs/log-20250115.txt` (rotación diaria)

### Email SMTP

```json
"EmailSettings": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SmtpUsername": "tu-email@gmail.com",
  "SmtpPassword": "tu-app-password",
  "FromEmail": "tu-email@gmail.com",
  "FromName": "Tu Aplicación",
  "EnableSsl": "true"
}
```

### Internacionalización

```json
"Localization": {
  "DefaultCulture": "es"  // "es" o "en"
}
```

---

## 🚀 Uso Rápido

### 1. Registrar usuario
```bash
curl -X POST https://localhost:7085/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "juan",
    "email": "juan@example.com",
    "password": "SecurePass123!",
    "firstName": "Juan",
    "lastName": "Pérez"
  }'
```

**Respuesta:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "kX9mZ2pL...",
    "user": {
      "id": "550e8400-e29b-41d4",
      "username": "juan",
      "email": "juan@example.com"
    }
  }
}
```

### 2. Login
```bash
curl -X POST https://localhost:7085/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "juan", "password": "SecurePass123!"}'
```

### 3. Usar Access Token
```bash
curl https://localhost:7085/api/cliente \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."
```

### 4. Cambiar contraseña
```bash
curl -X POST https://localhost:7085/api/auth/change-password \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{
    "currentPassword": "SecurePass123!",
    "newPassword": "NewSecurePass456!",
    "confirmNewPassword": "NewSecurePass456!"
  }'
```

---

## 📡 API Endpoints

### Autenticación

| Método | Endpoint | Descripción | Auth |
|--------|----------|---|---|
| POST | `/api/auth/register` | Registrar nuevo usuario | ? |
| POST | `/api/auth/login` | Login | ? |
| POST | `/api/auth/refresh-token` | Renovar access token | ? |
| POST | `/api/auth/logout` | Cerrar sesión | ? |
| POST | `/api/auth/forgot-password` | Solicitar reset de contraseña | ? |
| POST | `/api/auth/reset-password` | Restablecer contraseña | ? |
| POST | `/api/auth/change-password` | Cambiar contraseña | ? |
| PUT | `/api/auth/profile` | Actualizar perfil | ? |
| POST | `/api/auth/validate-password-strength` | Validar fortaleza de contraseña | ? |

### Clientes

| Método | Endpoint | Descripción | Auth |
|--------|----------|---|---|
| GET | `/api/cliente` | Listar todos los clientes | ? |
| GET | `/api/cliente/{id}` | Obtener cliente por ID | ? |
| POST | `/api/cliente` | Crear nuevo cliente | ? |
| PUT | `/api/cliente/{id}` | Actualizar cliente | ? |
| DELETE | `/api/cliente/{id}` | Eliminar cliente | ? |

### Documentación

| Endpoint | Descripción |
|----------|---|
| `/swagger` | **Swagger UI interactiva** |
| `/swagger/v1/swagger.json` | OpenAPI JSON |

---

## 🔐 Seguridad

### Implementado ?

? **Autenticación JWT** con refresh tokens y rotación automática  
? **Encriptación BCrypt** para contraseñas  
? **Validación de entrada** robusta (Email, Username, Password, Token)  
? **Rate Limiting** (100 requests/minuto)  
? **CORS restrictivo** por origen  
? **Headers de seguridad** (HSTS, CSP, X-Frame-Options, Referrer-Policy)  
? **Auditoría completa** (logs de acceso, intentos fallidos, actividades sospechosas)  
? **Detección de ataques** (SQL injection, XSS, path traversal, bots)  
? **Bloqueo de cuenta** (5 intentos fallidos = 15 minutos bloqueado)  
? **Política de contraseñas fuerte** (8+ caracteres, mayúsculas, números, símbolos)  
? **Sanitización de input** (protección contra XSS)  
? **Entity Framework Core** (protección contra SQL injection)  

**?? Puntuación de Seguridad: 85/100**

Detalles completos en: [SECURITY_AUDIT_REPORT.md](./Documentation/SECURITY_AUDIT_REPORT.md)

---

## 🧪 Pruebas

### Ejecutar todas las pruebas
```bash
dotnet test
```

### Pruebas específicas
```bash
dotnet test --filter "AuthServiceTests"
dotnet test --filter "AuthControllerTests"
dotnet test --filter "UserRepositoryTests"
```

### Con cobertura de código
```bash
dotnet test /p:CollectCoverage=true
```

### ?? Cobertura: 58+ tests

- **AuthServiceTests**: 25+ tests (Login, Register, Password Reset, etc.)
- **AuthControllerTests**: 15+ tests (HTTP endpoints)
- **AuthRepositoriesTests**: 18+ tests (CRUD operations)

Detalles en: [TESTING_SUMMARY.md](./Documentation/TESTING_SUMMARY.md)

---

## 📁 Estructura del Proyecto

```
HexagonalArchitectureTemplate/
?
??? Domain/                          # Entidades de negocio
?   ??? Entities/
?   ?   ??? User.cs                  # Usuario con validaciones
?   ?   ??? Cliente.cs               # Cliente
?   ?   ??? RefreshToken.cs          # Token de refresco
?   ?   ??? PasswordResetToken.cs    # Token de reset
?   ??? Domain.csproj
?
??? Application/                     # Lógica de aplicación
?   ??? Services/
?   ?   ??? AuthService.cs           # Lógica de autenticación
?   ?   ??? ClienteService.cs        # Lógica de clientes
?   ?   ??? PasswordPolicyService.cs # Validación de contraseñas
?   ??? Ports/                       # Interfaces (Puertos)
?   ?   ??? IAuthService.cs
?   ?   ??? IClienteService.cs
?   ?   ??? IEmailService.cs
?   ??? DTOs/
?   ?   ??? Auth/
?   ?   ?   ??? LoginRequestDTO.cs
?   ?   ?   ??? RegisterRequestDTO.cs
?   ?   ?   ??? LoginResponseDTO.cs
?   ?   ??? Cliente/
?   ??? Validation/
?   ?   ??? SecurityValidators.cs    # Email, Username, Token validators
?   ??? Helpers/
?   ?   ??? ResourceTextHelper.cs    # Textos multiidioma
?   ??? Resources/
?   ?   ??? ValidationMessages.es.resx
?   ?   ??? ValidationMessages.en.resx
?   ??? Application.csproj
?
??? Infrastructure/                  # Adaptadores
?   ??? Persistence/
?   ?   ??? AppDbContext.cs
?   ?   ??? Repositories/
?   ?       ??? UserRepository.cs
?   ?       ??? RefreshTokenRepository.cs
?   ?       ??? PasswordResetTokenRepository.cs
?   ?       ??? ClienteRepository.cs
?   ??? Services/
?   ?   ??? EmailService.cs          # Envío de emails
?   ?   ??? BCryptPasswordHasher.cs   # Encriptación
?   ?   ??? EmailTemplates/          # Plantillas Razor
?   ?       ??? WelcomeEmail.cshtml
?   ?       ??? PasswordResetEmail.cshtml
?   ?       ??? ... (más plantillas)
?   ??? Mapping/
?   ?   ??? AutoMapperProfile.cs
?   ??? Infrastructure.csproj
?
??? Api/                             # Presentación HTTP
?   ??? Controllers/
?   ?   ??? AuthController.cs
?   ?   ??? ClienteController.cs
?   ?   ??? BaseApiController.cs
?   ??? Middleware/
?   ?   ??? SecurityHeadersMiddleware.cs
?   ?   ??? SecurityAuditMiddleware.cs
?   ?   ??? GlobalExceptionMiddleware.cs
?   ?   ??? RequestLoggingMiddleware.cs
?   ?   ??? ServiceExtensions.cs
?   ??? DTOs/Common/
?   ?   ??? ApiResponse.cs
?   ??? Helpers/
?   ?   ??? ApiResponseHelper.cs
?   ??? Configuration/
?   ?   ??? CorsPolicyConfig.cs
?   ??? Program.cs
?   ??? appsettings.json
?   ??? appsettings.Development.json
?   ??? Api.csproj
?
??? Tests/                           # Pruebas automatizadas (58+)
?   ??? Application/Services/
?   ??? Api/Controllers/
?   ??? Infrastructure/Repositories/
?   ??? Fixtures/
?   ??? TestBase.cs
?   ??? Tests.csproj
?
??? Documentation/
?   ??? SECURITY_AUDIT_REPORT.md     # Auditoría de seguridad (85/100)
?   ??? TESTING_SUMMARY.md           # Guía de pruebas (58+ tests)
?   ??? LOCALIZATION_SETUP.md        # Sistema de i18n (ES/EN)
?
??? HexagonalArchitectureTemplate.sln
```

---

## 📚 Documentación

### Reportes Detallados

| Documento | Contenido |
|-----------|----------|
| [SECURITY_AUDIT_REPORT.md](./Documentation/SECURITY_AUDIT_REPORT.md) | Análisis completo de seguridad (85/100) |
| [TESTING_SUMMARY.md](./Documentation/TESTING_SUMMARY.md) | Guía de 58+ pruebas automatizadas |
| [LOCALIZATION_SETUP.md](./Documentation/LOCALIZATION_SETUP.md) | Sistema de internacionalización (ES/EN) |

### Swagger UI
Accede a la documentación interactiva en:
```
https://localhost:7085/swagger
```

---

## 🚢 Producción

### Checklist Antes de Desplegar

- [ ] **Secretos en variables de entorno** (JWT Secret, DB Password, Email Password)
- [ ] **HTTPS habilitado** con certificado válido
- [ ] **CORS restrictivo** por dominio específico
- [ ] **AllowedHosts** configurado correctamente
- [ ] **Base de datos** con backups automáticos
- [ ] **Logging** configurado y monitoreado
- [ ] **Email SMTP** funcionando correctamente
- [ ] **Rate limiting** ajustado según necesidad
- [ ] **Pruebas** pasadas (`dotnet test`)

### Variables de Entorno para Producción

```bash
# Linux/macOS
export Authentication__SecretKey="tu-llave-secreta-super-larga-minimo-64-chars"
export ConnectionStrings__dbContext="tu-cadena-conexion-produccion"
export EmailSettings__SmtpPassword="tu-contraseña-email"
export AllowedHosts="api.miapp.com,api-staging.miapp.com"

# Windows PowerShell
$env:Authentication__SecretKey = "tu-llave-secreta"
$env:ConnectionStrings__dbContext = "tu-conexion"
$env:EmailSettings__SmtpPassword = "tu-password"
```

### Docker (Opcional)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 as build
WORKDIR /app
COPY . .
RUN dotnet build
RUN dotnet publish -c Release

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/bin/Release/net8.0/publish .
ENTRYPOINT ["dotnet", "Api.dll"]
```

---

## 🆘 Soporte

### ¿Necesitas ayuda?

- ?? **Documentación**: Revisa `/Documentation`
- ?? **Problemas**: Abre un Issue en GitHub
- ?? **API**: Accede a Swagger en `/swagger`
- ?? **Seguridad**: Revisa [SECURITY_AUDIT_REPORT.md](./Documentation/SECURITY_AUDIT_REPORT.md)
- ?? **Pruebas**: Revisa [TESTING_SUMMARY.md](./Documentation/TESTING_SUMMARY.md)

---

## 📝 Licencia

MIT License - Libre para usar en proyectos personales y comerciales.

---

## 🚀 Roadmap Futuro

- [ ] 2FA (Two-Factor Authentication)
- [ ] OAuth2 / OpenID Connect
- [ ] GraphQL endpoint
- [ ] Docker & Kubernetes support
- [ ] CI/CD pipeline (GitHub Actions)
- [ ] Performance monitoring
- [ ] Advanced caching

---

**Creado con 🛠️ usando .NET 8 y Arquitectura Hexagonal**

*Última actualización: 2025*

*Puntuación de Seguridad: 85/100 - Pruebas: 58+ - Idiomas: 2 (ES/EN)*
