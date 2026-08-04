using System.ComponentModel.DataAnnotations;
using System.Net;
using ABP.Application.Exceptions;

namespace ABP.Application.Common.Contracts;

public static class ProblemDetailsFactory
{
    public static AbpProblemDetails Create(
        Exception exception,
        string? traceId,
        string? instance)
    {
        var problem = new AbpProblemDetails
        {
            Type = "about:blank",
            TraceId = traceId,
            Instance = instance
        };

        switch (exception)
        {
            case ApiException apiException:
                var status = apiException.StatusCode;
                if (status is < (int)HttpStatusCode.BadRequest or >= (int)HttpStatusCode.InternalServerError)
                {
                    status = (int)HttpStatusCode.InternalServerError;
                }

                problem.Status = status;
                problem.Title = GetTitle(status);
                problem.Detail = apiException.Message;
                problem.ErrorCode = apiException.ErrorCode;
                break;

            case FluentValidation.ValidationException validationException:
                problem.Status = (int)HttpStatusCode.BadRequest;
                problem.Title = GetTitle(problem.Status);
                problem.Detail = "One or more validation errors occurred.";
                problem.Errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());
                break;

            case KeyNotFoundException:
                problem.Status = (int)HttpStatusCode.NotFound;
                problem.Title = GetTitle(problem.Status);
                problem.Detail = exception.Message;
                break;

            case UnauthorizedAccessException:
                problem.Status = (int)HttpStatusCode.Forbidden;
                problem.Title = GetTitle(problem.Status);
                problem.Detail = exception.Message;
                break;

            case ArgumentException or ValidationException:
                problem.Status = (int)HttpStatusCode.BadRequest;
                problem.Title = GetTitle(problem.Status);
                problem.Detail = exception.Message;
                break;

            default:
                problem.Status = (int)HttpStatusCode.InternalServerError;
                problem.Title = GetTitle(problem.Status);
                problem.Detail = "An unexpected error occurred.";
                break;
        }

        return problem;
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        (int)HttpStatusCode.BadRequest => "Bad Request",
        (int)HttpStatusCode.Unauthorized => "Unauthorized",
        (int)HttpStatusCode.Forbidden => "Forbidden",
        (int)HttpStatusCode.NotFound => "Not Found",
        (int)HttpStatusCode.Conflict => "Conflict",
        _ => "An unexpected error occurred"
    };
}
