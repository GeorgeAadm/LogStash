using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using EventLogger.Api.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace EventLogger.Api.Infrastructure.Configuration
{
    public class DynamoDbConfiguration
    {
        public string TableName { get; set; } = string.Empty;
        public string? ServiceUrl { get; set; }
    }


    public class DynamoDbHealthCheck : IHealthCheck
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly DynamoDbConfiguration _configuration;
        private readonly ILogger<DynamoDbHealthCheck> _logger;

        public DynamoDbHealthCheck(
            IAmazonDynamoDB dynamoDb,
            IOptions<DynamoDbConfiguration> configuration,
            ILogger<DynamoDbHealthCheck> logger)
        {
            _dynamoDb = dynamoDb;
            _configuration = configuration.Value;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Try to describe the table
                var request = new DescribeTableRequest
                {
                    TableName = _configuration.TableName
                };

                var response = await _dynamoDb.DescribeTableAsync(request, cancellationToken);

                if (response.Table.TableStatus == TableStatus.ACTIVE)
                {
                    return HealthCheckResult.Healthy($"DynamoDB table '{_configuration.TableName}' is active");
                }

                return HealthCheckResult.Degraded($"DynamoDB table '{_configuration.TableName}' status: {response.Table.TableStatus}");
            }
            catch (ResourceNotFoundException)
            {
                _logger.LogWarning("DynamoDB table '{TableName}' not found", _configuration.TableName);
                return HealthCheckResult.Unhealthy($"DynamoDB table '{_configuration.TableName}' not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DynamoDB health check failed");
                return HealthCheckResult.Unhealthy("DynamoDB health check failed", ex);
            }
        }
    }
}