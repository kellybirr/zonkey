using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;

namespace Zonkey
{
    public partial class DataClassAdapter<T>
    {
        private DbCommand _bulkInsertCommand;
        private PropertyInfo[] _bulkInsertProperties;

        /// <summary>
        /// Bulk inserts an entire collection of objects/records into the database.
        /// Does not preform any select-back or modify the state of the object
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns>The number of inserted items.</returns>
        public async Task<int> BulkInsert(ICollection<T> collection)
        {
            // prep for bulk insert operations
            if (_bulkInsertCommand == null)
                CommandBuilder.GetBulkInsertInfo(out _bulkInsertCommand, out _bulkInsertProperties);

            // init counter
            int nRecords = 0;

            // insert objects/records
            foreach (T obj in collection)
            {
                try
                {
                    await BulkInsertObjectInternal(obj).ConfigureAwait(false);
                    nRecords++;
                }
                catch (Exception ex)
                {
                    throw new BulkInsertException(nRecords, ex);
                }
            }

            return nRecords;
        }

        /// <summary>
        /// Bulk inserts a single object/record into the database.
        /// Does not preform any select-back or modify the state of the object
        /// </summary>
        /// <param name="obj">The object.</param>
        public async Task BulkInsert(T obj)
        {
            // prep for bulk insert operations
            if (_bulkInsertCommand == null)
                CommandBuilder.GetBulkInsertInfo(out _bulkInsertCommand, out _bulkInsertProperties);

            // insert object/record
            try
            {
                await BulkInsertObjectInternal(obj).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new BulkInsertException(0, ex);
            }
        }

        private async Task BulkInsertObjectInternal(T obj)
        {
            // set parameter values from obj instance
            for (int i = 0; i < _bulkInsertProperties.Length; i++)
            {
                PropertyInfo pi = _bulkInsertProperties[i];
                object oValue = pi.GetValue(obj, null);

                // fix empty guids
                if ((oValue is Guid) && (Guid.Empty == (Guid)oValue))
                    oValue = DBNull.Value;

                if (pi.PropertyType == typeof (string))
                    _bulkInsertCommand.Parameters[i].Value = (oValue ?? _nullStringDefault);
                else
                    _bulkInsertCommand.Parameters[i].Value = (oValue ?? DBNull.Value);
            }

            // execute insert command
            await ExecuteNonQueryInternal(_bulkInsertCommand);
        }
    }
}