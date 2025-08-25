using AspNetCoreApp.Models;
using AspNetCoreApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

////////////////////////////////////////////////////////////////////////////////
// Init builder
////////////////////////////////////////////////////////////////////////////////

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
    // Основная информация об API
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "AspNetCoreApp",
        Description = "A simple ASP.NET Core web API for learning",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "Support",
            Email = "support@example.com",
            Url = new Uri("https://example.com/support")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Подключаем XML-комментарии (если включены в .csproj)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Опционально: улучшаем отображение enum'ов и nullable
    options.EnableAnnotations(); // Улучшает работу с атрибутами, например [Required]
});

////////////////////////////////////////////////////////////////////////////////
// Init app
////////////////////////////////////////////////////////////////////////////////

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

////////////////////////////////////////////////////////////////////////////////
// Minimal API. Target functions
////////////////////////////////////////////////////////////////////////////////

// API для получения "Hello, World!"
// Тест в cmd: curl https://localhost:7094/api1/hello
void Func01()
{
    app.MapGet("/api1/hello", () => "Hello, World!")
        .WithSummary("Получить приветствие")
        .WithDescription("Возвращает простое текстовое приветствие 'Hello, World!'")
        .WithTags("#1: Hello, World!");
}

// Получение JSON-данных пользователя
// Возвращает объект с Id, Name и Email пользователя
// Тест в cmd: curl https://localhost:7094/api2/user
void Func02()
{
    app.MapGet("/api2/user", () =>
    {
        return new UserResponseModel(Id: 1, Name: "John Doe", Email: "john@example.com");
    })
    .WithSummary("Получить данные одного дефолтного пользователя")
    .WithDescription("Возвращает фиксированные данные одного дефолтного пользователя в формате JSON")
    .Produces<UserResponseModel>(StatusCodes.Status200OK)
    .WithTags("#2: Пользователь")
    .WithOpenApi();
}

// Прием данных через POST и возврат ответа
// Тест в cmd: curl -X POST https://localhost:7094/api3/greet -H "Content-Type: application/json" -d "{\"Name\": \"Alice\"}"
void Func03()
{
    app.MapPost("/api3/greet", (UserRequestModel request) =>
    {
        return Results.Ok($"Hello, {request.Name}!");
    })
    .WithSummary("Приветствие по имени")
    .WithDescription("Принимает имя пользователя и возвращает персонализированное приветствие")
    .Accepts<UserRequestModel>("application/json")
    .Produces<string>(StatusCodes.Status200OK)
    .WithTags("#3: Приветствие")
    .WithOpenApi(operation => new OpenApiOperation(operation)
    {
        RequestBody = new OpenApiRequestBody
        {
            Content = {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = operation.RequestBody.Content["application/json"].Schema,
                    Example = new OpenApiString("{\"Name\": \"Alice\"}")
                }
            }
        }
    });
}

// Валидация входных данных. Проверить, что поле Name не пустое. Показать код результата выполнения
// Тест в cmd: curl -X POST https://localhost:7094/api4/validate -H "Content-Type: application/json" -d "{\"Name\": \"Alice\"}"
// Тест в cmd: curl -X POST https://localhost:7094/api4/validate -H "Content-Type: application/json" -d "{\"Name\": \"\"}"
void Func04()
{
    app.MapPost("/api4/validate", (UserRequestModel request) =>
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            // Возвращаем 400 с деталями
            return Results.Json(
                new ApiResponseModel<string>
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
            new ApiResponseModel<string>
            {
                StatusCode = 200,
                Message = $"Hello, {request.Name}!",
                Data = request.Name
            }
            ,statusCode: 200
        );
    })
    .WithSummary("Валидация имени")
    .WithDescription("Проверяет, что имя не пустое. Возвращает детализированный JSON-ответ")
    .Accepts<UserRequestModel>("application/json")
    .Produces<ApiResponseModel<string>>(StatusCodes.Status200OK)
    .Produces<ApiResponseModel<string>>(StatusCodes.Status400BadRequest)
    .WithTags("#4: Валидация")
    .WithOpenApi();
}

