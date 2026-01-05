using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.Common
{
    /// <summary>
    /// Generic API Response wrapper
    /// </summary>
    /// <typeparam name="T">Data type</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string Message { get; set; } = "";
        public List<string> Errors { get; set; } = new List<string>();
        public int? StatusCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Create success response
        /// </summary>
        public static ApiResponse<T> SuccessResult(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message,
                StatusCode = 200
            };
        }

        /// <summary>
        /// Create error response
        /// </summary>
        public static ApiResponse<T> ErrorResult(string message, List<string> errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>(),
                StatusCode = 400
            };
        }

        /// <summary>
        /// Create error response with specific status code
        /// </summary>
        public static ApiResponse<T> ErrorResult(string message, int statusCode, List<string> errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                StatusCode = statusCode,
                Errors = errors ?? new List<string>()
            };
        }
    }
}
