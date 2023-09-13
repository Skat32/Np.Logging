using System.Collections.Immutable;
using Serilog.Events;
using Serilog.Formatting.Elasticsearch;

namespace Np.Logging.Logger.Formatters
{
    internal class AddTypePostfixElasticSearchJsonFormatter 
        : ElasticsearchJsonFormatter
    {
        private static readonly ImmutableHashSet<Type> IntegerTypes = new HashSet<Type>
        {
            typeof(long),
            typeof(int),
            typeof(short),
            typeof(byte),
            typeof(sbyte),
            typeof(ulong),
            typeof(uint),
            typeof(ushort)
        }.ToImmutableHashSet();

        private static readonly ImmutableHashSet<Type> FloatTypes = new HashSet<Type>
        {
            typeof(float),
            typeof(double),
            typeof(decimal)
        }.ToImmutableHashSet();

        private static readonly ImmutableHashSet<Type> DateTimeTypes = new HashSet<Type>
        {
            typeof(DateTime),
            typeof(DateTimeOffset)
        }.ToImmutableHashSet();

        private static readonly ImmutableHashSet<Type> StringTypes = new HashSet<Type>
        {
            typeof(string),
            typeof(Guid),
            typeof(Uri)
        }.ToImmutableHashSet();

        // /// <summary>
        // /// Не пишем блок renderings в логах, т.к. он нам не нужен
        // /// </summary>
        // protected override void WriteRenderings(
        //     IGrouping<string, PropertyToken>[] tokensWithFormat,
        //     IReadOnlyDictionary<string, LogEventPropertyValue> properties,
        //     TextWriter output) { }
        //
        // protected override void WritePropertiesValues(IReadOnlyDictionary<string, LogEventPropertyValue> properties,
        //     TextWriter output)
        // {
        //     var precedingDelimiter = string.Empty;
        //     foreach (var keyValuePair in properties)
        //     {
        //         WriteJsonProperty(ComposeKey(keyValuePair.Key, keyValuePair.Value),
        //             keyValuePair.Value, ref precedingDelimiter, output);
        //     }
        // }
        //
        // protected override void WriteDictionary(IReadOnlyDictionary<ScalarValue, LogEventPropertyValue> elements,
        //     TextWriter output) =>
        //     base.WriteDictionary(elements.ToDictionary(ComposeScalarKey, property => property.Value), output);
        //
        // protected override void WriteStructure(string typeTag, IEnumerable<LogEventProperty> properties,
        //     TextWriter output) =>
        //     base.WriteStructure(typeTag, properties.Select(ComposeLogEventPropertyKey), output);


        private static ScalarValue ComposeScalarKey(KeyValuePair<ScalarValue, LogEventPropertyValue> property) =>
            new(ComposeKey(property.Key.Value, property.Value));

        private static LogEventProperty ComposeLogEventPropertyKey(LogEventProperty property) =>
            new(ComposeKey(property.Name, property.Value), property.Value);

        private static string ComposeKey(object key, LogEventPropertyValue value) => $"{key}{GetTypePostfix(value)}";

        private static string GetTypePostfix(LogEventPropertyValue logEventProperty)
        {
            if (logEventProperty is not ScalarValue scalarValue)
                return string.Empty;
            
            if (scalarValue.Value == null) 
                return string.Empty;

            var valueType = scalarValue.Value.GetType();

            if (valueType == typeof(bool)) return "_b";
            if (valueType == typeof(TimeSpan)) return "_ts";
            if (IntegerTypes.Contains(valueType)) return "_i";
            if (FloatTypes.Contains(valueType)) return "_f";
            if (DateTimeTypes.Contains(valueType)) return "_dt";
            if (StringTypes.Contains(valueType)) return "_s";

            return string.Empty;
        }
    }
}