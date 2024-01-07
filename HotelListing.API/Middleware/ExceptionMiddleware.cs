using HotelListing.API.Exceptions;
using Newtonsoft.Json;
using System.Net;

namespace HotelListing.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;  // intercepts all requests objects
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            this._next = next;
            this._logger = logger;
        }

        public async Task InvokeAsync(HttpContext context) 
        {
            try
            {
                await _next(context);  // intercept/handle all requests
            }
            catch (Exception ex)  // catch any HTTP exception
            {
                _logger.LogError(ex,
                    $"Something went wrong while processing {nameof(context.Request.Path)}");
                await HandleExceptionAsync(context, ex);  //handle with own custom method
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;  // 500 InternalServerError
            var errorDetails = new ErrorDetails  // to be stored in JSON
            {
                ErrorType = "Failure",
                ErrorMessage = ex.Message,
            };
            switch (ex)  // switch between Exception types and assign our own logic
            {
                case NotFoundException notFoundException:  //catching our custom global exception

                    statusCode = HttpStatusCode.NotFound;
                    errorDetails.ErrorType = "Not Found";
                    break;
                default:
                    break;
            }
            string response = JsonConvert.SerializeObject(errorDetails);  //return custom response
            context.Response.StatusCode = (int)statusCode;  // assign custom status code
            return context.Response.WriteAsync(response);
        }
    }

    public class ErrorDetails
    {
        public string ErrorType { get; set; }
        public string ErrorMessage { get; set; }
    }
}
