# BusTracker

Sistema de rastreamento de ônibus em tempo real para Belo Horizonte.

## Arquitetura

O projeto segue a arquitetura Clean Architecture com as seguintes camadas:

- **BusTracker.Core**: Entidades, DTOs, Interfaces e Configurações
- **BusTracker.Infrastructure**: Implementações de Repositórios, Serviços e Data Access
- **BusTracker.API**: API REST para consultas
- **BusTracker.Worker**: Background service para coleta de dados
- **BusTracker.DataImporter**: Utilitário para importação inicial de dados

## Padrões e Boas Práticas Implementadas

### 1. Clean Architecture
- Separação clara de responsabilidades
- Independência de frameworks na camada Core
- Inversão de dependências

### 2. Repository Pattern e Unit of Work
- Abstração do acesso a dados
- Transações consistentes
- Facilita testes unitários

### 3. Dependency Injection
- Todos os serviços são injetados
- Configuração centralizada
- Facilita mocking para testes

### 4. Configuration Management
- Settings externalizados em appsettings.json
- Suporte para múltiplos ambientes (Development, Production)
- Configurações sensíveis através de variáveis de ambiente

### 5. Error Handling
- Middleware global de exceções
- Logging estruturado
- Respostas padronizadas (ProblemDetails)

### 6. Health Checks
- Endpoints de saúde para API e Worker
- Verificação de conectividade com banco de dados

### 7. Rate Limiting
- Limite de requisições por IP/usuário
- Diferentes políticas por endpoint:
  - **Search endpoints**: 50 requisições simultâneas
  - **Prediction endpoint**: 30 requisições/minuto (sliding window)
  - **Geral**: 100 requisições/minuto (fixed window)
- Configurável via appsettings.json
- Pode ser desabilitado para desenvolvimento

### 8. CORS e Segurança
- CORS configurado
- HTTPS redirect em produção

## Requisitos

- .NET 10 SDK
- PostgreSQL com PostGIS
- Docker e Docker Compose (opcional)

## Configuração

### Local (sem Docker)

1. Instale PostgreSQL com PostGIS
2. Configure a connection string em `appsettings.Development.json`
3. Execute as migrações:

```bash
cd BusTracker.Infrastructure
dotnet ef database update --startup-project ../BusTracker.API
```

4. Obtenha os arquivos de dados:

**Consulte o guia completo:** [DATA_SOURCE.md](DATA_SOURCE.md)

Resumo:
- Acesse: https://dados.pbh.gov.br/
- Baixe o arquivo GTFS (stops.txt)
- Baixe o cadastro de linhas (bhtrans_bdlinha.csv)
- Coloque os arquivos em `BusTracker.DataImporter/Data/`

5. Importe os dados iniciais:

```bash
cd BusTracker.DataImporter
dotnet run
```

6. Execute a API:

```bash
cd BusTracker.API
dotnet run
```

7. Execute o Worker:

```bash
cd BusTracker.Worker
dotnet run
```

### Com Docker Compose (Desenvolvimento)

```bash
# Apenas banco de dados
docker-compose -f docker-compose.dev.yml up -d

# Execute migrações
cd BusTracker.Infrastructure
dotnet ef database update --startup-project ../BusTracker.API

# Prepare os dados (veja BusTracker.DataImporter/Data/README.md)
# Copie stops.txt e bhtrans_bdlinha.csv para BusTracker.DataImporter/Data/

# Importe os dados
cd ../BusTracker.DataImporter
dotnet run

# Execute API e Worker localmente
cd ../BusTracker.API
dotnet run

cd ../BusTracker.Worker
dotnet run
```

### Produção com Docker Compose

```bash
# Build e start de todos os serviços
docker-compose up -d

# Execute migrações (primeira vez)
docker exec bustracker-api dotnet ef database update

# Para importar dados
cd BusTracker.DataImporter
# Ajuste a connection string para apontar para o container
dotnet run
```

## Endpoints da API

### Health Check
```
GET /health
```

### Buscar Pontos de Ônibus
```
GET /api/bus/stops?query={termo}
```

### Buscar Linhas
```
GET /api/bus/lines?query={termo}
```

### Previsão de Chegada
```
GET /api/bus/prediction?stopCode={codigo}&lineNum={linha}
```

## Configurações

Todas as configurações estão em `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=BusTrackerDb;Username=postgres;Password=postgres"
  },
  "BusTracker": {
    "PbhDataUrl": "https://temporeal.pbh.gov.br/?param=C",
    "UpdateIntervalSeconds": 20,
    "PositionRetentionMinutes": 20,
    "PredictionTimeWindowMinutes": 5
  },
  "RateLimit": {
    "Enabled": true,
    "PermitLimit": 100,
    "WindowSeconds": 60,
    "QueueLimit": 10
  }
}
```

### Variáveis de Ambiente (Produção)

- `ConnectionStrings__DefaultConnection`: String de conexão do banco
- `ASPNETCORE_ENVIRONMENT`: Ambiente (Development, Production)
- `BusTracker__PbhDataUrl`: URL da API da PBH
- `BusTracker__UpdateIntervalSeconds`: Intervalo de atualização em segundos
- `RateLimit__Enabled`: Habilitar/desabilitar rate limiting
- `RateLimit__PermitLimit`: Número de requisições permitidas por janela
- `RateLimit__WindowSeconds`: Tamanho da janela em segundos

## Estrutura do Projeto

```
BusTracker/
??? BusTracker.Core/
?   ??? Entities/          # Entidades de domínio
?   ??? DTOs/              # Data Transfer Objects
?   ??? Interfaces/        # Contratos de serviços e repositórios
?   ??? Configuration/     # Classes de configuração
??? BusTracker.Infrastructure/
?   ??? Data/              # DbContext e Migrations
?   ??? Repositories/      # Implementação de repositórios
?   ??? Services/          # Implementação de serviços
??? BusTracker.API/
?   ??? Controllers/       # Controladores REST
?   ??? Middleware/        # Middleware customizado
?   ??? Program.cs         # Configuração da API
??? BusTracker.Worker/
?   ??? Worker.cs          # Background service
?   ??? Program.cs         # Configuração do Worker
??? BusTracker.DataImporter/
    ??? Program.cs         # Importador de dados

```

## Logging

O sistema usa o logging padrão do .NET com níveis configuráveis por ambiente:

- **Development**: Debug detalhado
- **Production**: Warnings e Errors apenas

## Monitoramento

- Health checks: `/health`
- Logs estruturados
- Métricas de performance (ready para Application Insights)

## Melhorias Futuras

- [ ] Autenticação e autorização
- [ ] Rate limiting
- [ ] Cache distribuído (Redis)
- [ ] Message queue para processamento assíncrono
- [ ] Testes unitários e de integração
- [ ] CI/CD pipeline
- [ ] Métricas e telemetria (Application Insights)
- [ ] API versioning
- [ ] GraphQL endpoint
- [ ] WebSockets para updates em tempo real

## Licença

MIT
