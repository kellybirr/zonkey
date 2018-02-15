using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Zonkey.ObjectModel;

namespace Zonkey
{
    public partial class DataClassAdapter<T>
    {
        /// <summary>
        /// Saves the specified object
        /// </summary>
        /// <param name="obj">The object to save</param>
        /// <returns>true/false</returns>
        public async Task<bool> Save(T obj)
        {
            return HandleSaveResult( await TrySave(obj, UpdateCriteria.Default, UpdateAffect.ChangedFields, SelectBack.Default).ConfigureAwait(false) );
        }

        /// <summary>
        /// Saves the specified object
        /// </summary>
        /// <param name="obj">The object to save</param>
        /// <param name="criteria">The criteria of type <see cref="Zonkey.UpdateCriteria"/>.</param>
        /// <returns>true/false</returns>
        public async Task<bool> Save(T obj, UpdateCriteria criteria)
        {
            return HandleSaveResult( await TrySave(obj, criteria, UpdateAffect.ChangedFields, SelectBack.Default).ConfigureAwait(false));
        }

        /// <summary>
        /// Saves the specified object
        /// </summary>
        /// <param name="obj">The object to save</param>
        /// <param name="criteria">The criteria of type <see cref="Zonkey.UpdateCriteria"/>.</param>
        /// <param name="affect">The <see cref="Zonkey.UpdateAffect"/> value that determines which rows to affect.</param>
        /// <param name="selectBack">The <see cref="Zonkey.SelectBack"/> value that determines whether to select back the changed rows.</param>
        /// <returns>true/false</returns>
        public async Task<bool> Save(T obj, UpdateCriteria criteria, UpdateAffect affect, SelectBack selectBack)
        {
            return HandleSaveResult( await TrySave(obj, criteria, affect, selectBack).ConfigureAwait(false));
        }

        /// <summary>
        /// Tries to save the object
        /// </summary>
        /// <param name="obj">The object to save</param>
        /// <returns>A value of type <see cref="SaveResultStatus"/></returns>
        public Task<SaveResult> TrySave(T obj)
        {
            return TrySave(obj, UpdateCriteria.Default, UpdateAffect.ChangedFields, SelectBack.Default);
        }

        /// <summary>
        /// Tries to save the object
        /// </summary>
        /// <param name="obj">The object to save</param>
        /// <param name="criteria">The criteria of type <see cref="Zonkey.UpdateCriteria"/>.</param>
        /// <returns>A value of type <see cref="SaveResultStatus"/></returns>
        public Task<SaveResult> TrySave(T obj, UpdateCriteria criteria)
        {
            return TrySave(obj, criteria, UpdateAffect.ChangedFields, SelectBack.Default);
        }

        /// <summary>
        /// Tries to save the object
        /// </summary>
        /// <param name="obj">The object to save</param>
        /// <param name="criteria">The criteria of type <see cref="Zonkey.UpdateCriteria"/>.</param>
        /// <param name="affect">The <see cref="Zonkey.UpdateAffect"/> value that determines which rows to affect.</param>
        /// <param name="selectBack">The <see cref="Zonkey.SelectBack"/> value that determines whether to select back the changed rows.</param>
        /// <returns>A value of type <see cref="SaveResultStatus"/></returns>
        public async Task<SaveResult> TrySave(T obj, UpdateCriteria criteria, UpdateAffect affect, SelectBack selectBack)
        {
            if (! (obj is ISavable objSV))
                throw new ArgumentException("Save() is only supported on classes that implement Zonkey.ObjectModel.ISavable", nameof(obj));

            var objDCX = obj as DataClass;

            // raise pre-save event
            objDCX?.OnBeforeSave();

            SaveResult result;
            switch (objSV.DataRowState)
            {
                case DataRowState.Added:
                    result = await TryInsert(obj, selectBack).ConfigureAwait(false);
                    break;
                case DataRowState.Detached:
                    throw new InvalidOperationException("Cannot save objects in a detached state. Did you forget to use the new record constructor?");

                case DataRowState.Modified:
                    if (((selectBack == SelectBack.None) || (selectBack == SelectBack.AllFields))
                        && (criteria <= UpdateCriteria.ChangedFields) && (affect == UpdateAffect.ChangedFields))
                        result = await TryUpdate2(obj, criteria, (selectBack == SelectBack.AllFields)).ConfigureAwait(false);
                    else
                        result = await TryUpdate(obj, criteria, affect, selectBack).ConfigureAwait(false);

                    break;

                default:
                    return new SaveResult(SaveResultStatus.Skipped, SaveType.None);
            }

            // raise post-save event and commit values
            if (result.Status == SaveResultStatus.Success)
            {
                objDCX?.OnAfterSave(true);
                objSV.CommitValues();
            }

            return result;
        }

