# HttpClientService

`HttpClientService` is the concrete, sealed implementation of [`IHttpClientService`](IHttpClientService.md). It wraps an injected `HttpClient`, serializes and deserializes JSON, applies per-request headers, logs failures, and retries transient responses.

## Construction

```csharp
public HttpClientService(
    HttpClient httpClient,
    ILogger<HttpClientService> logger,
    HttpClientOptions options = null)
```

The constructor requires an `HttpClient` and logger. If `options` is omitted, it creates default `HttpClientOptions`. It sets the supplied client's timeout from `options.TimeoutSeconds` (30 seconds by default).

## Public methods

### `Task<T> GetAsync<T>(string url, Dictionary<string, string> headers = null)`

Sends a GET request, optionally adds the supplied request headers, requires a successful HTTP status, and deserializes the JSON response body as `T`.

### `Task<T> PostAsync<T>(string url, object body, Dictionary<string, string> headers = null)`

Serializes `body` as JSON, sends it in a UTF-8 `application/json` POST request, optionally adds the supplied request headers, requires a successful HTTP status, and deserializes the JSON response body as `T`.

### `Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string content = null)`

Sends a request using the specified method. When `content` is not null or empty, it is sent as UTF-8 `application/json`. The response is returned without calling `EnsureSuccessStatusCode`, so callers must inspect or validate its status themselves.

`GetAsync<T>` and `PostAsync<T>` call `EnsureSuccessStatusCode` after retry processing and therefore throw for a final non-success response. Serialization, transport, timeout, and HTTP errors are logged and then propagated.

## Retry policy

The initial request is followed by at most `HttpClientOptions.MaxRetries` retries (three by default). A response is considered transient when its status is:

- `408 Request Timeout`
- `429 Too Many Requests`
- Any server-error status from `500` through `599`

Transient HTTP responses use exponential backoff with a base delay of 1000 ms:

```text
delay = 1000 ms × 2^(retry number - 1)
```

With the default three retries, the delays are 1000 ms, 2000 ms, and 4000 ms. Other 4xx responses are not retried.

A `TaskCanceledException`, including one caused by the configured `HttpClient` timeout, is also retried up to the same limit. That path currently uses `1000 ms × retry number` (1000 ms, 2000 ms, and 3000 ms by default), rather than exponential backoff.

## Configuration

`HttpClientOptions` is declared in the same source file and exposes these public properties:

```csharp
public int TimeoutSeconds { get; set; } = 30;
public int MaxRetries { get; set; } = 3;
public bool EnableCompression { get; set; } = true;
public bool EnableConnectionPooling { get; set; } = true;
```

`HttpClientService` directly applies `TimeoutSeconds` and `MaxRetries`. `EnableCompression` and `EnableConnectionPooling` are configuration values but are not read by this class.

## Example

```csharp
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Integration;

public sealed record Customer(int Id, string Name);

var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://api.example.com/")
};

var options = new HttpClientOptions
{
    TimeoutSeconds = 15,
    MaxRetries = 3
};

IHttpClientService service = new HttpClientService(
    httpClient,
    loggerFactory.CreateLogger<HttpClientService>(),
    options);

var headers = new Dictionary<string, string>
{
    ["X-Tenant-Id"] = "tenant-42"
};

Customer customer = await service.GetAsync<Customer>(
    "customers/123",
    headers);
```

Typing the instance as `IHttpClientService` keeps callers coupled to the contract documented in [`IHttpClientService.md`](IHttpClientService.md), while `HttpClientService` supplies its retry, timeout, JSON, and logging behavior.
