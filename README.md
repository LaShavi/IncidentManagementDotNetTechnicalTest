# Sistema de Gestion de Incidentes

API REST desarrollada con .NET 8 y Arquitectura Hexagonal para la gestion integral de incidentes. 

> Para conocer las decisiones técnicas, arquitectura y análisis de riesgos, consulta el [Informe Técnico](Documentation/TECHNICAL_REPORT.md).

---

## Requisitos

- .NET 8 SDK o superior
- SQL Server 2019+ o SQL Server Express
- Visual Studio 2022 / VS Code / Rider

---

## Instalacion Rapida

### 1. Clonar el repositorio

```bash
git clone https://github.com/LaShavi/IncidentManagementDotNetTechnicalTest.git
cd IncidentManagementDotNetTechnicalTest
```

### 2. Configurar la base de datos

Edita `Api/appsettings.json` con tu cadena de conexion:

```json
{
  "ConnectionStrings": {
    "dbContext": "Server=localhost;Database=BdHexagonalArchitectureTemplate;User Id=sa;Password=TU_PASSWORD;"
  }
}
```

### 3. Ejecutar scripts de base de datos (IMPORTANTE)

Los scripts SQL estan en la carpeta `Scripts/` y deben ejecutarse en este orden:

```powershell
# Opcion 1: Desde SQL Server Management Studio (SSMS)
# Abrir y ejecutar en orden:
Scripts/0_Create_Database.sql
Scripts/1_Setup_Authentication.sql
Scripts/2_Create_Tables_Incident_Management_System.sql

# Opcion 2: Desde la terminal con sqlcmd
sqlcmd -S localhost -U sa -P TU_PASSWORD -i Scripts/0_Create_Database.sql
sqlcmd -S localhost -U sa -P TU_PASSWORD -i Scripts/1_Setup_Authentication.sql
sqlcmd -S localhost -U sa -P TU_PASSWORD -i Scripts/2_Create_Tables_Incident_Management_System.sql
```

**Que hace cada script:**
- `0_Create_Database.sql` - Crea la base de datos
- `1_Setup_Authentication.sql` - Crea tablas de usuarios y autenticacion
- `2_Create_Tables_Incident_Management_System.sql` - Crea tablas de incidentes y datos iniciales

### 4. Restaurar dependencias

```bash
dotnet restore
```

### 5. Compilar el proyecto

```bash
dotnet build
```

### 6. Ejecutar la aplicacion

```bash
cd Api
dotnet run
```

La API estara disponible en:
- HTTPS: `https://localhost:7085`
- HTTP: `http://localhost:5085`
- Swagger: `https://localhost:7085/swagger`

---

## Endpoints Principales

### Autenticacion

| Metodo | Endpoint | Descripcion |
|--------|----------|-------------|
| POST | `/api/auth/register` | Registrar usuario |
| POST | `/api/auth/login` | Iniciar sesion |

### Gestion de Incidentes (nuevo)

| Metodo | Endpoint | Descripcion | Auth |
|--------|----------|-------------|------|
| GET | `/api/incident` | Listar todos los incidentes | Si |
| GET | `/api/incident/{id}` | Obtener incidente por ID | Si |
| GET | `/api/incident/user/mine` | Mis incidentes | Si |
| GET | `/api/incident/category/{id}` | Incidentes por categoria | Si |
| GET | `/api/incident/status/{id}` | Incidentes por estado | Si |
| POST | `/api/incident` | Crear incidente | Si |
| PUT | `/api/incident/{id}` | Actualizar incidente | Si |
| PUT | `/api/incident/{id}/assign` | Reasignar usuario | Si |
| DELETE | `/api/incident/{id}` | Eliminar incidente | Si |
| POST | `/api/incident/{id}/comments` | Agregar comentario | Si |
| GET | `/api/incident/{id}/updates` | Ver historial | Si |
| GET | `/api/incident/categories` | Listar categorias | No |
| GET | `/api/incident/statuses` | Listar estados | No |

---

## Uso Basico

### 1. Registrar usuario

```bash
POST /api/auth/register
Content-Type: application/json

{
  "username": "juan",
  "email": "juan@example.com",
  "password": "SecurePass/567123!",
  "firstName": "Juan",
  "lastName": "Perez"
}
```

### 2. Iniciar sesion

```bash
POST /api/auth/login
Content-Type: application/json

{
  "username": "juan",
  "password": "SecurePass/567123!"
}

# Respuesta:
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "kX9mZ2pL..."
}
```

### 3. Crear incidente

