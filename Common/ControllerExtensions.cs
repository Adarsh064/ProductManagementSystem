using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using ProductManagementSystem.DTOs;
using System.Net;
using System.Text;

namespace ProductManagementSystem.Common
{
    public static class ControllerExtensions
    {

        public static ActionResult ApiResponse<T>(this ControllerBase controller, string message, HttpStatusCode statusCode = HttpStatusCode.OK, T? data = default)
        {
            var response = new ApiResponseDto<T>
            {
                Status = statusCode == HttpStatusCode.OK,
                Message = message,
                Data = data
            };

            return controller.StatusCode((int)statusCode, response);
        }

        /// <summary>
        /// Return BadRequest (400) with validation errors
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="failures">The validation failures to be included in the response.</param>
        /// <returns>An ActionResult containing the validation error response.</returns>
        public static ActionResult ValidationError(this ControllerBase controller, IEnumerable<ValidationFailure> failures)
        {
            var message = FormatValidationErrors(failures);
            return controller.BadRequest(new ApiResponseDto<object>
            {
                Status = false,
                Message = message
            });
        }

        /// <summary>
        /// Helper to format validation errors
        /// </summary>
        /// <param name="failures">A list of validation failures to be formatted into a single error message.</param>
        /// <returns></returns>
        private static string FormatValidationErrors(IEnumerable<ValidationFailure> failures)
        {
            var sb = new StringBuilder();
            foreach (var error in failures)
            {
                sb.AppendLine(error.ErrorMessage);
            }
            return sb.ToString().TrimEnd();
        }
    }
}
