# IExceptionProcessor

A lightweight contract for translating .NET exceptions into structured, HTTP-friendly error responses. Implementations are expected to categorize exceptions, map them to appropriate HTTP status codes, and enrich responses with contextual details while preserving inner-exception information.

## API

### `ExceptionProcessor` (public sealed class)
