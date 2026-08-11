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
                break;

            case FluentValidation.ValidationException validationException:
                problem.Status = (int)HttpStatusCode.BadRequest;
                problem.Title = GetTitle(problem.Status);
                problem.Detail = "Uno o más datos proporcionados no son válidos.";
                problem.Errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());
                break;

            case FinancialConcurrencyException:
                problem.Status = (int)HttpStatusCode.Conflict;
                problem.Title = GetTitle(problem.Status);
                problem.Detail = exception.Message;
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
                problem.Detail = "Ocurrió un error inesperado.";
                break;
        }

        return problem;
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        (int)HttpStatusCode.BadRequest => "Solicitud inválida",
        (int)HttpStatusCode.Unauthorized => "No autorizado",
        (int)HttpStatusCode.Forbidden => "Acceso denegado",
        (int)HttpStatusCode.NotFound => "No encontrado",
        (int)HttpStatusCode.Conflict => "Conflicto",
        _ => "Error inesperado"
    };
}
