FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["BHD.Documents.API/BHD.Documents.API.csproj", "BHD.Documents.API/"]
COPY ["BHD.Documents.APPLICATION/BHD.Documents.APPLICATION.csproj", "BHD.Documents.APPLICATION/"]
COPY ["BHD.Documents.DOMAIN/BHD.Documents.DOMAIN.csproj", "BHD.Documents.DOMAIN/"]
COPY ["BHD.Documents.INFRASTRUCTURE/BHD.Documents.INFRASTRUCTURE.csproj", "BHD.Documents.INFRASTRUCTURE/"]

RUN dotnet restore "BHD.Documents.API/BHD.Documents.API.csproj"
RUN dotnet restore "BHD.Documents.APPLICATION/BHD.Documents.APPLICATION.csproj"
RUN dotnet restore "BHD.Documents.INFRASTRUCTURE/BHD.Documents.INFRASTRUCTURE.csproj"

COPY . .
WORKDIR "/src/BHD.Documents.API"
RUN dotnet build "./BHD.Documents.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BHD.Documents.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BHD.Documents.API.dll"]