# Etapa base
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5001

# Etapa build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Application/Application.csproj ./Application/
COPY Infrastructure/Infrastructure.csproj ./Infrastructure/
COPY Domain/Domain.csproj ./Domain/
COPY WebApi/WebApi.csproj ./WebApi/

RUN dotnet restore WebApi/WebApi.csproj

COPY Application/. ./Application/
COPY Infrastructure/. ./Infrastructure/
COPY Domain/. ./Domain/
COPY WebApi/. ./WebApi/

WORKDIR /src/WebApi
RUN dotnet build WebApi.csproj -c Release -o /app/build

# Etapa publish
FROM build AS publish
WORKDIR /src/WebApi
RUN dotnet publish WebApi.csproj -c Release -o /app/publish

# Imagen final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApi.dll"]
