using Application.DTOs.Cliente;
using Application.Ports;
using Application.Helpers;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Api.DTOs.Common;
using AutoMapper;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Proteger todo el controlador con JWT
    public class ClienteController : BaseApiController
    {
        private readonly IClienteService _clienteService;
        private readonly IMapper _mapper;

        public ClienteController(
            IClienteService clienteService,
            IMapper mapper,
            ILogger<ClienteController> logger) : base(logger)
        {
            _clienteService = clienteService;
            _mapper = mapper;
        }

        /// <summary>
        /// Obtener todos los clientes
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<ClienteResponseDTO>>>> GetAll()
        {
            return await ExecuteAsync(async () =>
            {
                var clientes = await _clienteService.GetAllAsync();
                var response = _mapper.Map<IEnumerable<ClienteResponseDTO>>(clientes);
                return response;
            }, "Clientes obtenidos exitosamente");
        }

        /// <summary>
        /// Obtener cliente por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ClienteResponseDTO>>> GetById(Guid id)
        {
            return await ExecuteAsync(async () =>
            {
                var cliente = await _clienteService.GetByIdAsync(id);
                if (cliente == null)
                    throw new KeyNotFoundException(ResourceTextHelper.Get("ClienteNotFound"));

                return _mapper.Map<ClienteResponseDTO>(cliente);
            }, "Cliente obtenido exitosamente");
        }

        /// <summary>
        /// Crear un nuevo cliente
        /// </summary>
        [HttpPost]
        //public async Task<ActionResult<ApiResponse<ClienteResponseDTO>>> Create(CreateClienteDTO dto)
        public async Task<ActionResult<ApiResponse>> Create(CreateClienteDTO dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse();

            return await ExecuteAsync(async () =>
            {
                var cliente = new Cliente
                {
                    Id = Guid.NewGuid(),
                    Cedula = dto.Cedula,
                    Email = dto.Email,
                    Telefono = dto.Telefono,
                    Nombre = dto.Nombre,
                    Apellido = dto.Apellido
                };

                await _clienteService.AddAsync(cliente);

                //return _mapper.Map<ClienteResponseDTO>(cliente);
            }, ResourceTextHelper.Get("ClienteCreated"));
        }

        /// <summary>
        /// Actualizar un cliente existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse>> Update(Guid id, UpdateClienteDTO dto)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse();

            if (id != dto.Id)
                return BadRequestResponse(ResourceTextHelper.Get("IdMismatch"));

            return await ExecuteAsync(async () =>
            {
                var existingCliente = await _clienteService.GetByIdAsync(id);
                if (existingCliente == null)
                    throw new KeyNotFoundException(ResourceTextHelper.Get("ClienteNotFound"));

                var cliente = new Cliente
                {
                    Id = dto.Id,
                    Cedula = dto.Cedula,
                    Email = dto.Email,
                    Telefono = dto.Telefono,
                    Nombre = dto.Nombre,
                    Apellido = dto.Apellido
                };

                await _clienteService.UpdateAsync(cliente);
            }, ResourceTextHelper.Get("ClienteUpdated"));
        }

        /// <summary>
        /// Eliminar un cliente
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            return await ExecuteAsync(async () =>
            {
                var cliente = await _clienteService.GetByIdAsync(id);
                if (cliente == null)
                    throw new KeyNotFoundException(ResourceTextHelper.Get("ClienteNotFound"));

                await _clienteService.DeleteAsync(id);
            }, ResourceTextHelper.Get("ClienteDeleted"));
        }
    }
}
