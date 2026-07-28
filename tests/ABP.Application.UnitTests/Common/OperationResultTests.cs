using ABP.Application.Common;

namespace ABP.Application.UnitTests.Common;

public sealed class OperationResultTests
{
    [Fact]
    public void Failure_preserves_the_error_and_has_no_value()
    {
        var error = new Error("validation.required", "A required value is missing.");

        var result = OperationResult<string>.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }
}
