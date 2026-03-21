# Error Handling Strategy

## Executive Summary

StorageLens will implement a **standardized, cross-service error handling system** with:
- Unified `ErrorResponseDto` for all services
- Global exception handler middleware for consistent HTTP status code mapping
- Structured error codes by domain (validation, not found, conflict, system)
- Correlation ID propagation through all error responses
- Production-safe error details (no stack traces exposed)

**Current State**: 7.5/10 - Inconsistent error responses, bare exceptions, no middleware  
**Target State**: 9.5/10 - Centralized, documented, auditable error handling

---

## 1. Standardized ErrorResponseDto

### Schema

```csharp
namespace StorageLens.Shared.Contracts;

/// <summary>
/// Standardized error response returned by all StorageLens services.
/// Used for validation errors (400), not found (404), conflicts (409), and system errors (500).
/// </summary>
public sealed record ErrorResponseDto
{
    /// <summary>
    /// Service error code (e.g., "VALIDATION_ERROR", "LOCATION_NOT_FOUND", "SCAN_IN_PROGRESS")
    /// </summary>
    public required string ErrorCode { get; init; }

    /// <summary>
    /// User-facing error message. Safe for public exposure.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Correlation ID for request tracing across services.
    /// Enables log aggregation and incident debugging.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// UTC timestamp when error occurred. ISO 8601 format.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// HTTP status code (400, 404, 409, 500, 503, etc.)
    /// </summary>
    public required int HttpStatusCode { get; init; }

    /// <summary>
    /// Additional context for validation errors or detailed diagnostics.
    /// In production, contains only sanitized information (no stack traces).
    /// </summary>
    public Dictionary<string, object>? Details { get; init; }

    /// <summary>
    /// Unique error instance ID for support ticket references.
    /// </summary>
    public required string ErrorInstanceId { get; init; }
}
```

### Error Code Convention

All services follow this naming pattern:

```
{SERVICE_NAME}_{ERROR_CATEGORY}_{SPECIFIC_CONDITION}

Examples:
- LOCATION_NOT_FOUND
- LOCATION_VALIDATION_NAME_REQUIRED
- SCAN_JOB_IN_PROGRESS
- FILE_INVENTORY_BATCH_SIZE_EXCEEDED
- DUPLICATES_CALCULATION_FAILED
- ORCHESTRATOR_WORKFLOW_TIMEOUT
- HASHING_SERVICE_UNAVAILABLE
- SCANNER_PATH_ACCESS_DENIED
```

### Error Categories

| Category | HTTP Status | Use Case |
|----------|------------|----------|
| `VALIDATION_*` | 400 | Input validation failed, missing required fields |
| `AUTHENTICATION_*` | 401 | API key invalid, expired token |
| `AUTHORIZATION_*` | 403 | User lacks permission for resource |
| `NOT_FOUND` | 404 | Resource doesn't exist (location, scan job, file) |
| `CONFLICT_*` | 409 | Operation violates business logic (duplicate scan, state mismatch) |
| `INTERNAL_ERROR` | 500 | Unexpected system error |
| `SERVICE_UNAVAILABLE` | 503 | Dependency unavailable, database connection failed |

---

## 2. Global Exception Handler Middleware

### Implementation Template

Create `GlobalExceptionHandlerMiddleware.cs` in `StorageLens.Shared.Infrastructure`:

