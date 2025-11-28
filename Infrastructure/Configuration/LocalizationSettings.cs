namespace Infrastructure.Configuration
{
    /// <summary>
    /// Configuración de localización para la aplicación
    /// Agrupa cultura, zona horaria y otras configuraciones regionales
    /// </summary>
    public class LocalizationSettings
    {
        /// <summary>
        /// Cultura por defecto (ej: "es", "en", "en-US")
        /// </summary>
        public string DefaultCulture { get; set; } = "es";
        
        /// <summary>
        /// Configuración de zona horaria
        /// </summary>
        public TimeZoneSettings TimeZone { get; set; } = new();
    }

    /// <summary>
    /// Configuración de zona horaria
    /// </summary>
    public class TimeZoneSettings
    {
        /// <summary>
        /// ID de la zona horaria del sistema (ej: "SA Pacific Standard Time", "America/Bogota")
        /// </summary>
        public string TimeZoneId { get; set; } = "SA Pacific Standard Time";
        
        /// <summary>
        /// Formato de visualización de fechas (ej: "dd/MM/yyyy HH:mm")
        /// </summary>
        public string DisplayFormat { get; set; } = "dd/MM/yyyy HH:mm";
    }
}