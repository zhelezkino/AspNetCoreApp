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

// Регистрируем свой сервис
//builder.Services.AddScoped<IUserService, UserService>(); // При каждом HTTP-запросе создает новые экземпляры переменных
builder.Services.AddSingleton<IUserService, UserService>(); // Один экземпляр переменных на все приложение (для работы с in-memory данными)

// Добавляем Swagger
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

// Включаем Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

/*------------------------------------------------------------------------------------------*/
// Целевые функции проекта
/*------------------------------------------------------------------------------------------*/

// Простой API для получения "Hello, World!"
// Тест в браузере: https://localhost:7094/api/hello
void Func1()
{
    app.MapGet("/api/hello", () => "Hello, World!");
}

// Возврат JSON с данными пользователя
// Тест в браузере: https://localhost:7094/api/user
void Func2()
{
    app.MapGet("/api/user", () =>
    {
        return new {Id = 1, Name = "John Doe", Email = "john@example.com"};
    });
}

// Приём данных через POST и возврат ответа
// Вспомогательный класс: record UserRequest(string Name);
// Тест в cmd: curl -X POST https://localhost:7094/api/greet -H "Content-Type: application/json" -d "{\"Name\": \"Alice\"}"
void Func3()
{
    app.MapPost("/api/greet", (UserRequest request) =>
    {
        return Results.Ok($"Hello, {request.Name}!");
    });
}

// Валидация входных данных. Проверить, что поле Name не пустое
// Вспомогательный класс: record UserRequest(string Name);
// Тест 1 в cmd: curl -X POST https://localhost:7094/api/validate -H "Content-Type: application/json" -d "{\"Name\": \"Alice\"}"
// Тест 2 в cmd: curl -X POST https://localhost:7094/api/validate -H "Content-Type: application/json" -d "{\"Name\": \"\"}"
void Func4()
{
    app.MapPost("/api/validate", (UserRequest request) =>
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest("Name is required.");

        return Results.Ok($"Hello, {request.Name}!");
    });
}

// Валидация входных данных. Проверить, что поле Name не пустое
// Дополнительно показать код результата выполнения
// Вспомогательные классы: record UserRequest(string Name); record ApiResponse<T>
// Тест 1 в cmd: curl -X POST https://localhost:7094/api/validate41 -H "Content-Type: application/json" -d "{\"Name\": \"Alice\"}"
// Тест 2 в cmd: curl -X POST https://localhost:7094/api/validate41 -H "Content-Type: application/json" -d "{\"Name\": \"\"}"
void Func4_1()
{
    app.MapPost("/api/validate41", (UserRequest request) =>
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            // Возвращаем 400 с деталями
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

        // Возвращаем 200 с приветствием
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

// Параметры маршрута (Route Parameters)
// Получить id из URL и вернуть информацию о пользователе. Проверка id на положительное число
// Тест в браузере: https://localhost:7094/api/user_id/
// Тест в браузере: https://localhost:7094/api/user_id/123456
// Тест в браузере: https://localhost:7094/api/user_id/-123456
// Тест в браузере: https://localhost:7094/api/user_id/abc123456
// Тест в cmd: curl -v https://localhost:7094/api/user_id/123456
void Func5()
{
    // Проверяем, пустой ли параметр
    app.MapGet("/api/user_id", () =>
    {
        return Results.BadRequest(new {error = "User ID is required."});
    });

    app.MapGet("/api/user_id/{id}", (string id) =>
    {
        // Проверяем, пустой ли параметр
        if(string.IsNullOrEmpty(id))
            return Results.BadRequest(new {error = "User ID is required."});

        // Пытаемся преобразовать в int
        if(!int.TryParse(id, out int userId))
            return Results.BadRequest(new {error = "User ID must be a valid number."});

        if(userId < 0)
            return Results.BadRequest(new {error = "User ID must be a positive number."});

        // Если всё хорошо - возвращаем данные
        return Results.Ok(new {UserId = userId, Message = $"User with ID {userId} retrieved."});
    });
}

// In-Memory CRUD для пользователей
// Реализовать полный цикл - создание, чтение, обновление, удаление пользователей в оперативной памяти
// Вспомогательный класс: record User(int Id, string Name);
// Тест в cmd: curl -k https://localhost:7094/api/users
// Тест в cmd: curl -k https://localhost:7094/api/users/1
// Тест в cmd: curl -k -X POST https://localhost:7094/api/users -H "Content-Type: application/json" -d "{\"Name\": \"Charlie\"}"
// Тест в cmd: curl -k -X PUT https://localhost:7094/api/users/3 -H "Content-Type: application/json" -d "{\"Name\": \"Charlie Updated\"}"
// Тест в cmd: curl -k -X DELETE https://localhost:7094/api/users/3
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
        // Тут можно добавить проверку на то, что id - это положительное число
        // ...

        var user = users.FirstOrDefault(u => u.Id == id);
        return user is null ? Results.NotFound() : Results.Ok(user);
    });

    app.MapPost("/api/users", (User user) =>
    {
        // Проверка: имя обязательно
        if (string.IsNullOrWhiteSpace(user.Name))
            return Results.BadRequest("Name is required.");

        int newId = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;

        var newUser = user with {Id = newId}; // Создаём копию с новым Id
        users.Add(newUser);

        return Results.Created($"/api/users/{newUser.Id}", newUser);
    });

    app.MapPut("/api/users/{id}", (int id, User input) =>
    {
        var user = users.FirstOrDefault(u => u.Id == id);
        if(user is null) 
            return Results.NotFound();

        // Проверка: имя обязательно
        if(string.IsNullOrWhiteSpace(input.Name))
            return Results.BadRequest("Name is required.");

        // Создаём новый объект с новым именем, но тем же Id
        var updatedUser = user with {Name = input.Name};

        // Находим индекс объекта и заменяем на новый объект
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

// Поиск по имени (фильтрация)
// Добавить эндпоинт для поиска пользователей по подстроке в имени
// Тест в cmd: curl -k https://localhost:7094/api/users/search
// Тест в cmd: curl -k https://localhost:7094/api/users/search?name=ali
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

// Использование сервисов и DI (Dependency Injection)
// Вынести логику в отдельный сервис и использовать внедрение зависимостей
// Используется интерфейс и модель из файлов Services/IUserService.cs, Services/UserService.cs, Models/User.cs
// Тест в cmd: curl -k https://localhost:7094/api/users8
// Тест в cmd: curl -k https://localhost:7094/api/users8/1
// Тест в cmd: curl -k -X POST https://localhost:7094/api/users8 -H "Content-Type: application/json" -d "{\"Name\": \"Charlie\"}"
void Func8()
{
    // Маршруты
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

// Обработка ошибок (Global Exception Middleware)
// Перехватить любое исключение и вернуть структурированную JSON-ошибку
// Тест в cmd: curl -k https://localhost:7094/api/error
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

            Console.WriteLine($"Ошибка {context.Response.StatusCode}: {ex}");
        }
    });

    // Тестовый endpoint с ошибкой
    app.MapGet("/api/error", () =>
    {
        throw new InvalidOperationException("Something went wrong!");
    });
}

// Пагинация списка пользователей
// Возвращать пользователей порциями с метаданными (текущая страница, размер, общее количество)
// В дополнение к методам из Func8()
// Тест в cmd: curl -k "https://localhost:7094/api/users810?page=3&pageSize=1" -H "Content-Type: application/json"
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
