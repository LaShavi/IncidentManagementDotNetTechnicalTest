USE BdHexagonalArchitectureTemplate

---- Insertamos clientes
--INSERT INTO Clientes (Id, Cedula, Email, Telefono, Nombre, Apellido)
--VALUES 
--(NEWID(), '1234567890', 'juan.perez@example.com', '3001234567', 'Juan', 'Pérez'),
--(NEWID(), '0987654321', 'ana.gomez@example.com', '3007654321', 'Ana', 'Gómez'),
--(NEWID(), '1122334455', 'carlos.lopez@example.com', '3101122334', 'Carlos', 'López'),
--(NEWID(), '5566778899', 'maria.fernandez@example.com', '3205566778', 'María', 'Fernández'),
--(NEWID(), '6677889900', 'luis.martinez@example.com', '3506677889', 'Luis', 'Martínez'),
--(NEWID(), '3344556677', 'laura.rodriguez@example.com', '3153344556', 'Laura', 'Rodríguez'),
--(NEWID(), '7788990011', 'pedro.garcia@example.com', '3207788990', 'Pedro', 'García'),
--(NEWID(), '9900112233', 'sofia.ramirez@example.com', '3139900112', 'Sofía', 'Ramírez'),
--(NEWID(), '2211445566', 'andres.velasquez@example.com', '3172211445', 'Andrés', 'Velásquez'),
--(NEWID(), '8899001122', 'diana.castillo@example.com', '3118899001', 'Diana', 'Castillo');
---- //

-- Limpiamos data
--DELETE FROM Clientes;
--
SELECT * FROM Clientes;