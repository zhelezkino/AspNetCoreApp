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

void Func1()
{
    app.MapGet("/api/hello", () => "Hello, World!");
}

void Func2()
{
    app.MapGet("/api/user", () =>
    {
        return new { Id = 1, Name = "John Doe", Email = "john@example.com" };
    });
}

void Func3()
{
    app.MapPost("/api/greet", (UserRequest request) =>
    {
        return Results.Ok($"Hello, {request.Name}!");
    });
}

void Func4()
{

}

//Func1();
//Func2();
//Func3(); // curl -X POST https://localhost:7094/api/greet -H "Content-Type: application/json" -d "{\"Name\": \"Alice\"}"
Func4();

app.Run();

// Вспомогательный тип
record UserRequest(string Name);
