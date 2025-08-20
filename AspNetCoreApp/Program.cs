using AspNetCoreApp.Models;
using AspNetCoreApp.Services;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;

/*------------------------------------------------------------------------------------------*/
// Init builder
/*------------------------------------------------------------------------------------------*/

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// ������������ ���� ������
//builder.Services.AddScoped<IUserService, UserService>(); // ��� ������ HTTP-������� ������� ����� ���������� ����������
builder.Services.AddSingleton<IUserService, UserService>(); // ���� ��������� ���������� �� ��� ���������� (��� ������ � in-memory �������)

// ��������� Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "AspNetCoreApp",
        Description = "A simple ASP.NET Core web API for learning"
    });
});

/*------------------------------------------------------------------------------------------*/
// Init app
/*------------------------------------------------------------------------------------------*/

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

// �������� Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

/*------------------------------------------------------------------------------------------*/
// ������� ������� �������
/*------------------------------------------------------------------------------------------*/

// ������� API ��� ��������� "Hello, World!"
// ���� � ��������: https://localhost:7094/api/hello
void Func1()
{
    app.MapGet("/api/hello", () => "Hello, World!");
}

// ������� JSON � ������� ������������
// ���� � ��������: https://localhost:7094/api/user
void Func2()
{
    app.MapGet("/api/user", () =>
    {
        return new {Id = 1, Name = "John Doe", Email = "john@example.com"};
    });
}

// ���� ������ ����� POST � ������� ������
// ��������������� �����: record UserRequest(string Name);
// ���� � cmd: curl -X POST https://localhost:7094/api/greet -H "Content-Type: application/json" -d "{\"Name\": \"Alice\"}"
void Func3()
{
    app.MapPost("/api/greet", (UserRequest request) =>
    {
        return Results.Ok($"Hello, {request.Name}!");
    });
}

// ��������� ������� ������. ���������, ��� ���� Name �� ������
// ��������������� �����: record UserRequest(string Name);
// ���� 1 � cmd: curl -X POST https://localhost:7094/api/validate -H "Content-Type: application/json" -d "{\"Name\": \"Alice\"}"
// ���� 2 � cmd: curl -X POST https://localhost:7094/api/validate -H "Content-Type: application/json" -d "{\"Name\": \"\"}"
void Func4()
{
    app.MapPost("/api/validate", (UserRequest request) =>
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest("Name is required.");

        return Results.Ok($"Hello, {request.Name}!");
    });
}

// ��������� ������� ������. ���������, ��� ���� Name �� ������
// ������������� �������� ��� ���������� ����������
// ��������������� ������: record UserRequest(string Name); record ApiResponse<T>
// ���� 1 � cmd: curl -X POST https://localhost:7094/api/validate41 -H "Content-Type: application/json" -d "{\"Name\": \"Alice\"}"
// ���� 2 � cmd: curl -X POST https://localhost:7094/api/validate41 -H "Content-Type: application/json" -d "{\"Name\": \"\"}"
void Func4_1()
{
    app.MapPost("/api/validate41", (UserRequest request) =>
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            // ���������� 400 � ��������
            return Results.Json(
                new ApiResponse<string>
                {
                    StatusCode = 400,
                    Message = "Name is required.",
                    Data = null
                }
                ,statusCode: 400
            );
        }

        // ���������� 200 � ������������
        return Results.Json(
            new ApiResponse<string>
            {
                StatusCode = 200,
                Message = $"Hello, {request.Name}!",
                Data = request.Name
            }
            ,statusCode: 200
        );
    });
}

// ��������� �������� (Route Parameters)
// �������� id �� URL � ������� ���������� � ������������. �������� id �� ������������� �����
// ���� � ��������: https://localhost:7094/api/user_id/
// ���� � ��������: https://localhost:7094/api/user_id/123456
// ���� � ��������: https://localhost:7094/api/user_id/-123456
// ���� � ��������: https://localhost:7094/api/user_id/abc123456
// ���� � cmd: curl -v https://localhost:7094/api/user_id/123456
void Func5()
{
    // ���������, ������ �� ��������
    app.MapGet("/api/user_id", () =>
    {
        return Results.BadRequest(new {error = "User ID is required."});
    });

    app.MapGet("/api/user_id/{id}", (string id) =>
    {
        // ���������, ������ �� ��������
        if(string.IsNullOrEmpty(id))
            return Results.BadRequest(new {error = "User ID is required."});

        // �������� ������������� � int
        if(!int.TryParse(id, out int userId))
            return Results.BadRequest(new {error = "User ID must be a valid number."});

        if(userId < 0)
            return Results.BadRequest(new {error = "User ID must be a positive number."});

        // ���� �� ������ - ���������� ������
        return Results.Ok(new {UserId = userId, Message = $"User with ID {userId} retrieved."});
    });
}

