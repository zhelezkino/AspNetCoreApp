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

// ������������ ���� ������
//builder.Services.AddScoped<IUserService, UserService>(); // ��� ������ HTTP-������� ������� ����� ���������� ����������
builder.Services.AddSingleton<IUserService, UserService>(); // ���� ��������� ���������� �� ��� ���������� (��� ������ � in-memory �������)

// ��������� Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // �������� ���������� �� API
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

    // ���������� XML-����������� (���� �������� � .csproj)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // �����������: �������� ����������� enum'�� � nullable
    options.EnableAnnotations(); // �������� ������ � ����������, �������� [Required]
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

// �������� Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

////////////////////////////////////////////////////////////////////////////////
// Minimal API. Target functions
////////////////////////////////////////////////////////////////////////////////

// API ��� ��������� "Hello, World!"
// ���� � cmd: curl https://localhost:7094/api1/hello
void Func01()
{
    app.MapGet("/api1/hello", () => "Hello, World!")
        .WithSummary("�������� �����������")
        .WithDescription("���������� ������� ��������� ����������� 'Hello, World!'")
        .WithTags("#1: Hello, World!");
}

// ��������� JSON-������ ������������
// ���������� ������ � Id, Name � Email ������������
// ���� � cmd: curl https://localhost:7094/api2/user
void Func02()
{
    app.MapGet("/api2/user", () =>
    {
        return new UserResponseModel(Id: 1, Name: "John Doe", Email: "john@example.com");
    })
    .WithSummary("�������� ������ ������ ���������� ������������")
    .WithDescription("���������� ������������� ������ ������ ���������� ������������ � ������� JSON")
    .Produces<UserResponseModel>(StatusCodes.Status200OK)
    .WithTags("#2: ������������")
    .WithOpenApi();
}

