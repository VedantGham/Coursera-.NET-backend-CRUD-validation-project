var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<TokenValidationMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.MapGet("/", () => "Hello World!");

// In-memory data store
var users = new List<User>
{
    new User { Id = 1, Name = "Alice", Email = "alice@example.com", Age = 25 },
    new User { Id = 2, Name = "Bob", Email = "bob@example.com", Age = 30 }
};
                                  
// ✅ CREATE - Add new user with validation
app.MapPost("/users", (User user) =>
{
    // Validation rules
    if (string.IsNullOrWhiteSpace(user.Name))
        return Results.BadRequest("Name is required.");
    if (string.IsNullOrWhiteSpace(user.Email) || !user.Email.Contains("@"))
        return Results.BadRequest("Valid email is required.");
    if (user.Age <= 0)
        return Results.BadRequest("Age must be greater than 0.");

    // Edge case: prevent duplicate Ids if client sends one
    if (user.Id != 0 && users.Any(u => u.Id == user.Id))
        return Results.BadRequest($"User with Id {user.Id} already exists.");

    // Auto-generate Id if not provided
    user.Id = users.Any() ? users.Max(u => u.Id) + 1 : 1;
    users.Add(user);

    return Results.Created($"/users/{user.Id}", user);
});

// ✅ READ - Get all users
app.MapGet("/users", () =>
{
    return Results.Ok(users);
});

// ✅ READ - Get user by Id
app.MapGet("/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is not null ? Results.Ok(user) : Results.NotFound($"User with Id {id} not found.");
});

// ✅ UPDATE - Update user by Id with validation
app.MapPut("/users/{id}", (int id, User updatedUser) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound($"User with Id {id} not found.");

    // Validation rules
    if (string.IsNullOrWhiteSpace(updatedUser.Name))
        return Results.BadRequest("Name is required.");
    if (string.IsNullOrWhiteSpace(updatedUser.Email) || !updatedUser.Email.Contains("@"))
        return Results.BadRequest("Valid email is required.");
    if (updatedUser.Age <= 0)
        return Results.BadRequest("Age must be greater than 0.");

    user.Name = updatedUser.Name;
    user.Email = updatedUser.Email;
    user.Age = updatedUser.Age;

    return Results.Ok(user);
});

// ✅ DELETE - Remove user by Id
app.MapDelete("/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound($"User with Id {id} not found.");

    users.Remove(user);
    return Results.NoContent();
});

app.Run();

public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int Age { get; set; }
}

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Log request details
        var method = context.Request.Method;
        var path = context.Request.Path;
        _logger.LogInformation("Incoming Request: {Method} {Path}", method, path);

        // Call the next middleware in the pipeline
        await _next(context);

        // Log response details
        var statusCode = context.Response.StatusCode;
        _logger.LogInformation("Outgoing Response: {StatusCode}", statusCode);
    }
}
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Pass request down the pipeline
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the exception
            _logger.LogError(ex, "Unhandled exception occurred.");

            // Return consistent JSON error response
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var errorResponse = new { error = "Internal server error." };
            await context.Response.WriteAsJsonAsync(errorResponse);
        }
    }
}
public class TokenValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenValidationMiddleware> _logger;

    // For demo purposes, we'll use a hardcoded valid token.
    // In production, you'd validate JWTs or check against a database/identity provider.
    private const string ValidToken = "my-secret-token";

    public TokenValidationMiddleware(RequestDelegate next, ILogger<TokenValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Look for Authorization header
        if (!context.Request.Headers.TryGetValue("Authorization", out var token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized: Missing token." });
            return;
        }

        // Validate token (simple equality check here)
        if (token != $"Bearer {ValidToken}")
        {
            _logger.LogWarning("Invalid token received: {Token}", token.ToString());
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized: Invalid token." });
            return;
        }

        // Token is valid → continue down the pipeline
        await _next(context);
    }
}