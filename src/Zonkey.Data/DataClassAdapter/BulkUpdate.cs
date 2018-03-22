using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;

namespace Zonkey
{
    public partial class DataClassAdapter<T>
    {
        private DbCommand _bulkUpdateCommand;
        private bool _bulkUpdateKeys = false;
        private PropertyInfo[] _bulkUpdateProperties;

        /// <summary>
        /// Gets or sets a value indicating whether to bulk update key field values.
        /// </summary>
        /// <value>if <c>true</c> key field values will be updated.</value>
        public bool BulkUpdateKeys
        {
            get { return _bulkUpdateKeys; }
            set
            {
                _bulkUpdateKeys = value;
                _bulkUpdateCommand = null;
                _bulkUpdateProperties = null;
            }
        }

        /// <summary>
        /// Bulk update an entire collection of objects/records.
        /// Does not preform any select-back or modify the state of the object
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns>The number of updated items.</returns>
        public async Task<int> BulkUpdate(ICollection<T> collection)
        {
            // prep for bulk insert operations
            if (_bulkUpdateCommand == null)
                CommandBuilder.GetBulkUpdateInfo(_bulkUpdateKeys, out _bulkUpdateCommand, out _bulkUpdateProperties);

            // init counter
            int nRecords = 0;

            // update objects/records
            foreach (T obj in collection)
            {
                try
                {
                    await BulkUpdateObjectInternal(obj).ConfigureAwait(false);
                    nRecords++;
                }
                catch (Exception ex)
                {
                    throw new BulkUpdateException(nRecords, ex);
                }
            }

            return nRecords;
        }

        /// <summary>
        /// Bulk updates a single object/record.
        /// Does not preform any select-back or modify the state of the object
        /// </summary>
        /// <param name="obj">The object.</param>
        public async Task BulkUpdate(T obj)
        {
            // prep for bulk insert operations
            if (_bulkUpdateCommand == null)
                CommandBuilder.GetBulkUpdateInfo(_bulkUpdateKeys, out _bulkUpdateCommand, out _bulkUpdateProperties);

            // update object/record
            try
            {
                await BulkUpdateObjectInternal(obj).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new BulkUpdateException(0, ex);
            }
        }

        private async Task BulkUpdateObjectInternal(T obj)
        {
            // set parameter values from obj instance
            for (int i = 0; i < _bulkUpdateProperties.Length; i++)
            {
                PropertyInfo pi = _bulkUpdateProperties[i];

                object oValue = pi.GetValue(obj, null);
                _bulkUpdateCommand.Parameters[i].Value = (oValue ?? DBNull.Value);
            }

            // execute insert command
            await ExecuteNonQueryInternal(_bulkUpdateCommand).ConfigureAwait(false);
        }
    }
}