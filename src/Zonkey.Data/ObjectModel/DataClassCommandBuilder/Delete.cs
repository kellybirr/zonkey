using System.Data.Common;
using System.Text;

namespace Zonkey.ObjectModel
{
    /// <summary>
    /// Provides methods to create SQL commands to interact with a <see cref="Zonkey.ObjectModel.DataClass"/>.
    /// </summary>
    public partial class DataClassCommandBuilder
    {
        /// <summary>
        /// Gets the DELETE item command.
        /// </summary>
        public DbCommand DeleteItemCommand
        {
            get
            {
                DbCommand command = GetTextCommand("");
                string whereString = BuildWhereClauseFromKeys(command);
                command.CommandText = string.Format("DELETE FROM {0} WHERE {1}", SaveToTable, whereString);

                if (_dialect.SupportsChangeContext && ChangeTrackingContext != null)
                    _dialect.ApplyChangeTrackingContext(command, ChangeTrackingContext);

                return command;
            }
        }

        /// <summary>
        /// Gets the DELETE command.
        /// </summary>
        /// <param name="filter">The filter (WHERE clause).</param>
        /// <returns>An instance of a <see cref="System.Data.Common.DbCommand"/> object.</returns>
        public DbCommand GetDeleteCommand(string filter)
        {
            var commandText = new StringBuilder();
            commandText.AppendFormat("DELETE FROM {0}", SaveToTable);

            if (! string.IsNullOrEmpty(filter))
                commandText.AppendFormat(" WHERE {0}", filter);

            DbCommand deleteCommand = GetTextCommand(commandText.ToString());

            if (_dialect.SupportsChangeContext && ChangeTrackingContext != null)
                _dialect.ApplyChangeTrackingContext(deleteCommand, ChangeTrackingContext);

            return deleteCommand;
        }

        /// <summary>
        /// Gets the DELETE command.
        /// </summary>
        /// <param name="filters">The <see cref="Zonkey.SqlFilter"/> filter array (WHERE clause).</param>
        /// <returns>An instance of a <see cref="System.Data.Common.DbCommand"/> object.</returns>
        public DbCommand GetDeleteCommand(SqlFilter[] filters)
        {
            var commandText = new StringBuilder();
            commandText.AppendFormat("DELETE FROM {0}", SaveToTable);

            DbCommand command = GetTextCommand("");
            if ((filters != null) && (filters.Length > 0))
            {
                commandText.Append(" WHERE ");
                for (int i = 0; i < filters.Length; i++)
                {
                    if (i > 0) commandText.Append(" AND ");
                    commandText.Append(filters[i].ToString(_dialect, i));
                    filters[i].AddToCommandParams(command, _dialect, i);
                }
            }

            command.CommandText = commandText.ToString();

            if (_dialect.SupportsChangeContext && ChangeTrackingContext != null)
                _dialect.ApplyChangeTrackingContext(command, ChangeTrackingContext);

            return command;
        }
    }
}