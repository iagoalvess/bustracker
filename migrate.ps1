# Migration Helper Script
# Usage: .\migrate.ps1 [command] [name]
# Commands: add, apply, remove, list, script

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("add", "apply", "remove", "list", "script")]
    [string]$Command = "list",
    
    [Parameter(Mandatory=$false)]
    [string]$Name = ""
)

$InfraPath = "BusTracker.Infrastructure"

function Show-Help {
    Write-Host ""
    Write-Host "BusTracker Migration Helper" -ForegroundColor Cyan
    Write-Host "============================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage: .\migrate.ps1 [command] [name]" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Commands:" -ForegroundColor Green
    Write-Host "  add [name]     - Create a new migration"
    Write-Host "  apply          - Apply all pending migrations to database"
    Write-Host "  remove         - Remove the last migration (if not applied)"
    Write-Host "  list           - List all migrations"
    Write-Host "  script [name]  - Generate SQL script for migrations"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Green
    Write-Host "  .\migrate.ps1 add AddUserTable"
    Write-Host "  .\migrate.ps1 apply"
    Write-Host "  .\migrate.ps1 list"
    Write-Host "  .\migrate.ps1 script migration.sql"
    Write-Host ""
}

switch ($Command) {
    "add" {
        if ([string]::IsNullOrWhiteSpace($Name)) {
            Write-Host "Error: Migration name is required" -ForegroundColor Red
            Write-Host "Usage: .\migrate.ps1 add YourMigrationName" -ForegroundColor Yellow
            exit 1
        }
        
        Write-Host "Creating migration: $Name" -ForegroundColor Cyan
        Set-Location $InfraPath
        dotnet ef migrations add $Name
        Set-Location ..
    }
    
    "apply" {
        Write-Host "Applying migrations to database..." -ForegroundColor Cyan
        Set-Location $InfraPath
        dotnet ef database update
        Set-Location ..
    }
    
    "remove" {
        Write-Host "Removing last migration..." -ForegroundColor Cyan
        $confirmation = Read-Host "Are you sure? (y/n)"
        if ($confirmation -eq 'y') {
            Set-Location $InfraPath
            dotnet ef migrations remove --force
            Set-Location ..
        }
        else {
            Write-Host "Cancelled" -ForegroundColor Yellow
        }
    }
    
    "list" {
        Write-Host "Listing all migrations:" -ForegroundColor Cyan
        Set-Location $InfraPath
        dotnet ef migrations list
        Set-Location ..
    }
    
    "script" {
        $outputFile = if ([string]::IsNullOrWhiteSpace($Name)) { "migration.sql" } else { $Name }
        Write-Host "Generating SQL script: $outputFile" -ForegroundColor Cyan
        Set-Location $InfraPath
        dotnet ef migrations script --output $outputFile
        Set-Location ..
        Write-Host "Script generated successfully: $InfraPath\$outputFile" -ForegroundColor Green
    }
    
    default {
        Show-Help
    }
}
