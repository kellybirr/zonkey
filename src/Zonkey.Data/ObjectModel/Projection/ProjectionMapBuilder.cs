
using System;

namespace Zonkey.ObjectModel.Projection
{
    internal class ProjectionMapBuilder : IProjectionMapBuilder
    {
        public ProjectionMap FromDataMap(DataMap dataMap)
        {
            if (dataMap == null)
                throw new ArgumentNullException(nameof(dataMap));

            var projection = new ProjectionMap();
            foreach (var dataMapField in dataMap.ReadableFields)
            {
                var projectionField = new SimpleProjectionField(dataMapField.FieldName, dataMapField.UseQuotedIdentifier ?? true);
                projection.Map.Add(dataMapField.Property, projectionField);
            }
            return projection;
        }
    }
}
