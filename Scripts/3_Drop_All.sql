-- ============================================================================
-- SCRIPT: 3_Drop_All.sql
-- PROPOSITO: Eliminar todas las tablas y objetos
-- DESCRIPCION: Script destructivo para limpiar completamente la BD
-- BASE DE DATOS: BdIncidentManagementDotNetTechnicalTest
-- VERSION: 1.0
-- ============================================================================

USE [master];
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'BdIncidentManagementDotNetTechnicalTest')
BEGIN
    PRINT 'ADVERTENCIA: Base de datos [BdIncidentManagementDotNetTechnicalTest] no existe. Nada que eliminar.';
    PRINT '========== SALIENDO ==========';
END
ELSE
BEGIN
    USE [BdIncidentManagementDotNetTechnicalTest];
    
    PRINT '========== INICIANDO ELIMINACION DE OBJETOS ==========';
    PRINT '';
    PRINT 'ADVERTENCIA: Todos los datos seran eliminados';
    PRINT '';
    
    -- ============================================================================
    -- ELIMINAR TABLAS
    -- ============================================================================
    
    PRINT 'Eliminando tablas...';
    
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'IncidentMetrics')
    BEGIN
        DROP TABLE [dbo].[IncidentMetrics];
        PRINT 'OK - Tabla [IncidentMetrics] eliminada';
    END
    
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'IncidentAttachments')
    BEGIN
        DROP TABLE [dbo].[IncidentAttachments];
        PRINT 'OK - Tabla [IncidentAttachments] eliminada';
    END
    
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'IncidentUpdates')
    BEGIN
        DROP TABLE [dbo].[IncidentUpdates];
        PRINT 'OK - Tabla [IncidentUpdates] eliminada';
    END
    
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Incidents')
    BEGIN
        DROP TABLE [dbo].[Incidents];
        PRINT 'OK - Tabla [Incidents] eliminada';
    END
    
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'IncidentStatuses')
    BEGIN
        DROP TABLE [dbo].[IncidentStatuses];
        PRINT 'OK - Tabla [IncidentStatuses] eliminada';
    END
    
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'IncidentCategories')
    BEGIN
        DROP TABLE [dbo].[IncidentCategories];
        PRINT 'OK - Tabla [IncidentCategories] eliminada';
    END
    
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TokenBlacklist')
    BEGIN
        DROP TABLE [dbo].[TokenBlacklist];
        PRINT 'OK - Tabla [TokenBlacklist] eliminada';
    END
    
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'RefreshTokens')
    BEGIN
        DROP TABLE [dbo].[RefreshTokens];
        PRINT 'OK - Tabla [RefreshTokens] eliminada';
    END
    
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
    BEGIN
        DROP TABLE [dbo].[Users];
        PRINT 'OK - Tabla [Users] eliminada';
    END
    
    -- ============================================================================
    -- VERIFICACION FINAL
    -- ============================================================================
    
    PRINT '';
    PRINT '========== VERIFICACION ==========';
    
    DECLARE @TableCount INT;
    SELECT @TableCount = COUNT(*) FROM sys.tables;
    
    IF @TableCount = 0
    BEGIN
        PRINT 'OK - Base de datos limpia.  Todas las tablas eliminadas. ';
    END
    ELSE
    BEGIN
        PRINT 'ADVERTENCIA - Quedan ' + CAST(@TableCount AS NVARCHAR(10)) + ' tabla(s) en la BD';
    END
    
    PRINT '';
    PRINT '========== ELIMINACION COMPLETADA ==========';
END

GO