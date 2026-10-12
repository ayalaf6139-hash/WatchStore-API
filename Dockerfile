# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY WatchStore.Core/WatchStore.Core.csproj WatchStore.Core/
COPY WatchStore.Data/WatchStore.Data.csproj WatchStore.Data/
COPY WatchStore.Service/WatchStore.Service.csproj WatchStore.Service/
COPY WatchStore.API/WatchStore.API.csproj WatchStore.API/
RUN dotnet restore WatchStore.API/WatchStore.API.csproj

COPY . .
RUN dotnet publish WatchStore.API/WatchStore.API.csproj -c Release -o /app --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# Render מגדיר את PORT בזמן ריצה (ברירת מחדל 10000)
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-10000} exec dotnet WatchStore.API.dll"]
