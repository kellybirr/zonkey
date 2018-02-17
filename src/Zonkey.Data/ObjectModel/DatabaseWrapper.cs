using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Zonkey.ObjectModel
{
    /// <summary>
    /// The preferred method of managing database connections
    /// </summary>
    public abstract class DatabaseWrapper : IDisposable
    {
        /// <summary>
        /// Private cache of adapters
        /// </summary>
        private readonly ConcurrentDictionary<Type, DataClassAdapter> _adapters
            = new ConcurrentDictionary<Type, DataClassAdapter>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseWrapper"/> class and connects to the Database
        /// </summary>
        protected DatabaseWrapper(string connectionName)
        {
            Connection = DbConnectionFactory.CreateConnection(connectionName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseWrapper"/> class and connects to the Database
        /// </summary>
        protected DatabaseWrapper(DbConnection connection)
        {
            Connection = connection;
        }

        // finalize
        ~DatabaseWrapper()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets or sets the database connection.
        /// </summary>
        /// <value>The connection.</value>
        public virtual DbConnection Connection { get; }

        /// <summary>
        /// Gets a DataClassAdapter of type Tdc.
        /// </summary>
        /// <typeparam name="Tdc">DC type</typeparam>
        /// <returns></returns>
        public virtual DataClassAdapter<Tdc> Adapter<Tdc>() 
            where Tdc : class, new()
        {
            return Adapter<Tdc>(null);
        }

        /// <summary>
        /// Gets a DataClassAdapter of type Tdc, with a transaction.
        /// </summary>
        /// <typeparam name="Tdc">DC type</typeparam>
        /// <param name="trx">the active transaction</param>
        /// <returns></returns>
        public virtual DataClassAdapter<Tdc> Adapter<Tdc>(DbTransaction trx)
            where Tdc : class, new()
        {
            DataClassAdapter adapter = _adapters.GetOrAdd(typeof(Tdc), 
                _ => new DataClassAdapter<Tdc>(Connection) 
                );

            adapter.Transaction = trx;    

            return (DataClassAdapter<Tdc>)adapter;            
        }

        /// <summary>
        /// Begins a transaction on the current connection
        /// </summary>
        /// <returns></returns>
        public DbTransaction BeginTransaction()
        {
            return Connection.BeginTransaction();
        }

        /// <summary>
        /// Executes an action within a DB transaction
        /// </summary>
        /// <param name="code">The code to execute</param>
        public void WithTransaction(Action<DbTransaction> code)
        {
            using (var trx = BeginTransaction())
            {
                try
                {
                    code(trx);
                    trx.Commit();
                }
                catch
                {
                    trx.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Equivalent to calling DataClassAdapter.GetSingelItem(linq)
        /// </summary>
        /// <typeparam name="Tdc">The type of the dc.</typeparam>
        /// <param name="expression">The filter expression.</param>
        /// <returns></returns>
        public virtual Task<Tdc> GetOne<Tdc>(Expression<Func<Tdc, bool>> expression)
            where Tdc : class, new()
        {
            return Adapter<Tdc>().GetOne(expression);
        }

        /// <summary>
        /// Equivalent to calling DataClassAdapter.OpenReader(linq)
        /// </summary>
        /// <typeparam name="Tdc">The type of the dc.</typeparam>
        /// <param name="expression">The filter expression.</param>
        /// <returns></returns>
        public virtual Task<DataClassReader<Tdc>> OpenReader<Tdc>(Expression<Func<Tdc, bool>> expression)
            where Tdc : class, new()
        {
            return Adapter<Tdc>().OpenReader(expression);
        }

        /// <summary>
        /// Equivalent to calling DataClassAdapter.Save
        /// </summary>
        /// <typeparam name="Tdc">The type of the dc.</typeparam>
        /// <param name="obj">The obj.</param>
        /// <returns></returns>
        public virtual Task<bool> Save<Tdc>(Tdc obj)
            where Tdc : class, ISavable, new()
        {
            return Adapter<Tdc>().Save(obj);
        }

        /// <summary>
        /// Equivalent to calling DataClassAdapter.Save
        /// </summary>
        /// <typeparam name="Tdc">The type of the dc.</typeparam>
        /// <param name="obj">The obj.</param>
        /// <param name="updateCriteria">The update criteria</param>
        /// <returns></returns>
        public virtual Task<bool> Save<Tdc>(Tdc obj, UpdateCriteria updateCriteria)
            where Tdc : class, ISavable, new()
        {
            return Adapter<Tdc>().Save(obj, updateCriteria);
        }

        /// <summary>
        /// Equivalent to calling DataClassAdapter.Save
        /// </summary>
        /// <typeparam name="Tdc">The type of the dc.</typeparam>
        /// <param name="obj">The obj.</param>
        /// <param name="updateCriteria">The update criteria</param>
        /// <param name="updateAffect">Affect which fields</param>
        /// <param name="selectBack">Select what back</param>
        /// <returns></returns>
        public virtual Task<bool> Save<Tdc>(Tdc obj, UpdateCriteria updateCriteria, UpdateAffect updateAffect, SelectBack selectBack)
            where Tdc : class, ISavable, new()
        {
            return Adapter<Tdc>().Save(obj, updateCriteria, updateAffect, selectBack);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {			
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            _adapters.Clear();
            Connection?.Dispose();

            if (disposing)
                GC.SuppressFinalize(this);
        }
    }
}
