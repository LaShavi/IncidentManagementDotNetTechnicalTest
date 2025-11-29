-- ============================================================================
-- SCRIPT: 2_Create_Tables_Incident_Management_System.sql
-- PROPOSITO: Crear tablas del sistema de gestion de incidentes
-- DESCRIPCION: Crea IncidentCategories, IncidentStatuses, Incidents, etc.
-- BASE DE DATOS: BdIncidentManagementDotNetTechnicalTest
-- VERSION: 2.1
-- ============================================================================

USE [master];
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'BdIncidentManagementDotNetTechnicalTest')
BEGIN
    PRINT 'ERROR - Base de datos [BdIncidentManagementDotNetTechnicalTest] no existe';
    PRINT 'Ejecuta primero el script 0_Create_Database.sql';
    RAISERROR('Base de datos no encontrada', 16, 1);
END

USE [BdIncidentManagementDotNetTechnicalTest];
GO

-- Validar que tabla Users existe
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    PRINT 'ERROR - Tabla [Users] no existe';
    PRINT 'Ejecuta primero el script 1_Setup_Authentication. sql';
    RAISERROR('Tabla Users no encontrada', 16, 1);
END

GO

PRINT '========== INICIANDO CREACION DE TABLAS - INCIDENT MANAGEMENT SYSTEM ==========';
PRINT '';

-- ============================================================================
-- 1. TABLA: IncidentCategories
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys. tables WHERE name = 'IncidentCategories')
BEGIN
    CREATE TABLE dbo.IncidentCategories (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Name NVARCHAR(100) NOT NULL UNIQUE,
        Description NVARCHAR(500) NULL,
        Color NVARCHAR(7) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2(7) NULL
    );
    
    CREATE INDEX IX_IncidentCategories_Name ON dbo.IncidentCategories(Name);
    CREATE INDEX IX_IncidentCategories_IsActive ON dbo.IncidentCategories(IsActive);
    
    PRINT 'OK - Tabla [IncidentCategories] creada exitosamente';
END
ELSE
BEGIN
    PRINT 'ADVERTENCIA - Tabla [IncidentCategories] ya existe';
END

GO

-- ============================================================================
-- 2. TABLA: IncidentStatuses
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'IncidentStatuses')
BEGIN
    CREATE TABLE dbo.IncidentStatuses (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Name NVARCHAR(50) NOT NULL UNIQUE,
        DisplayName NVARCHAR(50) NOT NULL,
        Description NVARCHAR(200) NULL,
        OrderSequence INT NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE()
    );
    
    INSERT INTO dbo.IncidentStatuses (Name, DisplayName, Description, OrderSequence, IsActive)
    VALUES 
        ('OPEN', 'Abierto', 'Incidente recien creado, pendiente de atender', 1, 1),
        ('IN_PROGRESS', 'En Progreso', 'Incidente en atencion por el equipo tecnico', 2, 1),
        ('CLOSED', 'Cerrado', 'Incidente resuelto y validado', 3, 1),
        ('ON_HOLD', 'En Espera', 'Incidente pausado, esperando informacion del usuario', 4, 1),
        ('CANCELLED', 'Cancelado', 'Incidente cancelado o duplicado', 5, 1);
    
    PRINT 'OK - Tabla [IncidentStatuses] creada con 5 estados predeterminados';
END
ELSE
BEGIN
    PRINT 'ADVERTENCIA - Tabla [IncidentStatuses] ya existe';
END

GO

