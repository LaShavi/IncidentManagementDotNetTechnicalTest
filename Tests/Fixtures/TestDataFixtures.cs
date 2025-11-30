using Domain.Entities;

namespace Tests.Fixtures;

public static class TestDataFixtures
{
    /// <summary>
    /// Creates a test user with default or custom values
    /// </summary>
    public static User CreateTestUser(
        Guid? id = null,
        string username = "testuser",
        string email = "testuser@test.com",
        string passwordHash = "hashedPassword123",
        string firstName = "Test",
        string lastName = "User",
        string role = "User",
        bool isActive = true)
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            IsActive = isActive,
            FailedAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            LastAccess = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates multiple test users
    /// </summary>
    public static List<User> CreateTestUsers(int count = 3)
    {
        var users = new List<User>();
        
        for (int i = 0; i < count; i++)
        {
            users.Add(CreateTestUser(
                username: $"testuser{i}",
                email: $"testuser{i}@test.com",
                firstName: $"Test{i}",
                lastName: $"User{i}"
            ));
        }

        return users;
    }

    /// <summary>
    /// Creates a test refresh token
    /// </summary>
    public static RefreshToken CreateTestRefreshToken(
        Guid? id = null,
        Guid? userId = null,
        string token = "testRefreshToken123",
        bool isRevoked = false,
        DateTime? expiresAt = null)
    {
        return new RefreshToken
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            Token = token,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
            RevokedAt = isRevoked ? DateTime.UtcNow : null
        };
    }

    /// <summary>
    /// Creates a test password reset token
    /// </summary>
    public static PasswordResetToken CreateTestPasswordResetToken(
        Guid? id = null,
        Guid? userId = null,
        string token = "resetToken123",
        bool isUsed = false,
        DateTime? expiresAt = null)
    {
        return new PasswordResetToken
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            Token = token,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddHours(1),
            IsUsed = isUsed
        };
    }

    /// <summary>
    /// Crea un IncidentAttachment de prueba
    /// </summary>
    public static IncidentAttachment CreateTestIncidentAttachment(
        Guid? id = null,
        Guid? incidentId = null,
        string fileName = "test.txt",
        long fileSize = 1024)
    {
        return new IncidentAttachment
        {
            Id = id ?? Guid.NewGuid(),
            IncidentId = incidentId ?? Guid.NewGuid(),
            FileName = fileName,
            FilePath = $"/uploads/{fileName}",
            FileSize = fileSize,
            FileType = "text/plain",
            UploadedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Crea un IncidentMetric de prueba
    /// </summary>
    public static IncidentMetric CreateTestIncidentMetric(
        Guid? id = null,
        Guid? incidentId = null,
        int commentCount = 0,
        int attachmentCount = 0)
    {
        return new IncidentMetric
        {
            Id = id ?? Guid.NewGuid(),
            IncidentId = incidentId ?? Guid.NewGuid(),
            CommentCount = commentCount,
            AttachmentCount = attachmentCount,
            TimeToClose = null,
            AverageResolutionTime = null,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Crea un IncidentStatus de prueba
    /// </summary>
    public static IncidentStatus CreateTestIncidentStatus(
        int id = 1,
        string name = "OPEN",
        string displayName = "Abierto")
    {
        return new IncidentStatus
        {
            Id = id,
            Name = name,
            DisplayName = displayName,
            Description = $"Estado {displayName}",
            OrderSequence = id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Crea un IncidentCategory de prueba
    /// </summary>
    public static IncidentCategory CreateTestIncidentCategory(
        Guid? id = null,
        string name = "Bug",
        bool isActive = true)
    {
        return new IncidentCategory
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Description = $"Categoria {name}",
            Color = "#FF0000",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }
}