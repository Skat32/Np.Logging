using System.Diagnostics;
using System.Web;
using Np.Logging.Logger.Helpers;
using Np.Logging.Logger.Http.Models;
using Serilog;

namespace Np.Logging.Logger.Http
{
    /// <summary>
    /// Логирующий хендлер
    /// </summary>
    public class LoggingHandler : DelegatingHandler
    {
        private readonly ILogger _logger;

        /// <param name="innerHandler"></param>
        /// <param name="logger"></param>
        public LoggingHandler(HttpMessageHandler innerHandler, ILogger logger) : base(innerHandler)
            => _logger = logger.ForContext<LoggingHandler>() ?? throw new ArgumentNullException(nameof(logger));
        
        /// <param name="innerHandler"></param>
        /// <param name="logger"></param>
        public LoggingHandler(SocketsHttpHandler innerHandler, ILogger logger) : base(innerHandler)
            => _logger = logger.ForContext<LoggingHandler>() ?? throw new ArgumentNullException(nameof(logger));
        
        /// <param name="logger"></param>
        public LoggingHandler(ILogger logger)
            => _logger = logger.ForContext<LoggingHandler>() ?? throw new ArgumentNullException(nameof(logger));

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await LogRequest(request);

            var sw = Stopwatch.StartNew();
            HttpResponseMessage response;

            try
            {
                response = await base.SendAsync(request, cancellationToken);
                sw.Stop();
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogError(request, ex, sw.Elapsed.TotalMilliseconds);
                throw;
            }

            await LogResponse(request, response, sw.Elapsed.TotalMilliseconds);

            return response;
        }

        private async Task LogRequest(HttpRequestMessage request)
        {
            var requestBody = await GetContentAsSerializableObject(request.Content!);
            var requestHeaders = request.Headers
                .ToDictionary(h => h.Key, h => string.Join(" ,", h.Value))
                .MaskSecretHeaders();

            _logger
                .ForContextJson("HttpBody", requestBody)
                .ForContextJson("HttpHeaders", requestHeaders)
                .Information("Http OutRequest {HttpMethod} {HttpUri}",
                    request.Method.ToString(),
                    request.RequestUri?.AbsoluteUri);
        }

        private async Task LogResponse(HttpRequestMessage request, HttpResponseMessage response, double duration)
        {
            var responseBody = await GetContentAsSerializableObject(response.Content);
            var responseHeaders = response.Headers
                .ToDictionary(h => h.Key, h => string.Join(" ,", h.Value))
                .MaskSecretHeaders();

            _logger
                .ForContextJson("HttpBody", responseBody)
                .ForContextJson("HttpHeaders", responseHeaders)
                .Information(
                    "Http OutResponse {HttpMethod} {HttpUri} responded {HttpStatusCode} in {ElapsedMilliseconds:0.0000} ms",
                    request.Method.ToString(),
                    request.RequestUri?.AbsoluteUri,
                    ((int)response.StatusCode).ToString(),
                    duration);
        }

        private void LogError(HttpRequestMessage request, Exception ex, double duration) =>
            _logger
                .ForContextJson("Exception", ex)
                .Information(
                    "Http OutError {HttpMethod} {HttpUri} failed in {ElapsedMilliseconds:0.0000} ms",
                    request.Method.ToString(),
                    request.RequestUri?.AbsoluteUri,
                    duration);

        private static async Task<object> GetContentAsSerializableObject(HttpContent? content)
        {
            if (content == null)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(content.Headers.ContentType?.MediaType))
                return new JsonErrorModel("Content is skipped because of empty content type");

            if (HttpBodyHelper.IsOctetStream(content.Headers.ContentType.MediaType))
                return new JsonErrorModel("Content is skipped because of binary content");

            if (HttpBodyHelper.IsMultipartFormEncoded(content.Headers.ContentType.MediaType)
                && content is MultipartFormDataContent formData)
            {
                var dictionaryToLog = new Dictionary<string, string>();
                foreach (var part in formData)
                {
                    var contentDisposition = part?.Headers?.ContentDisposition;
                    if (contentDisposition == null)
                        continue;

                    var value = string.IsNullOrEmpty(contentDisposition.FileName)
                        ? await part!.ReadAsStringAsync()
                        : "here lies a masked file";

                    dictionaryToLog.Add(contentDisposition.Name!, value);
                }

                return dictionaryToLog;
            }

            var stringContent = await content.ReadAsStringAsync();

            if (HttpBodyHelper.IsJsonContentType(content.Headers.ContentType.MediaType))
                return stringContent;

            if (!HttpBodyHelper.IsFormUrlEncoded(content.Headers.ContentType.MediaType))
                return new JsonErrorModel("Unexpected content type", stringContent);
            
            var parsedParams = HttpUtility.ParseQueryString(stringContent);
            return parsedParams.AllKeys.ToDictionary(x => x!, x => parsedParams[x]);
        }
    }
}
