namespace AspNetCoreApp.Models
{
    record UserRequest(string Name);

    record ApiResponse<T>
    {
        public int StatusCode { get; init; }
        public string Message { get; init; } = string.Empty;
        public T? Data { get; init; }
    }

    record User
    {
        public int Id { get; init; }
        public required string Name { get; init; }
    }

    public record UserFunc8(int Id, string Name);

    public record CreateUserRequest(string Name);

    record PaginationParams(int Page = 1, int PageSize = 10);
}
