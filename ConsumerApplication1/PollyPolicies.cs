using Polly;
using Polly.Extensions.Http;

namespace ConsumerApplication1
{
    public static class PollyPolicies
    {
        public static IAsyncPolicy<HttpResponseMessage> GetWaitAndRetryPolicy(int retryCount, int retrySleepPowerDuration)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(
                    retryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(retrySleepPowerDuration, retryAttempt)));
        }

        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(int handledEventsAllowedBeforeBreaking, int durationOfBreakInSecond)
        {
            var durationOfBreak = TimeSpan.FromSeconds(durationOfBreakInSecond);

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking,
                    durationOfBreak);
        }
    }
}
