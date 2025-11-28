using Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Helpers
{
    /// <summary>
    /// Ayudante para gestionar conversiones de zona horaria
    /// Obtiene la configuración desde LocalizationSettings
    /// </summary>
    public class TimeZoneHelper
    {
        private readonly LocalizationSettings _localizationSettings;
        private readonly TimeZoneInfo _timeZone;

        public TimeZoneHelper(IOptions<LocalizationSettings> localizationSettings)
        {
            _localizationSettings = localizationSettings.Value;
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(_localizationSettings.TimeZone.TimeZoneId);
        }

        /// <summary>
        /// Convierte una fecha UTC a la zona horaria configurada
        /// </summary>
        public DateTime ConvertToLocal(DateTime utcDateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _timeZone);
        }

        /// <summary>
        /// Convierte y formatea una fecha UTC según el formato configurado
        /// </summary>
        public string ConvertAndFormat(DateTime utcDateTime)
        {
            var localDateTime = ConvertToLocal(utcDateTime);
            return localDateTime.ToString(_localizationSettings.TimeZone.DisplayFormat);
        }

        /// <summary>
        /// Obtiene la zona horaria actual configurada
        /// </summary>
        public TimeZoneInfo GetTimeZone() => _timeZone;

        /// <summary>
        /// Obtiene el formato de visualización configurado
        /// </summary>
        public string GetDisplayFormat() => _localizationSettings.TimeZone.DisplayFormat;
    }
}