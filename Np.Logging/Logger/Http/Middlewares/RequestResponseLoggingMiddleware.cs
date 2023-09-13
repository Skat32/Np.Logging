using System.Diagnostics;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Np.Logging.Logger.Helpers;
using Np.Logging.Logger.Http.Models;
using OpenTracing;
using Serilog;

namespace Np.Logging.Logger.Http.Middlewares
{
    /// <summary>
    /// Middleware для логирования запросов к серверу
    /// </summary>
    public class RequestResponseLoggingMiddleware
    {
        private static readonly string[] MaskedQueryParams =
        {
            "access_token"
        };

        private readonly RequestDelegate _next;
        private readonly ITracer _tracer;
        private readonly ILogger _logger;
        private readonly LogSettings _settings;

        // private IScope _scope;
        public static string TraceId;

        /// <param name="next">RequestDelegate</param>
        /// <param name="logger">Логгер</param>
        /// <param name="settings"></param>
        /// <param name="tracer"></param>
        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger logger, IOptions<LogSettings> settings, ITracer tracer)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _tracer = tracer;
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger?.ForContext<RequestResponseLoggingMiddleware>()
                      ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Invoke method
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            var url = GetRequestUri(context.Request);

            await LogRequest(context, url);

            var response = context.Response;

            // если хотим логировать тело запроса, то нужно будет за собой отматать стрим, поэтому две ветки
            if (!_settings.EnableHttpBodyLog)
            {
                await _next(context);
                sw.Stop();
                await LogResponse(context, url, sw.Elapsed.TotalMilliseconds);

                return;
            }

            var original = response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;
            await _next(context);
            sw.Stop();
            await LogResponse(context, url, sw.Elapsed.TotalMilliseconds);

            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(original);
        }

        private async Task LogRequest(HttpContext context, string url)
        {
            var request = context.Request;
            var logger = _logger;
            
            TraceId = Guid.NewGuid().ToString();

            if (_settings.EnableHttpBodyLog)
            {
                var requestBody = await GetRequestBodyAsSerializableObject(request);
                logger = logger.ForContextJson("HttpBody", requestBody);
            }

            if (_settings.EnableHttpHeadersLog)
            {
                var requestHeaders = request.Headers.ToDictionary(h => h.Key, h => _settings.IsHeaderAllowed(h.Key)
                    ? string.Join(" ,", h.Value)
                    : "******");
                
                logger = logger.ForContextJson("HttpHeaders", requestHeaders);
            }

            logger.Information($"Http InRequest {request.Method} {url}");
        }

        private async Task LogResponse(HttpContext context, string url, double duration)
        {
            var response = context.Response;
            var logger = _logger;

            if (_settings.EnableHttpBodyLog)
            {
                var body = await GetResponseBodyAsSerializableObject(response);
                logger = logger.ForContextJson("HttpBody", body);
            }

            if (_settings.EnableHttpHeadersLog)
            {
                var responseHeaders = response.Headers.ToDictionary(h => h.Key, h => _settings.IsHeaderAllowed(h.Key)
                    ? string.Join(" ,", h.Value)
                    : "******");
                logger = logger.ForContextJson("HttpHeaders", responseHeaders);
            }

            logger.Information(
                $"Http InResponse {context.Request.Method} {url} responded {response.StatusCode.ToString()} in {duration:0.0000} ms");

            TraceId = string.Empty;
        }

        private static string GetRequestUri(HttpRequest request)
        {
            if (!request.QueryString.HasValue)
                return $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";

            var values = HttpUtility.ParseQueryString(request.QueryString.Value!);

            var maskedKeys = values.AllKeys
                .Where(x => MaskedQueryParams.Contains(x, StringComparer.InvariantCultureIgnoreCase));

            foreach (var maskedKey in maskedKeys)
            {
                values.Set(maskedKey, "****");
            }

            var queryString = new StringBuilder().Append("?").Append(values).ToString();

            return $"{request.Scheme}://{request.Host}{request.Path}{queryString}";
        }

        private static async Task<object> GetRequestBodyAsSerializableObject(HttpRequest request)
        {
            if (!(request.ContentLength > 0))
                return string.Empty;

            if (HttpBodyHelper.IsOctetStream(request.ContentType!))
                return new JsonErrorModel("Content is skipped because of binary content");

            // if multipart form data, return dict without files (files stored in request.Form.Files)
            // also checking if form data has valid boundary, because request.Form triggers form binding
            // and throws InvalidDataException when boundary is missing
            if (request.HasFormContentType && HttpBodyHelper.HasValidMultipartBoundary(request.ContentType!))
            {
                var bodyWithoutFiles = request.Form.ToDictionary(
                    x => x.Key,
                    x => string.Join(", ", request.Form[x.Key]));
                bodyWithoutFiles.Add("__filesCount", request.Form.Files.Count.ToString());

                return bodyWithoutFiles;
            }

            request.EnableBuffering();

            var buffer = new byte[Convert.ToInt32(request.ContentLength)];

            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            request.Body.Seek(0, SeekOrigin.Begin);

            return Encoding.UTF8.GetString(buffer);
        }

        private static async Task<object> GetResponseBodyAsSerializableObject(HttpResponse response)
        {
            if (HttpBodyHelper.IsOctetStream(response.ContentType))
                return new JsonErrorModel("Content is skipped because of binary content");

            response.Body.Seek(0, SeekOrigin.Begin);
            var bodyAsText = await new StreamReader(response.Body).ReadToEndAsync();

            return bodyAsText;
        }
    }
}
