using BanterApp.Api.Services;

namespace BanterApp.Api.Common;

public sealed class ErrorEnvelopeFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return await next(context);
    }
}

public static class ErrorEnvelopeExtensions
{
    public static RouteGroupBuilder WithErrorEnvelope(this RouteGroupBuilder group) =>
        group.AddEndpointFilter<ErrorEnvelopeFilter>();

    public static RouteHandlerBuilder WithErrorEnvelope(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<ErrorEnvelopeFilter>();
}
