# Этап 1: минимальная среда для запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80/tcp
EXPOSE 443/tcp

# Этап 2: сборка с SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Копируем .csproj и восстанавливаем зависимости (чтобы использовать кэш)
COPY *.csproj ./
RUN dotnet restore

# Копируем остальные файлы и публикуем
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Этап 3: финальный образ — только runtime
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AspNetCoreApp.dll"]
