# ---------- Этап 1: сборка SPA ----------
FROM node:22-alpine AS spa
WORKDIR /spa
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
# outDir из vite.config переопределяется, чтобы сборка осталась внутри контейнерного каталога
RUN npm run build -- --outDir dist

# ---------- Этап 2: сборка .NET ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TravelPlanner.slnx ./
COPY src/TravelPlanner.Core/TravelPlanner.Core.csproj src/TravelPlanner.Core/
COPY src/TravelPlanner.Infrastructure/TravelPlanner.Infrastructure.csproj src/TravelPlanner.Infrastructure/
COPY src/TravelPlanner.Api/TravelPlanner.Api.csproj src/TravelPlanner.Api/
COPY tests/TravelPlanner.Tests/TravelPlanner.Tests.csproj tests/TravelPlanner.Tests/
RUN dotnet restore src/TravelPlanner.Api/TravelPlanner.Api.csproj
COPY . .
COPY --from=spa /spa/dist src/TravelPlanner.Api/wwwroot/
RUN dotnet publish src/TravelPlanner.Api/TravelPlanner.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- Этап 3: рантайм ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Default="Data Source=/app/App_Data/travelplanner.db"
VOLUME /app/App_Data
EXPOSE 8080
ENTRYPOINT ["dotnet", "TravelPlanner.Api.dll"]