        /// <summary>
        /// Saves the collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns></returns>
        public Task<int> SaveCollection(ICollection<T> collection)
        {
            return SaveCollection(collection, UpdateCriteria.Default, UpdateAffect.ChangedFields, SelectBack.Default);            
        }

        /// <summary>
        /// Saves the collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        public Task<int> SaveCollection(ICollection<T> collection, UpdateCriteria criteria)
        {
            return SaveCollection(collection, criteria, UpdateAffect.ChangedFields, SelectBack.Default);            
        }

        /// <summary>
        /// Saves the collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="criteria">The criteria.</param>
        /// <param name="affect">The affect.</param>
        /// <param name="selectBack">The select back.</param>
        /// <returns></returns>
        public async Task<int> SaveCollection(ICollection<T> collection, UpdateCriteria criteria, UpdateAffect affect, SelectBack selectBack)
        {
            var result = await TrySaveCollection(collection, criteria, affect, selectBack).ConfigureAwait(false);
            
            if ((result.Conflicted.Count > 0) || (result.Failed.Count > 0))
                throw new CollectionSaveException<T>(result);

            return (result.Inserted.Count + result.Updated.Count);
        }

        /// <summary>
        /// Saves the collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns>A value of type <see cref="Zonkey.CollectionSaveResult{T}" />
        /// </returns>
        public Task<CollectionSaveResult<T>> TrySaveCollection(ICollection<T> collection)
        {
            return TrySaveCollection(collection, UpdateCriteria.Default, UpdateAffect.ChangedFields, SelectBack.Default, false);
        }

        /// <summary>
        /// Saves the collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="continueOnError">if set to <c>true</c> continues past errors.</param>
        /// <returns>
        /// A value of type <see cref="Zonkey.CollectionSaveResult{T}"/>
        /// </returns>
        public Task<CollectionSaveResult<T>> TrySaveCollection(ICollection<T> collection, bool continueOnError)
        {
            return TrySaveCollection(collection, UpdateCriteria.Default, UpdateAffect.ChangedFields, SelectBack.Default, continueOnError);
        }

        /// <summary>
        /// Saves the collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="criteria">The criteria.</param>
        /// <returns>A value of type <see cref="Zonkey.CollectionSaveResult{T}"/></returns>
        public Task<CollectionSaveResult<T>> TrySaveCollection(ICollection<T> collection, UpdateCriteria criteria)
        {
            return TrySaveCollection(collection, criteria, UpdateAffect.ChangedFields, SelectBack.Default);
        }

        /// <summary>
        /// Saves the collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="criteria">The criteria.</param>
        /// <param name="continueOnError">if set to <c>true</c> continues past errors.</param>
        /// <returns>A value of type <see cref="Zonkey.CollectionSaveResult{T}"/></returns>
        public Task<CollectionSaveResult<T>> TrySaveCollection(ICollection<T> collection, UpdateCriteria criteria, bool continueOnError)
        {
            return TrySaveCollection(collection, criteria, UpdateAffect.ChangedFields, SelectBack.Default, false);
        }

        /// <summary>
        /// Saves the collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="criteria">The criteria of type <see cref="Zonkey.UpdateCriteria"/>.</param>
        /// <param name="affect">The <see cref="Zonkey.UpdateAffect"/> value that determines which rows to affect.</param>
        /// <param name="selectBack">The <see cref="Zonkey.SelectBack"/> value that determines whether to select back the changed rows.</param>
        /// <returns>A value of type <see cref="Zonkey.CollectionSaveResult{T}"/></returns>
        public Task<CollectionSaveResult<T>> TrySaveCollection(ICollection<T> collection, UpdateCriteria criteria, UpdateAffect affect, SelectBack selectBack)
        {
            return TrySaveCollection(collection, UpdateCriteria.Default, UpdateAffect.ChangedFields, SelectBack.Default, false);
        }

