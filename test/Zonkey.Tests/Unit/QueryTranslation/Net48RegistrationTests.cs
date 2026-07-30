#if NETFRAMEWORK
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Xunit;
using Zonkey.Dialects;
using Zonkey.ObjectModel;
using Zonkey.ObjectModel.QueryTranslation;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit.QueryTranslation
{
    // MethodTranslators registers a handful of modern-.NET-only overloads (the
    // Contains(string, StringComparison) overload, and the C# 14 first-class-span Contains
    // overloads used for array.Contains) using `str.GetMethod(...)`/reflection lookups that
    // return null on net48; MethodTranslators.Register is null-tolerant so the static
    // constructor doesn't throw, it just ends up with fewer registrations. These tests run
    // ONLY on net48 (this whole file compiles empty under net10) and pin that: the
    // reflection lookup really is null here, the type initializer survives it, and the
    // plain-overload / Enumerable.Contains translation paths still work as a fallback.
    public class Net48RegistrationTests
    {
        private static SqlWhereClause T(Expression<Func<Animal, bool>> e, SqlDialect d = null)
            => TranslationTestHelper.Translate(e, d);

        [Fact]
        public void StringComparisonContainsOverload_DoesNotExistOnNet48()
        {
            var method = typeof(string).GetMethod("Contains", new[] { typeof(string), typeof(StringComparison) });
            Assert.Null(method);
        }

        [Fact]
        public void MethodTranslators_StaticInitialization_DidNotThrow()
        {
            // If the static constructor had thrown (e.g. NullReferenceException from registering
            // a null MethodInfo), every translation would fail with a TypeInitializationException.
            // Simply translating anything proves the type initializer completed successfully.
            var r = T(a => a.SpeciesId == 1);
            Assert.Equal("(SpeciesId = $0)", r.SqlText);
        }

        [Fact]
        public void PlainContainsOverload_StillTranslates()
        {
            // The plain Contains(string) overload is registered unconditionally and still works
            // even though the StringComparison overload above couldn't be registered.
            var r = T(a => a.Name.Contains("Mei"));
            Assert.Equal("(Name LIKE $0)", r.SqlText);
            Assert.Equal("%Mei%", r.Parameters[0]);
        }

        [Fact]
        public void ArrayContains_TranslatesViaEnumerableContains_NotSpan()
        {
            // On net48 there's no MemoryExtensions.Contains(ReadOnlySpan<T>, T) registration (that
            // block is #if !NETFRAMEWORK in MethodTranslators), so array.Contains(...) must bind to
            // and translate through the ordinary static Enumerable.Contains<T> registration instead.
            var ids = new[] { 1, 2, 3 };
            var r = T(a => ids.Contains(a.SpeciesId));
            Assert.Equal("(SpeciesId IN ($0,$1,$2))", r.SqlText);
            Assert.Equal(new object[] { 1, 2, 3 }, r.Parameters);
        }

        [Fact]
        public void RegexIsMatch_OnPostgres_StillTranslates()
        {
            var r = T(a => Regex.IsMatch(a.Name, "^Mei"), new PostgreSqlDialect());
            Assert.Equal("(Name ~ $0)", r.SqlText);
            Assert.Equal(new object[] { "^Mei" }, r.Parameters);
        }

        [Fact]
        public void DatePartsAndMath_StillTranslate()
        {
            Assert.Equal("(EXTRACT(YEAR FROM DateOfBirth) = $0)", T(a => a.DateOfBirth.Value.Year == 2020).SqlText);
            Assert.Equal("(ABS(Weight) > $0)", T(a => Math.Abs(a.Weight.Value) > 5m).SqlText);
        }
    }
}
#endif

#if !NETFRAMEWORK
namespace Zonkey.Tests.Unit.QueryTranslation
{
    using System;
    using Xunit;
    using Zonkey.Tests.Models;

    // Mirror guard for the net48 test above: on modern TFMs the Contains(string, StringComparison)
    // overload DOES exist and IS registered, so it must actually translate (not just resolve).
    public class Net48RegistrationMirrorTests
    {
        [Fact]
        public void StringComparisonContainsOverload_TranslatesOnModernTfms()
        {
            var r = TranslationTestHelper.Translate<Animal>(a => a.Name.Contains("mei", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("(UPPER(Name) LIKE UPPER($0))", r.SqlText);
            Assert.Equal("%mei%", r.Parameters[0]);
        }
    }
}
#endif
