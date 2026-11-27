using Itacr.Application.Common.Results;

namespace Itacr.Api.Tests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Value_ShouldThrow_WhenTheResultFailed()
    {
        // Arrange
        var result = Result<int>.Failure(new Error(ErrorKind.NotFound, "Not found", "No existe."));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => result.Value);

        // Assert
        Assert.Equal("A failed result has no value.", exception.Message);
    }
}
