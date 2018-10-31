using System;
using System.Linq;
using Zonkey.Dialects;

namespace Zonkey.ObjectModel.Projection
{
    internal class ProjectionParser : IProjectionParser
    {
        private const string Separator = ", ";

        private readonly SqlDialect _sqlDialect;

        public ProjectionParser(SqlDialect sqlDialect)
        {
            _sqlDialect = sqlDialect ?? throw new ArgumentNullException(nameof(sqlDialect));
        }

        public string GetQueryString(ProjectionMap projection)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));

            var expressions = projection.Fields.Select(f => _sqlDialect.FormatFieldName(f.ExpressionField, f.UseQuotedIdentifier));
            return string.Join(Separator, expressions);
        }
    }
}
