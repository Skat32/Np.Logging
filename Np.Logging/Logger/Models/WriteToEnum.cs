namespace Np.Logging.Logger.Models;

/// <summary>
/// Список поддерживаемых инпутов для логов
/// </summary>
public enum WriteToEnum
{
    /// <summary>
    /// Elastic 
    /// </summary>
    Elastic,
    
    /// <summary>
    /// Loki
    /// </summary>
    Loki
}