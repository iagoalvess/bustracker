FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["BusTracker.Worker/BusTracker.Worker.csproj", "BusTracker.Worker/"]
COPY ["BusTracker.Infrastructure/BusTracker.Infrastructure.csproj", "BusTracker.Infrastructure/"]
COPY ["BusTracker.Core/BusTracker.Core.csproj", "BusTracker.Core/"]

RUN dotnet restore "BusTracker.Worker/BusTracker.Worker.csproj"

COPY . .

WORKDIR "/src/BusTracker.Worker"
RUN dotnet build "BusTracker.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BusTracker.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create data directory for legacy line map and other files
RUN mkdir -p /app/data

ENTRYPOINT ["dotnet", "BusTracker.Worker.dll"]
