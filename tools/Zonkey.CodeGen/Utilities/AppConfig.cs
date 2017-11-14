using System.Configuration;

namespace ZonkeyCodeGen.Utilities
{
    static class AppConfig
    {
        public static DebugLevel DebugLevel
        {
            [System.Diagnostics.DebuggerStepThrough()]
            get { return (DebugLevel)System.Enum.Parse(typeof(DebugLevel), ConfigurationManager.AppSettings["DebugLevel"], true); }
        }
    }
}