using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Domain.Entities
{
    public class Cliente
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Cedula { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(15)]
        public string Telefono { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Apellido { get; set; } = string.Empty;

        // Método de dominio para obtener nombre completo
        public string ObtenerNombreCompleto()
        {
            return $"{Nombre} {Apellido}".Trim();
        }

        // Método de dominio para validar si el cliente es válido
        public bool EsValido()
        {
            return !string.IsNullOrWhiteSpace(Cedula) &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Telefono) &&
                   !string.IsNullOrWhiteSpace(Nombre) &&
                   !string.IsNullOrWhiteSpace(Apellido) &&
                   EsEmailValido(Email);
        }

        private bool EsEmailValido(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Regex simple para validar email
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
                return emailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }
    }
}
