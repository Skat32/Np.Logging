using System.Text.RegularExpressions;
using Destructurama;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Np.Logging.Logger.Enrichers;
using Np.Logging.Logger.Http;
using Np.Logging.Logger.Http.Models;
using Np.Logging.Logger.LogLevelSwitcher;
using Np.Logging.Logger.Models;
using Serilog;
using Serilog.Core;
using Serilog.Enrichers.Sensitive;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.Grafana.Loki;

namespace Np.Logging.Logger
{
   /// <summary>
    /// Useful extensions methods for serilog logger
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Using serilog with some defaults
        /// </summary>
        public static IWebHostBuilder UseLogging(
            this IWebHostBuilder builder,
            string applicationName,
            Action<WebHostBuilderContext, LoggerConfiguration> configureLogger,
            LogEventLevel minLogLevel = LogEventLevel.Information,
            IConfiguration? configuration = null,
            WriteToEnum[]? inputs = default,
            params IMaskingOperator[] maskingOperators)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configureLogger == null) throw new ArgumentNullException(nameof(configureLogger));

            var logSwitch = new LoggingLevelSwitch(minLogLevel);
            var loggingSwitcher = new LoggingLevelSwitcher(logSwitch);
            
            var fullConfigureLogger = BasicConfigureLogger + configureLogger;

            builder.UseSerilog(fullConfigureLogger);

            builder.ConfigureServices((context, services) =>
            {
                services.AddLogSettings(context.Configuration);
                services.AddSingleton(Log.Logger);
                services.AddTransient<LoggingHandler>();
                services.AddSingleton<IStartupFilter, LoggingStartupFilter>();
                services.AddSingleton<ILoggingLevelSwitcher>(loggingSwitcher);
            });

            // Отключение стандартных сообщений при старте и остановке приложения. Они не структурированы и не могут быть корректно спарсены.
            builder.SuppressStatusMessages(true);

            return builder;
            
            void BasicConfigureLogger(WebHostBuilderContext ctx, LoggerConfiguration config)
            {
              
                var isLoglevel = Enum.TryParse<LogEventLevel>(ctx.Configuration["DEFAULT_LOGLEVEL"], true, out var result);

                var level = isLoglevel ? result : LogEventLevel.Information;
                config.MinimumLevel.Is(level)
                    .IgnoreTechnicalLogs()
                    .Enrich.With<OpenTracingLogsEnricher>()
                    .Enrich.WithSensitiveDataMasking(MaskingMode.Globally, maskingOperators)
                    .WriteToDefault()
                    .WriteToSelectedInput(inputs ?? Array.Empty<WriteToEnum>(), configuration)
                    .MinimumLevel.ControlledBy(logSwitch)
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", applicationName)
                    .Enrich.WithMachineName();
            }
        }

        /// <summary>
        /// Common "Write.To" implementation
        /// </summary>
        /// <remarks>
        /// in Development env - write to console in friendly format, write to file
        /// in non Development env - write to console
        /// </remarks>
        private static LoggerConfiguration WriteToDefault(
            this LoggerConfiguration loggerConfiguration)
        {
            loggerConfiguration.WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {NewLine}{Exception}");
            
            return loggerConfiguration;
        }

        private static LoggerConfiguration WriteToSelectedInput(
            this LoggerConfiguration loggerConfiguration,
            WriteToEnum[] writeTo,
            IConfiguration? configuration = null)
        {
            if (writeTo.Contains(WriteToEnum.Elastic))
                loggerConfiguration = loggerConfiguration.WriteToElastic(configuration);

            if (writeTo.Contains(WriteToEnum.Loki))
                loggerConfiguration = loggerConfiguration.WriteToLoki(configuration);
            
            return loggerConfiguration;
        }

        /// <summary>
        /// Ignore log by log event property (works for first level property only)
        /// </summary>
        /// <param name="loggerConfiguration"></param>
        /// <param name="key"></param>
        /// <param name="regexp"></param>
        /// <returns></returns>
        private static LoggerConfiguration IgnoreLog(
            this LoggerConfiguration loggerConfiguration,
            string key, Regex regexp)
        {
            loggerConfiguration.Filter.ByExcluding(e =>
            {
                var hasKey = e.Properties.ContainsKey(key);
                if (!hasKey)
                    return false;

                var path = e.Properties[key]?.ToString();

                if (path == null)
                    return false;

                var matched = regexp.IsMatch(path);

                return matched;
            });

            return loggerConfiguration;
        }

        /// <summary>
        /// Ignores http logs with RequestPath /health /ready /cap /swagger /metrics-text
        /// </summary>
        /// <param name="loggerConfiguration"></param>
        /// <returns></returns>
        private static LoggerConfiguration IgnoreTechnicalLogs(this LoggerConfiguration loggerConfiguration)
        {
            return loggerConfiguration
                .IgnoreLog("HttpUri", new Regex(@".*\/cap(\/|\s|"")"))
                .IgnoreLog("HttpUri", new Regex(@".*\/health(\/|\s|"")"))
                .IgnoreLog("HttpUri", new Regex(@".*\/ready(\/|\s|"")"))
                .IgnoreLog("HttpUri", new Regex(@".*\/swagger(\/|\s|"")"))
                .IgnoreLog("HttpUri", new Regex(@".*\/metrics(\/|\s|"")"))
                .IgnoreLog("HttpUri", new Regex(@".*\/metrics-text(\/|\s|"")"))
                .IgnoreLog("RequestPath", new Regex(@".*\/cap(\/|\s|"")"))
                .IgnoreLog("RequestPath", new Regex(@".*\/health(\/|\s|"")"))
                .IgnoreLog("RequestPath", new Regex(@".*\/ready(\/|\s|"")"))
                .IgnoreLog("RequestPath", new Regex(@".*\/swagger(\/|\s|"")"))
                .IgnoreLog("RequestPath", new Regex(@".*\/metrics(\/|\s|"")"))
                .IgnoreLog("RequestPath", new Regex(@".*\/metrics-text(\/|\s|"")"));
        }

        /// <summary>
        /// Exclude fields by pathes
        /// </summary>
        /// <param name="config">Logger configuration</param>
        /// <param name="fieldPaths">Field pathes</param>
        /// <returns></returns>
        public static LoggerConfiguration ExcludeFields(this LoggerConfiguration config, params string[] fieldPaths)
        {
            config.Enrich.With(new ExcludeFieldEnricher(fieldPaths));
            return config;
        }

        /// <summary>
        /// Exclude fields by pathes
        /// </summary>
        /// <param name="config">Logger configuration</param>
        /// <param name="fieldPaths">Field pathes (Field's separator in path: dot)</param>
        /// <returns></returns>
        public static LoggerConfiguration ExcludeFields(this LoggerConfiguration config, IEnumerable<string> fieldPaths)
        {
            return config.ExcludeFields(fieldPaths.ToArray());
        }

        /// <summary>
        /// Mask fields by pathes
        /// </summary>
        /// <param name="config">Logger configuration</param>
        /// <param name="fieldPaths">Field pathes</param>
        /// <returns></returns>
        public static LoggerConfiguration MaskFields(this LoggerConfiguration config, params string[] fieldPaths)
        {
            config.Enrich.With(new MaskFieldEnricher(fieldPaths));
            return config;
        }

        /// <summary>
        /// Mask fields by pathes
        /// </summary>
        /// <param name="config">Logger configuration</param>
        /// <param name="fieldPaths">Field pathes (Field's separator in path: dot)</param>
        /// <returns></returns>
        public static LoggerConfiguration MaskFields(this LoggerConfiguration config, IEnumerable<string> fieldPaths)
        {
            return config.MaskFields(fieldPaths.ToArray());
        }

        /// <summary>
        /// Add destructure serialized Json fields
        /// </summary>
        /// <param name="config">Logger configuration</param>
        /// <returns></returns>
        public static LoggerConfiguration DestructureJson(this LoggerConfiguration config)
        {
            config.Destructure.JsonNetTypes();
            return config;
        }

        private static void AddLogSettings(this IServiceCollection services, IConfiguration config)
        {
            var settingsSection = config.GetSection(nameof(LogSettings));

            services.AddOptions<LogSettings>()
                .Bind(settingsSection)
                .ValidateDataAnnotations();

            services.PostConfigure<LogSettings>(o => o.Init());
        }

        private static ElasticsearchSinkOptions ConfigureElasticSink(
            string elasticUrl,
            string? environment,
            string applicationName)
        {
            return new ElasticsearchSinkOptions(new Uri(elasticUrl))
            {
                AutoRegisterTemplate = true,
                TypeName = null,
                BatchAction = ElasticOpType.Create,
                IndexFormat = $"{applicationName.ToLower().Replace(".", "-")}" +
                              $"-{environment?.ToLower().Replace(".", "-")}"
            };
        }

        /// <summary>
        /// Получение настроек для ELK из конфигурации приложения
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">не был найден хотя бы один параметр</exception>
        private static LokiConfiguration GetLokiConfiguration(IConfiguration configuration)
        {
            const string uriName = "LokiConfiguration:Uri";
            const string labelKey = "LokiConfiguration:LabelKey";
            const string labelValue = "LokiConfiguration:LabelValue";
            const string propertyAsLabel = "LokiConfiguration:PropertyAsLabel";
            
            return new LokiConfiguration
            {
                Uri = configuration.GetSection(uriName).Value ?? throw new ArgumentNullException(uriName),
                LabelKey = configuration.GetSection(labelKey).Value ?? throw new ArgumentNullException(labelKey),
                LabelValue =configuration.GetSection(labelValue).Value ?? throw new ArgumentNullException(labelValue),
                PropertyAsLabel = configuration.GetSection(propertyAsLabel).Value 
                                  ?? throw new ArgumentNullException(propertyAsLabel)
            };
        }

        /// <summary>
        /// Получение настроек для ELK из env
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">не был найден хотя бы один параметр</exception>
        private static LokiConfiguration GetLokiConfiguration()
        {
            const string uri = "LokiConfiguration__Uri";
            const string labelKey = "LokiConfiguration__LabelKey";
            const string labelValue = "LokiConfiguration__LabelValue";
            const string propertyAsLabel = "LokiConfiguration__PropertyAsLabel";

            return new LokiConfiguration
            {
                Uri = Environment.GetEnvironmentVariable(uri) ?? throw new ArgumentNullException(uri),
                LabelKey = Environment.GetEnvironmentVariable(labelKey) ?? throw new ArgumentNullException(labelKey),
                LabelValue = Environment.GetEnvironmentVariable(labelValue) 
                             ?? throw new ArgumentNullException(labelValue),
                PropertyAsLabel = Environment.GetEnvironmentVariable(propertyAsLabel) 
                                  ?? throw new ArgumentNullException(propertyAsLabel)
            };
        }
        
        /// <summary>
        /// Получение настроек для ELK из конфигурации приложения
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">не был найден хотя бы один параметр</exception>
        private static ElasticConfiguration GeElasticConfiguration(IConfiguration configuration)
        {
            const string uriName = "ElasticConfiguration:Uri";
            const string loginName = "ElasticConfiguration:Login";
            const string passName = "ElasticConfiguration:Password";
            const string appName = "ElasticConfiguration:AppName";
            
            var uri = configuration.GetSection(uriName).Value;
            var login = configuration.GetSection(loginName).Value;
            var pass = configuration.GetSection(passName).Value;
            var app = configuration.GetSection(appName).Value;
            
            return new ElasticConfiguration
            {
                Uri = uri ?? throw new ArgumentNullException(uriName),
                Login = login ?? throw new ArgumentNullException(loginName),
                Password = pass ?? throw new ArgumentNullException(passName),
                AppName = app ?? throw new ArgumentNullException(appName)
            };
        }

        /// <summary>
        /// Получение настроек для ELK из env
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">не был найден хотя бы один параметр</exception>
        private static ElasticConfiguration GeElasticConfiguration()
        {
            const string uri = "ElasticConfiguration__Uri";
            const string login = "ElasticConfiguration__Login";
            const string password = "ElasticConfiguration__Password";
            const string appName = "ElasticConfiguration__AppName";

            return new ElasticConfiguration
            {
                Uri = Environment.GetEnvironmentVariable(uri) ?? throw new ArgumentNullException(uri),
                Login = Environment.GetEnvironmentVariable(login) ?? throw new ArgumentNullException(login),
                Password = Environment.GetEnvironmentVariable(password) ?? throw new ArgumentNullException(password),
                AppName = Environment.GetEnvironmentVariable(appName) ?? throw new ArgumentNullException(appName)
            };
        }

        private static LoggerConfiguration WriteToElastic(
            this LoggerConfiguration loggerConfiguration,
            IConfiguration? configuration = default)
        {
            var elasticSettings = configuration is null
                ? GeElasticConfiguration()
                : GeElasticConfiguration(configuration);

            // https://user:password@stack-server:port
            var elasticUrl = new Uri($"http://{elasticSettings.Login}:{elasticSettings.Password}@{elasticSettings.Uri}")
                .ToString();

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        
            return loggerConfiguration
                .WriteTo.Elasticsearch(ConfigureElasticSink(elasticUrl, environment, elasticSettings.AppName));
        }

        private static LoggerConfiguration WriteToLoki(
            this LoggerConfiguration loggerConfiguration,
            IConfiguration? configuration = null)
        {
            var lokiSettings = configuration is null
                ? GetLokiConfiguration()
                : GetLokiConfiguration(configuration);
            
            return loggerConfiguration
                .WriteTo.GrafanaLoki(lokiSettings.Uri,
                    new[] { new LokiLabel { Key = lokiSettings.LabelKey, Value = lokiSettings.LabelValue } },
                    lokiSettings.GetPropertiesAsLabels());
        }
    }
}
