namespace Np.Logging.Logger.Http.Models
{
    /// <summary>
    /// Log settings
    /// </summary>
    public class LogSettings
    {
        /// <summary>
        /// Нужно или нет логировать тело HTTP запросов
        /// </summary>
        public bool EnableHttpBodyLog { get; set; } = true;

        /// <summary>
        /// Нужно или нет логировать заголовки HTTP запросов
        /// Логироваться будут заголовки указанные в HeadersAllowList. Если список пустой, то логируюся всё
        /// </summary>
        public bool EnableHttpHeadersLog { get; set; } = true;

        /// <summary>
        /// Список заголовков, доступных для логирования
        /// Формат - через запятую без пробелов. Пример: "host,user-agent,x-forwarded-for"
        /// </summary>
        public string HeadersAllowList { get; set; } = "";

        /// <summary>
        /// Список заголовков для исключения из логов ВНЕ зависимости от _allowList
        /// Должен содержать элементы в lower case
        /// </summary>
        private HashSet<string> _blockList = new HashSet<string>
        {
            "authorization"
        };
        private HashSet<string> _allowList;
        private bool _allowAll;

        /// <summary>
        /// Проинициализировать поля, нужные для работы метода <see cref="IsHeaderAllowed" />
        /// </summary>
        internal void Init()
        {
            _allowAll = EnableHttpHeadersLog && string.IsNullOrEmpty(HeadersAllowList);
            var allowItems = HeadersAllowList.Split(',').Select(x => x.Trim().ToLower());
            _allowList = new HashSet<string>(allowItems.Except(_blockList));
        }

        /// <summary>
        /// Проверить разрешено ли логировать заголовок
        /// </summary>
        public bool IsHeaderAllowed(string header)
        {
            if (!EnableHttpHeadersLog)
                return false;

            if (_allowAll)
                return !IsHeaderBlocked(header);

            if (_allowList.Contains(header))
                return true;

            var h = header.Trim().ToLower();
            
            if (!_allowList.Contains(h)) 
                return false;
            
            _allowList.Add(h);
            
            return true;
        }

        private bool IsHeaderBlocked(string header)
        {
            if (_blockList.Contains(header))
                return true;

            var h = header.Trim().ToLower();
            
            if (!_blockList.Contains(h))
                return false;
            
            _blockList.Add(h);
            
            return true;
        }
    }
}
