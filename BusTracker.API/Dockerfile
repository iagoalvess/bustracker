FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["BusTracker.API/BusTracker.API.csproj", "BusTracker.API/"]
COPY ["BusTracker.Infrastructure/BusTracker.Infrastructure.csproj", "BusTracker.Infrastructure/"]
COPY ["BusTracker.Core/BusTracker.Core.csproj", "BusTracker.Core/"]

RUN dotnet restore "BusTracker.API/BusTracker.API.csproj"

COPY . .

WORKDIR "/src/BusTracker.API"
RUN dotnet build "BusTracker.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BusTracker.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "BusTracker.API.dll"]
