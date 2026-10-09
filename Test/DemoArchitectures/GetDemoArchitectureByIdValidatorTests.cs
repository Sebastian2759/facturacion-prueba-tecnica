using Application.UseCases.DemoArchitectures.GetDemoArchitectureById;
using FluentAssertions;

namespace Test.DemoArchitectures;

public sealed class GetDemoArchitectureByIdValidatorTests
{
    private readonly GetDemoArchitectureByIdValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WhenIdIsEmpty_ReturnsValidationError()
    {
        var request = new GetDemoArchitectureByIdRequest();

        var result = await _validator.ValidateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(request.Id));
    }

    [Fact]
    public async Task ValidateAsync_WhenIdHasValue_IsValid()
    {
        var request = new GetDemoArchitectureByIdRequest
        {
            Id = Guid.NewGuid(),
        };

        var result = await _validator.ValidateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }
}
