namespace AspNetCoreApp.Models
{
    /// <summary>Ответ с данными пользователя Id, Name, Email</summary>
    public record UserResponseModel(int Id, string Name, string Email);

    /// <summary>Запрос имени пользователя</summary>
    /// <param name="Name">Имя пользователя</param>
    public record UserRequestModel(string Name);

    /// <summary>Универсальный ответ API с кодом, сообщением и данными</summary>
    /// <typeparam name="T">Тип данных в ответе</typeparam>
    public record ApiResponseModel<T>
    {
        /// <summary>HTTP статус-код</summary>
        public int StatusCode { get; init; }
        /// <summary>Сообщение от сервера</summary>
        public string Message { get; init; } = string.Empty;
        /// <summary>Полезные данные</summary>
        public T? Data { get; init; }
    }

    /// <summary>Ответ с данными пользователя по ID</summary>
    public record GetUserByIdResponseModel(
        /// <summary>Идентификатор пользователя</summary>
        int UserId,
        /// <summary>Сообщение об успешном получении</summary>
        string Message);

    /// <summary>Ответ с ошибкой валидации</summary>
    public record ErrorResponseModel(
        /// <summary>Описание ошибки</summary>
        string Error);

    /// <summary> Модель пользователя с обязательным Name</summary>
    public record UserModel
    {
        /// <summary>Идентификатор пользователя</summary>
        public int Id { get; init; } = -1;
        /// <summary>Имя пользователя. Обязательно</summary>
        public required string Name { get; init; }
    }

    public record CreateUserRequestModel(string Name = "noname");

    public record PaginationParamsModel
    {
        /// <summary>Номер страницы (начинается с 1)</summary>
        public int Page { get; set; } = 1;
        /// <summary>Размер страницы (по умолчанию 10)</summary>
        public int PageSize { get; set; } = 10;
    }
}
