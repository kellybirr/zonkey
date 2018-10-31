using System.Data;

namespace Zonkey.ObjectModel.Projection
{
    public abstract class ProjectionField
    {
        protected ProjectionField(bool useQuotedIdentifier)
        {
            UseQuotedIdentifier = useQuotedIdentifier;
        }

        public abstract string ExpressionField { get; }

        public bool UseQuotedIdentifier { get; }
    }
}