```csharp
namespace StorageLens.Shared.Infrastructure;

/// <summary>
/// Global exception handler middleware that converts all unhandled exceptions
/// to standardized ErrorResponseDto with proper HTTP status codes and correlation IDs.
/// 
/// Registration order (in Program.cs):
/// 1. app.UseGlobalExceptionHandler() - FIRST in pipeline
/// 2. app.UseRouting()
/// 3. other logging/auth middleware
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract or generate correlation ID for this request
        var correlationId = ExtractOrGenerateCorrelationId(context);
        context.Items["CorrelationId"] = correlationId;

        // Add correlation ID to response headers for traceability
        context.Response.Headers.Add("X-Correlation-Id", correlationId);

        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception in request. CorrelationId: {CorrelationId}, Path: {Path}",
                correlationId,
                context.Request.Path);

            await HandleExceptionAsync(context, exception, correlationId);
        }
    }

    private static string ExtractOrGenerateCorrelationId(HttpContext context)
    {
        const string headerName = "X-Correlation-Id";

        if (context.Request.Headers.TryGetValue(headerName, out var existingId))
        {
            return existingId.ToString();
        }

        var newId = Guid.NewGuid().ToString();
        context.Request.Headers[headerName] = newId;
        return newId;
    }

    private static Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        string correlationId)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, errorCode, message, details) = MapExceptionToErrorResponse(exception, context);

        context.Response.StatusCode = statusCode;

        var errorResponse = new ErrorResponseDto
        {
            ErrorCode = errorCode,
            Message = message,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            HttpStatusCode = statusCode,
            Details = details,
            ErrorInstanceId = Guid.NewGuid().ToString()
        };

        return context.Response.WriteAsJsonAsync(errorResponse);
    }

    /// <summary>
    /// Maps exception types to HTTP status codes and error codes.
    /// Extend this method to handle service-specific exceptions.
    /// </summary>
    private static (int StatusCode, string ErrorCode, string Message, Dictionary<string, object>? Details)
        MapExceptionToErrorResponse(Exception exception, HttpContext context)
    {
        return exception switch
        {
            // Validation exceptions (400)
            ValidationException validationEx => (
                400,
                "VALIDATION_ERROR",
                validationEx.Message,
                new Dictionary<string, object>
                {
                    ["errors"] = validationEx.ValidationErrors
                }),

            ArgumentNullException argNullEx => (
                400,
                "VALIDATION_ERROR",
                $"Required parameter missing: {argNullEx.ParamName}",
                null),

            ArgumentException argEx => (
                400,
                "VALIDATION_ERROR",
                argEx.Message,
                null),

            // Not found exceptions (404)
            ResourceNotFoundException notFoundEx => (
                404,
                "NOT_FOUND",
                notFoundEx.Message,
                null),

            // Conflict exceptions (409)
            ConflictException conflictEx => (
                409,
                conflictEx.ErrorCode,
                conflictEx.Message,
                null),

            // Service unavailable (503)
            ServiceUnavailableException serviceUnavailableEx => (
                503,
                "SERVICE_UNAVAILABLE",
                serviceUnavailableEx.Message,
                null),

            HttpRequestException httpEx when httpEx.StatusCode.HasValue => (
                (int?)httpEx.StatusCode ?? 503,
                "SERVICE_UNAVAILABLE",
                "Downstream service unavailable",
                new Dictionary<string, object>
                {
                    ["statusCode"] = httpEx.StatusCode.ToString()
                }),

            // Default: Internal Server Error (500)
            _ => (
                500,
                "INTERNAL_ERROR",
                "An unexpected error occurred. Please contact support.",
                null)
        };
    }
}

/// <summary>
/// Extension method for middleware registration in Program.cs
/// </summary>
public static class GlobalExceptionHandlerExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
```

### Exception Base Classes

Add to `StorageLens.Shared.Infrastructure`:

```csharp
namespace StorageLens.Shared.Infrastructure;

/// <summary>
/// Base class for validation errors with structured error collection.
/// </summary>
public class ValidationException : Exception
{
    public Dictionary<string, string[]> ValidationErrors { get; }

    public ValidationException(string message, Dictionary<string, string[]> errors)
        : base(message)
    {
        ValidationErrors = errors;
    }

    public ValidationException(string fieldName, string error)
        : base(error)
    {
        ValidationErrors = new Dictionary<string, string[]> { [fieldName] = [error] };
    }
}

/// <summary>
/// Thrown when a requested resource doesn't exist.
/// </summary>
public class ResourceNotFoundException : Exception
{
    public string ResourceType { get; }
    public required object ResourceId { get; init; }

    public ResourceNotFoundException(string resourceType, object resourceId)
        : base($"{resourceType} with ID '{resourceId}' not found")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}

/// <summary>
/// Thrown when an operation conflicts with current business rules.
/// </summary>
public class ConflictException : Exception
{
    public string ErrorCode { get; }
    public string CorrelationId { get; }

    public ConflictException(
        string errorCode,
        string message,
        string? correlationId = null)
        : base(message)
    {
        ErrorCode = errorCode;
        CorrelationId = correlationId ?? string.Empty;
    }
}

/// <summary>
/// Thrown when a dependent service is unavailable or not responding.
/// </summary>
public class ServiceUnavailableException : Exception
{
    public string ServiceName { get; }
    public string? InnerServiceError { get; }

    public ServiceUnavailableException(
        string serviceName,
        string message,
        string? innerError = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ServiceName = serviceName;
        InnerServiceError = innerError;
    }
}
```

---

## 3. Service-to-Service Error Handling

### HTTP Client Error Translation

When calling another service, translate the response to appropriate exception:

