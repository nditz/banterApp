using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BanterApp.Api.Common;
using BanterApp.Api.Middleware;
using BanterApp.Api.Services;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BanterApp.Api.Tests.Errors;

public class ExceptionHandlingMiddlewareTests : IClassFixture<BanterAppWebApplicationFactory>
{
    private readonly BanterAppWebApplicationFactory _factory;

    public ExceptionHandlingMiddlewareTests(BanterAppWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task InvokeAsync_Production_DoesNotExposeDetail()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<BanterApp.Api.Data.AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment(Environments.Production));
        services.AddSingleton<IWebHostEnvironment>(sp => (IWebHostEnvironment)new TestHostEnvironment(Environments.Production));
        services.AddScoped<IErrorTrackingService, ErrorTrackingService>();
        var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Method = "GET";
        context.Response.Body = new MemoryStream();
        context.RequestServices = provider;
        context.Items[RequestIdMiddleware.ItemKey] = "req_test12345678";

        RequestDelegate next = _ => throw new InvalidOperationException("Sensitive internal failure at SecretModule");
        var middleware = new ExceptionHandlingMiddleware(next, provider.GetRequiredService<IServiceScopeFactory>(), provider.GetRequiredService<IHostEnvironment>());

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var bodyText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        using var doc = JsonDocument.Parse(bodyText);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal(ErrorCodes.InternalServerError, doc.RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.False(doc.RootElement.GetProperty("error").TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String);
        Assert.DoesNotContain("SecretModule", bodyText);
    }

    [Fact]
    public async Task ClientEndpoint_PreservesIncomingRequestId()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(RequestIdMiddleware.HeaderName, "test-request-id-12345678");

        var response = await client.PostAsJsonAsync("/api/errors/client", new { message = "test" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(RequestIdMiddleware.HeaderName, out var values));
        Assert.Equal("test-request-id-12345678", values!.Single());
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment, IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "BanterApp.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
