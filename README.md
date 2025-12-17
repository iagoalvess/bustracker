# BusTracker ??

Sistema de rastreamento de ônibus em tempo real para Belo Horizonte, utilizando dados da API pública da PBH (Prefeitura de Belo Horizonte).

## ?? Descrição

O BusTracker é composto por três componentes principais:

- **BusTracker.API** - API REST para consulta de paradas, linhas e previsões de chegada
- **BusTracker.Worker** - Serviço em background que coleta posições dos ônibus em tempo real
- **BusTracker.DataImporter** - Utilitário para importar dados GTFS (paradas, linhas e rotas)

## ??? Tecnologias

- .NET 10
- PostgreSQL com PostGIS (suporte geoespacial)
- Entity Framework Core
- NetTopologySuite

## ?? Estrutura do Projeto

```
BusTracker/
??? BusTracker.API/            # API REST
??? BusTracker.Worker/         # Serviço de coleta de dados
??? BusTracker.DataImporter/   # Importador de dados GTFS
??? BusTracker.Core/           # Entidades, interfaces e DTOs
??? BusTracker.Infrastructure/ # Repositórios e serviços
```

## ?? Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/download/) com extensão PostGIS
- Dados GTFS da BHTrans (para importação inicial)

## ?? Como Executar

### 1. Configurar o Banco de Dados

Crie um banco PostgreSQL com PostGIS:

```sql
CREATE DATABASE bustrackerdb;
\c bustrackerdb
CREATE EXTENSION postgis;
```

### 2. Configurar Connection String

Edite os arquivos `appsettings.json` nos projetos `BusTracker.API` e `BusTracker.Worker` se necessário:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=bustrackerdb;Username=postgres;Password=123"
  }
}
```

### 3. Aplicar Migrations

```bash
cd BusTracker.API
dotnet ef database update --project ../BusTracker.Infrastructure
```

### 4. Importar Dados GTFS (Opcional)

Coloque os arquivos GTFS (`stops.txt`, `routes.txt`, `trips.txt`, `stop_times.txt`) na pasta `BusTracker.DataImporter/Data/` e execute:

```bash
cd BusTracker.DataImporter
dotnet run
```

### 5. Executar os Serviços

**Terminal 1 - API:**
```bash
cd BusTracker.API
dotnet run
```

**Terminal 2 - Worker:**
```bash
cd BusTracker.Worker
dotnet run
```

## ?? Documentação da API

Após iniciar a API, acesse a documentação Swagger em:

- http://localhost:5286/swagger

## ?? Endpoints Principais

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/api/bus/stops/nearby` | Busca paradas próximas a uma coordenada |
| GET | `/api/bus/stops/{stopId}/lines` | Lista linhas que passam em uma parada |
| GET | `/api/bus/lines/{lineNumber}/stops` | Lista paradas de uma linha |
| GET | `/api/bus/lines` | Lista todas as linhas |
| GET | `/api/bus/predictions` | Previsão de chegada de ônibus |
| GET | `/health` | Health check da aplicação |

## ? Configurações

As principais configurações estão em `appsettings.json`:

```json
{
  "BusTracker": {
    "PbhDataUrl": "https://temporeal.pbh.gov.br/?param=C",
    "UpdateIntervalSeconds": 30,
    "PositionRetentionMinutes": 10,
    "PredictionTimeWindowMinutes": 5
  }
}
```

## ?? Licença

Este projeto é apenas para fins educacionais e de estudo.
