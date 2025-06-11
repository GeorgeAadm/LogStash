using Amazon.Lambda.AspNetCoreServer;

namespace EventLogger.Api
{
    /// <summary>
    /// This class extends from APIGatewayProxyFunction which contains the method FunctionHandlerAsync which is the 
    /// actual Lambda function entry point. The Lambda handler field should be set to
    /// 
    /// EventLogger.Api::EventLogger.Api.LambdaEntryPoint::FunctionHandlerAsync
    /// </summary>
    public class LambdaEntryPoint : APIGatewayProxyFunction
    {
        protected override void Init(IWebHostBuilder builder)
        {
            builder
                .UseStartup<Program>();
        }

        protected override void Init(IHostBuilder builder)
        {
        }
    }
}