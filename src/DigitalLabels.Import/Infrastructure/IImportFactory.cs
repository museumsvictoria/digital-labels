using System.Collections.Generic;
using IMu;

namespace DigitalLabels.Import.Infrastructure
{
    public interface IImportFactory<T>
    {
        string ModuleName { get; }

        string[] Columns { get; }

        Terms Terms { get; }

        T Make(Map map);

        IList<T> Fetch();
    }
}
