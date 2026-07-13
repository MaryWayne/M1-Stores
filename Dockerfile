# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first so dependency layers cache between builds
COPY server/M1Stores.sln server/
COPY server/src/M1.Domain/M1.Domain.csproj server/src/M1.Domain/
COPY server/src/M1.Application/M1.Application.csproj server/src/M1.Application/
COPY server/src/M1.Infrastructure/M1.Infrastructure.csproj server/src/M1.Infrastructure/
COPY server/src/M1.Api/M1.Api.csproj server/src/M1.Api/
RUN dotnet restore server/src/M1.Api/M1.Api.csproj

COPY server/ server/
RUN dotnet publish server/src/M1.Api/M1.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render injects PORT; default 8080 for local runs
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "M1.Api.dll"]
