using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;

namespace ShootEmUp.Tests.E2E
{
    public class TestRunSummary
    {
        public int TotalTests { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public double ElapsedMilliseconds { get; set; }

        public Dictionary<TestTier, (int passed, int failed)> TierBreakdown { get; } =
            new Dictionary<TestTier, (int passed, int failed)>();

        public List<TestResultItem> Results { get; } = new List<TestResultItem>();
    }

    public class TestResultItem
    {
        public string TestId { get; set; }
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public TestTier Tier { get; set; }
        public FeatureId Feature { get; set; }
        public string Description { get; set; }
        public bool Passed { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public double DurationMs { get; set; }
    }

    public class E2ETestRunner
    {
        public static readonly Type[] AllTestFixtureTypes = new Type[]
        {
            typeof(Tier1_FeatureCoverageTests_Part1),
            typeof(Tier1_FeatureCoverageTests_Part2),
            typeof(Tier2_BoundaryCornerTests_Part1),
            typeof(Tier2_BoundaryCornerTests_Part2),
            typeof(Tier3_CrossFeaturePairwiseTests),
            typeof(Tier4_RealWorldScenarioTests)
        };

        public static TestRunSummary RunAllTests()
        {
            var summary = new TestRunSummary();
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("================================================================================");
            Console.WriteLine("                    GAME BẮN CHIM — E2E TEST SUITE RUNNER                      ");
            Console.WriteLine("================================================================================");

            foreach (var fixtureType in AllTestFixtureTypes)
            {
                RunFixture(fixtureType, summary);
            }

            stopwatch.Stop();
            summary.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

            PrintSummary(summary);
            return summary;
        }

        private static void RunFixture(Type fixtureType, TestRunSummary summary)
        {
            var fixtureAttr = fixtureType.GetCustomAttribute<E2ETestFixtureAttribute>();
            string fixtureName = fixtureAttr != null ? fixtureAttr.Name : fixtureType.Name;
            TestTier defaultTier = fixtureAttr != null ? fixtureAttr.Tier : TestTier.Tier1_FeatureCoverage;

            Console.WriteLine($"\n--- Running: {fixtureName} ---");

            object instance;
            try
            {
                instance = Activator.CreateInstance(fixtureType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL] Failed to instantiate fixture {fixtureType.Name}: {ex.Message}");
                return;
            }

            MethodInfo[] methods = fixtureType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methods)
            {
                var testAttr = method.GetCustomAttribute<E2ETestAttribute>();
                if (testAttr == null) continue;

                summary.TotalTests++;
                TestTier tier = defaultTier;
                var item = new TestResultItem
                {
                    TestId = testAttr.TestId,
                    ClassName = fixtureType.Name,
                    MethodName = method.Name,
                    Tier = tier,
                    Feature = testAttr.Feature,
                    Description = testAttr.Description
                };

                // Guarantee test isolation: reset GameEvents before and after every single test
                GameEvents.ResetAllEvents();

                var testTimer = Stopwatch.StartNew();
                try
                {
                    method.Invoke(instance, null);
                    testTimer.Stop();

                    item.Passed = true;
                    item.DurationMs = testTimer.Elapsed.TotalMilliseconds;
                    summary.PassedCount++;
                    RecordTierResult(summary, tier, true);

                    Console.WriteLine($"  [PASS] {item.TestId,-12} {item.Description} ({item.DurationMs:F2}ms)");
                }
                catch (TargetInvocationException tie)
                {
                    testTimer.Stop();
                    Exception inner = tie.InnerException ?? tie;
                    item.Passed = false;
                    item.DurationMs = testTimer.Elapsed.TotalMilliseconds;
                    item.ErrorMessage = inner.Message;
                    item.StackTrace = inner.StackTrace;
                    summary.FailedCount++;
                    RecordTierResult(summary, tier, false);

                    Console.WriteLine($"  [FAIL] {item.TestId,-12} {item.Description}");
                    Console.WriteLine($"         Error: {inner.Message}");
                }
                catch (Exception ex)
                {
                    testTimer.Stop();
                    item.Passed = false;
                    item.DurationMs = testTimer.Elapsed.TotalMilliseconds;
                    item.ErrorMessage = ex.Message;
                    item.StackTrace = ex.StackTrace;
                    summary.FailedCount++;
                    RecordTierResult(summary, tier, false);

                    Console.WriteLine($"  [FAIL] {item.TestId,-12} {item.Description}");
                    Console.WriteLine($"         Error: {ex.Message}");
                }
                finally
                {
                    GameEvents.ResetAllEvents();
                }

                summary.Results.Add(item);
            }
        }

        private static void RecordTierResult(TestRunSummary summary, TestTier tier, bool passed)
        {
            if (!summary.TierBreakdown.TryGetValue(tier, out var counts))
            {
                counts = (0, 0);
            }

            if (passed) counts.passed++;
            else counts.failed++;

            summary.TierBreakdown[tier] = counts;
        }

        private static void PrintSummary(TestRunSummary summary)
        {
            Console.WriteLine("\n================================================================================");
            Console.WriteLine("                             E2E TEST RUN SUMMARY                               ");
            Console.WriteLine("================================================================================");
            Console.WriteLine($"  Total Tests Executed : {summary.TotalTests}");
            Console.WriteLine($"  Passed               : {summary.PassedCount}");
            Console.WriteLine($"  Failed               : {summary.FailedCount}");
            Console.WriteLine($"  Pass Rate            : {(summary.TotalTests > 0 ? (summary.PassedCount * 100.0 / summary.TotalTests) : 0):F2}%");
            Console.WriteLine($"  Execution Time       : {summary.ElapsedMilliseconds:F2} ms ({summary.ElapsedMilliseconds / 1000.0:F3} seconds)");
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine("  Breakdown by Tier:");
            foreach (var kvp in summary.TierBreakdown)
            {
                Console.WriteLine($"    - {kvp.Key,-30} : Passed {kvp.Value.passed,3} | Failed {kvp.Value.failed,2}");
            }
            Console.WriteLine("================================================================================");

            if (summary.FailedCount == 0)
            {
                Console.WriteLine("  >>> ALL E2E TESTS PASSED SUCCESSFULLY! (100% PASS RATE) <<<");
            }
            else
            {
                Console.WriteLine($"  >>> FAILED: {summary.FailedCount} test(s) did not pass. Check diagnostics above. <<<");
            }
            Console.WriteLine("================================================================================\n");
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/E2E Tests/Run All Tests")]
        public static void RunFromEditorMenu()
        {
            var summary = RunAllTests();
            string msg = $"E2E Tests Complete: {summary.PassedCount}/{summary.TotalTests} passed in {summary.ElapsedMilliseconds:F1}ms.";
            if (summary.FailedCount == 0)
            {
                UnityEngine.Debug.Log($"<color=green><b>[E2E SUCCESS]</b></color> {msg}");
            }
            else
            {
                UnityEngine.Debug.LogError($"<color=red><b>[E2E FAILURE]</b></color> {msg} ({summary.FailedCount} failures)");
            }
        }

        public static void RunAllTestsBatch()
        {
            var summary = RunAllTests();
            UnityEditor.EditorApplication.Exit(summary.FailedCount > 0 ? 1 : 0);
        }
#endif
    }
}
