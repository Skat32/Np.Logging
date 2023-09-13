using Serilog.Events;

namespace Np.Logging.Logger.LogLevelSwitcher
{
    /// <summary>
    /// Project logging levels switcher
    /// </summary>
    public interface ILoggingLevelSwitcher
    {
        /// <summary>
        /// Minimum global logging level
        /// </summary>
        LogEventLevel GlobalLogLevel { get; set; }
    }
}