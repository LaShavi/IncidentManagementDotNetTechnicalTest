-- COMPLETE SCRIPT FOR INITIAL AUTHENTICATION SETUP WITH TOKEN BLACKLIST
-- Execute in SQL Server Management Studio

-- BD En Local.
--USE [BdHexagonalArchitectureTemplate]

-- BD En Azure.
USE [HexagonalArchitectureDB]

-- 1. CREATE TABLES IF NOT EXIST
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
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
    PRINT 'Table Users created successfully';
END
ELSE
BEGIN
    PRINT 'Table Users already exists';
END

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='RefreshTokens' AND xtype='U')
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
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
    );
    PRINT 'Table RefreshTokens created successfully';
END
ELSE
BEGIN
    PRINT 'Table RefreshTokens already exists';
END

-- NEW: TokenBlacklist Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TokenBlacklist' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[TokenBlacklist] (
        [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        [TokenHash] NVARCHAR(512) NOT NULL UNIQUE,
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [RevokedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ExpiresAt] DATETIME2 NOT NULL,
        [Reason] NVARCHAR(50) NOT NULL DEFAULT 'Manual revocation',
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
    );
    PRINT 'Table TokenBlacklist created successfully';
END
ELSE
BEGIN
    PRINT 'Table TokenBlacklist already exists';
END

-- 2. CREATE INDEXES FOR PERFORMANCE
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Username')
BEGIN
    CREATE INDEX IX_Users_Username ON [dbo].[Users] ([Username]);
    PRINT 'Index IX_Users_Username created successfully';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Email')
BEGIN
    CREATE INDEX IX_Users_Email ON [dbo].[Users] ([Email]);
    PRINT 'Index IX_Users_Email created successfully';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RefreshTokens_Token')
BEGIN
    CREATE INDEX IX_RefreshTokens_Token ON [dbo].[RefreshTokens] ([Token]);
    PRINT 'Index IX_RefreshTokens_Token created successfully';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RefreshTokens_UserId')
BEGIN
    CREATE INDEX IX_RefreshTokens_UserId ON [dbo].[RefreshTokens] ([UserId]);
    PRINT 'Index IX_RefreshTokens_UserId created successfully';
END

-- NEW: TokenBlacklist Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TokenBlacklist_TokenHash')
BEGIN
    CREATE INDEX IX_TokenBlacklist_TokenHash ON [dbo].[TokenBlacklist] ([TokenHash]);
    PRINT 'Index IX_TokenBlacklist_TokenHash created successfully';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TokenBlacklist_UserId')
BEGIN
    CREATE INDEX IX_TokenBlacklist_UserId ON [dbo].[TokenBlacklist] ([UserId]);
    PRINT 'Index IX_TokenBlacklist_UserId created successfully';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TokenBlacklist_ExpiresAt')
BEGIN
    CREATE INDEX IX_TokenBlacklist_ExpiresAt ON [dbo].[TokenBlacklist] ([ExpiresAt]);
    PRINT 'Index IX_TokenBlacklist_ExpiresAt created successfully (for cleanup of expired tokens)';
END

-- 3. INSERT DEFAULT ADMIN USER
-- NOTE: The password will be "Admin123!" hashed with BCrypt
IF NOT EXISTS (SELECT * FROM [dbo].[Users] WHERE [Username] = 'admin')
BEGIN
    INSERT INTO [dbo].[Users] 
    ([Id], [Username], [Email], [PasswordHash], [FirstName], [LastName], [Role], [IsActive])
    VALUES 
    (NEWID(), 'admin', 'admin@hexagonal-template.com', 
    '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewKyNvreop4XUPR2', -- Password: Admin123! (BCrypt)
    'Administrator', 'System', 'Admin', 1);
    
    PRINT 'Admin user created successfully';
    PRINT 'Username: admin';
    PRINT 'Password: Admin123!';
    PRINT 'Email: admin@hexagonal-template.com';
END
ELSE
BEGIN
    PRINT 'The admin user already exists';
END

-- 4. VERIFY INSTALLATION
PRINT '';
PRINT '========== VERIFICATION RESULTS ==========';
SELECT 'Users' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[Users];
SELECT 'RefreshTokens' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[RefreshTokens];
SELECT 'TokenBlacklist' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[TokenBlacklist];

PRINT '';
PRINT 'Admin user details:';
SELECT [Id], [Username], [Email], [FirstName], [LastName], [Role], [IsActive], [CreatedAt] 
FROM [dbo].[Users] 
WHERE [Username] = 'admin';

PRINT '';
PRINT '========== AUTHENTICATION SETUP COMPLETED SUCCESSFULLY ==========';