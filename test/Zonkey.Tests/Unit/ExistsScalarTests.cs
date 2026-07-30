using System;
using System.Threading.Tasks;
using Xunit;
using Zonkey.Mocks;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit
{
    /// <summary>
    /// DataClassAdapter&lt;T&gt;.Exists() normalizes the scalar returned by the provider through
    /// Convert.ToInt64, because different providers box the "1" from
    /// "CASE WHEN EXISTS(...) THEN 1 ELSE 0 END" differently (int on SqlClient, long on
    /// SQLite/MySQL, decimal on Oracle). This pins that normalization against the
    /// Zonkey.Mocks fake connection/command, which lets ExecuteScalar return an arbitrary
    /// boxed value without a real provider or schema.
    /// </summary>
    public class ExistsScalarTests
    {
        private static async Task<bool> ExistsWithScalar(object scalarValue)
        {
            var conn = new MockDbConnection();
            conn.Open();
            conn.SetupCommandFunc = cmd => cmd.DoExecuteScalar = _ => scalarValue;

            var adapter = new DataClassAdapter<Animal>(conn);
            return await adapter.Exists("SpeciesId = 1");
        }

        [Fact]
        public async Task BoxedInt_One_ReturnsTrue()
        {
            Assert.True(await ExistsWithScalar(1));
        }

        [Fact]
        public async Task BoxedLong_One_ReturnsTrue()
        {
            Assert.True(await ExistsWithScalar(1L));
        }

        [Fact]
        public async Task BoxedDecimal_One_ReturnsTrue()
        {
            Assert.True(await ExistsWithScalar(1m));
        }

        [Fact]
        public async Task BoxedInt_Zero_ReturnsFalse()
        {
            Assert.False(await ExistsWithScalar(0));
        }

        [Fact]
        public async Task DBNullValue_ReturnsFalse_DoesNotThrow()
        {
            // ExistsInternal short-circuits on "result != DBNull.Value" before ever calling
            // Convert.ToInt64, so a DBNull scalar returns false rather than throwing
            // InvalidCastException (which is what Convert.ToInt64(DBNull.Value) alone would do).
            Assert.False(await ExistsWithScalar(DBNull.Value));
        }
    }
}