```bash
POST /api/incident
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "title": "Bug en login",
  "description": "Los usuarios no pueden iniciar sesion con Google",
  "categoryId": "550e8400-e29b-41d4-91bd-f56a1e2e8e2a",
  "priority": 5
}
```

### 4. Listar mis incidentes

```bash
GET /api/incident/user/mine
Authorization: Bearer {accessToken}
```

### 5. Agregar comentario

```bash
POST /api/incident/{id}/comments
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "comment": "Estoy investigando el problema..."
}
```

---

## Pruebas Unitarias

Ejecutar todas las pruebas:

```bash
dotnet test
```

Ejecutar solo pruebas de incidentes:

```bash
dotnet test --filter "IncidentServiceTests"
```

Ver resultados detallados:

```bash
dotnet test --logger "console;verbosity=detailed"
```

**Cobertura actual:** 26 pruebas unitarias (100% pasando)

---

## Scripts SQL Adicionales

Ademas de los scripts de instalacion, hay 2 scripts utiles:

### Limpiar datos de prueba

```sql
-- Ejecutar: Scripts/4_Clean_Data.sql
-- Elimina todos los incidentes y comentarios (mantiene usuarios y categorias)
```

### Eliminar todo

```sql
-- Ejecutar: Scripts/3_Drop_All.sql
-- Elimina todas las tablas de incidentes (NO borra usuarios)
```

---

## Configuracion JWT

En `appsettings.json`:

```json
{
  "Authentication": {
    "SecretKey": "LLAVE-DE-SEGURIDAD-MUY-LARGA-MINIMO-64-CARACTERES",
    "Issuer": "IncidentManagementDotNetTechnicalTest",
    "Audience": "IncidentManagementDotNetTechnicalTest-Users",
    "AccessTokenExpiration": "15",
    "RefreshTokenExpiration": "7"
  }
}
```

---

## Caracteristicas Implementadas

- Autenticacion JWT con refresh tokens
- CRUD completo de incidentes
- Sistema de categorias y estados
- Prioridades (1-5)
- Comentarios y actualizaciones
- Reasignacion de usuarios
- Filtros por usuario, categoria y estado
- Validaciones con FluentValidation
- Logging con Serilog
- Arquitectura Hexagonal
- 26 pruebas unitarias

---

## Tecnologias Utilizadas

- .NET 8
- C# 12
- Entity Framework Core
- SQL Server
- AutoMapper
- FluentValidation
- Serilog
- xUnit + FluentAssertions + Moq
- Swagger/OpenAPI

---

## Resultados Reales Pruebas de carga - 29 Noviembre 2025

### TEST 1: GET Requests (1000 solicitudes)
```
Exito:             100% (1000/1000)
Req/segundo:       584.81 (Meta: 16.67)
Latencia promedio: 32ms
Latencia manima:   11ms
Latencia maxima:   62ms
Tiempo total:      1.71s

Veredicto: EXCELENTE (35x meta)
```

### TEST 2: POST Requests (1000 solicitudes)
```
Exito:             100% (1000/1000)
Req/segundo:       222.66 (Meta: 5)
Latencia promedio: 65ms
Latencia manima:   24ms
Latencia maxima:   118ms
Tiempo total:      4.49s

Veredicto: EXCELENTE (44x meta)
```

### TEST 3: Carga Mixta (700 GET + 300 POST)
```
Exito:             100% (1000/1000)
Req/segundo:       440.20 (Meta: 7)
Latencia promedio: 43ms
Latencia manima:   8ms
Latencia maxima:   109ms
Tiempo total:      2.27s

Veredicto: EXCELENTE (62x meta)
```

### TEST 4: Resistencia - Soak Test (10 batches x 100 req)
```
Exito:            100% (1000/1000)
Batch promedio:   0.31s (consistente)
Batch mas rapido: 0.29s
Batch mas lento:  0.32s
SIN DEGRADACION:  (no hay memory leaks)
Tiempo total:     3.08s

Veredicto: EXCELENTE - API estable bajo presion
```

### Resumen de Performance

| Metrica     |    Resultado   | Meta  | Cumplimiento |
|-------------|----------------|-------|--------------|
| GET req/s   | 584.81         | 16.67 | 35x          |
| POST req/s  | 222.66         | 5     | 44x          |
| Mixed req/s | 440.20         | 7     | 62x          |
| Tasa Exito  | 100%           | > 90% | Perfecto     |
| Stabilidad  | 0% degradacion | 0%    | Excelente    |

---

**Ultima actualizacion:** Noviembre 2025  
**Version**: 1.0  
**Estado**:  Produccion