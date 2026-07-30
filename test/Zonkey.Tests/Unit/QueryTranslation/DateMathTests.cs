using System;
using System.Linq.Expressions;
using Xunit;
using Zonkey;
using Zonkey.Dialects;
using Zonkey.ObjectModel;
using Zonkey.ObjectModel.QueryTranslation;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit.QueryTranslation
{
    public class DateMathTests
    {
        private static SqlWhereClause T(Expression<Func<Animal, bool>> e, SqlDialect d = null)
            => TranslationTestHelper.Translate(e, d);

        [Fact]
        public void DateParts_Ansi()
        {
            Assert.Equal("(EXTRACT(YEAR FROM DateOfBirth) = $0)", T(a => a.DateOfBirth.Value.Year == 2020).SqlText);
            Assert.Equal("(EXTRACT(MONTH FROM DateOfBirth) = $0)", T(a => a.DateOfBirth.Value.Month == 6).SqlText);
            Assert.Equal("(EXTRACT(DAY FROM DateOfBirth) = $0)", T(a => a.DateOfBirth.Value.Day == 15).SqlText);
        }

        [Fact]
        public void DateParts_SqlServer_UseDatepart()
        {
            Assert.Equal("(DATEPART(year, [DateOfBirth]) = $0)",
                T(a => a.DateOfBirth.Value.Year == 2020, new SqlServerDialect()).SqlText);
        }

        [Fact]
        public void DateParts_Sqlite_UseStrftime()
        {
            Assert.Equal("(CAST(strftime('%Y', [DateOfBirth]) AS INTEGER) = $0)",
                T(a => a.DateOfBirth.Value.Year == 2020, new SqliteDialect()).SqlText);
        }

        [Fact]
        public void DateProperty_CastsToDate()
        {
            Assert.Equal("(CAST(DateOfBirth AS DATE) = $0)",
                T(a => a.DateOfBirth.Value.Date == new DateTime(2020, 6, 15)).SqlText);
        }

        [Fact]
        public void MathFunctions_Translate()
        {
            Assert.Equal("(ABS(Weight) > $0)", T(a => Math.Abs(a.Weight.Value) > 5m).SqlText);
            Assert.Equal("(FLOOR(Weight) = $0)", T(a => Math.Floor(a.Weight.Value) == 5m).SqlText);
            Assert.Equal("(CEILING(Weight) = $0)", T(a => Math.Ceiling(a.Weight.Value) == 6m).SqlText);
            Assert.Equal("(ROUND(Weight) = $0)", T(a => Math.Round(a.Weight.Value) == 5m).SqlText);
            Assert.Equal("(ROUND(Weight, $0) = $1)", T(a => Math.Round(a.Weight.Value, 1) == 5.5m).SqlText);
        }

        [Fact]
        public void DateTimeNow_FoldsClientSide_NoServerFunction()
        {
            var r = T(a => a.DateOfBirth > DateTime.Now.AddYears(-1));
            Assert.Equal("(DateOfBirth > $0)", r.SqlText);
            Assert.IsType<DateTime>(r.Parameters[0]);
        }

        // T7: Math.Abs overload exactness. All four registered overloads (int/long/decimal/double)
        // resolve to the same "ABS" logical function name; casts around the operand are transparent,
        // so every overload renders the bare column regardless of which Math.Abs signature was bound.
        [Fact]
        public void MathAbs_IntOverload_Translates()
        {
            Assert.Equal("(ABS(SpeciesId) > $0)", T(a => Math.Abs(a.SpeciesId) > 3).SqlText);
        }

        [Fact]
        public void MathAbs_LongOverload_Translates()
        {
            Assert.Equal("(ABS(SpeciesId) > $0)", T(a => Math.Abs((long)a.SpeciesId) > 3L).SqlText);
        }

        [Fact]
        public void MathAbs_DecimalOverload_Translates()
        {
            Assert.Equal("(ABS(Weight) > $0)", T(a => Math.Abs(a.Weight.Value) > 5m).SqlText);
        }

        [Fact]
        public void MathAbs_DoubleOverload_Translates()
        {
            Assert.Equal("(ABS(Weight) > $0)", T(a => Math.Abs((double)a.Weight.Value) > 5.0).SqlText);
        }

        [Fact]
        public void MathRound_OneAndTwoArgOverloads_Translate()
        {
            Assert.Equal("(ROUND(Weight) = $0)", T(a => Math.Round(a.Weight.Value) == 5m).SqlText);
            Assert.Equal("(ROUND(Weight, $0) = $1)", T(a => Math.Round(a.Weight.Value, 2) == 5.55m).SqlText);
        }

        [Fact]
        public void MathRound_MidpointRoundingOverload_IsUnregisteredAndThrows()
        {
            // Math.Round(decimal, MidpointRounding) is a distinct 2-arg overload whose second
            // parameter isn't typeof(int), so the MethodTranslators static registration loop never
            // matches it - it is simply not in the translation table.
            var ex = Assert.Throws<SqlExpressionException>(
                () => T(a => Math.Round(a.Weight.Value, MidpointRounding.AwayFromZero) == 5m));
            Assert.Contains("Round", ex.Message);
        }

        [Fact]
        public void FloorAndCeiling_DecimalAndDouble_Translate()
        {
            Assert.Equal("(FLOOR(Weight) = $0)", T(a => Math.Floor(a.Weight.Value) == 5m).SqlText);
            Assert.Equal("(FLOOR(Weight) = $0)", T(a => Math.Floor((double)a.Weight.Value) == 5.0).SqlText);
            Assert.Equal("(CEILING(Weight) = $0)", T(a => Math.Ceiling(a.Weight.Value) == 6m).SqlText);
            Assert.Equal("(CEILING(Weight) = $0)", T(a => Math.Ceiling((double)a.Weight.Value) == 6.0).SqlText);
        }
    }
}
