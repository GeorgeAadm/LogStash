# simple-setup.ps1 - Simplified EventLogger Development Environment Setup

param([switch]$Clean)

# Simple colored output functions
function Write-Step($msg) { Write-Host "[*] $msg" -ForegroundColor Cyan }
function Write-Success($msg) { Write-Host "[+] $msg" -ForegroundColor Green }
function Write-Info($msg) { Write-Host "[i] $msg" -ForegroundColor Blue }
function Write-Warn($msg) { Write-Host "[!] $msg" -ForegroundColor Yellow }

Write-Host "`nEventLogger Development Environment Setup" -ForegroundColor Yellow
Write-Host "========================================`n" -ForegroundColor Yellow

try {
    # Step 1: Check Docker
    Write-Step "Checking Docker..."
    $null = docker --version
    if ($LASTEXITCODE -ne 0) { throw "Docker not available" }
    Write-Success "Docker is ready"

    # Step 2: Clean if requested
    if ($Clean) {
        Write-Step "Cleaning environment..."
        docker-compose down -v | Out-Host
        Write-Success "Cleanup complete"
    }

    # Step 3: Start containers
    Write-Step "Starting containers..."
    docker-compose up -d | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Failed to start containers" }
    Write-Success "Containers started"

    # Step 4: Wait for services
    Write-Step "Waiting for services (60 seconds)..."
    Start-Sleep -Seconds 60
    Write-Success "Wait complete"

    # Step 5: Setup DynamoDB
    Write-Step "Creating DynamoDB table..."
    try {
        aws dynamodb create-table `
            --endpoint-url http://localhost:8000 `
            --table-name EventDetails-Local `
            --attribute-definitions AttributeName=EventId,AttributeType=S `
            --key-schema AttributeName=EventId,KeyType=HASH `
            --billing-mode PAY_PER_REQUEST `
            --region us-east-1 | Out-Host
        Write-Success "DynamoDB table created"
    }
    catch {
        Write-Warn "DynamoDB table creation may have failed (check manually)"
    }

    # Step 6: Setup SQL Server
    Write-Step "Setting up SQL Server database..."
    
    # Create database
    Write-Info "Creating database..."
    docker exec sqlserver-local /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -C -Q "
    IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'EventsDb')
        CREATE DATABASE EventsDb;
    PRINT 'Database ready';" | Out-Host

    # Create tables
    Write-Info "Creating tables..."
    docker exec sqlserver-local /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -C -d EventsDb -Q "
    -- EventMetadata table
    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='EventMetadata' AND xtype='U')
    BEGIN
        CREATE TABLE [EventMetadata] (
            [EventId] uniqueidentifier NOT NULL,
            [UserId] nvarchar(100) NOT NULL,
            [EventType] nvarchar(100) NOT NULL,
            [Timestamp] datetime2 NOT NULL,
            [Source] nvarchar(100) NULL,
            CONSTRAINT [PK_EventMetadata] PRIMARY KEY ([EventId])
        );
        CREATE INDEX [IX_EventMetadata_UserId] ON [EventMetadata] ([UserId]);
        PRINT 'EventMetadata table created';
    END" | Out-Host

    Write-Success "SQL Server setup complete"

    # Success message
    Write-Host "`nSetup Complete!" -ForegroundColor Green
    Write-Host "===============" -ForegroundColor Green
    Write-Host ""
    Write-Host "Services:" -ForegroundColor Cyan
    Write-Host "  - DynamoDB Local:    http://localhost:8000" -ForegroundColor White
    Write-Host "  - DynamoDB Admin:    http://localhost:8001" -ForegroundColor White  
    Write-Host "  - SQL Server:        localhost,1433 (sa/YourStrong@Passw0rd)" -ForegroundColor White
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  - Start API: cd src\EventLogger.Api && dotnet run" -ForegroundColor White
    Write-Host "  - Swagger:   http://localhost:5153/swagger" -ForegroundColor White
    Write-Host ""
    Write-Host "Management:" -ForegroundColor Cyan
    Write-Host "  - Stop:      docker-compose down" -ForegroundColor White
    Write-Host "  - Clean:     docker-compose down -v" -ForegroundColor White
    Write-Host ""

}
catch {
    Write-Host "`nSetup failed: $_" -ForegroundColor Red
    Write-Host "`nTroubleshooting:" -ForegroundColor Yellow
    Write-Host "  - Check Docker Desktop is running" -ForegroundColor White
    Write-Host "  - Check container status: docker-compose ps" -ForegroundColor White
    Write-Host "  - Check logs: docker-compose logs" -ForegroundColor White
    Write-Host "  - Try clean start: .\simple-setup.ps1 -Clean" -ForegroundColor White
    exit 1
}