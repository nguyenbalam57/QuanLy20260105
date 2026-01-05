using System;
using System.Collections.Generic;
using System.Text;

namespace ManagementFile.Contracts.DTOs.Common
{
    /// <summary>
    /// Admin API Response wrapper (extends ApiResponse)
    /// </summary>
    /// <typeparam name="T">Data type</typeparam>
    public class AdminApiResponse<T> : ApiResponse<T>
    {
        public string AdminAction { get; set; } = "";
        public string PerformedBy { get; set; } = "";

        /// <summary>
        /// Create admin success response
        /// </summary>
        public static AdminApiResponse<T> SuccessResult(T data, string message = "Success")
        {
            return new AdminApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message,
                StatusCode = 200
            };
        }

        /// <summary>
        /// Create admin error response
        /// </summary>
        public static AdminApiResponse<T> ErrorResult(string message)
        {
            return new AdminApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = new List<string>(),
                StatusCode = 400
            };
        }
    }
}
