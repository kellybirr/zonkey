using System.Collections.Generic;
using System.Reflection;

namespace Zonkey.ObjectModel.Projection
{
    public class ProjectionMap
    {
        public Dictionary<PropertyInfo, ProjectionField> Map { get; } = new Dictionary<PropertyInfo, ProjectionField>();

        public IEnumerable<ProjectionField> Fields => Map.Values;
    }
}
