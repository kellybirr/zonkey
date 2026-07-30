using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;
using Zonkey.Dialects;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit.QueryTranslation
{
    // T10: registry concurrency smoke test. MethodTranslators populates its Methods/Members
    // dictionaries once in a static constructor (CLR-guarantees this runs exactly once, thread-safely,
    // before first use), so this test should always be green today. It exists as a tripwire: if a
    // future change moves any of that registration to lazy/on-demand mutation of the dictionaries,
    // concurrent translation across unrelated method families would start throwing or racing, and
    // this test would catch it.
    public class RegistryConcurrencyTests
    {
        private static readonly Func<string>[] Families =
        {
            () => TranslationTestHelper.Translate<Animal>(a => a.Name.StartsWith("Mei")).SqlText,
            () => TranslationTestHelper.Translate<Animal>(a => a.Name.Contains("50%_off")).SqlText,
            () => TranslationTestHelper.Translate<Animal>(a => new[] { 1, 2, 3 }.Contains(a.SpeciesId)).SqlText,
            () => TranslationTestHelper.Translate<Animal>(a => a.DateOfBirth.Value.Year == 2020).SqlText,
            () => TranslationTestHelper.Translate<Animal>(a => Math.Round(a.Weight.Value, 1) == 5.5m).SqlText,
            () => TranslationTestHelper.Translate<Animal>(a => Regex.IsMatch(a.Name, "^M"), new PostgreSqlDialect()).SqlText,
        };

        [Fact]
        public void ConcurrentTranslation_AcrossMethodFamilies_IsStableAndThrowsNothing()
        {
            const int threadCount = 16;
            const int iterations = 5;

            string[] expected = Families.Select(f => f()).ToArray();
            var exceptions = new ConcurrentBag<Exception>();
            var mismatches = new ConcurrentBag<string>();
            var barrier = new Barrier(threadCount);

            // Use real Thread objects (not Parallel.For/ThreadPool): the ThreadPool may not have
            // threadCount workers available immediately, which would stall Barrier.SignalAndWait
            // indefinitely. Explicit threads guarantee all participants exist before the first wait.
            var threads = new Thread[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                int threadIndex = t;
                threads[t] = new Thread(() =>
                {
                    try
                    {
                        int family = threadIndex % Families.Length;
                        for (int i = 0; i < iterations; i++)
                        {
                            barrier.SignalAndWait();
                            string actual = Families[family]();
                            if (actual != expected[family])
                                mismatches.Add($"family {family}, thread {threadIndex}, iter {i}: '{actual}' != '{expected[family]}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            }

            foreach (Thread th in threads) th.Start();
            foreach (Thread th in threads) th.Join();

            Assert.Empty(exceptions);
            Assert.Empty(mismatches);
        }
    }
}
