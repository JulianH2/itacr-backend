using Itacr.Api.Features.Auth;

namespace Itacr.Api.Tests.Auth;

public sealed class EmployeeDisplayNameTests
{
    [Theory]
    [InlineData("José Julián Hernández Hernández", "José H.")]
    [InlineData("Mesero de Prueba", "Mesero P.")]
    [InlineData("  Ana   martínez ", "Ana M.")]
    [InlineData("Carla", "Carla")]
    public void From_ShouldReturnTheFirstNameAndLastInitial_WhenTheFullNameHasSeveralWords(string fullName, string expected)
    {
        // Act
        var displayName = EmployeeDisplayName.From(fullName);

        // Assert
        Assert.Equal(expected, displayName);
    }
}
