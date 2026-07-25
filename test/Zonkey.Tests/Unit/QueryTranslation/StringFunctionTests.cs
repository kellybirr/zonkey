using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using Xunit;
using Zonkey;
using Zonkey.Dialects;
using Zonkey.ObjectModel;
using Zonkey.ObjectModel.QueryTranslation;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit.QueryTranslation
{
    public class StringFunctionTests
    {
        private static SqlWhereClause T(Expression<Func<Animal, bool>> e, SqlDialect d = null)
            => TranslationTestHelper.Translate(e, d);

        [Fact]
        public void ToUpper_OnColumn_RendersUpper()
        {
            Assert.Equal("(UPPER(Name) = $0)", T(a => a.Name.ToUpper() == "MEI MEI").SqlText);
            Assert.Equal("(LOWER(Name) = $0)", T(a => a.Name.ToLower() == "mei mei").SqlText);
        }

        [Fact]
        public void ToUpper_OnCapturedValue_FoldsClientSide()
        {
            string name = "mei mei";
            var r = T(a => a.Name == name.ToUpper());
            Assert.Equal("(Name = $0)", r.SqlText);
            Assert.Equal("MEI MEI", r.Parameters[0]);
        }

        [Fact]
        public void Trim_Length_Replace()
        {
            Assert.Equal("(TRIM(Name) = $0)", T(a => a.Name.Trim() == "x").SqlText);
            Assert.Equal("(LENGTH(Name) > $0)", T(a => a.Name.Length > 3).SqlText);
            Assert.Equal("(REPLACE(Name, $0, $1) = $2)", T(a => a.Name.Replace("a", "b") == "x").SqlText);
        }

        [Fact]
        public void Length_OnSqlServer_RendersLen()
        {
            Assert.Equal("(LEN([Name]) > $0)", T(a => a.Name.Length > 3, new SqlServerDialect()).SqlText);
        }

        [Fact]
        public void Substring_BothOverloads_Are1Based()
        {
            Assert.Equal("(SUBSTRING(Name FROM $0 FOR $1) = $2)", T(a => a.Name.Substring(0, 3) == "Mei").SqlText);
            var r = T(a => a.Name.Substring(0, 3) == "Mei");
            Assert.Equal(new object[] { 1, 3, "Mei" }, r.Parameters);   // 0-based C# start becomes 1-based SQL
            Assert.Equal("(SUBSTRING(Name FROM $0) = $1)", T(a => a.Name.Substring(2) == "i").SqlText);
        }

        [Fact]
        public void IndexOf_IsZeroBased()
        {
            var r = T(a => a.Name.IndexOf("Mei") == 0);
            Assert.Equal("((POSITION($0 IN Name) - 1) = $1)", r.SqlText);
        }

        [Fact]
        public void IsNullOrEmpty_Translates()
        {
            Assert.Equal("(Notes IS NULL OR Notes = '')", T(a => string.IsNullOrEmpty(a.Notes)).SqlText);
            Assert.Equal("(NOT (Notes IS NULL OR Notes = ''))", T(a => !string.IsNullOrEmpty(a.Notes)).SqlText);
        }

        [Fact]
        public void StringEquals_InstanceAndStatic()
        {
            Assert.Equal("(Name = $0)", T(a => a.Name.Equals("x")).SqlText);
            Assert.Equal("(Name = $0)", T(a => string.Equals(a.Name, "x")).SqlText);
        }
    }
}
