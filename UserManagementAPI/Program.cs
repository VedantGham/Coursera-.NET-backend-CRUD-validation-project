var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

// In-memory data store
var users = new List<User>
{
    new User { Id = 1, Name = "Alice", Email = "alice@example.com", Age = 25 },
    new User { Id = 2, Name = "Bob", Email = "bob@example.com", Age = 30 }
};

// ✅ CREATE - Add new user
app.MapPost("/users", (User user) =>
{
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
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

// ✅ UPDATE - Update user by Id
app.MapPut("/users/{id}", (int id, User updatedUser) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound();

    user.Name = updatedUser.Name;
    user.Email = updatedUser.Email;
    user.Age = updatedUser.Age;

    return Results.Ok(user);
});

// ✅ DELETE - Remove user by Id
app.MapDelete("/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound();

    users.Remove(user);
    return Results.NoContent();
});


app.Run();
public class User
{
   public int Id {get;set;}
   public String? Name {get;set;}
   public String? Email {get;set;}
   public int Age {get;set;}
}