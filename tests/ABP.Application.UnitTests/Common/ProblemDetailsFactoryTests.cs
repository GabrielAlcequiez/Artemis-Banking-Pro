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
            "/api/credit-card");

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
            "/api/credit-card/card-id/limit");

        Assert.Equal(409, problem.Status);
        Assert.Equal("Conflicto", problem.Title);
        Assert.DoesNotContain("detalle interno", problem.Detail);
    }

    [Fact]
    public void Persistence_conflict_returns_conflict_without_database_details()
    {
        var exception = new PersistenceConflictException(
            new InvalidOperationException("índice IX_Commerces_Email"));

        var problem = ProblemDetailsFactory.Create(
            exception,
            "trace-3",
            "/api/commerce");

        Assert.Equal(409, problem.Status);
        Assert.Equal("Conflicto", problem.Title);
        Assert.Equal(
            "La operación entra en conflicto con datos que ya existen.",
            problem.Detail);
        Assert.DoesNotContain("IX_Commerces_Email", problem.Detail);
    }

    [Fact]
    public void Unexpected_exception_returns_generic_spanish_problem_details()
    {
        var problem = ProblemDetailsFactory.Create(
            new InvalidOperationException("dato sensible"),
            "trace-4",
            "/api/credit-card");

        Assert.Equal(500, problem.Status);
        Assert.Equal("Error inesperado", problem.Title);
        Assert.Equal("Ocurrió un error inesperado.", problem.Detail);
        Assert.DoesNotContain("dato sensible", problem.Detail);
    }
}
