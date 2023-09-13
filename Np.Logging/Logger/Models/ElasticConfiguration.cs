namespace Np.Logging.Logger.Models;

/// <summary>
/// Настройки для подключения к Elastic
/// </summary>
public class ElasticConfiguration
{
    /// <summary>
    /// Ссылка
    /// </summary>
    public string Uri { get; set; }

    /// <summary>
    /// Логин
    /// </summary>
    public string Login { get; set; }

    /// <summary>
    /// Пароль
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Имя приложения отображаемого в логах
    /// </summary>
    public string AppName { get; set; }
}