// ����� ������ ����� POST � ������� ������
// ���� � cmd: curl -X POST https://localhost:7094/api3/greet -H "Content-Type: application/json" -d "{\"Name\": \"Alice\"}"
void Func03()
{
    app.MapPost("/api3/greet", (UserRequestModel request) =>
    {
        return Results.Ok($"Hello, {request.Name}!");
    })
    .WithSummary("����������� �� �����")
    .WithDescription("��������� ��� ������������ � ���������� ������������������� �����������")
    .Accepts<UserRequestModel>("application/json")
    .Produces<string>(StatusCodes.Status200OK)
    .WithTags("#3: �����������")
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

// ��������� ������� ������. ���������, ��� ���� Name �� ������. �������� ��� ���������� ����������
// ���� � cmd: curl -X POST https://localhost:7094/api4/validate -H "Content-Type: application/json" -d "{\"Name\": \"Alice\"}"
// ���� � cmd: curl -X POST https://localhost:7094/api4/validate -H "Content-Type: application/json" -d "{\"Name\": \"\"}"
void Func04()
{
    app.MapPost("/api4/validate", (UserRequestModel request) =>
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            // ���������� 400 � ��������
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

        // ���������� 200 � ������������
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
    .WithSummary("��������� �����")
    .WithDescription("���������, ��� ��� �� ������. ���������� ���������������� JSON-�����")
    .Accepts<UserRequestModel>("application/json")
    .Produces<ApiResponseModel<string>>(StatusCodes.Status200OK)
    .Produces<ApiResponseModel<string>>(StatusCodes.Status400BadRequest)
    .WithTags("#4: ���������")
    .WithOpenApi();
}

// ��������� �������� (Route Parameters)
// �������� id �� URL � ������� ���������� � ������������. �������� id �� ������������� �����
// ���� � cmd: curl https://localhost:7094/api5/user_id/
// ���� � cmd: curl https://localhost:7094/api5/user_id/123456
// ���� � cmd: curl https://localhost:7094/api5/user_id/-123456
// ���� � cmd: curl https://localhost:7094/api5/user_id/123abc
void Func05()
{
    // ���������, ������ �� ��������
    app.MapGet("/api5/user_id", () =>
    {
        return Results.BadRequest(new {error = "User ID is required."});
    })
    .WithSummary("������: ID �� ������")
    .WithDescription("����������, ����� ID �� ������� � URL")
    .Produces<object>(StatusCodes.Status400BadRequest)
    .WithTags("#5: ������������");

    app.MapGet("/api5/user_id/{id}", (string id) =>
    {
        // ���������, ������ �� ��������
        if(string.IsNullOrEmpty(id))
            return Results.BadRequest(new {error = "User ID is required."});

        // �������� ������������� � int
        if(!int.TryParse(id, out int userId))
            return Results.BadRequest(new {error = "User ID must be a valid number."});

        if(userId < 0)
            return Results.BadRequest(new {error = "User ID must be a positive number."});

        // ���� ��� ������ - ���������� ������
        return Results.Ok(new {UserId = userId, Message = $"User with ID {userId} retrieved."});
    })
    .WithSummary("�������� ������������ �� ID")
    .WithDescription("��������� ID �� URL, ��������� ��� �� ���������� � ���������� ������")
    .Produces<GetUserByIdResponseModel>(StatusCodes.Status200OK)
    .Produces<ErrorResponseModel>(StatusCodes.Status400BadRequest)
    .WithTags("#5: ������������")
    .WithOpenApi(operation =>
    {
        operation.Parameters[0].Description = "������������� ID ������������. ������ ���� ������������� ������.";
        return operation;
    });
}

// In-Memory CRUD ��� �������������
// ����������� ������ ���� - ��������, ������, ����������, �������� ������������� � ����������� ������
// ���� � cmd: curl -k https://localhost:7094/api6/users
// ���� � cmd: curl -k https://localhost:7094/api6/users/1
// ���� � cmd: curl -k -X POST https://localhost:7094/api6/users -H "Content-Type: application/json" -d "{\"Name\": \"Tom\"}"
// ���� � cmd: curl -k -X PUT https://localhost:7094/api6/users/3 -H "Content-Type: application/json" -d "{\"Name\": \"Tomasson (new name)\"}"
// ���� � cmd: curl -k -X DELETE https://localhost:7094/api6/users/3
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
        .WithSummary("�������� ���� �������������")
        .WithDescription("���������� ������ ���� ������������� �� ����������� ������")
        .Produces<List<UserModel>>(StatusCodes.Status200OK)
        .WithTags("#6: CRUD - In-Memory")
        .WithOpenApi();

    app.MapGet("/api6/users/{id}", (int id) =>
    {
        // ��� ����� �������� �������� �� ��, ��� id - ��� ������������� �����
        // ...

        var user = users.FirstOrDefault(u => u.Id == id);
        return user is null ? Results.NotFound() : Results.Ok(user);
    })
        .WithSummary("�������� ������������ �� ID")
        .WithDescription("���������� ������������ �� ���������� ID. ���� �� ������ � 404")
        .Produces<UserModel>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags("#6: CRUD - In-Memory")
        .WithOpenApi(operation => {
            operation.Parameters[0].Description = "ID ������������ (����� ������������� �����)";
            return operation;
        });

    app.MapPost("/api6/users", (UserModel user) =>
    {
        // ��������: ��� �����������
        if (string.IsNullOrWhiteSpace(user.Name))
            return Results.BadRequest("Name is required.");

        int newId = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;

        var newUser = user with {Id = newId}; // ������� ����� � ����� Id
        users.Add(newUser);

        return Results.Created($"/api6/users/{newUser.Id}", newUser);
    })
        .WithSummary("������� ������ ������������")
        .WithDescription("������� ������ ������������. ���� `Name` �����������")
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

        // ��������: ��� �����������
        if(string.IsNullOrWhiteSpace(input.Name))
            return Results.BadRequest("Name is required.");

        // ������� ����� ������ � ����� ������, �� ��� �� Id
        var updatedUser = user with {Name = input.Name};
        // ������� ������ ������� � �������� �� ����� ������
        var index = users.IndexOf(user);
        users[index] = updatedUser;

        return Results.NoContent();
    })
        .WithSummary("�������� ������������")
        .WithDescription("��������� ��� ������������ �� ID. ���������� 204 No Content ��� ������")
        .Accepts<UserModel>("application/json")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags("#6: CRUD - In-Memory")
        .WithOpenApi(operation => {
            operation.Parameters[0].Description = "ID ������������ ��� ����������";
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
        .WithSummary("������� ������������")
        .WithDescription("������� ������������ �� ID. ���������� 204 No Content ��� ������")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags("#6: CRUD - In-Memory")
        .WithOpenApi(operation => {
            operation.Parameters[0].Description = "ID ������������ ��� ��������";
            return operation;
        });
}

// ����� �� ����� (����������)
// �������� �������� ��� ������ ������������� �� ��������� � �����
// ���� � cmd: curl -k https://localhost:7094/api7/users/search
// ���� � cmd: curl -k https://localhost:7094/api7/users/search?name=ali
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
        .WithSummary("����� ������������� �� �����")
        .WithDescription("���������� ������ �������������, ��� ��� �������� ��������� ��������� (������������������). ���� �������� `name` �� ����� � ���������� ����")
        .Produces<IEnumerable<UserModel>>(StatusCodes.Status200OK)
        .WithTags("#7: ����� �� ���������")
        .WithOpenApi(operation => {
            var parameter = operation.Parameters[0];
            parameter.Description = "��������� ��� ������ � ����� (�������������� ��������)";
            parameter.Required = false;
            return operation;
        });
}