```csharp
// Example: FileInventory service calling another service response
public class ServiceHttpClientExtensions
{
    public static async Task<T> GetAsyncSafe<T>(
        this HttpClient client,
        string requestUri,
        ILogger logger,
        string serviceName,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.GetAsync(requestUri, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<T>();
            }

            // Attempt to parse error response
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponseDto>(errorContent);

            logger.LogWarning(
                "Service call failed. Service: {ServiceName}, Status: {Status}, ErrorCode: {ErrorCode}, CorrelationId: {CorrelationId}",
                serviceName,
                response.StatusCode,
                errorResponse?.ErrorCode,
                errorResponse?.CorrelationId);

            throw response.StatusCode switch
            {
                System.Net.HttpStatusCode.NotFound =>
                    new ResourceNotFoundException(serviceName, requestUri),

                System.Net.HttpStatusCode.Conflict =>
                    new ConflictException(
                        errorResponse?.ErrorCode ?? "CONFLICT",
                        errorResponse?.Message ?? "Conflict with downstream service",
                        errorResponse?.CorrelationId),

                System.Net.HttpStatusCode.ServiceUnavailable =>
                    new ServiceUnavailableException(
                        serviceName,
                        errorResponse?.Message ?? "Service unavailable",
                        errorResponse?.ErrorCode),

                _ => new InvalidOperationException(
                    $"Service call failed with status {response.StatusCode}: {errorContent}")
            };
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(
                ex,
                "HTTP communication failed to {ServiceName}",
                serviceName);

            throw new ServiceUnavailableException(
                serviceName,
                "Failed to communicate with service",
                ex.Message,
                ex);
        }
    }
}
```

---

## 4. Program.cs Registration

### Startup Configuration

```csharp
// In each service's Program.cs, FIRST line of middleware registration:

var builder = WebApplicationBuilder.CreateBuilder(args);

// ... service registrations ...

var app = builder.Build();

// CRITICAL: Register exception handler FIRST, before other middleware
app.UseGlobalExceptionHandler();

app.UseRouting();
app.MapControllers();

app.Run();
```

---

## 5. Controller Implementation Pattern

### Example: LocationsController

```csharp
namespace StorageLens.Services.Locations.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly ILocationsService _service;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(
        ILocationsService service,
        ILogger<LocationsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("create")]
    public async Task<ActionResult<StorageLocationDto>> CreateLocation(
        CreateLocationRequest request,
        CancellationToken cancellationToken)
    {
        // Validation using FluentValidation
        var validationResult = await new CreateLocationRequestValidator().ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(e => e.ErrorMessage).ToArray());

            throw new ValidationException("Validation failed", errors);
        }

        // Extract correlation ID from context or generate one
        var correlationId = (string?)HttpContext.Items["CorrelationId"] ?? Guid.NewGuid().ToString();

        var result = await _service.CreateLocationAsync(request, correlationId, cancellationToken);

        return CreatedAtAction(nameof(GetLocation), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StorageLocationDto>> GetLocation(
        Guid id,
        CancellationToken cancellationToken)
    {
        var correlationId = (string?)HttpContext.Items["CorrelationId"] ?? Guid.NewGuid().ToString();

        try
        {
            var location = await _service.GetLocationAsync(id, cancellationToken);

            if (location is null)
            {
                throw new ResourceNotFoundException(nameof(StorageLocationDto), id);
            }

            return Ok(location);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting location {LocationId}. CorrelationId: {CorrelationId}",
                id,
                correlationId);
            throw;
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateLocation(
        Guid id,
        UpdateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = (string?)HttpContext.Items["CorrelationId"] ?? Guid.NewGuid().ToString();

        var result = await _service.UpdateLocationAsync(id, request, correlationId, cancellationToken);

        if (!result)
        {
            throw new ResourceNotFoundException(nameof(StorageLocationDto), id);
        }

        return Ok();
    }
}
```

### Validation Using FluentValidation

```csharp
namespace StorageLens.Services.Locations.Validators;

public class CreateLocationRequestValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Location name is required")
            .MaximumLength(256).WithMessage("Location name cannot exceed 256 characters");

        RuleFor(x => x.RootPath)
            .NotEmpty().WithMessage("Root path is required")
            .Must(x => Path.IsPathRooted(x) || x.StartsWith("\\\\")).WithMessage("Path must be absolute");

        RuleFor(x => x.LocationType)
            .IsInEnum().WithMessage("Invalid location type");
    }
}
```

---

## 6. Unit Testing Error Handling

