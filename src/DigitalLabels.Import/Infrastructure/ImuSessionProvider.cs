using System.Configuration;

namespace DigitalLabels.Import.Infrastructure
{
    public static class ImuSessionProvider
    {
        public static ImuSession CreateInstance(string moduleName)
        {
            return new ImuSession(moduleName, ConfigurationManager.AppSettings["EmuServerHost"], int.Parse(ConfigurationManager.AppSettings["EmuServerPort"]));
        }
    }
}