        /// <summary>
        /// Saves the collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="criteria">The criteria of type <see cref="Zonkey.UpdateCriteria"/>.</param>
        /// <param name="affect">The <see cref="Zonkey.UpdateAffect"/> value that determines which rows to affect.</param>
        /// <param name="selectBack">The <see cref="Zonkey.SelectBack"/> value that determines whether to select back the changed rows.</param>
        /// <param name="continueOnError">if set to <c>true</c> continues past errors.</param>
        /// <returns>
        /// A value of type <see cref="Zonkey.CollectionSaveResult{T}"/>
        /// </returns>
        public async Task<CollectionSaveResult<T>> TrySaveCollection(ICollection<T> collection, UpdateCriteria criteria, UpdateAffect affect, SelectBack selectBack, bool continueOnError)
        {
            var colResult = new CollectionSaveResult<T>();

            if (collection is ITrackDeletedItems<T> bindCol)
            {
                foreach (T obj in bindCol.DeletedItems)
                {
                    if (obj is ISavable objSV)
                    {
                        if (objSV.DataRowState == DataRowState.Deleted)
                        {
                            try
                            {
                                await DeleteItem(obj).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                colResult.Exceptions.Add(new CollectionSaveExceptionItem<T>(obj, ex));
                                if (! continueOnError) throw;
                            }
                            
                            colResult.Deleted.Add(obj);
                        }
                        else
                            colResult.Skipped.Add(obj);
                    }
                    else
                        colResult.Failed.Add(obj);
                }
            }

            foreach (T obj in collection)
            {
                if (obj is ISavable objSV)
                {
                    if (objSV.DataRowState != DataRowState.Unchanged)
                    {
                        try
                        {
                            SaveResult saveResult = await TrySave(obj, criteria, affect, selectBack).ConfigureAwait(false);
                            switch (saveResult.Status)
                            {
                                case SaveResultStatus.Skipped:
                                    colResult.Skipped.Add(obj);
                                    break;
                                case SaveResultStatus.Conflict:
                                    colResult.Conflicted.Add(obj);
                                    break;
                                case SaveResultStatus.Fail:
                                    colResult.Failed.Add(obj);
                                    break;
                                case SaveResultStatus.Success:
                                    if (saveResult.SaveType == SaveType.Insert)
                                        colResult.Inserted.Add(obj);
                                    else
                                        colResult.Updated.Add(obj);

                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            colResult.Exceptions.Add(new CollectionSaveExceptionItem<T>(obj, ex));
                            if (!continueOnError) throw;
                        }

                    }
                    else
                        colResult.Skipped.Add(obj);
                }
                else
                    colResult.Failed.Add(obj);
            }

            return colResult;
        }

        /// <summary>
        /// Tries to update the specified object
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="criteria">The criteria of type <see cref="Zonkey.UpdateCriteria"/>.</param>
        /// <param name="affect">The <see cref="Zonkey.UpdateAffect"/> value that determines which rows to affect.</param>
        /// <param name="selectBack">The <see cref="Zonkey.SelectBack"/> value that determines whether to select back the changed rows.</param>
        /// <returns>A <see cref="SaveResultStatus"/> value based on the outcome of the update operation.</returns>
        public async Task<SaveResult> TryUpdate(T obj, UpdateCriteria criteria, UpdateAffect affect, SelectBack selectBack)
        {
            try
            {
                DbCommand[] commands = CommandBuilder.GetUpdateCommands(obj, criteria, affect, selectBack);
                return await UpdateInternal(obj, commands).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new SaveFailedException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Updates the specified object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="criteria">The criteria of type <see cref="Zonkey.UpdateCriteria"/>.</param>
        /// <param name="affect">The <see cref="Zonkey.UpdateAffect"/> value that determines which rows to affect.</param>
        /// <param name="selectBack">The <see cref="Zonkey.SelectBack"/> value that determines whether to select back the changed rows.</param>
        /// <returns>true/false based on the outcome of the update operation.</returns>
        public async Task<bool> Update(T obj, UpdateCriteria criteria, UpdateAffect affect, SelectBack selectBack)
        {
            return HandleSaveResult( await TryUpdate(obj, criteria, affect, selectBack).ConfigureAwait(false));
        }

        /// <summary>
        /// Updates the specified object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="criteria">The criteria of type <see cref="Zonkey.UpdateCriteria"/>.</param>
        /// <param name="doSelectBack">if set to <c>true</c> [do select back].</param>
        /// <returns>A <see cref="SaveResultStatus"/> value based on the outcome of the update operation.</returns>
        public async Task<SaveResult> TryUpdate2(T obj, UpdateCriteria criteria, bool doSelectBack)
        {
            if (! (obj is ISavable))
                throw new ArgumentException("Update2() is only supported on classes that implement Zonkey.ObjectModel.ISavable", nameof(obj));

            try
            {
                DbCommand[] commands = CommandBuilder.GetUpdate2Commands((ISavable)obj, criteria, doSelectBack);
                return await UpdateInternal(obj, commands).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new SaveFailedException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Updates the specified object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="criteria">The criteria of type <see cref="Zonkey.UpdateCriteria"/>.</param>
        /// <param name="doSelectBack">if set to <c>true</c> [do select back].</param>
        /// <returns></returns>
        public async Task<bool> Update2(T obj, UpdateCriteria criteria, bool doSelectBack)
        {
            return HandleSaveResult( await TryUpdate2(obj, criteria, doSelectBack).ConfigureAwait(false));
        }

        private async Task<SaveResult> UpdateInternal(T obj, DbCommand[] commands)
        {
            DoBeforeSave(SaveType.Update, obj);

            if (commands == null)
            {
                if (obj is ISavable objSV)
                    objSV.DataRowState = DataRowState.Unchanged;

                return new SaveResult(SaveResultStatus.Skipped, SaveType.None);
            }

            DbCommand updateCommand = commands[0];
            DbCommand readerCommand = (commands.Length > 1) ? commands[1] : null;

            // execute update;
            int nRecordsAffected = await ExecuteNonQueryInternal(updateCommand).ConfigureAwait(false);

            if (readerCommand == null)
            {
                var resultStatus = ((nRecordsAffected == 1) || IgnoreUpdateRowCount) 
                    ? SaveResultStatus.Success 
                    : SaveResultStatus.Fail;
                
                return new SaveResult(resultStatus, SaveType.Update, nRecordsAffected);
            } 

            if ( (nRecordsAffected > 1) && (! IgnoreUpdateRowCount) )
                return new SaveResult(SaveResultStatus.Fail, SaveType.Update, nRecordsAffected);

            using (DbDataReader reader = await ExecuteReaderInternal(readerCommand, CommandBehavior.SingleRow).ConfigureAwait(false))
            {
                if (await reader.ReadAsync())
                {
                    if ((nRecordsAffected == 1) || IgnoreUpdateRowCount)
                    {
                        PopulateSingleObject(obj, reader, false);
                        return new SaveResult(SaveResultStatus.Success, SaveType.Update, nRecordsAffected);						
                    }

                    return new SaveResult(SaveResultStatus.Conflict, SaveType.Update, nRecordsAffected);
                }

                return new SaveResult(SaveResultStatus.Fail, SaveType.Update, nRecordsAffected);
            }
        }

        /// <summary>
        /// Inserts the specified object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="selectBack">The <see cref="Zonkey.SelectBack"/> value that determines which rows, if any, to return.</param>
        /// <returns>A <see cref="SaveResultStatus"/> value based on the outcome of the insert operation.</returns>
        public async Task<SaveResult> TryInsert(T obj, SelectBack selectBack)
        {
            try
            {
                DoBeforeSave(SaveType.Insert, obj);

                DbCommand[] commands = CommandBuilder.GetInsertCommands(obj, selectBack);
                DbCommand readerCommand = commands[commands.Length - 1];

                if ((selectBack == SelectBack.None) || (commands.Length == 2))
                {
                    int nRecordsAffected = await ExecuteNonQueryInternal(commands[0]).ConfigureAwait(false);

                    if (selectBack == SelectBack.None)
                        return new SaveResult(SaveResultStatus.Success, SaveType.Insert, nRecordsAffected);
                }

                using (DbDataReader reader = await ExecuteReaderInternal(readerCommand, CommandBehavior.SingleRow).ConfigureAwait(false))
                {
                    if (await reader.ReadAsync())
                    {
                        PopulateSingleObject(obj, reader, false);
                        return new SaveResult(SaveResultStatus.Success, SaveType.Insert, reader.RecordsAffected);
                    }
                    
                    return new SaveResult(SaveResultStatus.Fail, SaveType.Insert, reader.RecordsAffected);
                }
            }
            catch (Exception ex)
            {
                throw new SaveFailedException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Inserts the specified obj.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="selectBack">The select back.</param>
        /// <returns></returns>
        public async Task<bool> Insert(T obj, SelectBack selectBack)
        {
            return HandleSaveResult( await TryInsert(obj, selectBack).ConfigureAwait(false));
        }

        /// <summary>
        /// Handles the save result.
        /// </summary>
        /// <param name="result">The result</param>
        /// <returns></returns>
        private static bool HandleSaveResult(SaveResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            switch (result.Status)
            {
                case SaveResultStatus.Skipped:
                    return false;
                case SaveResultStatus.Success:
                    return true;
                case SaveResultStatus.Conflict:
                    throw new UpdateConflictException(result);
                case SaveResultStatus.Fail:
                    throw new SaveFailedException(result);
                default:
                    throw new ArgumentException("Unrecognized or invalid SaveResultStatus");
            }
        }
    }
}