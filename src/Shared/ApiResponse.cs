using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public static ApiResponse<T> SuccessResponse(T data, string? message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }
        public static ApiResponse<T> FailureResponse(List<string>? errors= null, string? message = "Failure")
        {
            return new ApiResponse<T>
            {
                Success = false,
                Errors = errors,
                Message = message
            };
        }
    }
}
