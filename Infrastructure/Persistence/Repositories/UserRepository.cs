using Application.Ports;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repository for managing user entities in the database.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<UserRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRepository"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="mapper">The AutoMapper instance.</param>
        /// <param name="logger">The logger instance.</param>
        public UserRepository(AppDbContext context, IMapper mapper, ILogger<UserRepository> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _logger.LogDebug("UserRepository initialized successfully");
        }

        /// <summary>
        /// Gets a user by their unique identifier.
        /// </summary>
        /// <param name="id">The user's ID.</param>
        /// <returns>The user if found; otherwise, null.</returns>
        public async Task<User?> GetByIdAsync(Guid id)
        {
            _logger.LogDebug("Retrieving user by ID: {UserId}", id);
            
            try
            {
                var entity = await _context.Users.FindAsync(id);
                
                if (entity == null)
                {
                    _logger.LogDebug("User not found with ID: {UserId}", id);
                    return null;
                }
                
                var user = _mapper.Map<User>(entity);
                _logger.LogDebug("User retrieved successfully: {UserId} - {Username}", id, user.Username);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by ID: {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Gets a user by their username.
        /// </summary>
        /// <param name="username">The username to search for.</param>
        /// <returns>The user if found; otherwise, null.</returns>
        public async Task<User?> GetByUsernameAsync(string username)
        {
            _logger.LogDebug("Retrieving user by username: {Username}", username);
            
            try
            {
                var entity = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);
                    
                if (entity == null)
                {
                    _logger.LogDebug("User not found with username: {Username}", username);
                    return null;
                }
                
                var user = _mapper.Map<User>(entity);
                _logger.LogDebug("User retrieved successfully by username: {Username} (UserId: {UserId})", username, user.Id);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by username: {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Gets a user by their email address.
        /// </summary>
        /// <param name="email">The email to search for.</param>
        /// <returns>The user if found; otherwise, null.</returns>
        public async Task<User?> GetByEmailAsync(string email)
        {
            _logger.LogDebug("Retrieving user by email: {Email}", email);
            
            try
            {
                var entity = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
                    
                if (entity == null)
                {
                    _logger.LogDebug("User not found with email: {Email}", email);
                    return null;
                }
                
                var user = _mapper.Map<User>(entity);
                _logger.LogDebug("User retrieved successfully by email: {Email} (UserId: {UserId}, Username: {Username})", 
                    email, user.Id, user.Username);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// Gets all users from the database.
        /// </summary>
        /// <returns>A collection of all users.</returns>
        public async Task<IEnumerable<User>> GetAllAsync()
        {
            _logger.LogInformation("Retrieving all users");
            
            try
            {
                var entities = await _context.Users.ToListAsync();
                var users = _mapper.Map<IEnumerable<User>>(entities);
                var count = users.Count();
                
                _logger.LogInformation("Retrieved {Count} users successfully", count);
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users");
                throw;
            }
        }

        /// <summary>
        /// Adds a new user to the database.
        /// </summary>
        /// <param name="user">The user to add.</param>
        public async Task AddAsync(User user)
        {
            _logger.LogInformation("Adding new user: {Username} (Email: {Email})", user.Username, user.Email);
            
            try
            {
                var entity = _mapper.Map<UserEntity>(user);
                _context.Users.Add(entity);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("User added successfully: {UserId} - {Username}", user.Id, user.Username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user: {Username} (Email: {Email})", user.Username, user.Email);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing user in the database.
        /// </summary>
        /// <param name="user">The user with updated values.</param>
        public async Task UpdateAsync(User user)
        {
            _logger.LogDebug("Updating user: {UserId} - {Username}", user.Id, user.Username);
            
            try
            {
                var entity = await _context.Users.FindAsync(user.Id);
                if (entity != null)
                {
                    // Map updated fields from domain user to entity
                    _mapper.Map(user, entity);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogDebug("User updated successfully: {UserId} - {Username}", user.Id, user.Username);
                }
                else
                {
                    _logger.LogWarning("Attempted to update non-existent user: {UserId}", user.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId} - {Username}", user.Id, user.Username);
                throw;
            }
        }

        /// <summary>
        /// Deletes a user from the database by their ID.
        /// </summary>
        /// <param name="id">The user's ID.</param>
        public async Task DeleteAsync(Guid id)
        {
            _logger.LogInformation("Deleting user: {UserId}", id);
            
            try
            {
                var entity = await _context.Users.FindAsync(id);
                if (entity != null)
                {
                    _context.Users.Remove(entity);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("User deleted successfully: {UserId} - {Username}", id, entity.Username);
                }
                else
                {
                    _logger.LogWarning("Attempted to delete non-existent user: {UserId}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Checks if a username already exists in the database.
        /// </summary>
        /// <param name="username">The username to check.</param>
        /// <returns>True if the username exists; otherwise, false.</returns>
        public async Task<bool> ExistsUsernameAsync(string username)
        {
            _logger.LogDebug("Checking if username exists: {Username}", username);
            
            try
            {
                var exists = await _context.Users.AnyAsync(u => u.Username == username);
                _logger.LogDebug("Username existence check for {Username}: {Exists}", username, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking username existence: {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Checks if an email already exists in the database.
        /// </summary>
        /// <param name="email">The email to check.</param>
        /// <returns>True if the email exists; otherwise, false.</returns>
        public async Task<bool> ExistsEmailAsync(string email)
        {
            _logger.LogDebug("Checking if email exists: {Email}", email);
            
            try
            {
                var exists = await _context.Users.AnyAsync(u => u.Email == email);
                _logger.LogDebug("Email existence check for {Email}: {Exists}", email, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email existence: {Email}", email);
                throw;
            }
        }
    }
}