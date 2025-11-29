-- ============================================================================
-- SCRIPT: 0_Create_Database.sql
-- PROPÓSITO: Crear la base de datos principal
-- DESCRIPCIÓN: Script idempotente que crea la BD si no existe
-- BASE DE DATOS: BdIncidentManagementDotNetTechnicalTest
-- VERSIÓN: 1.0
-- ============================================================================

USE [master];
GO

-- Crear BD si no existe (SQL Server usa rutas por defecto automáticamente)
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'BdIncidentManagementDotNetTechnicalTest')
BEGIN
    CREATE DATABASE [BdIncidentManagementDotNetTechnicalTest];
    PRINT 'Base de datos [BdIncidentManagementDotNetTechnicalTest] creada exitosamente';
END
ELSE
BEGIN
    PRINT 'Base de datos [BdIncidentManagementDotNetTechnicalTest] ya existe';
END

GO

-- Verificar que la BD fue creada correctamente
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'BdIncidentManagementDotNetTechnicalTest')
BEGIN
    PRINT 'Verificación: BD existente y lista para usar';
END
ELSE
BEGIN
    PRINT 'ERROR: No se pudo crear la base de datos.  Verifica permisos. ';
    RAISERROR('Fallo al crear la base de datos', 16, 1);
END

GO

-- Cambiar contexto a la nueva BD
USE [BdIncidentManagementDotNetTechnicalTest];
GO

PRINT '';
PRINT '========================================';
PRINT 'Base de datos lista para creacion de tablas';
PRINT '========================================';