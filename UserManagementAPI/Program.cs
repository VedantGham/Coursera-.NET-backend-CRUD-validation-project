var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

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