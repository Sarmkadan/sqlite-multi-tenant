#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Integration;
using Xunit;

namespace SqliteMultiTenant.Tests;

public sealed class HttpClientWrapperTests
{
    [Fact]
    public async Task GetAsync_ThrowsForNullOrEmptyUrl()
    {
        // Arrange
        var wrapper = CreateWrapper(new StubHandler());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => wrapper.GetAsync<Item>(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => wrapper.GetAsync<Item>(string.Empty));
    }

    [Fact]
    public async Task GetAsync_DeserializesBody_WhenResponseIsSuccessful()
    {
        // Arrange
        var handler = new StubHandler(Respond(HttpStatusCode.OK, "{\"name\":\"alpha\",\"id\":7}"));
        var wrapper = CreateWrapper(handler);

        // Act
        var result = await wrapper.GetAsync<Item>("items/7");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("alpha", result!.Name);
        Assert.Equal(7, result.Id);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenStatusIsNotSuccessful()
    {
        // Arrange
        var handler = new StubHandler(Respond(HttpStatusCode.NotFound, "{}"));
        var wrapper = CreateWrapper(handler);

        // Act
        var result = await wrapper.GetAsync<Item>("items/404");

        // Assert
        Assert.Null(result);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetAsync_RetriesServerError_ThenReturnsBody()
    {
        // Arrange
        var handler = new StubHandler(
            Respond(HttpStatusCode.InternalServerError, ""),
            Respond(HttpStatusCode.OK, "{\"name\":\"retried\",\"id\":1}"));
        var wrapper = CreateWrapper(handler, maxRetries: 3);

        // Act
        var result = await wrapper.GetAsync<Item>("items/1");

        // Assert
        Assert.Equal("retried", result?.Name);
        Assert.Equal(2, handler.Requests.Count);

        // Arrange: with maxRetries = 2 and every attempt failing, the last 500 is returned, not thrown
        var exhausted = new StubHandler(
            Respond(HttpStatusCode.InternalServerError, ""),
            Respond(HttpStatusCode.InternalServerError, ""));
        var limited = CreateWrapper(exhausted, maxRetries: 2);

        // Act
        var failed = await limited.GetAsync<Item>("items/1");

        // Assert
        Assert.Null(failed);
        Assert.Equal(2, exhausted.Requests.Count);
    }

    [Fact]
    public async Task PostAsync_SendsJsonPayload_AndThrows_WhenPayloadIsNull()
    {
        // Arrange
        var handler = new StubHandler(Respond(HttpStatusCode.Created, "{\"name\":\"beta\",\"id\":2}"));
        var wrapper = CreateWrapper(handler);

        // Act
        var result = await wrapper.PostAsync<Item>("items", new { Name = "beta" });

        // Assert
        Assert.Equal("beta", result?.Name);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("application/json", handler.Requests[0].Content?.Headers.ContentType?.MediaType);
        Assert.Contains("\"Name\":\"beta\"", handler.Bodies[0]);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => wrapper.PostAsync<Item>("items", null!));
    }

    [Fact]
    public async Task PutAsync_ReturnsSuccessFlag_WithoutRetryingClientError()
    {
        // Arrange
        var handler = new StubHandler(
            Respond(HttpStatusCode.NoContent, ""),
            Respond(HttpStatusCode.BadRequest, ""));
        var wrapper = CreateWrapper(handler, maxRetries: 3);

        // Act
        var succeeded = await wrapper.PutAsync("items/1", new { Name = "gamma" });
        var rejected = await wrapper.PutAsync("items/1", new { Name = "gamma" });

        // Assert
        Assert.True(succeeded);
        Assert.False(rejected);
        Assert.Equal(HttpMethod.Put, handler.Requests[0].Method);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenMaxRetriesIsZero()
    {
        // Arrange: with zero retries the retry loop never runs, so no request is sent
        var handler = new StubHandler(Respond(HttpStatusCode.OK, ""));
        var wrapper = CreateWrapper(handler, maxRetries: 0);

        // Act
        var deleted = await wrapper.DeleteAsync("items/1");

        // Assert
        Assert.False(deleted);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task AddDefaultHeader_AndSetBearerToken_ApplyToRequests()
    {
        // Arrange
        var handler = new StubHandler(Respond(HttpStatusCode.OK, "{\"name\":\"delta\",\"id\":3}"));
        var wrapper = CreateWrapper(handler);
        wrapper.AddDefaultHeader("X-Tenant", "acme");
        wrapper.SetBearerToken("test-token");

        // Act
        await wrapper.GetAsync<Item>("items/3");

        // Assert
        var request = handler.Requests[0];
        Assert.Contains("acme", request.Headers.GetValues("X-Tenant"));
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("test-token", request.Headers.Authorization?.Parameter);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => wrapper.AddDefaultHeader("X-Empty", string.Empty));
        Assert.Throws<ArgumentException>(() => wrapper.SetBearerToken(string.Empty));
    }

    private static HttpClientWrapper CreateWrapper(StubHandler handler, int maxRetries = 3)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        return new HttpClientWrapper(httpClient, NullLogger<HttpClientWrapper>.Instance, maxRetries, retryDelayMs: 0);
    }

    private static HttpResponseMessage Respond(HttpStatusCode statusCode, string body) =>
        new(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed record Item(string? Name, int Id);

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public StubHandler(params HttpResponseMessage[] responses) => _responses = new Queue<HttpResponseMessage>(responses);

        public List<HttpRequestMessage> Requests { get; } = new();
        public List<string?> Bodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Bodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));
            return _responses.Dequeue();
        }
    }
}
