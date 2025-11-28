CREATE DATABASE BdHexagonalArchitectureTemplate;
GO

USE BdHexagonalArchitectureTemplate

-- Clientes
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Clientes' AND xtype='U')
CREATE TABLE Clientes (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Cedula VARCHAR(10) NOT NULL,
    Email VARCHAR(50) NOT NULL,
    Telefono VARCHAR(10) NOT NULL,
    Nombre VARCHAR(50) NOT NULL,
    Apellido VARCHAR(50) NOT NULL
);