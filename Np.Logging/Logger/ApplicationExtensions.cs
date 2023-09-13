using Jaeger;
using Jaeger.Reporters;
using Jaeger.Samplers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTracing;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Util;

namespace Np.Logging.Logger;

/// <summary>
/// ApplicationExtensions
/// </summary>
public static class ApplicationExtensions
{
    /// <summary>
    /// Конфигурация трейсинга
    /// </summary>
    public static void ConfigureTracer(this IServiceCollection services, string serviceName)
    {
        services.AddOpenTracing();
        // Adds the Jaeger Tracer.
        services.AddSingleton<ITracer>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var sampler = new ConstSampler(true);
            var tracer = new Tracer.Builder(serviceName)
                .WithReporter(
                    new RemoteReporter.Builder()
                        .WithLoggerFactory(loggerFactory)
                        .Build())
                .WithLoggerFactory(loggerFactory)
                .WithSampler(sampler)
                .Build();

            if (!GlobalTracer.IsRegistered())
                GlobalTracer.Register(tracer);

            return tracer;
        });
            
        services.AddOpenTracing(builder =>
        {
            builder.ConfigureAspNetCore(options =>
            {
                // This example shows how to ignore certain requests to prevent spamming the tracer with irrelevant data
                options.Hosting.IgnorePatterns.Add(request => request.Request.Path.Value?.StartsWith("/healthz") == true);
            });
        });

        services.Configure<HttpHandlerDiagnosticOptions>(_ => { });
            
        services.Configure<HttpHandlerDiagnosticOptions>(options =>
            options.OperationNameResolver =
                request => $"{request.Method.Method}: {request.RequestUri?.AbsoluteUri}");
    }
}
