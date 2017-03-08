using IMu;

namespace DigitalLabels.Import.Infrastructure
{
    public interface IImportFactory<out T>
    {
        string ModuleName { get; }

        string[] Columns { get; }

        Terms Terms { get; }

        T Make(Map map);
    }
}
