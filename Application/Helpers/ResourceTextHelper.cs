using System.Globalization;
using System.Resources;
using Microsoft.Extensions.Configuration;

namespace Application.Helpers
{
    /// <summary>
    /// Helper para acceder a cadenas localizadas desde los archivos .resx
    /// Soporta localización automática basada en configuración o cultura explícita
    /// </summary>
    public static class ResourceTextHelper
    {
        private static readonly ResourceManager ResourceManager = new ResourceManager("Application.Resources.LocalizedStrings", typeof(ResourceTextHelper).Assembly);
        private static IConfiguration? _configuration;

        /// <summary>
        /// Inicializa el helper con la configuración de la aplicación
        /// </summary>
        /// <param name="configuration">Configuración de la aplicación</param>
        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Obtiene una cadena localizada usando la cultura configurada por defecto
        /// </summary>
        /// <param name="key">Clave del recurso</param>
        /// <returns>Cadena localizada o la clave si no se encuentra</returns>
        public static string Get(string key)
        {
            var culture = GetCurrentCulture();
            return ResourceManager.GetString(key, culture) ?? key;
        }

        /// <summary>
        /// Obtiene una cadena localizada usando una cultura específica
        /// </summary>
        /// <param name="key">Clave del recurso</param>
        /// <param name="cultureName">Nombre de la cultura (ej: "es", "en", "en-US")</param>
        /// <returns>Cadena localizada o la clave si no se encuentra</returns>
        public static string Get(string key, string cultureName)
        {
            if (string.IsNullOrWhiteSpace(cultureName))
            {
                return Get(key);
            }

            try
            {
                var culture = new CultureInfo(cultureName);
                return ResourceManager.GetString(key, culture) ?? key;
            }
            catch (CultureNotFoundException)
            {
                // Si la cultura no es válida, usar la predeterminada
                return Get(key);
            }
        }

        /// <summary>
        /// Obtiene la cultura actual configurada en la aplicación
        /// </summary>
        /// <returns>CultureInfo basada en la configuración</returns>
        private static CultureInfo GetCurrentCulture()
        {
            // Obtener el idioma desde la configuración
            var defaultCulture = _configuration?["Localization:DefaultCulture"] ?? "es";

            try
            {
                return new CultureInfo(defaultCulture);
            }
            catch (CultureNotFoundException)
            {
                // Si el idioma configurado no es válido, usar español como fallback
                return new CultureInfo("es");
            }
        }
    }
}