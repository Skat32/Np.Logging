using Np.Logging.Logger.Http.Middlewares;
using OpenTracing.Util;
using Serilog.Core;
using Serilog.Events;

namespace Np.Logging.Logger;

public class OpenTracingLogsEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddOrUpdateProperty(new LogEventProperty("Date", new ScalarValue(DateTime.Now)));
        
        const string traceIdLogName = "TraceId";

        var traceId = GlobalTracer.Instance?.ActiveSpan?.Context?.TraceId;
        
        if (string.IsNullOrEmpty(traceId)) traceId = RequestResponseLoggingMiddleware.TraceId;

        var traceIdProperty = propertyFactory.CreateProperty(traceIdLogName, traceId);
        logEvent.AddPropertyIfAbsent(traceIdProperty);
    }
}