// Параметры маршрута (Route Parameters)
// Получить id из URL и вернуть информацию о пользователе. Проверка id на положительное число
// Тест в cmd: curl https://localhost:7094/api5/user_id/
// Тест в cmd: curl https://localhost:7094/api5/user_id/123456
// Тест в cmd: curl https://localhost:7094/api5/user_id/-123456
// Тест в cmd: curl https://localhost:7094/api5/user_id/123abc
void Func05()
{
    // Проверяем, пустой ли параметр
    app.MapGet("/api5/user_id", () =>
    {
        return Results.BadRequest(new {error = "User ID is required."});
    })
    .WithSummary("Ошибка: ID не указан")
    .WithDescription("Вызывается, когда ID не передан в URL")
    .Produces<object>(StatusCodes.Status400BadRequest)
    .WithTags("#5: Пользователь");

    app.MapGet("/api5/user_id/{id}", (string id) =>
    {
        // Проверяем, пустой ли параметр
        if(string.IsNullOrEmpty(id))
            return Results.BadRequest(new {error = "User ID is required."});

        // Пытаемся преобразовать в int
        if(!int.TryParse(id, out int userId))
            return Results.BadRequest(new {error = "User ID must be a valid number."});

        if(userId < 0)
            return Results.BadRequest(new {error = "User ID must be a positive number."});

        // Если все хорошо - возвращаем данные
        return Results.Ok(new {UserId = userId, Message = $"User with ID {userId} retrieved."});
    })
    .WithSummary("Получить пользователя по ID")
    .WithDescription("Принимает ID из URL, проверяет его на валидность и возвращает данные")
    .Produces<GetUserByIdResponseModel>(StatusCodes.Status200OK)
    .Produces<ErrorResponseModel>(StatusCodes.Status400BadRequest)
    .WithTags("#5: Пользователь")
    .WithOpenApi(operation =>
    {
        operation.Parameters[0].Description = "Целочисленный ID пользователя. Должен быть положительным числом.";
        return operation;
    });
}

// In-Memory CRUD для пользователей
// Реализовать полный цикл - создание, чтение, обновление, удаление пользователей в оперативной памяти
// Тест в cmd: curl -k https://localhost:7094/api6/users
// Тест в cmd: curl -k https://localhost:7094/api6/users/1
// Тест в cmd: curl -k -X POST https://localhost:7094/api6/users -H "Content-Type: application/json" -d "{\"Name\": \"Tom\"}"
// Тест в cmd: curl -k -X PUT https://localhost:7094/api6/users/3 -H "Content-Type: application/json" -d "{\"Name\": \"Tomasson (new name)\"}"
// Тест в cmd: curl -k -X DELETE https://localhost:7094/api6/users/3
void Func06()
{
    int count = 1;
    var users = new List<UserModel>
    {
        new UserModel {Id = count++, Name = "Alice"},
        new UserModel {Id = count++, Name = "Bob"},
        new UserModel {Id = count++, Name = "Mark"}
    };

    app.MapGet("/api6/users", () => Results.Ok(users))
        .WithSummary("Получить всех пользователей")
        .WithDescription("Возвращает список всех пользователей из оперативной памяти")
        .Produces<List<UserModel>>(StatusCodes.Status200OK)
        .WithTags("#6: CRUD - In-Memory")
        .WithOpenApi();

    app.MapGet("/api6/users/{id}", (int id) =>
    {
        // Тут можно добавить проверку на то, что id - это положительное число
        // ...

        var user = users.FirstOrDefault(u => u.Id == id);
        return user is null ? Results.NotFound() : Results.Ok(user);
    })
        .WithSummary("Получить пользователя по ID")
        .WithDescription("Возвращает пользователя по указанному ID. Если не найден — 404")
        .Produces<UserModel>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags("#6: CRUD - In-Memory")
        .WithOpenApi(operation => {
            operation.Parameters[0].Description = "ID пользователя (целое положительное число)";
            return operation;
        });

    app.MapPost("/api6/users", (UserModel user) =>
    {
        // Проверка: имя обязательно
        if (string.IsNullOrWhiteSpace(user.Name))
            return Results.BadRequest("Name is required.");

        int newId = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;

        var newUser = user with {Id = newId}; // Создаем копию с новым Id
        users.Add(newUser);

        return Results.Created($"/api6/users/{newUser.Id}", newUser);
    })
        .WithSummary("Создать нового пользователя")
        .WithDescription("Создает нового пользователя. Поле `Name` обязательно")
        .Accepts<UserModel>("application/json")
        .Produces<UserModel>(StatusCodes.Status201Created)
        .Produces<string>(StatusCodes.Status400BadRequest)
        .WithTags("#6: CRUD - In-Memory")
        .WithOpenApi(operation => new OpenApiOperation(operation)
        {
            RequestBody = new OpenApiRequestBody
            {
                Content = {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = operation.RequestBody.Content["application/json"].Schema,
                        Example = new OpenApiString("{\"Name\": \"Tom\"}")
                    }
                }
            }
        });

    app.MapPut("/api6/users/{id}", (int id, UserModel input) =>
    {
        var user = users.FirstOrDefault(u => u.Id == id);
        if(user is null) 
            return Results.NotFound();

        // Проверка: имя обязательно
        if(string.IsNullOrWhiteSpace(input.Name))
            return Results.BadRequest("Name is required.");

        // Создаем новый объект с новым именем, но тем же Id
        var updatedUser = user with {Name = input.Name};
        // Находим индекс объекта и заменяем на новый объект
        var index = users.IndexOf(user);
        users[index] = updatedUser;

        return Results.NoContent();
    })
        .WithSummary("Обновить пользователя")
        .WithDescription("Обновляет имя пользователя по ID. Возвращает 204 No Content при успехе")
        .Accepts<UserModel>("application/json")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags("#6: CRUD - In-Memory")
        .WithOpenApi(operation => {
            operation.Parameters[0].Description = "ID пользователя для обновления";
            return operation;
        });

    app.MapDelete("/api6/users/{id}", (int id) =>
    {
        var user = users.FirstOrDefault(u => u.Id == id);
        if(user is null) 
            return Results.NotFound();

        users.Remove(user);
        return Results.NoContent();
    })
        .WithSummary("Удалить пользователя")
        .WithDescription("Удаляет пользователя по ID. Возвращает 204 No Content при успехе")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags("#6: CRUD - In-Memory")
        .WithOpenApi(operation => {
            operation.Parameters[0].Description = "ID пользователя для удаления";
            return operation;
        });
}

