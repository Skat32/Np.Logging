namespace Np.Logging.Logger.Http.Models
{
    /// <summary>
    /// Модель для содержимого не приводимого в json
    /// </summary>
    public class JsonErrorModel
    {
        /// <summary>
        /// Причина невозможности конвертации содержимого в json
        /// </summary>
        public string Reason { get; private set; }

        /// <summary>
        /// Описание с подсказкой по модели
        /// </summary>
        public string Description => "This is autogenerated model by Mb.Logging package. See \"Content\" field for original raw data.";

        /// <summary>
        /// Содержимое в строковом представлении (если возможно)
        /// </summary>
        public string? Content { get; private set; }

        /// <summary> ctor </summary>
        public JsonErrorModel(string reason, string? content = null)
        {
            Reason = reason;
            Content = content;
        }
    }
}