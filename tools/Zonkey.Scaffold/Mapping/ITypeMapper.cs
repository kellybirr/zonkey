using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Schema;

namespace Zonkey.Scaffold.Mapping;

public interface ITypeMapper
{
    ColumnMapping Map(TableInfo table, ColumnInfo column, bool nullableRefs,
        ICollection<string> warnings);
}
