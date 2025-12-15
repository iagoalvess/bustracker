# BusTracker Production Deployment Script
# Usage: .\deploy.ps1 [build|start|stop|restart|logs|status]

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("build", "start", "stop", "restart", "logs", "status", "migrate")]
    [string]$Command = "status"
)

$ErrorActionPreference = "Stop"

function Show-Help {
    Write-Host ""
    Write-Host "BusTracker Deployment Helper" -ForegroundColor Cyan
    Write-Host "=============================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage: .\deploy.ps1 [command]" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Commands:" -ForegroundColor Green
    Write-Host "  build    - Build all Docker images"
    Write-Host "  start    - Start all services"
    Write-Host "  stop     - Stop all services"
    Write-Host "  restart  - Restart all services"
    Write-Host "  logs     - Show logs from all services"
    Write-Host "  status   - Show status of all services"
    Write-Host "  migrate  - Run database migrations"
    Write-Host ""
}

function Check-EnvFile {
    if (-not (Test-Path ".env")) {
        Write-Host "Warning: .env file not found!" -ForegroundColor Yellow
        Write-Host "Creating .env from .env.example..." -ForegroundColor Cyan
        
        if (Test-Path ".env.example") {
            Copy-Item ".env.example" ".env"
            Write-Host "Please edit .env file with your production settings!" -ForegroundColor Yellow
            Write-Host ""
            return $false
        } else {
            Write-Host "Error: .env.example not found!" -ForegroundColor Red
            return $false
        }
    }
    return $true
}

switch ($Command) {
    "build" {
        Write-Host "Building Docker images..." -ForegroundColor Cyan
        docker-compose -f docker-compose.prod.yml build
        Write-Host "Build completed!" -ForegroundColor Green
    }
    
    "start" {
        if (-not (Check-EnvFile)) {
            exit 1
        }
        
        Write-Host "Starting BusTracker services..." -ForegroundColor Cyan
        docker-compose -f docker-compose.prod.yml up -d
        
        Write-Host ""
        Write-Host "Services starting..." -ForegroundColor Green
        Write-Host "  - PostgreSQL: localhost:5432" -ForegroundColor Yellow
        Write-Host "  - API: http://localhost:8080" -ForegroundColor Yellow
        Write-Host "  - Worker: Running in background" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Run '.\deploy.ps1 logs' to see logs" -ForegroundColor Cyan
        Write-Host "Run '.\deploy.ps1 migrate' to apply database migrations" -ForegroundColor Cyan
    }
    
    "stop" {
        Write-Host "Stopping BusTracker services..." -ForegroundColor Cyan
        docker-compose -f docker-compose.prod.yml down
        Write-Host "Services stopped!" -ForegroundColor Green
    }
    
    "restart" {
        Write-Host "Restarting BusTracker services..." -ForegroundColor Cyan
        docker-compose -f docker-compose.prod.yml restart
        Write-Host "Services restarted!" -ForegroundColor Green
    }
    
    "logs" {
        Write-Host "Showing logs (Press Ctrl+C to exit)..." -ForegroundColor Cyan
        docker-compose -f docker-compose.prod.yml logs -f
    }
    
    "status" {
        Write-Host "BusTracker Services Status:" -ForegroundColor Cyan
        Write-Host "===========================" -ForegroundColor Cyan
        docker-compose -f docker-compose.prod.yml ps
    }
    
    "migrate" {
        Write-Host "Running database migrations..." -ForegroundColor Cyan
        
        $containerRunning = docker ps --filter "name=bustracker-api" --format "{{.Names}}"
        if ($containerRunning) {
            docker exec bustracker-api dotnet ef database update --project /src/BusTracker.Infrastructure
            Write-Host "Migrations applied successfully!" -ForegroundColor Green
        } else {
            Write-Host "Error: API container is not running. Start services first with '.\deploy.ps1 start'" -ForegroundColor Red
        }
    }
    
    default {
        Show-Help
    }
}
