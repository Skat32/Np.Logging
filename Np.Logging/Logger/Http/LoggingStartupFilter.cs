using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Np.Logging.Logger.Http.Middlewares;

namespace Np.Logging.Logger.Http
{
    /// <inheritdoc />
    internal class LoggingStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.UseMiddleware<RequestResponseLoggingMiddleware>();
                
                next(app);
            };
        }
    }
}