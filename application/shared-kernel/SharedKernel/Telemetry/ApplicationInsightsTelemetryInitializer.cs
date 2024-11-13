using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.SharedKernel.Telemetry;

public class ApplicationInsightsTelemetryInitializer : ITelemetryInitializer
{
    private static readonly AsyncLocal<IExecutionContext> ExecutionContext = new();

    public void Initialize(ITelemetry telemetry)
    {
        var executionContext = ExecutionContext.Value;

        if (executionContext is null)
        {
            return;
        }

        telemetry.Context.Location.Ip = executionContext.ClientIpAddress.ToString();

        if (executionContext.TenantId is not null)
        {
            telemetry.Context.User.AccountId = executionContext.TenantId.Value;
        }

        if (executionContext.UserInfo.UserId is not null)
        {
            telemetry.Context.User.Id = executionContext.UserInfo.UserId;
        }

        if (executionContext.UserInfo.IsAuthenticated)
        {
            telemetry.Context.User.AuthenticatedUserId = executionContext.UserInfo.UserId;
        }

        // Also track TenantId and UserId as custom properties, to be consistent with OpenTelemetry where build-in properties cannot be tracked
        // Set custom properties, ensure any changes here are also added to OpenTelemetryEnricher
        AddCustomProperty(telemetry, "tenant_id", executionContext.TenantId?.Value);
        AddCustomProperty(telemetry, "user_id", executionContext.UserInfo.UserId);
        AddCustomProperty(telemetry, "user_IsAuthenticated", executionContext.UserInfo.IsAuthenticated);
        AddCustomProperty(telemetry, "user_Locale", executionContext.UserInfo.Locale);
        AddCustomProperty(telemetry, "user_Role", executionContext.UserInfo.UserRole);
    }

    public static void SetContext(IExecutionContext executionContext)
    {
        ExecutionContext.Value = executionContext;
    }

    private static void AddCustomProperty(ITelemetry telemetry, string name, object? value)
    {
        var stringValue = value?.ToString();
        if (stringValue is null) return;
        telemetry.Context.GlobalProperties[name] = stringValue;
    }
}
