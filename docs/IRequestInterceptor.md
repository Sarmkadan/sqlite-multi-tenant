# IRequestInterceptor
The `IRequestInterceptor` interface is designed to allow for the interception and modification of requests in the sqlite-multi-tenant project. It provides a way to execute custom logic before and after a request is processed, enabling features such as request validation, tenant context management, and correlation ID tracking.

## API
The `IRequestInterceptor` interface has two main methods: `OnRequestAsync` and `OnResponseAsync`. 
- `Task<bool> OnRequestAsync`: This method is called before a request is processed. It returns a boolean value indicating whether the request should be allowed to proceed. If `false` is returned, the request will be cancelled.
- `Task OnResponseAsync`: This method is called after a request has been processed. It does not return any value and is used to perform any necessary cleanup or post-processing tasks.

## Usage
Here are two examples of using the `IRequestInterceptor` interface:
```csharp
// Example 1: Implementing a custom request interceptor
public class CustomInterceptor : IRequestInterceptor
{
    public async Task<bool> OnRequestAsync()
    {
        // Custom logic to validate or modify the request
        return true;
    }

    public async Task OnResponseAsync()
    {
        // Custom logic to perform post-processing tasks
    }
}

// Example 2: Registering an interceptor with the InterceptorPipeline
var pipeline = new InterceptorPipeline();
pipeline.Register(new RequestValidationInterceptor());
pipeline.Register(new CorrelationIdInterceptor());

// Execute the interceptors
await pipeline.ExecuteRequestInterceptorsAsync();
await pipeline.ExecuteResponseInterceptorsAsync();
```

## Notes
When implementing the `IRequestInterceptor` interface, it is essential to consider thread-safety and potential edge cases. Since the `OnRequestAsync` and `OnResponseAsync` methods are asynchronous, they may be executed on different threads, and any shared state should be properly synchronized. Additionally, if an exception occurs during the execution of an interceptor, it will be propagated to the caller, and it is the responsibility of the interceptor to handle any exceptions that may occur during its execution. The `IRequestInterceptor` interface does not provide any built-in exception handling mechanisms, so implementers should take care to handle any potential exceptions that may arise.