-- ============================================================================
-- 3.  TABLA: Incidents
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Incidents')
BEGIN
    CREATE TABLE dbo.Incidents (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        CategoryId UNIQUEIDENTIFIER NOT NULL,
        StatusId INT NOT NULL DEFAULT 1,
        Priority INT NOT NULL DEFAULT 2,
        CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2(7) NULL,
        ClosedAt DATETIME2(7) NULL,
        CONSTRAINT FK_Incidents_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
        CONSTRAINT FK_Incidents_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.IncidentCategories(Id),
        CONSTRAINT FK_Incidents_Statuses FOREIGN KEY (StatusId) REFERENCES dbo.IncidentStatuses(Id)
    );
    
    CREATE NONCLUSTERED INDEX IX_Incidents_UserId_StatusId ON dbo.Incidents(UserId, StatusId) INCLUDE (Title, CreatedAt);
    CREATE NONCLUSTERED INDEX IX_Incidents_CategoryId ON dbo.Incidents(CategoryId);
    CREATE NONCLUSTERED INDEX IX_Incidents_StatusId ON dbo.Incidents(StatusId);
    CREATE NONCLUSTERED INDEX IX_Incidents_CreatedAt ON dbo.Incidents(CreatedAt DESC);
    CREATE NONCLUSTERED INDEX IX_Incidents_Priority ON dbo.Incidents(Priority);
    CREATE NONCLUSTERED INDEX IX_Incidents_UserId_CreatedAt ON dbo.Incidents(UserId, CreatedAt DESC);
    
    PRINT 'OK - Tabla [Incidents] creada con 6 indices optimizados';
END
ELSE
BEGIN
    PRINT 'ADVERTENCIA - Tabla [Incidents] ya existe';
END

GO

-- ============================================================================
-- 4. TABLA: IncidentUpdates
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys. tables WHERE name = 'IncidentUpdates')
BEGIN
    CREATE TABLE dbo.IncidentUpdates (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        IncidentId UNIQUEIDENTIFIER NOT NULL,
        AuthorId UNIQUEIDENTIFIER NOT NULL,
        Comment NVARCHAR(MAX) NOT NULL,
        UpdateType NVARCHAR(50) NOT NULL,
        OldValue NVARCHAR(MAX) NULL,
        NewValue NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_IncidentUpdates_Incidents FOREIGN KEY (IncidentId) REFERENCES dbo.Incidents(Id) ON DELETE CASCADE,
        CONSTRAINT FK_IncidentUpdates_Users FOREIGN KEY (AuthorId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION
    );
    
    CREATE NONCLUSTERED INDEX IX_IncidentUpdates_IncidentId ON dbo. IncidentUpdates(IncidentId, CreatedAt DESC);
    CREATE NONCLUSTERED INDEX IX_IncidentUpdates_AuthorId ON dbo.IncidentUpdates(AuthorId);
    CREATE NONCLUSTERED INDEX IX_IncidentUpdates_UpdateType ON dbo.IncidentUpdates(UpdateType);
    
    PRINT 'OK - Tabla [IncidentUpdates] creada con 3 indices optimizados';
END
ELSE
BEGIN
    PRINT 'ADVERTENCIA - Tabla [IncidentUpdates] ya existe';
END

GO

-- ============================================================================
-- 5. TABLA: IncidentAttachments
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'IncidentAttachments')
BEGIN
    CREATE TABLE dbo. IncidentAttachments (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        IncidentId UNIQUEIDENTIFIER NOT NULL,
        FileName NVARCHAR(255) NOT NULL,
        FilePath NVARCHAR(500) NOT NULL,
        FileSize BIGINT NOT NULL,
        FileType NVARCHAR(50) NOT NULL,
        UploadedBy UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_IncidentAttachments_Incidents FOREIGN KEY (IncidentId) REFERENCES dbo.Incidents(Id) ON DELETE CASCADE,
        CONSTRAINT FK_IncidentAttachments_Users FOREIGN KEY (UploadedBy) REFERENCES dbo.Users(Id) ON DELETE NO ACTION
    );
    
    CREATE NONCLUSTERED INDEX IX_IncidentAttachments_IncidentId ON dbo.IncidentAttachments(IncidentId);
    CREATE NONCLUSTERED INDEX IX_IncidentAttachments_CreatedAt ON dbo.IncidentAttachments(CreatedAt DESC);
    
    PRINT 'OK - Tabla [IncidentAttachments] creada';
END
ELSE
BEGIN
    PRINT 'ADVERTENCIA - Tabla [IncidentAttachments] ya existe';
END

GO

