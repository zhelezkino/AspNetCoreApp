var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

// ������� Web API ��� ��������� "Hello, World!"
// ������ � ��������: https://localhost:7000/api/hello
void Func1()
{
    app.MapGet("/api/hello", () => "Hello, World!");
}

// ������� JSON � ������� ������������
// ������ � ��������: https://localhost:7000/api/user
void Func2()
{
    app.MapGet("/api/user", () =>
    {
        return new { Id = 1, Name = "John Doe", Email = "john@example.com" };
    });
}

// ���� ������ ����� POST � ������� ������
// ��������������� �����: record UserRequest(string Name);
// ���� ����� Postman ��� curl: 
// curl -X POST https://localhost:7000/api/greet -H "Content-Type: application/json" -d "{\"Name\": \"Alice\"}"
void Func3()
{
    app.MapPost("/api/greet", (UserRequest request) =>
    {
        return Results.Ok($"Hello, {request.Name}!");
    });
}

// ��������� ������� ������. ���������, ��� ���� Name �� ������.
void Func4()
{

}

void Func5()
{

}

void Func6()
{

}

void Func7()
{

}

void Func8()
{

}

void Func9()
{

}

void Func10()
{

}

//Func1();

//Func2();

//Func3();
// curl -X POST https://localhost:7094/api/greet -H "Content-Type: application/json" -d "{\"Name\": \"Alice\"}"

Func4();

Func5();

Func6();

Func7();

Func8();

Func9();

Func10();

app.Run();

// ��������������� ���
record UserRequest(string Name);
