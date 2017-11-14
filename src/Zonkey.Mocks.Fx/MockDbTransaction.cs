using System;
using System.Data;
using System.Data.Common;

namespace Zonkey.Mocks
{
    public class MockDbTransaction : DbTransaction
    {
        internal MockDbTransaction(MockDbConnection connection, IsolationLevel isolationLevel)
        {
            _dbConnection = connection;
            _isolationLevel = isolationLevel;
            State = MockTransactionState.Uncomitted;
        }

        public override void Commit()
        {
            if (State != MockTransactionState.Uncomitted)
                throw new InvalidOperationException("Transaction is not in a valid state to be committed");

            _dbConnection.ActiveTransaction = null;
            State = MockTransactionState.Comitted;
        }

        protected override DbConnection DbConnection
        {
            get { return _dbConnection; }
        }
        private readonly MockDbConnection _dbConnection;

        public override IsolationLevel IsolationLevel
        {
            get { return _isolationLevel; }
        }
        private readonly IsolationLevel _isolationLevel;

        public override void Rollback()
        {
            if (State != MockTransactionState.Uncomitted)
                throw new InvalidOperationException("Transaction is not in a valid state to be committed");

            _dbConnection.ActiveTransaction = null;
            State = MockTransactionState.RolledBack;
        }

        public MockTransactionState State { get; private set; }
    }

    public enum MockTransactionState
    {
        Uncomitted,
        Comitted,
        RolledBack
    }
}
