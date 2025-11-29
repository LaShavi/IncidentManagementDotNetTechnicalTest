-- ============================================================================
-- SCRIPT: 4_Clean_Data.sql
-- PROPÓSITO: Limpiar SOLO datos, manteniendo estructura
-- DESCRIPCIÓN: Elimina todos los registros pero conserva las tablas
-- BASE DE DATOS: BdIncidentManagementDotNetTechnicalTest
-- VERSIÓN: 1.0
-- ============================================================================

USE [master];
GO

IF NOT EXISTS (SELECT name FROM sys. databases WHERE name = 'BdIncidentManagementDotNetTechnicalTest')
BEGIN
    PRINT 'ERROR: Base de datos [BdIncidentManagementDotNetTechnicalTest] no existe';
    RAISERROR('Base de datos no encontrada', 16, 1);
END

USE [BdIncidentManagementDotNetTechnicalTest];
GO

PRINT '========== INICIANDO LIMPIEZA DE DATOS ==========';
PRINT '';
PRINT 'Todos los registros serán eliminados (estructura se mantiene)';
PRINT '';

-- Desabilitar constraints temporalmente
EXEC sp_MSForEachTable 'ALTER TABLE ?  NOCHECK CONSTRAINT ALL'
PRINT 'Constraints deshabilitadas temporalmente... ';

-- ============================================================================
-- LIMPIAR DATOS EN ORDEN DE DEPENDENCIAS
-- ============================================================================

PRINT '';
PRINT 'Limpiando tablas... ';

DECLARE @MetricsDeleted INT = 0;
DECLARE @AttachmentsDeleted INT = 0;
DECLARE @UpdatesDeleted INT = 0;
DECLARE @IncidentsDeleted INT = 0;
DECLARE @CategoriesDeleted INT = 0;
DECLARE @BlacklistDeleted INT = 0;
DECLARE @RefreshDeleted INT = 0;
DECLARE @UsersDeleted INT = 0;

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'IncidentMetrics')
BEGIN
    SET @MetricsDeleted = (SELECT COUNT(*) FROM dbo.IncidentMetrics);
    DELETE FROM dbo.IncidentMetrics;
    PRINT 'IncidentMetrics: ' + CAST(@MetricsDeleted AS NVARCHAR(10)) + ' registros eliminados';
END

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'IncidentAttachments')
BEGIN
    SET @AttachmentsDeleted = (SELECT COUNT(*) FROM dbo.IncidentAttachments);
    DELETE FROM dbo. IncidentAttachments;
    PRINT 'IncidentAttachments: ' + CAST(@AttachmentsDeleted AS NVARCHAR(10)) + ' registros eliminados';
END

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'IncidentUpdates')
BEGIN
    SET @UpdatesDeleted = (SELECT COUNT(*) FROM dbo.IncidentUpdates);
    DELETE FROM dbo.IncidentUpdates;
    PRINT 'IncidentUpdates: ' + CAST(@UpdatesDeleted AS NVARCHAR(10)) + ' registros eliminados';
END

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Incidents')
BEGIN
    SET @IncidentsDeleted = (SELECT COUNT(*) FROM dbo.Incidents);
    DELETE FROM dbo.Incidents;
    PRINT 'Incidents: ' + CAST(@IncidentsDeleted AS NVARCHAR(10)) + ' registros eliminados';
END

IF EXISTS (SELECT 1 FROM sys. tables WHERE name = 'IncidentCategories')
BEGIN
    SET @CategoriesDeleted = (SELECT COUNT(*) FROM dbo. IncidentCategories);
    DELETE FROM dbo.IncidentCategories;
    PRINT 'IncidentCategories: ' + CAST(@CategoriesDeleted AS NVARCHAR(10)) + ' registros eliminados';
END

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TokenBlacklist')
BEGIN
    SET @BlacklistDeleted = (SELECT COUNT(*) FROM dbo.TokenBlacklist);
    DELETE FROM dbo.TokenBlacklist;
    PRINT 'TokenBlacklist: ' + CAST(@BlacklistDeleted AS NVARCHAR(10)) + ' registros eliminados';
END

