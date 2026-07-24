# Sprint 8 Commit 1: Backend Foundation. Standard multi-stage ASP.NET Core
# Dockerfile - build stage restores/publishes with the full SDK image,
# runtime stage only ships the much smaller ASP.NET runtime image plus the
# published output. No production deployment configuration here (that is
# explicitly out of this commit's scope) - this is for local development
# via docker-compose.yml only (see its own comment).

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Rojan.Server.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/Rojan.Server.Api/Rojan.Server.Api.csproj", "src/Rojan.Server.Api/"]
COPY ["src/Rojan.Server.Application/Rojan.Server.Application.csproj", "src/Rojan.Server.Application/"]
COPY ["src/Rojan.Server.Infrastructure/Rojan.Server.Infrastructure.csproj", "src/Rojan.Server.Infrastructure/"]
COPY ["src/Rojan.Server.Domain/Rojan.Server.Domain.csproj", "src/Rojan.Server.Domain/"]
RUN dotnet restore "src/Rojan.Server.Api/Rojan.Server.Api.csproj"
COPY . .
WORKDIR "/src/src/Rojan.Server.Api"
RUN dotnet build "Rojan.Server.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Rojan.Server.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Rojan.Server.Api.dll"]
