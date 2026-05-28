FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/NotificationPlatform.Api/NotificationPlatform.Api.csproj", "src/NotificationPlatform.Api/"]
COPY ["src/NotificationPlatform.Application/NotificationPlatform.Application.csproj", "src/NotificationPlatform.Application/"]
COPY ["src/NotificationPlatform.Domain/NotificationPlatform.Domain.csproj", "src/NotificationPlatform.Domain/"]
COPY ["src/NotificationPlatform.Infrastructure/NotificationPlatform.Infrastructure.csproj", "src/NotificationPlatform.Infrastructure/"]
RUN dotnet restore "src/NotificationPlatform.Api/NotificationPlatform.Api.csproj"
COPY . .
RUN dotnet publish "src/NotificationPlatform.Api/NotificationPlatform.Api.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationPlatform.Api.dll"]
