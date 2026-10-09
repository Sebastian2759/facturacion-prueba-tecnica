using Application.Behaviors;
using FluentAssertions;
using FluentValidation;
using MediatR;

namespace Test.Application;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithAsyncValidationRule_AwaitsValidatorAndStopsPipeline()
    {
        var validator = new AsyncValidationRequestValidator();
        var behavior = new ValidationBehavior<AsyncValidationRequest, string>(
            [validator]);
        var nextWasCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextWasCalled = true;
            return Task.FromResult("handled");
        };

        Func<Task> action = async () =>
            await behavior.Handle(
                new AsyncValidationRequest("invalid"),
                next,
                TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<ValidationException>();
        nextWasCalled.Should().BeFalse();
    }

    private sealed record AsyncValidationRequest(string Value) : IRequest<string>;

    private sealed class AsyncValidationRequestValidator
        : AbstractValidator<AsyncValidationRequest>
    {
        public AsyncValidationRequestValidator()
        {
            RuleFor(request => request.Value)
                .MustAsync(async (_, cancellationToken) =>
                {
                    await Task.Yield();
                    cancellationToken.ThrowIfCancellationRequested();
                    return false;
                });
        }
    }
}
