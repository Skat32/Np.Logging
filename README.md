# Np.Logging

Пакет кастомного логирования с использованием Serilog и интеграцией с OpenTracing.\
По умолчанию приводит логи к более читаемому виду. Так же добавляет логам ключи для ElasticSearch'а.

## Установка

В первую очередь необходимо добавить в переменные окружения параметры, по которым сервис будет подключаться в ELK

``` json
"ElasticConfiguration__Uri": "localhost:9200",
"ElasticConfiguration__Login": "elastic",
"ElasticConfiguration__Password": "elastic"
```

Подключать с помощью расширения `.UseLogging( ... )` для `IWebHostBuilder`.

``` C#

    // Program.cs
    private static IWebHostBuilder CreateWebHostBuilder(string[] args)
    {
        return WebHost.CreateDefaultBuilder(args)
            /// ...
            .UseLogging((ctx, config) =>
            {
                config.WriteToDefault(ctx.HostingEnvironment);
            })
            /// ...
            .UseStartup<Startup>();
    }

```

## Дополнительная конфигурация

1) По умолчанию уровень логирования установлен как `LogEventLevel.Information`. Для изменения следует воспользоваться дополнительным параметром `minLogLevel` в методе `UseLogging`, либо изменить в рантайме с помощью специального endpoint'а.

``` C#

    // Program.cs
    .UseLogging((ctx, config) =>
    {
        config.WriteToDefault(ctx.HostingEnvironment);
    }, minLogLevel: LogEventLevel: Warning)

```

2) При необходимости можно настроить пути для логирования самостоятельно, для этого нужно заменить своими настройками строку `config.WriteToDefault(ctx.HostingEnvironment);`

``` C#

    // Program.cs
    .UseLogging((ctx, config) =>
    {
        config.WriteTo.Console();
    })

```

3) При необходимости можно добавить маскированние определенной информации передав настройки в параметр `maskingOperators`, для этого нужно создать класс наследующийся от `RegexMaskingOperator`

``` C#
/// <summary>
/// Класс для маскирования данных карточки
/// </summary>
public class CvvFieldMaskingOperator : RegexMaskingOperator
{
    private const string CvvFieldReplacePattern = @"(""cvv""\s*:\s*"")(\d{3})("")";

    /// <inheritdoc />
    public CvvFieldMaskingOperator() 
        : base(CvvFieldReplacePattern, RegexOptions.IgnoreCase | RegexOptions.Compiled) { }

    /// <inheritdoc />
    protected override string PreprocessMask(string mask) =>
        $"$1{mask}$3";
}
```

после чего, его можно будет передать в парамет или использовать вместе с базовыми операторами

``` C#

    // Program.cs
    .UseLogging(
        maskingOperators: new IMaskingOperator[]
        {
            new CreditCardMaskingOperator(false),
            new CvvFieldMaskingOperator()
        })

```

4) Для добавления в проект конфигурирующего логи endpoint'а нужно воспользоваться методом-расширением `.UseLoggingEndpoint()` для `IApplicationBuilder`

``` C#

    // Startup.cs
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        /// ...
        app.UseLoggingEndpoint(); // Путь до endpoint'а по умолчанию: "/logging/loglevel"

        // Пример изменения пути по умолчанию
        // app.UseLoggingEndpoint(options =>
        // {
        //     options.EndpointPath = "/path-to-logs-endpoint";
        // });
        /// ...
    }
    
```
