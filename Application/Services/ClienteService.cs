using Application.Ports;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class ClienteService : IClienteService
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly ILogger<ClienteService> _logger;

        public ClienteService(IClienteRepository clienteRepository, ILogger<ClienteService> logger)
        {
            _clienteRepository = clienteRepository;
            _logger = logger;
            _logger.LogDebug("ClienteService initialized successfully");
        }

        public async Task<IEnumerable<Cliente>> GetAllAsync()
        {
            _logger.LogInformation("Retrieving all clientes");
            
            var clientes = await _clienteRepository.GetAllAsync();
            var count = clientes.Count();
            
            _logger.LogInformation("Retrieved {Count} clientes successfully", count);
            return clientes;
        }

        public async Task<Cliente?> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Retrieving cliente by ID: {ClienteId}", id);
            
            var cliente = await _clienteRepository.GetByIdAsync(id);
            
            if (cliente == null)
            {
                _logger.LogWarning("Cliente not found with ID: {ClienteId}", id);
                return null;
            }
            
            _logger.LogInformation("Cliente retrieved successfully: {ClienteId} - {Nombre} {Apellido}", 
                cliente.Id, cliente.Nombre, cliente.Apellido);
            return cliente;
        }

        public async Task AddAsync(Cliente cliente)
        {
            _logger.LogInformation("Creating new cliente: {Nombre} {Apellido} (Email: {Email}, Cedula: {Cedula})", 
                cliente.Nombre, cliente.Apellido, cliente.Email, cliente.Cedula);
            
            if (!cliente.EsValido())
            {
                _logger.LogWarning("Attempted to create invalid cliente: {Nombre} {Apellido} (ID: {ClienteId})", 
                    cliente.Nombre, cliente.Apellido, cliente.Id);
            }
            
            await _clienteRepository.AddAsync(cliente);
            
            _logger.LogInformation("Cliente created successfully: {ClienteId} - {Nombre} {Apellido}", 
                cliente.Id, cliente.Nombre, cliente.Apellido);
        }

        public async Task UpdateAsync(Cliente cliente)
        {
            _logger.LogInformation("Updating cliente: {ClienteId} - {Nombre} {Apellido}", 
                cliente.Id, cliente.Nombre, cliente.Apellido);
            
            if (!cliente.EsValido())
            {
                _logger.LogWarning("Attempted to update with invalid data for cliente: {ClienteId}", cliente.Id);
            }
            
            await _clienteRepository.UpdateAsync(cliente);
            
            _logger.LogInformation("Cliente updated successfully: {ClienteId} - {Nombre} {Apellido}", 
                cliente.Id, cliente.Nombre, cliente.Apellido);
        }

        public async Task DeleteAsync(Guid id)
        {
            _logger.LogInformation("Deleting cliente: {ClienteId}", id);
            
            await _clienteRepository.DeleteAsync(id);
            
            _logger.LogInformation("Cliente deleted successfully: {ClienteId}", id);
        }
    }
}