// Поиск по имени (фильтрация)
// Добавить эндпоинт для поиска пользователей по подстроке в имени
// Тест в cmd: curl -k https://localhost:7094/api7/users/search
// Тест в cmd: curl -k https://localhost:7094/api7/users/search?name=ali
void Func07()
{
    int count = 1;
    var users = new List<UserModel>
    {
        new UserModel {Id = count++, Name = "111-Alice"},
        new UserModel {Id = count++, Name = "111-Bob"},
        new UserModel {Id = count++, Name = "Alice-222"},
        new UserModel {Id = count++, Name = "Bob-222"},
        new UserModel {Id = count++, Name = "333-Alice-333"},
        new UserModel {Id = count++, Name = "333-Bob-333"}
    };

    app.MapGet("/api7/users/search", (string? name) =>
    {
        var result = string.IsNullOrEmpty(name)
            ? users
            : users.Where(u => u.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        return Results.Ok(result);
    })
        .WithSummary("Поиск пользователей по имени")
        .WithDescription("Возвращает список пользователей, чье имя содержит указанную подстроку (регистронезависимо). Если параметр `name` не задан — возвращает всех")
        .Produces<IEnumerable<UserModel>>(StatusCodes.Status200OK)
        .WithTags("#7: Поиск по подстроке")
        .WithOpenApi(operation => {
            var parameter = operation.Parameters[0];
            parameter.Description = "Подстрока для поиска в имени (необязательный параметр)";
            parameter.Required = false;
            return operation;
        });
}

// Использование сервисов и DI (Dependency Injection)
// Вынести логику в отдельный сервис и использовать внедрение зависимостей
// Используется интерфейс и модель из файлов Services/IUserService.cs, Services/UserService.cs, Models/UserModel.cs
// Тест в cmd: curl -k https://localhost:7094/api8/users
// Тест в cmd: curl -k https://localhost:7094/api8/users/1
// Тест в cmd: curl -k -X POST https://localhost:7094/api8/users -H "Content-Type: application/json" -d "{\"Name\": \"Billy\"}"
void Func08()
{
    // Маршруты
    app.MapGet("/api8/users", (IUserService service) => Results.Ok(service.GetAll()))
        .WithSummary("Получить всех пользователей (через сервис)")
        .WithDescription("Использует внедренный сервис IUserService для получения списка всех пользователей")
        .Produces<List<UserModel>>(StatusCodes.Status200OK)
        .WithTags("#8: DI и Сервисы")
        .WithOpenApi();

    app.MapGet("/api8/users/{id}", (IUserService service, int id) =>
    {
        var user = service.GetById(id);
        return user is null ? Results.NotFound() : Results.Ok(user);
    })
        .WithSummary("Получить пользователя по ID (сервис)")
        .WithDescription("Возвращает пользователя через сервис. Если не найден — 404")
        .Produces<UserModel>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags("#8: DI и Сервисы")
        .WithOpenApi(operation => {
            operation.Parameters[0].Description = "ID пользователя";
            return operation;
        });

    app.MapPost("/api8/users", (IUserService service, CreateUserRequestModel request) =>
    {
        if(string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest("Name required");

        var user = service.Create(request.Name);
        return Results.Created($"/api8/users/{user.Id}", user);
    })
        .WithSummary("Создать пользователя (сервис)")
        .WithDescription("Создает пользователя через сервис. Требуется поле `Name`")
        .Accepts<CreateUserRequestModel>("application/json")
        .Produces<UserModel>(StatusCodes.Status201Created)
        .Produces<string>(StatusCodes.Status400BadRequest)
        .WithTags("#8: DI и Сервисы")
        .WithOpenApi(operation => new OpenApiOperation(operation)
        {
            RequestBody = new OpenApiRequestBody
            {
                Content = {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = operation.RequestBody.Content["application/json"].Schema,
                        Example = new OpenApiString("{\"Name\": \"Billy\"}")
                    }
                }
            }
        });
}

// Обработка ошибок (Global Exception Middleware)
// Перехватить любое исключение и вернуть структурированную JSON-ошибку
// Тест в cmd: curl -k https://localhost:7094/api9/error
void Func09()
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
    app.MapGet("/api9/error", () =>
    {
        throw new InvalidOperationException("Something went wrong!");
    })
        .WithSummary("Тест ошибки")
        .WithDescription("Эндпоинт для тестирования глобального обработчика исключений. Всегда выбрасывает исключение")
        .Produces<object>(StatusCodes.Status500InternalServerError)
        .WithTags("#9: Обработка ошибок")
        .WithOpenApi(operation => {
            operation.Responses["500"] = new OpenApiResponse
            {
                Description = "Всегда возвращает 500 Internal Server Error с JSON-ошибкой",
                Content = {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties =
                            {
                                ["Error"] = new OpenApiSchema { Type = "string" },
                                ["Message"] = new OpenApiSchema { Type = "string" }
                            }
                        },
                        Example = new OpenApiObject
                        {
                            ["Error"] = new OpenApiString("Swagger: Internal Server Error"),
                            ["Message"] = new OpenApiString("Swagger: Something went wrong!")
                        }
                    }
                }
            };
            return operation;
        });
}

