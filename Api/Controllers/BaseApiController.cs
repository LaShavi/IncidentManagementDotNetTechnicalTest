using Api.DTOs.Common;
using Api.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    /// <summary>
    /// Base controller with standardized response methods and centralized error handling.
    /// </summary>
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        protected readonly ILogger Logger;

        protected BaseApiController(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Executes an action safely with automatic error handling.
        /// </summary>
        protected async Task<ActionResult<ApiResponse<T>>> ExecuteAsync<T>(
            Func<Task<T>> action, 
            string successMessage = "Operation completed successfully",
            object? meta = null)
        {
            try
            {
                var result = await action();
                var response = ApiResponseHelper.Success(result, successMessage, meta);
                return Ok(response);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in {ControllerName}: {Message}", 
                    GetType().Name, ex.Message);
                
                // Re-throw para que el middleware global lo maneje
                throw;
            }
        }

        /// <summary>
        /// Executes an action safely without return data.
        /// </summary>
        protected async Task<ActionResult<ApiResponse>> ExecuteAsync(
            Func<Task> action, 
            string successMessage = "Operation completed successfully")
        {
            try
            {
                await action();
                var response = new ApiResponse
                {
                    StatusCode = 200,
                    Success = true,
                    Message = successMessage
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in {ControllerName}: {Message}", 
                    GetType().Name, ex.Message);
                
                // Re-throw para que el middleware global lo maneje
                throw;
            }
        }

        /// <summary>
        /// Returns a standardized success response.
        /// </summary>
        protected ActionResult<ApiResponse<T>> SuccessResponse<T>(T data, string message = "Operation completed successfully", object? meta = null)
        {
            var response = ApiResponseHelper.Success(data, message, meta);
            return Ok(response);
        }

        /// <summary>
        /// Returns a standardized created response.
        /// </summary>
        protected ActionResult<ApiResponse<T>> CreatedResponse<T>(T data, string message = "Resource created successfully")
        {
            var response = ApiResponseHelper.Created(data, message);
            return StatusCode(201, response);
        }

        /// <summary>
        /// Returns a standardized validation error response.
        /// </summary>
        protected ActionResult<ApiResponse> ValidationErrorResponse(string message = "Validation failed")
        {
            var response = ApiResponseHelper.ValidationError(ModelState, message);
            return BadRequest(response);
        }

        /// <summary>
        /// Returns a standardized not found response.
        /// </summary>
        protected ActionResult<ApiResponse> NotFoundResponse(string message = "Resource not found")
        {
            var response = ApiResponseHelper.NotFound(message);
            return NotFound(response);
        }

        /// <summary>
        /// Returns a standardized bad request response.
        /// </summary>
        protected ActionResult<ApiResponse> BadRequestResponse(string message, object? error = null)
        {
            var response = ApiResponseHelper.BadRequest(message, error);
            return BadRequest(response);
        }

        /// <summary>
        /// Returns a standardized unauthorized response (401).
        /// </summary>
        protected ActionResult<ApiResponse> UnauthorizedResponse(string message = "Unauthorized access")
        {
            var response = ApiResponseHelper.Unauthorized(message);
            return Unauthorized(response);
        }

        /// <summary>
        /// Returns a standardized forbidden response (403).
        /// </summary>
        protected ActionResult<ApiResponse> ForbiddenResponse(string message = "Access forbidden")
        {
            var response = ApiResponseHelper.Forbidden(message);
            return StatusCode(403, response);
        }

        /// <summary>
        /// Returns a standardized no content response (204).
        /// </summary>
        protected ActionResult<ApiResponse> NoContentResponse(string message = "Operation completed successfully")
        {
            var response = ApiResponseHelper.NoContent(message);
            return StatusCode(204, response);
        }

        /// <summary>
        /// Returns a standardized conflict response (409).
        /// </summary>
        protected ActionResult<ApiResponse> ConflictResponse(string message = "Resource conflict")
        {
            var response = ApiResponseHelper.Conflict(message);
            return Conflict(response);
        }
    }
}