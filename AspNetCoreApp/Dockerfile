# Используем SDK для сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Копируем файл проекта и восстанавливаем зависимости
COPY *.csproj ./
RUN dotnet restore

# Копируем всё остальное и публикуем
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Финальный образ — только runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80/tcp
EXPOSE 443/tcp

# Запуск приложения
ENTRYPOINT ["dotnet", "AspNetCoreApp.dll"]
