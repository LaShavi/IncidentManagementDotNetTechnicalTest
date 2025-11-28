using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Persistence.Entities
{
    public class ClienteEntity
    {
        [Key]        
        public Guid Id { get; set; }

        public string? Cedula { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
    }
}
