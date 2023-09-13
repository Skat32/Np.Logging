namespace Np.Logging.Logger.Models;

/// <summary>
/// Настройки для подключения к Loki by Grafana
/// </summary>
public class LokiConfiguration
{
    /// <summary>
    /// Ссылка
    /// </summary>
    public string Uri { get; set; }

    /// <summary>
    /// Label's name
    /// </summary>
    public string LabelKey { get; set; }

    /// <summary>
    /// Label's value
    /// </summary>
    public string LabelValue { get; set; }

    /// <summary>
    /// Список свойств, которые будут сопоставлены с метками.
    /// Как разделитель использовать ","
    /// </summary>
    public string PropertyAsLabel { get; set; }

    /// <summary>
    /// Получить массив из свойства <see cref="PropertyAsLabel"/>
    /// </summary>
    public string[] GetPropertiesAsLabels() => PropertyAsLabel.Split(',');
}
