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
    }
}