// In-Memory CRUD ��� �������������
// ����������� ������ ���� - ��������, ������, ����������, �������� ������������� � ����������� ������
// ��������������� �����: record User(int Id, string Name);
// ���� � cmd: curl -k https://localhost:7094/api/users
// ���� � cmd: curl -k https://localhost:7094/api/users/1
// ���� � cmd: curl -k -X POST https://localhost:7094/api/users -H "Content-Type: application/json" -d "{\"Name\": \"Charlie\"}"
// ���� � cmd: curl -k -X PUT https://localhost:7094/api/users/3 -H "Content-Type: application/json" -d "{\"Name\": \"Charlie Updated\"}"
// ���� � cmd: curl -k -X DELETE https://localhost:7094/api/users/3
void Func6()
{
    var users = new List<User>
    {
        new User {Id = 1, Name = "Alice"},
        new User {Id = 2, Name = "Bob"}
    };

    app.MapGet("/api/users", () => Results.Ok(users));

    app.MapGet("/api/users/{id}", (int id) =>
    {
        // ��� ����� �������� �������� �� ��, ��� id - ��� ������������� �����
        // ...

        var user = users.FirstOrDefault(u => u.Id == id);
        return user is null ? Results.NotFound() : Results.Ok(user);
    });

    app.MapPost("/api/users", (User user) =>
    {
        // ��������: ��� �����������
        if (string.IsNullOrWhiteSpace(user.Name))
            return Results.BadRequest("Name is required.");

        int newId = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;

        var newUser = user with {Id = newId}; // ������ ����� � ����� Id
        users.Add(newUser);

        return Results.Created($"/api/users/{newUser.Id}", newUser);
    });

    app.MapPut("/api/users/{id}", (int id, User input) =>
    {
        var user = users.FirstOrDefault(u => u.Id == id);
        if(user is null) 
            return Results.NotFound();

        // ��������: ��� �����������
        if(string.IsNullOrWhiteSpace(input.Name))
            return Results.BadRequest("Name is required.");

        // ������ ����� ������ � ����� ������, �� ��� �� Id
        var updatedUser = user with {Name = input.Name};

        // ������� ������ ������� � �������� �� ����� ������
        var index = users.IndexOf(user);
        users[index] = updatedUser;

        return Results.NoContent();
    });

    app.MapDelete("/api/users/{id}", (int id) =>
    {
        var user = users.FirstOrDefault(u => u.Id == id);
        if(user is null) 
            return Results.NotFound();

        users.Remove(user);
        return Results.NoContent();
    });
}

// ����� �� ����� (����������)
// �������� �������� ��� ������ ������������� �� ��������� � �����
// ���� � cmd: curl -k https://localhost:7094/api/users/search
// ���� � cmd: curl -k https://localhost:7094/api/users/search?name=ali
void Func7()
{
    var users = new List<User>
    {
        new User {Id = 1, Name = "111-Alice"},
        new User {Id = 2, Name = "111-Bob"},
        new User {Id = 3, Name = "Alice-222"},
        new User {Id = 4, Name = "Bob-222"},
        new User {Id = 5, Name = "333-Alice-333"},
        new User {Id = 6, Name = "333-Bob-333"}
    };

    app.MapGet("/api/users/search", (string? name) =>
    {
        var result = string.IsNullOrEmpty(name)
            ? users
            : users.Where(u => u.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        return Results.Ok(result);
    });
}

// ������������� �������� � DI (Dependency Injection)
// ������� ������ � ��������� ������ � ������������ ��������� ������������
// ������������ ��������� � ������ �� ������ Services/IUserService.cs, Services/UserService.cs, Models/User.cs
// ���� � cmd: curl -k https://localhost:7094/api/users8
// ���� � cmd: curl -k https://localhost:7094/api/users8/1
// ���� � cmd: curl -k -X POST https://localhost:7094/api/users8 -H "Content-Type: application/json" -d "{\"Name\": \"Charlie\"}"
void Func8()
{
    // ��������
    app.MapGet("/api/users8", (IUserService service) => Results.Ok(service.GetAll()));

    app.MapGet("/api/users8/{id}", (IUserService service, int id) =>
    {
        var user = service.GetById(id);
        return user is null ? Results.NotFound() : Results.Ok(user);
    });

    app.MapPost("/api/users8", (IUserService service, CreateUserRequest request) =>
    {
        if(string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest("Name required");

        var user = service.Create(request.Name);
        return Results.Created($"/api/users8/{user.Id}", user);
    });
}

// ��������� ������ (Global Exception Middleware)
// ����������� ����� ���������� � ������� ����������������� JSON-������
// ���� � cmd: curl -k https://localhost:7094/api/error
void Func9()
{
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch(Exception ex)
        {
            if(ex is ArgumentException)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new
                {
                    Error = "Bad Request",
                    Message = ex.Message
                });
            }
            else
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new
                {
                    Error = "Internal Server Error",
                    Message = ex.Message
                });
            }

            Console.WriteLine($"������ {context.Response.StatusCode}: {ex}");
        }
    });

    // �������� endpoint � �������
    app.MapGet("/api/error", () =>
    {
        throw new InvalidOperationException("Something went wrong!");
    });
}

// ��������� ������ �������������
// ���������� ������������� �������� � ����������� (������� ��������, ������, ����� ����������)
// � ���������� � ������� �� Func8()
// ���� � cmd: curl -k "https://localhost:7094/api/users810?page=3&pageSize=1" -H "Content-Type: application/json"
void Func10()
{
    app.MapGet("/api/users810", (IUserService service, [AsParameters] PaginationParams pagination) =>
    {
        var all = service.GetAll().ToList();
        var paged = all
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize);

        return Results.Ok(new
        {
            Data = paged,
            Total = all.Count,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        });
    });
}

Func1();
Func2();
Func3();
Func4();
Func4_1();
Func5();
Func6();
Func7();
Func8();
Func9();
Func10();

app.Run();
