using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    public class ApiException : Exception
    {
        public int StatusCode { get; }
        public ApiException(string message, int statusCode = StatusCodes.Status400BadRequest) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
