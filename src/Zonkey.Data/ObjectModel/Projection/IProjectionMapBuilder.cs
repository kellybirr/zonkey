namespace Zonkey.ObjectModel.Projection
{
    internal interface IProjectionMapBuilder
    {
        ProjectionMap FromDataMap(DataMap dataMap);
    }
}