using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShootEmUp.Tests.E2E
{
    public class E2EAssertionException : Exception
    {
        public E2EAssertionException(string message) : base(message) { }
    }

    /// <summary>
    /// High-precision, zero-allocation assertion engine for E2E testing.
    /// Provides explicit expected vs. actual diagnostics with stack traces.
    /// </summary>
    public static class E2EAssert
    {
        public static void AreEqual<T>(T expected, T actual, string message = "")
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                string msg = string.IsNullOrEmpty(message) ? "" : $" Context: {message}.";
                throw new E2EAssertionException($"Assertion Failed. Expected: <{expected}>, Actual: <{actual}>.{msg}");
            }
        }

        public static void AreNotEqual<T>(T notExpected, T actual, string message = "")
        {
            if (EqualityComparer<T>.Default.Equals(notExpected, actual))
            {
                string msg = string.IsNullOrEmpty(message) ? "" : $" Context: {message}.";
                throw new E2EAssertionException($"Assertion Failed. Expected NOT equal to: <{notExpected}>, but Actual was identical.{msg}");
            }
        }

        public static void AreApproximatelyEqual(float expected, float actual, float tolerance = 0.001f, string message = "")
        {
            if (Mathf.Abs(expected - actual) > tolerance)
            {
                string msg = string.IsNullOrEmpty(message) ? "" : $" Context: {message}.";
                throw new E2EAssertionException($"Assertion Failed. Expected approx: <{expected}> (+/- {tolerance}), Actual: <{actual}> (Diff: {Mathf.Abs(expected - actual)}).{msg}");
            }
        }

        public static void AreApproximatelyEqual(Vector2 expected, Vector2 actual, float tolerance = 0.001f, string message = "")
        {
            if (Vector2.Distance(expected, actual) > tolerance)
            {
                string msg = string.IsNullOrEmpty(message) ? "" : $" Context: {message}.";
                throw new E2EAssertionException($"Assertion Failed. Expected approx Vector2: <{expected}>, Actual: <{actual}> (Dist: {Vector2.Distance(expected, actual)} > {tolerance}).{msg}");
            }
        }

        public static void AreApproximatelyEqual(Vector3 expected, Vector3 actual, float tolerance = 0.001f, string message = "")
        {
            if (Vector3.Distance(expected, actual) > tolerance)
            {
                string msg = string.IsNullOrEmpty(message) ? "" : $" Context: {message}.";
                throw new E2EAssertionException($"Assertion Failed. Expected approx Vector3: <{expected}>, Actual: <{actual}> (Dist: {Vector3.Distance(expected, actual)} > {tolerance}).{msg}");
            }
        }

        public static void IsTrue(bool condition, string message = "")
        {
            if (!condition)
            {
                string msg = string.IsNullOrEmpty(message) ? "" : $" Context: {message}.";
                throw new E2EAssertionException($"Assertion Failed. Expected: True, Actual: False.{msg}");
            }
        }

        public static void IsFalse(bool condition, string message = "")
        {
            if (condition)
            {
                string msg = string.IsNullOrEmpty(message) ? "" : $" Context: {message}.";
                throw new E2EAssertionException($"Assertion Failed. Expected: False, Actual: True.{msg}");
            }
        }

        public static void GreaterThan(float actual, float threshold, string message = "")
        {
            if (actual <= threshold)
            {
                string msg = string.IsNullOrEmpty(message) ? "" : $" Context: {message}.";
                throw new E2EAssertionException($"Assertion Failed. Expected <{actual}> to be strictly greater than <{threshold}>.{msg}");
            }
        }

        public static void GreaterThanOrEqual(float actual, float threshold, string message = "")
        {
            if (actual < threshold)
            {
                string msg = string.IsNullOrEmpty(message) ? "" : $" Context: {message}.";
                throw new E2EAssertionException($"Assertion Failed. Expected <{actual}> to be >= <{threshold}>.{msg}");
            }
        }

        public static void LessThan(float actual, float threshold, string message = "")
        {
            if (actual >= threshold)
            {
                string msg = string.IsNullOrEmpty(message) ? "" : $" Context: {message}.";
                throw new E2EAssertionException($"Assertion Failed. Expected <{actual}> to be strictly less than <{threshold}>.{msg}");
            }
        }

        public static void LessThanOrEqual(float actual, float threshold, string message = "")
        {
            if (actual > threshold)
            {
                string msg = string.IsNullOrEmpty(message) ? "" : $" Context: {message}.";
                throw new E2EAssertionException($"Assertion Failed. Expected <{actual}> to be <= <{threshold}>.{msg}");
            }
        }

        public static void InRange(float actual, float min, float max, string message = "")
        {
            if (actual < min || actual > max)
            {
                string msg = string.IsNullOrEmpty(message) ? "" : $" Context: {message}.";
                throw new E2EAssertionException($"Assertion Failed. Expected <{actual}> to be in range [{min}, {max}].{msg}");
            }
        }

        public static void NotNull(object obj, string message = "")
        {
            if (obj == null || (obj is UnityEngine.Object uObj && uObj == null))
            {
                string msg = string.IsNullOrEmpty(message) ? "" : $" Context: {message}.";
                throw new E2EAssertionException($"Assertion Failed. Expected NOT null, but was null.{msg}");
            }
        }

        public static void IsNull(object obj, string message = "")
        {
            if (obj != null && (!(obj is UnityEngine.Object uObj) || uObj != null))
            {
                string msg = string.IsNullOrEmpty(message) ? "" : $" Context: {message}.";
                throw new E2EAssertionException($"Assertion Failed. Expected null, but was <{obj}>.{msg}");
            }
        }

        public static void Throws<TException>(Action action, string message = "") where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return; // Expected exception caught
            }
            catch (Exception ex)
            {
                string msg = string.IsNullOrEmpty(message) ? "" : $" Context: {message}.";
                throw new E2EAssertionException($"Assertion Failed. Expected exception <{typeof(TException).Name}>, but threw <{ex.GetType().Name}>: {ex.Message}.{msg}");
            }

            string noExMsg = string.IsNullOrEmpty(message) ? "" : $" Context: {message}.";
            throw new E2EAssertionException($"Assertion Failed. Expected exception <{typeof(TException).Name}>, but no exception was thrown.{noExMsg}");
        }
    }
}
