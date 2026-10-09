using Itacr.Api.Common.Http;
using Itacr.Application.Common.Results;
using Microsoft.AspNetCore.Http;

namespace Itacr.Api.Tests;

public sealed class ErrorStatusCodeTests
{
    [Theory]
    [InlineData(ErrorKind.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorKind.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorKind.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorKind.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorKind.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorKind.Locked, StatusCodes.Status423Locked)]
    public void StatusCodeOf_ShouldReturnTheStatusOfTheKind_WhenAUseCaseFails(ErrorKind kind, int statusCode)
    {
        Assert.Equal(statusCode, ErrorActionResults.StatusCodeOf(kind));
    }

    [Fact]
    public void StatusCodeOf_ShouldCoverEveryKind_WhenANewKindIsAdded()
    {
        Assert.All(Enum.GetValues<ErrorKind>(), kind => ErrorActionResults.StatusCodeOf(kind));
    }
}