```csharp
namespace StorageLens.Services.Locations.Tests;

[TestClass]
public class ErrorHandlingTests
{
    private Mock<ILocationsService> _mockService = null!;
    private LocationsController _controller = null!;
    private Mock<ILogger<LocationsController>> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockService = new Mock<ILocationsService>();
        _mockLogger = new Mock<ILogger<LocationsController>>();
        _controller = new LocationsController(_mockService.Object, _mockLogger.Object);
    }

    [TestMethod]
    public async Task CreateLocation_WithInvalidRequest_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateLocationRequest
        {
            Name = "", // Invalid: empty name
            RootPath = "C:\\test"
        };

        // Act & Assert
        var validationEx = await Assert.ThrowsExceptionAsync<ValidationException>(
            async () => await _controller.CreateLocation(request, CancellationToken.None));

        Assert.IsTrue(validationEx.ValidationErrors.ContainsKey("Name"));
    }

    [TestMethod]
    public async Task GetLocation_WithNonexistentId_ThrowsResourceNotFoundException()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        _mockService.Setup(x => x.GetLocationAsync(locationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StorageLocationDto?)null);

        // Act & Assert
        var ex = await Assert.ThrowsExceptionAsync<ResourceNotFoundException>(
            async () => await _controller.GetLocation(locationId, CancellationToken.None));

        Assert.AreEqual("StorageLocationDto", ex.ResourceType);
        Assert.AreEqual(locationId, ex.ResourceId);
    }

    [TestMethod]
    public async Task CreateLocation_WhenServiceThrowsConflict_PropagatesError()
    {
        // Arrange
        var request = new CreateLocationRequest { Name = "Test", RootPath = "C:\\test" };
        var conflictEx = new ConflictException(
            "LOCATION_DUPLICATE_NAME",
            "Location with this name already exists");

        _mockService.Setup(x => x.CreateLocationAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(conflictEx);

        // Act & Assert
        var ex = await Assert.ThrowsExceptionAsync<ConflictException>(
            async () => await _controller.CreateLocation(request, CancellationToken.None));

        Assert.AreEqual("LOCATION_DUPLICATE_NAME", ex.ErrorCode);
    }
}
```

---

## 7. Deployment Checklist

- [ ] Define service-specific error codes in each service's documentation
- [ ] Add `GlobalExceptionHandlerMiddleware` to `StorageLens.Shared.Infrastructure`
- [ ] Add exception base classes to shared infrastructure
- [ ] Update each service's `Program.cs` to register middleware
- [ ] Migrate existing controllers to throw exceptions instead of returning BadRequest
- [ ] Update integration tests to expect `ErrorResponseDto` format
- [ ] Add correlation ID logging to all service implementations
- [ ] Test error propagation across service boundaries with HttpClient
- [ ] Verify middleware order (exception handler FIRST in pipeline)
- [ ] Update API documentation to show error response examples

---

## 8. Error Response Examples

### 400 - Validation Error

```json
{
  "errorCode": "LOCATION_VALIDATION_NAME_REQUIRED",
  "message": "Validation failed",
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2026-03-20T14:25:32Z",
  "httpStatusCode": 400,
  "details": {
    "errors": {
      "Name": ["Location name is required", "Location name cannot exceed 256 characters"],
      "RootPath": ["Root path must be absolute"]
    }
  },
  "errorInstanceId": "err_550e8400"
}
```

### 404 - Not Found

```json
{
  "errorCode": "LOCATION_NOT_FOUND",
  "message": "StorageLocationDto with ID '550e8400-e29b-41d4-a716-446655440000' not found",
  "correlationId": "550e8400-e29b-41d4-a716-446655440001",
  "timestamp": "2026-03-20T14:26:15Z",
  "httpStatusCode": 404,
  "details": null,
  "errorInstanceId": "err_550e8401"
}
```

### 409 - Conflict

```json
{
  "errorCode": "SCAN_JOB_IN_PROGRESS",
  "message": "A scan is already running for this location. Wait for it to complete before starting a new scan.",
  "correlationId": "550e8400-e29b-41d4-a716-446655440002",
  "timestamp": "2026-03-20T14:27:00Z",
  "httpStatusCode": 409,
  "details": {
    "activeScanJobId": "550e8400-e29b-41d4-a716-446655440003",
    "activeScanStatus": "Running"
  },
  "errorInstanceId": "err_550e8402"
}
```

### 500 - Internal Server Error

```json
{
  "errorCode": "INTERNAL_ERROR",
  "message": "An unexpected error occurred. Please contact support.",
  "correlationId": "550e8400-e29b-41d4-a716-446655440004",
  "timestamp": "2026-03-20T14:28:45Z",
  "httpStatusCode": 500,
  "details": null,
  "errorInstanceId": "err_550e8404"
}
```

### 503 - Service Unavailable

```json
{
  "errorCode": "SERVICE_UNAVAILABLE",
  "message": "FileInventory service is currently unavailable",
  "correlationId": "550e8400-e29b-41d4-a716-446655440005",
  "timestamp": "2026-03-20T14:29:12Z",
  "httpStatusCode": 503,
  "details": {
    "serviceName": "FileInventory",
    "retryAfterSeconds": 30
  },
  "errorInstanceId": "err_550e8405"
}
```

---

## Success Criteria

- ✅ All services return `ErrorResponseDto` for errors
- ✅ Correlation IDs present in all error responses and logs
- ✅ No stack traces exposed in production error responses
- ✅ Consistent HTTP status code mapping across all services
- ✅ All validation errors return 400 with structured error details
- ✅ All service-to-service calls properly translate error responses
- ✅ Integration tests validate error scenarios