// ������������� �������� � DI (Dependency Injection)
// ������� ������ � ��������� ������ � ������������ ��������� ������������
// ������������ ��������� � ������ �� ������ Services/IUserService.cs, Services/UserService.cs, Models/UserModel.cs
// ���� � cmd: curl -k https://localhost:7094/api8/users
// ���� � cmd: curl -k https://localhost:7094/api8/users/1
// ���� � cmd: curl -k -X POST https://localhost:7094/api8/users -H "Content-Type: application/json" -d "{\"Name\": \"Billy\"}"
void Func08()
{
    // ��������
    app.MapGet("/api8/users", (IUserService service) => Results.Ok(service.GetAll()))
        .WithSummary("�������� ���� ������������� (����� ������)")
        .WithDescription("���������� ���������� ������ IUserService ��� ��������� ������ ���� �������������")
        .Produces<List<UserModel>>(StatusCodes.Status200OK)
        .WithTags("#8: DI � �������")
        .WithOpenApi();

    app.MapGet("/api8/users/{id}", (IUserService service, int id) =>
    {
        var user = service.GetById(id);
        return user is null ? Results.NotFound() : Results.Ok(user);
    })
        .WithSummary("�������� ������������ �� ID (������)")
        .WithDescription("���������� ������������ ����� ������. ���� �� ������ � 404")
        .Produces<UserModel>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags("#8: DI � �������")
        .WithOpenApi(operation => {
            operation.Parameters[0].Description = "ID ������������";
            return operation;
        });

    app.MapPost("/api8/users", (IUserService service, CreateUserRequestModel request) =>
    {
        if(string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest("Name required");

        var user = service.Create(request.Name);
        return Results.Created($"/api8/users/{user.Id}", user);
    })
        .WithSummary("������� ������������ (������)")
        .WithDescription("������� ������������ ����� ������. ��������� ���� `Name`")
        .Accepts<CreateUserRequestModel>("application/json")
        .Produces<UserModel>(StatusCodes.Status201Created)
        .Produces<string>(StatusCodes.Status400BadRequest)
        .WithTags("#8: DI � �������")
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

// ��������� ������ (Global Exception Middleware)
// ����������� ����� ���������� � ������� ����������������� JSON-������
// ���� � cmd: curl -k https://localhost:7094/api9/error
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

            Console.WriteLine($"������ {context.Response.StatusCode}: {ex}");
        }
    });

    // �������� endpoint � �������
    app.MapGet("/api9/error", () =>
    {
        throw new InvalidOperationException("Something went wrong!");
    })
        .WithSummary("���� ������")
        .WithDescription("�������� ��� ������������ ����������� ����������� ����������. ������ ����������� ����������")
        .Produces<object>(StatusCodes.Status500InternalServerError)
        .WithTags("#9: ��������� ������")
        .WithOpenApi(operation => {
            operation.Responses["500"] = new OpenApiResponse
            {
                Description = "������ ���������� 500 Internal Server Error � JSON-�������",
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

// ��������� ������ �������������
// ���������� ������������� �������� � ����������� (������� ��������, ������, ����� ����������)
// ���� � cmd: curl -k "https://localhost:7094/api10/users?page=2&pageSize=2" -H "Content-Type: application/json"
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
        .WithSummary("�������� ������������� � ����������")
        .WithDescription("���������� ������ ������������� ��������. ������������ ��������� `page` � `pageSize` (�� ���������: page=1, pageSize=10)")
        .Produces<object>(StatusCodes.Status200OK)
        .WithTags("#10: ���������")
        .WithOpenApi(operation =>
        {
            // ���������� FirstOrDefault, �� ������ �������� � ���������
            var pageParam = operation.Parameters.FirstOrDefault(p => p.Name == "page");
            if (pageParam != null)
            {
                pageParam.Description = "����� �������� (���������� � 1)";
                pageParam.Schema.Default = new OpenApiInteger(1);
            }

            var pageSizeParam = operation.Parameters.FirstOrDefault(p => p.Name == "pageSize");
            if (pageSizeParam != null)
            {
                pageSizeParam.Description = "������ �������� (�� ��������� 10)";
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
