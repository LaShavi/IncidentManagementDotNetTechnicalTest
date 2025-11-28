using Domain.Entities;

namespace Tests.Fixtures;

public static class TestDataFixtures
{
    public static Cliente CreateTestCliente(
        Guid? id = null,
        string cedula = "12345678",
        string email = "test@test.com",
        string telefono = "555-1234",
        string nombre = "Juan",
        string apellido = "Perez")
    {
        return new Cliente
        {
            Id = id ?? Guid.NewGuid(),
            Cedula = cedula,
            Email = email,
            Telefono = telefono,
            Nombre = nombre,
            Apellido = apellido
        };
    }

    public static List<Cliente> CreateTestClientes(int count = 3)
    {
        var clientes = new List<Cliente>();
        
        for (int i = 0; i < count; i++)
        {
            clientes.Add(CreateTestCliente(
                cedula: $"1234567{i}",
                email: $"test{i}@test.com",
                telefono: $"555-123{i}",
                nombre: $"Test{i}",
                apellido: $"User{i}"
            ));
        }

        return clientes;
    }

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
}