-- ============================================================================
-- 6. TABLA: IncidentMetrics
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys. tables WHERE name = 'IncidentMetrics')
BEGIN
    CREATE TABLE dbo.IncidentMetrics (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        IncidentId UNIQUEIDENTIFIER NOT NULL UNIQUE,
        TimeToClose BIGINT NULL,
        CommentCount INT NOT NULL DEFAULT 0,
        AttachmentCount INT NOT NULL DEFAULT 0,
        AverageResolutionTime INT NULL,
        UpdatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_IncidentMetrics_Incidents FOREIGN KEY (IncidentId) REFERENCES dbo.Incidents(Id) ON DELETE CASCADE
    );
    
    CREATE NONCLUSTERED INDEX IX_IncidentMetrics_UpdatedAt ON dbo.IncidentMetrics(UpdatedAt DESC);
    
    PRINT 'OK - Tabla [IncidentMetrics] creada';
END
ELSE
BEGIN
    PRINT 'ADVERTENCIA - Tabla [IncidentMetrics] ya existe';
END

GO

-- ============================================================================
-- 7. INSERTAR CATEGORIAS PREDETERMINADAS (Alineadas con requerimientos)
-- ============================================================================

PRINT '';
PRINT 'Validando categorias... ';

IF NOT EXISTS (SELECT 1 FROM dbo.IncidentCategories WHERE Name = 'Defecto')
BEGIN
    INSERT INTO dbo.IncidentCategories (Name, Description, Color, IsActive)
    VALUES 
        ('Defecto', 'Problemas o defectos en la aplicacion que requieren reparacion', '#FF0000', 1),
        ('Solicitud de Mejora', 'Mejoras o enhancements solicitados en la funcionalidad existente', '#0000FF', 1),
        ('Nueva Funcionalidad', 'Nuevas caracteristicas o funcionalidades requeridas', '#00FF00', 1),
        ('Problema de Rendimiento', 'Incidentes relacionados con rendimiento, escalabilidad o carga del sistema', '#FF00FF', 1),
        ('Problema de Seguridad', 'Vulnerabilidades o problemas de seguridad criticos', '#FFA500', 1),
        ('Problema de Integracion', 'Errores en la integracion con sistemas externos o APIs', '#FF6347', 1),
        ('Consulta Tecnica', 'Consultas, dudas o soporte tecnico general', '#87CEEB', 1);
    
    PRINT 'OK - 7 categorias predeterminadas insertadas (alineadas con prueba tecnica)';
END
ELSE
BEGIN
    PRINT 'ADVERTENCIA - Las categorias ya existen';
END

GO

-- ============================================================================
-- 8. VERIFICACION FINAL
-- ============================================================================

PRINT '';
PRINT '========== VERIFICACION FINAL ==========';

DECLARE @CategoriesCount INT;
DECLARE @StatusesCount INT;

SELECT @CategoriesCount = COUNT(*) FROM dbo.IncidentCategories;
SELECT @StatusesCount = COUNT(*) FROM dbo.IncidentStatuses;

PRINT 'Categorias: ' + CAST(@CategoriesCount AS NVARCHAR(10));
PRINT 'Estados: ' + CAST(@StatusesCount AS NVARCHAR(10));

PRINT '';
PRINT 'Categorias disponibles:';
SELECT '  - ' + Name AS Categoria FROM dbo.IncidentCategories WHERE IsActive = 1 ORDER BY Name;

PRINT '';
PRINT 'Estados disponibles:';
SELECT '  - ' + DisplayName AS Estado FROM dbo.IncidentStatuses WHERE IsActive = 1 ORDER BY OrderSequence;

PRINT '';
PRINT 'Tablas creadas:';
PRINT '  OK - IncidentCategories';
PRINT '  OK - IncidentStatuses';
PRINT '  OK - Incidents';
PRINT '  OK - IncidentUpdates';
PRINT '  OK - IncidentAttachments';
PRINT '  OK - IncidentMetrics';

PRINT '';
PRINT '========== SCRIPT COMPLETADO EXITOSAMENTE ==========';