IF EXISTS (SELECT 1 FROM sys. tables WHERE name = 'RefreshTokens')
BEGIN
    SET @RefreshDeleted = (SELECT COUNT(*) FROM dbo.RefreshTokens);
    DELETE FROM dbo.RefreshTokens;
    PRINT 'RefreshTokens: ' + CAST(@RefreshDeleted AS NVARCHAR(10)) + ' registros eliminados';
END

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
BEGIN
    SET @UsersDeleted = (SELECT COUNT(*) FROM dbo.Users WHERE Username != 'admin');
    DELETE FROM dbo.Users WHERE Username != 'admin';
    PRINT 'Users: ' + CAST(@UsersDeleted AS NVARCHAR(10)) + ' registros eliminados (admin preservado)';
END

-- Re-habilitar constraints
EXEC sp_MSForEachTable 'ALTER TABLE ? CHECK CONSTRAINT ALL'
PRINT '';
PRINT 'Constraints re-habilitadas...';

-- ============================================================================
-- RESETEAR IDENTIDADES
-- ============================================================================

PRINT '';
PRINT 'Reseteando secuencias...';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'IncidentStatuses')
BEGIN
    DBCC CHECKIDENT ('dbo.IncidentStatuses', RESEED, 0);
    PRINT 'Identidad IncidentStatuses reseteada';
END

-- ============================================================================
-- RE-INSERTAR DATOS POR DEFECTO
-- ============================================================================

PRINT '';
PRINT 'Re-insertando datos por defecto...';

IF (SELECT COUNT(*) FROM dbo.IncidentStatuses) = 0
BEGIN
    INSERT INTO dbo.IncidentStatuses (Name, DisplayName, Description, OrderSequence, IsActive)
    VALUES 
        ('OPEN', 'Abierto', 'Incidente recién creado', 1, 1),
        ('IN_PROGRESS', 'En Progreso', 'Incidente en atención', 2, 1),
        ('CLOSED', 'Cerrado', 'Incidente resuelto', 3, 1),
        ('ON_HOLD', 'En Espera', 'Incidente pausado', 4, 1),
        ('CANCELLED', 'Cancelado', 'Incidente cancelado', 5, 1);
    PRINT '5 estados predeterminados re-insertados';
END

IF (SELECT COUNT(*) FROM dbo.IncidentCategories) = 0
BEGIN
    INSERT INTO dbo.IncidentCategories (Name, Description, Color, IsActive)
    VALUES 
        ('Bug', 'Problemas o defectos en la aplicación', '#FF0000', 1),
        ('Feature', 'Nueva funcionalidad solicitada', '#00FF00', 1),
        ('Enhancement', 'Mejora de funcionalidad existente', '#0000FF', 1),
        ('Documentation', 'Problemas con documentación', '#FFFF00', 1),
        ('Performance', 'Problemas de rendimiento', '#FF00FF', 1),
        ('Security', 'Problemas de seguridad', '#FFA500', 1);
    PRINT '6 categorías predeterminadas re-insertadas';
END

GO

-- ============================================================================
-- VERIFICACIÓN FINAL
-- ============================================================================

PRINT '';
PRINT '========== VERIFICACIÓN FINAL ==========';

DECLARE @FinalUsers INT = (SELECT COUNT(*) FROM dbo. Users);
DECLARE @FinalIncidents INT = (SELECT COUNT(*) FROM dbo.Incidents);
DECLARE @FinalCategories INT = (SELECT COUNT(*) FROM dbo.IncidentCategories);
DECLARE @FinalStatuses INT = (SELECT COUNT(*) FROM dbo.IncidentStatuses);

PRINT 'Estado actual:';
PRINT '  - Users: ' + CAST(@FinalUsers AS NVARCHAR(10)) + ' (solo admin)';
PRINT '  - Incidents: ' + CAST(@FinalIncidents AS NVARCHAR(10));
PRINT '  - Categories: ' + CAST(@FinalCategories AS NVARCHAR(10));
PRINT '  - Statuses: ' + CAST(@FinalStatuses AS NVARCHAR(10));

PRINT '';
PRINT '========== LIMPIEZA COMPLETADA ==========';