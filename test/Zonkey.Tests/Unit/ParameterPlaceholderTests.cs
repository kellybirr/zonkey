using System;
using System.Data;
using System.Linq;
using Xunit;
using Zonkey.Dialects;
using Zonkey.Mocks;

namespace Zonkey.Tests.Unit
{
    /// <summary>
    /// DataManager.AddIndexedParameter substitutes "$N" placeholders in CommandText with the
    /// provider-formatted parameter name. The original implementation used a plain string
    /// Replace, which is not boundary-aware: "$1" is a substring of "$10", so replacing index 1
    /// before/after index 10 can corrupt the text (most visibly for positional "?" dialects,
    /// where "$10" becomes "?0" instead of "?"). These tests pin the boundary-aware regex
    /// replacement and confirm that reusing the same placeholder multiple times in one
    /// statement still binds to a single DbParameter.
    /// </summary>
    public class ParameterPlaceholderTests
    {
        private static MockDbCommand CreateCommand(string commandText)
        {
            var conn = new MockDbConnection();
            conn.Open();
            var cmd = (MockDbCommand)conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = commandText;
            return cmd;
        }

        [Fact]
        public void ReusedPlaceholder_ReplacesBothOccurrences_AddsSingleParameter()
        {
            var dialect = new SqlServerDialect();
            var cmd = CreateCommand("A = $0 OR B = $0");

            // Pin the actual FormatParameterName output before relying on it.
            string expectedName = dialect.FormatParameterName(0, CommandType.Text);
            Assert.Equal("@p0", expectedName);

            DataManager.AddIndexedParameter(cmd, dialect, '$', 0, "value");

            Assert.Equal("A = @p0 OR B = @p0", cmd.CommandText);
            Assert.Single(cmd.Parameters);
        }

        // Field names use letters, not digits, so any leftover digit in CommandText after
        // substitution can only be corruption from the placeholder replacement itself (and not
        // an innocent collision with a column name like "F10").
        private static readonly string[] FieldNames =
            { "Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel", "India", "Juliet", "Kilo" };

        [Fact]
        public void TenPlusIndexes_SuffixFormatNames_NoCorruption()
        {
            var dialect = new SqlServerDialect();
            string text = string.Join(" AND ", Enumerable.Range(0, 11).Select(i => FieldNames[i] + " = $" + i));
            var cmd = CreateCommand(text);

            for (int i = 0; i < 11; i++)
                DataManager.AddIndexedParameter(cmd, dialect, '$', i, i);

            Assert.Equal(11, cmd.Parameters.Count);
            Assert.Contains("Bravo = @p1 ", cmd.CommandText);
            Assert.Contains("Kilo = @p10", cmd.CommandText);
            Assert.DoesNotContain("$", cmd.CommandText);
        }

        [Fact]
        public void GenericDialect_TenPlusIndexes_NoDigitResidue()
        {
            // This is the real bug: GenericSqlDialect.FormatParameterName always returns "?", so
            // replacing "$1" with plain string.Replace before "$10" has been consumed turns "$10"
            // into "?0" (the trailing "0" left over from "$10" after "$1" -> "?" was substituted).
            // Against the old plain-Replace implementation this test fails (RED); the boundary-aware
            // regex replace fixes it.
            var dialect = new GenericSqlDialect();
            string text = string.Join(" AND ", Enumerable.Range(0, 11).Select(i => FieldNames[i] + " = $" + i));
            var cmd = CreateCommand(text);

            for (int i = 0; i < 11; i++)
                DataManager.AddIndexedParameter(cmd, dialect, '$', i, i);

            Assert.Equal(11, cmd.Parameters.Count);

            int questionMarkCount = cmd.CommandText.Count(c => c == '?');
            Assert.Equal(11, questionMarkCount);
            Assert.DoesNotContain("$", cmd.CommandText);
            Assert.False(cmd.CommandText.Any(char.IsDigit), "CommandText should contain no leftover digits from placeholder corruption: " + cmd.CommandText);
        }

        [Fact]
        public void AdjacentDigitBoundary_Index1And12_ReplacedIndependently()
        {
            var dialect = new SqlServerDialect();
            var cmd = CreateCommand("X = $1 AND Y = $12");

            // Add all 13 values (indexes 0..12) as the real call site would, but we only need to
            // verify replacement for indexes 1 and 12 against each other.
            DataManager.AddIndexedParameter(cmd, dialect, '$', 1, "one");
            DataManager.AddIndexedParameter(cmd, dialect, '$', 12, "twelve");

            Assert.Equal("X = @p1 AND Y = @p12", cmd.CommandText);
            Assert.Equal(2, cmd.Parameters.Count);
        }
    }
}
