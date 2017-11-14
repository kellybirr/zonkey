using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Zonkey
{
    /// <summary>
    /// Provides Helper methods for updating configurations from environment variables
    /// </summary>
    public static class EnvironmentHelper
    {
        private static readonly ConcurrentDictionary<string, string> _cache = new ConcurrentDictionary<string, string>();
        private const string RX_PATTERN = @"%(?<var>\w+)(\[(?<start>\d+),(?<len>\d+)])?%";

        /// <summary>
        /// Disable string caching to deal with volatile environment variables.
        /// </summary>
        public static bool DisableCache { get; set; } = false;

        /// <summary>
        /// Processes the source string and replaces %ENV_VAR% values with matching
        /// values from the system environment.
        /// Also support partial strings via %ENV_VAR[start,len]%  (i.e. %ENV_VAR[3,4]%)
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static string ProcessString(string src)
        {
            return (DisableCache) ? ProcessStringInternal(src) : _cache.GetOrAdd(src, ProcessStringInternal);
        }

        private static string ProcessStringInternal(string str)
        {
            try
            {
                var rx = new Regex(RX_PATTERN, RegexOptions.ExplicitCapture);

                Match m = rx.Match(str);
                while (m.Success)
                {
                    string value = Environment.GetEnvironmentVariable(m.Groups["var"].Value) ?? string.Empty;
                    if (! string.IsNullOrEmpty(m.Groups["start"]?.Value))
                    {
                        int start = int.Parse(m.Groups["start"].Value);
                        int len = int.Parse(m.Groups["len"].Value);

                        value = value.Substring(start, len);
                    }

                    str = str.Replace(m.Value, value);
                    m = m.NextMatch();
                }

                return str;
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Error processing String or Environment", ex);
            }
        }
    }
}
