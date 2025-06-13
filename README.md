# EventLogger API

A serverless event logging API built with .NET 8, AWS Lambda, SQL Server, and DynamoDB. This solution demonstrates a hybrid data storage approach where structured metadata is stored in SQL Server and flexible event details are stored as JSON in DynamoDB.

## Architecture Overview

- **API Layer**: ASP.NET Core Web API running on AWS Lambda
- **Metadata Storage**: SQL Server for structured event metadata
- **Details Storage**: DynamoDB for flexible JSON event details
- **Deployment**: AWS SAM for serverless deployment
- **Local Development**: Docker Compose for local SQL Server and DynamoDB

## Prerequisites

- .NET 8 SDK
- Docker Desktop
- AWS CLI (configured with credentials)
- AWS SAM CLI
- PowerShell (for setup scripts)

## Project Structure

```
EventLogger/
├── src/
│   ├── EventLogger.Api/          # Main API project
│   └── EventLogger.Tests/        # Unit and integration tests
├── docker-compose.yml            # Local database setup
├── template.yaml                 # SAM deployment template
├── simple-setup.ps1             # Quick setup script
└── test-api-adaptive.ps1        # API testing script
```

## Quick Start

### 1. Clone and Setup

```bash
git clone <repository-url>
cd EventLogger
```

### 2. Start Local Environment

```powershell
# Run the setup script to start databases
.\simple-setup.ps1

# Or manually with docker-compose
docker-compose up -d
```

This will start:
- SQL Server on `localhost:1433`
- DynamoDB Local on `http://localhost:8000`
- DynamoDB Admin UI on `http://localhost:8001`

### 3. Run the API Locally

```bash
cd src/EventLogger.Api
dotnet run
```

The API will be available at:
- Swagger UI: `http://localhost:5153/swagger`
- Health Check: `http://localhost:5153/api/healthcheck`

### 4. Run with SAM CLI (Lambda Simulation)

```bash
cd src/EventLogger.Api

# Build the Lambda package
dotnet clean
dotnet publish -c Release -o bin/Release/net8.0/publish

# Start SAM local API
sam local start-api -t serverless.template --docker-network bridge
```

The API will be available at `http://localhost:3000`

## API Endpoints

### POST /api/events
Record a new user event.

**Request:**
```json
{
  "userId": "alice@company.com",
  "eventType": "LOGIN",
  "source": "web",
  "eventDetails": {
    "browser": "Chrome",
    "ip": "192.168.1.100",
    "sessionId": "abc123"
  }
}
```

**Response (201 Created):**
```json
{
  "eventId": "123e4567-e89b-12d3-a456-426614174000",
  "userId": "alice@company.com",
  "eventType": "LOGIN",
  "timestamp": "2024-01-15T10:30:00Z",
  "source": "web"
}
```

### GET /api/events/{userId}
Retrieve events for a specific user.

**Query Parameters:**
- `eventType` (optional): Filter by event type
- `fromDate` (optional): Start date filter
- `toDate` (optional): End date filter
- `limit` (optional): Maximum results (default: 50, max: 1000)

**Response (200 OK):**
```json
[
  {
    "eventId": "123e4567-e89b-12d3-a456-426614174000",
    "userId": "alice@company.com",
    "eventType": "LOGIN",
    "timestamp": "2024-01-15T10:30:00Z",
    "source": "web",
    "eventDetails": {
      "browser": "Chrome",
      "ip": "192.168.1.100",
      "sessionId": "abc123"
    }
  }
]
```

## Testing

### Run Unit Tests

```bash
cd src/EventLogger.Tests
dotnet test
```

### Test the API

```powershell
# Test against local dotnet run instance
.\test-api-adaptive.ps1

# Test against SAM local instance
.\test-api-adaptive.ps1 -Sam

# Verbose mode for debugging
.\test-api-adaptive.ps1 -Verbose

# Combine flags
.\test-api-adaptive.ps1 -Sam -Verbose -DelaySeconds 2
```

## Configuration

### appsettings.json
- SQL Server connection string
- DynamoDB configuration
- Logging settings

### Environment Variables (for Lambda)
- `ConnectionStrings__SqlServer`
- `DynamoDb__TableName`
- `DynamoDb__ServiceUrl` (for local development)

## Data Model

### SQL Server - EventMetadata Table
```sql
CREATE TABLE EventMetadata (
    EventId uniqueidentifier PRIMARY KEY,
    UserId nvarchar(100) NOT NULL,
    EventType nvarchar(100) NOT NULL,
    Timestamp datetime2 NOT NULL,
    Source nvarchar(100) NULL
);
```

### DynamoDB - EventDetails Table
```json
{
  "EventId": "123e4567-e89b-12d3-a456-426614174000",
  "UserId": "alice@company.com",
  "EventType": "LOGIN",
  "CreatedAt": "2024-01-15T10:30:00Z",
  "Category": "Authentication",
  "Details": {
    // Flexible JSON structure
  }
}
```

## Deployment to AWS

### Prerequisites
- AWS account with appropriate permissions
- RDS SQL Server instance
- DynamoDB table created

### Deploy with SAM

```bash
# Build the application
sam build -t template.yaml

# Deploy (first time)
sam deploy --guided

# Subsequent deployments
sam deploy
```

## Monitoring and Debugging

### Local Debugging
- Check SQL Server: Connect with SSMS or Azure Data Studio
- Check DynamoDB: Visit `http://localhost:8001`
- View logs: Check console output or CloudWatch (in AWS)

### Health Checks
- `/api/healthcheck` - Overall API health
- Includes SQL Server connectivity check
- Includes DynamoDB table status check

## Best Practices Implemented

1. **Separation of Concerns**: Clean architecture with separate layers
2. **Repository Pattern**: Abstracted data access
3. **Dependency Injection**: Loose coupling and testability
4. **Validation**: FluentValidation for input validation
5. **Logging**: Structured logging with Serilog
6. **Error Handling**: Consistent error responses
7. **Health Checks**: Monitoring readiness
8. **API Documentation**: Swagger/OpenAPI
9. **Async/Await**: Non-blocking I/O operations
10. **Configuration**: Environment-specific settings

## Troubleshooting

### Common Issues

1. **Docker containers not starting**
   - Ensure Docker Desktop is running
   - Check port conflicts (1433, 8000, 8001)
   - Run `docker-compose logs` for details

2. **DynamoDB table not found**
   - Run setup script: `.\simple-setup.ps1`
   - Create manually via AWS CLI

3. **SQL Server connection failed**
   - Verify connection string
   - Check firewall settings
   - Ensure SQL Server container is healthy

4. **SAM local issues**
   - Update SAM CLI: `pip install --upgrade aws-sam-cli`
   - Check Docker is running
   - Use `host.docker.internal` for local services

5. **PowerShell**
   - Allow running script (session) `Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process`

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License.