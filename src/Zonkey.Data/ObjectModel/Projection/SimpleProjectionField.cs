using System;

namespace Zonkey.ObjectModel.Projection
{
    public class SimpleProjectionField : ProjectionField
    {
        public SimpleProjectionField(string fieldName, bool useQuotedIdentifier)
            : base(useQuotedIdentifier)
        {
            FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
        }

        public override string ExpressionField => FieldName;

        public string FieldName { get; }
    }
}
