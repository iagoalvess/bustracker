# Arquivos de Dados

Esta pasta deve conter os arquivos CSV necessários para importação inicial:

## Arquivos Necessários

### 1. stops.txt (Formato GTFS)
Pontos de ônibus de Belo Horizonte

**Formato:** CSV com vírgula
**Encoding:** UTF-8
**Colunas:**
- `stop_id`: Código do ponto
- `stop_name`: Nome do ponto
- `stop_lat`: Latitude
- `stop_lon`: Longitude

**Fonte:** https://dados.pbh.gov.br/

### 2. bhtrans_bdlinha.csv
Cadastro de linhas de ônibus

**Formato:** CSV com ponto-e-vírgula (;)
**Encoding:** UTF-8
**Colunas:**
- `NumeroLinha`: Código interno da linha
- `Linha`: Número exibido no ônibus
- `Nome`: Nome/descrição da linha

**Fonte:** https://dados.pbh.gov.br/

## Como Obter os Arquivos

1. Acesse o Portal de Dados Abertos de BH: https://dados.pbh.gov.br/
2. Procure por "GTFS" ou "Linhas de Ônibus"
3. Faça o download dos arquivos
4. Coloque nesta pasta antes de executar o DataImporter

## Estrutura Esperada

```
BusTracker.DataImporter/
??? Data/
    ??? stops.txt              (obrigatório)
    ??? bhtrans_bdlinha.csv    (obrigatório)
    ??? README.md              (este arquivo)
```

## Execução

Após colocar os arquivos aqui, execute:

```bash
cd BusTracker.DataImporter
dotnet run
```

## Configuração Alternativa

Você pode especificar caminhos diferentes via `appsettings.json`:

```json
{
  "DataImport": {
    "StopsFile": "C:\\MeusDados\\stops.txt",
    "LinesFile": "C:\\MeusDados\\linhas.csv"
  }
}
```

Ou via variáveis de ambiente:

```bash
export DataImport__StopsFile="/path/to/stops.txt"
export DataImport__LinesFile="/path/to/lines.csv"
dotnet run
```
