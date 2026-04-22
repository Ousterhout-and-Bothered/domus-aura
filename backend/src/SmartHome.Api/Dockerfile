# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["backend/src/SmartHome.Api/SmartHome.Api.csproj", "backend/src/SmartHome.Api/"]
COPY ["backend/src/SmartHome.Domain/SmartHome.Domain.csproj", "backend/src/SmartHome.Domain/"]
COPY ["backend/src/SmartHome.Infrastructure/SmartHome.Infrastructure.csproj", "backend/src/SmartHome.Infrastructure/"]
RUN dotnet restore "backend/src/SmartHome.Api/SmartHome.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/backend/src/SmartHome.Api"
RUN dotnet build "SmartHome.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "SmartHome.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create a non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartHome.Api.dll"]