// Пагинация списка пользователей
// Возвращать пользователей порциями с метаданными (текущая страница, размер, общее количество)
// Тест в cmd: curl -k "https://localhost:7094/api10/users?page=2&pageSize=2" -H "Content-Type: application/json"
void Func10()
{
    app.MapGet("/api10/users", (IUserService service, [AsParameters] PaginationParamsModel pagination) =>
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
    })
        .WithSummary("Получить пользователей с пагинацией")
        .WithDescription("Возвращает список пользователей порциями. Поддерживает параметры `page` и `pageSize` (по умолчанию: page=1, pageSize=10)")
        .Produces<object>(StatusCodes.Status200OK)
        .WithTags("#10: Пагинация")
        .WithOpenApi(operation =>
        {
            // Используем FirstOrDefault, на случай задержки в генерации
            var pageParam = operation.Parameters.FirstOrDefault(p => p.Name == "page");
            if (pageParam != null)
            {
                pageParam.Description = "Номер страницы (начинается с 1)";
                pageParam.Schema.Default = new OpenApiInteger(1);
            }

            var pageSizeParam = operation.Parameters.FirstOrDefault(p => p.Name == "pageSize");
            if (pageSizeParam != null)
            {
                pageSizeParam.Description = "Размер страницы (по умолчанию 10)";
                pageSizeParam.Schema.Default = new OpenApiInteger(10);
            }

            return operation;
        });
}

Func01();
Func02();
Func03();
Func04();
Func05();
Func06();
Func07();
Func08();
Func09();
Func10();

app.Run();
