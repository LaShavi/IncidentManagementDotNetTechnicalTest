using Api.DTOs.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Api.Helpers
{
    /// <summary>
    /// Helper methods for creating standardized API responses.
    /// </summary>
    public static class ApiResponseHelper
    {
        /// <summary>
        /// Creates a successful response with data.
        /// </summary>
        public static ApiResponse<T> Success<T>(T data, string message = "Operation completed successfully", object? meta = null)
        {
            return new ApiResponse<T>
            {
                StatusCode = 200,
                Success = true,
                Message = message,
                Data = data,
                Meta = meta
            };
        }

        /// <summary>
        /// Creates a successful response without data.
        /// </summary>
        public static ApiResponse Success(string message = "Operation completed successfully", object? meta = null)
        {
            return new ApiResponse
            {
                StatusCode = 200,
                Success = true,
                Message = message,
                Meta = meta
            };
        }

        /// <summary>
        /// Creates a created response (201).
        /// </summary>
        public static ApiResponse<T> Created<T>(T data, string message = "Resource created successfully")
        {
            return new ApiResponse<T>
            {
                StatusCode = 201,
                Success = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// Creates a no content response (204).
        /// </summary>
        public static ApiResponse NoContent(string message = "Operation completed successfully")
        {
            return new ApiResponse
            {
                StatusCode = 204,
                Success = true,
                Message = message
            };
        }

        /// <summary>
        /// Creates a bad request response (400).
        /// </summary>
        public static ApiResponse BadRequest(string message = "Bad request", object? error = null)
        {
            return new ApiResponse
            {
                StatusCode = 400,
                Success = false,
                Message = message,
                Error = error
            };
        }

        /// <summary>
        /// Creates a validation error response (400).
        /// </summary>
        public static ApiResponse ValidationError(ModelStateDictionary modelState, string message = "Validation failed")
        {
            var errors = modelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(x => x.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );

            return new ApiResponse
            {
                StatusCode = 400,
                Success = false,
                Message = message,
                ValidationErrors = errors
            };
        }

        /// <summary>
        /// Creates an unauthorized response (401).
        /// </summary>
        public static ApiResponse Unauthorized(string message = "Unauthorized access")
        {
            return new ApiResponse
            {
                StatusCode = 401,
                Success = false,
                Message = message
            };
        }

        /// <summary>
        /// Creates a forbidden response (403).
        /// </summary>
        public static ApiResponse Forbidden(string message = "Access forbidden")
        {
            return new ApiResponse
            {
                StatusCode = 403,
                Success = false,
                Message = message
            };
        }

        /// <summary>
        /// Creates a not found response (404).
        /// </summary>
        public static ApiResponse NotFound(string message = "Resource not found")
        {
            return new ApiResponse
            {
                StatusCode = 404,
                Success = false,
                Message = message
            };
        }

        /// <summary>
        /// Creates an internal server error response (500).
        /// </summary>
        public static ApiResponse InternalServerError(string message = "Internal server error", object? error = null)
        {
            return new ApiResponse
            {
                StatusCode = 500,
                Success = false,
                Message = message,
                Error = error
            };
        }

        /// <summary>
        /// Creates a conflict response (409).
        /// </summary>
        public static ApiResponse Conflict(string message = "Resource conflict")
        {
            return new ApiResponse
            {
                StatusCode = 409,
                Success = false,
                Message = message
            };
        }
    }
}