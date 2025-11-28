using System.Text.Json.Serialization;

namespace Api.DTOs.Common
{
    /// <summary>
    /// Standard API response wrapper for all endpoints.
    /// </summary>
    public class ApiResponse<T>
    {
        /// <summary>
        /// HTTP status code of the response.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Indicates if the operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Human-readable message describing the result.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Error details if the operation failed.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Error { get; set; }

        /// <summary>
        /// The actual response data.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T? Data { get; set; }

        /// <summary>
        /// Additional metadata (pagination, timestamps, etc.).
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Meta { get; set; }

        /// <summary>
        /// Validation errors from model validation.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string[]>? ValidationErrors { get; set; }
    }

    /// <summary>
    /// Non-generic version for responses without data.
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
    }
}