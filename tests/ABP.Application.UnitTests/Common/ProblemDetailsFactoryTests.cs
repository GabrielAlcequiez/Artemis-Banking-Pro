using ABP.Application.Common.Contracts;
using ABP.Application.Exceptions;
using FluentValidation;
using FluentValidation.Results;

namespace ABP.Application.UnitTests.Common;

public sealed class ProblemDetailsFactoryTests
{
    [Fact]
    public void Validation_exception_returns_spanish_problem_details()
    {
        var exception = new ValidationException(
            [new ValidationFailure("CreditLimit", "El límite debe ser mayor que cero.")]);

        var problem = ProblemDetailsFactory.Create(
            exception,
            "trace-1",
            "/api/v1/CreditCards");

        Assert.Equal(400, problem.Status);
        Assert.Equal("Solicitud inválida", problem.Title);
        Assert.Equal("Uno o más datos proporcionados no son válidos.", problem.Detail);
        Assert.Equal(
            ["El límite debe ser mayor que cero."],
            problem.Errors!["CreditLimit"]);
    }

    [Fact]
    public void Financial_concurrency_exception_returns_conflict_without_internal_details()
    {
        var exception = new FinancialConcurrencyException(
            new InvalidOperationException("detalle interno"));

        var problem = ProblemDetailsFactory.Create(
            exception,
            "trace-2",
            "/api/v1/CreditCards/card-id/limit");

        Assert.Equal(409, problem.Status);
        Assert.Equal("Conflicto", problem.Title);
        Assert.DoesNotContain("detalle interno", problem.Detail);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Persistence_conflict_returns_conflict_without_database_details()
    {
        var exception = new PersistenceConflictException(
            new InvalidOperationException("índice IX_Commerces_Email"));

        var problem = ProblemDetailsFactory.Create(
            exception,
            "trace-3",
            "/api/v1/Commerce");

        Assert.Equal(409, problem.Status);
        Assert.Equal("Conflicto", problem.Title);
        Assert.Equal(
            "La operación entra en conflicto con datos que ya existen.",
            problem.Detail);
        Assert.DoesNotContain("IX_Commerces_Email", problem.Detail);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Persistence_failure_returns_generic_details_without_database_exception()
    {
        var exception = new PersistenceFailureException(
            new InvalidOperationException("4000000000001234"));

        var problem = ProblemDetailsFactory.Create(
            exception,
            "trace-4",
            "/api/v1/CreditCards");

        Assert.Equal(500, problem.Status);
        Assert.Equal("Error inesperado", problem.Title);
        Assert.Equal("Ocurrió un error inesperado.", problem.Detail);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain("4000000000001234", problem.ToString());
    }

    [Fact]
    public void Unexpected_exception_returns_generic_spanish_problem_details()
    {
        var problem = ProblemDetailsFactory.Create(
            new InvalidOperationException("dato sensible"),
            "trace-5",
            "/api/v1/CreditCards");

        Assert.Equal(500, problem.Status);
        Assert.Equal("Error inesperado", problem.Title);
        Assert.Equal("Ocurrió un error inesperado.", problem.Detail);
        Assert.DoesNotContain("dato sensible", problem.Detail);
    }
}
