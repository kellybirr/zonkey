namespace Zonkey.ObjectModel.Projection
{
    internal interface IProjectionParser
    {
        string GetQueryString(ProjectionMap projection);
    }
}
