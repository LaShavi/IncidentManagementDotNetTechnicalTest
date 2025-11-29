-- ============================================================================
-- SCRIPT: 1_Setup_Authentication.sql
-- PROPÓSITO: Configurar tablas de autenticación y autorización
-- DESCRIPCIÓN: Crea tablas Users, RefreshTokens y TokenBlacklist
-- BASE DE DATOS: BdIncidentManagementDotNetTechnicalTest
-- VERSIÓN: 1.0
-- ============================================================================

USE [master];
GO

IF NOT EXISTS (SELECT name FROM sys. databases WHERE name = 'BdIncidentManagementDotNetTechnicalTest')
BEGIN
    PRINT 'ERROR: Base de datos [BdIncidentManagementDotNetTechnicalTest] no existe';
    PRINT 'Ejecuta primero el script 0_Create_Database. sql';
    RAISERROR('Base de datos no encontrada', 16, 1);
END

USE [BdIncidentManagementDotNetTechnicalTest];
GO

PRINT '========== INICIANDO SETUP DE AUTENTICACIÓN ==========';
PRINT '';

-- ============================================================================
-- 1. CREAR TABLAS (IDEMPOTENTE)
-- ============================================================================

-- TABLA: Users
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [Username] NVARCHAR(50) NOT NULL UNIQUE,
        [Email] NVARCHAR(100) NOT NULL UNIQUE,
        [PasswordHash] NVARCHAR(255) NOT NULL,        
        [FirstName] NVARCHAR(50) NOT NULL,
        [LastName] NVARCHAR(50) NOT NULL,
        [Role] NVARCHAR(20) NOT NULL DEFAULT 'User',
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastAccess] DATETIME2 NULL,
        [FailedAttempts] INT NOT NULL DEFAULT 0,
        [LockedUntil] DATETIME2 NULL
    );
    PRINT 'Tabla [Users] creada exitosamente';
END
ELSE
BEGIN
    PRINT 'Tabla [Users] ya existe';
END

GO

-- TABLA: RefreshTokens
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RefreshTokens')
BEGIN
    CREATE TABLE [dbo].[RefreshTokens] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [Token] NVARCHAR(500) NOT NULL UNIQUE,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ExpiresAt] DATETIME2 NOT NULL,
        [IsRevoked] BIT NOT NULL DEFAULT 0,
        [RevokedAt] DATETIME2 NULL,
        [ReplacedBy] NVARCHAR(500) NULL,
        CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
    );
    PRINT 'Tabla [RefreshTokens] creada exitosamente';
END
ELSE
BEGIN
    PRINT 'Tabla [RefreshTokens] ya existe';
END

GO

-- TABLA: TokenBlacklist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TokenBlacklist')
BEGIN
    CREATE TABLE [dbo].[TokenBlacklist] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [TokenHash] NVARCHAR(512) NOT NULL UNIQUE,
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [RevokedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ExpiresAt] DATETIME2 NOT NULL,
        [Reason] NVARCHAR(50) NOT NULL DEFAULT 'Manual revocation',
        CONSTRAINT FK_TokenBlacklist_Users FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
    );
    PRINT 'Tabla [TokenBlacklist] creada exitosamente';
END
ELSE
BEGIN
    PRINT 'Tabla [TokenBlacklist] ya existe';
END

GO

-- ============================================================================
-- 2. CREAR ÍNDICES (IDEMPOTENTE)
-- ============================================================================

PRINT '';
PRINT 'Creando índices... ';

-- Índices: Users
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Username' AND object_id = OBJECT_ID('dbo.Users'))
BEGIN
    CREATE INDEX IX_Users_Username ON [dbo].[Users] ([Username]);
    PRINT 'Índice IX_Users_Username creado';
END

IF NOT EXISTS (SELECT * FROM sys. indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('dbo.Users'))
BEGIN
    CREATE INDEX IX_Users_Email ON [dbo].[Users] ([Email]);
    PRINT 'Índice IX_Users_Email creado';
END

-- Índices: RefreshTokens
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RefreshTokens_Token' AND object_id = OBJECT_ID('dbo.RefreshTokens'))
BEGIN
    CREATE INDEX IX_RefreshTokens_Token ON [dbo].[RefreshTokens] ([Token]);
    PRINT 'Índice IX_RefreshTokens_Token creado';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RefreshTokens_UserId' AND object_id = OBJECT_ID('dbo.RefreshTokens'))
BEGIN
    CREATE INDEX IX_RefreshTokens_UserId ON [dbo].[RefreshTokens] ([UserId]);
    PRINT 'Índice IX_RefreshTokens_UserId creado';
END

-- Índices: TokenBlacklist
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TokenBlacklist_TokenHash' AND object_id = OBJECT_ID('dbo.TokenBlacklist'))
BEGIN
    CREATE INDEX IX_TokenBlacklist_TokenHash ON [dbo].[TokenBlacklist] ([TokenHash]);
    PRINT 'Índice IX_TokenBlacklist_TokenHash creado';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TokenBlacklist_UserId' AND object_id = OBJECT_ID('dbo.TokenBlacklist'))
BEGIN
    CREATE INDEX IX_TokenBlacklist_UserId ON [dbo].[TokenBlacklist] ([UserId]);
    PRINT 'Índice IX_TokenBlacklist_UserId creado';
END

IF NOT EXISTS (SELECT * FROM sys. indexes WHERE name = 'IX_TokenBlacklist_ExpiresAt' AND object_id = OBJECT_ID('dbo.TokenBlacklist'))
BEGIN
    CREATE INDEX IX_TokenBlacklist_ExpiresAt ON [dbo].[TokenBlacklist] ([ExpiresAt]);
    PRINT 'Índice IX_TokenBlacklist_ExpiresAt creado';
END

GO

-- ============================================================================
-- 3.  VERIFICACIÓN FINAL
-- ============================================================================

PRINT '';
PRINT '========== VERIFICACIÓN FINAL ==========';

DECLARE @UsersCount INT;
DECLARE @RefreshTokensCount INT;
DECLARE @TokenBlacklistCount INT;

SELECT @UsersCount = COUNT(*) FROM [dbo].[Users];
SELECT @RefreshTokensCount = COUNT(*) FROM [dbo].[RefreshTokens];
SELECT @TokenBlacklistCount = COUNT(*) FROM [dbo].[TokenBlacklist];

PRINT 'Registros por tabla:';
PRINT '  - Users: ' + CAST(@UsersCount AS NVARCHAR(10));
PRINT '  - RefreshTokens: ' + CAST(@RefreshTokensCount AS NVARCHAR(10));
PRINT '  - TokenBlacklist: ' + CAST(@TokenBlacklistCount AS NVARCHAR(10));

PRINT '';
PRINT '========== SETUP DE AUTENTICACIÓN COMPLETADO  ==========';