namespace ScaffoldVision.Tests;

/// <summary>
/// Minimal test runner. Reports pass/fail per test and returns a non-zero
/// exit code if any test fails so CI can pick up failures.
/// </summary>
public class TestRunner
{
    private int _passed;
    private int _failed;

    public void Run(string name, Action testCase)
    {
        try
        {
            testCase();
            _passed++;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("  PASS ");
            Console.ResetColor();
            Console.WriteLine(name);
        }
        catch (Exception ex)
        {
            _failed++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("  FAIL ");
            Console.ResetColor();
            Console.WriteLine($"{name}");
            Console.WriteLine($"       {ex.Message}");
        }
    }

    public int Summarize()
    {
        Console.WriteLine();
        Console.WriteLine($"  Passed: {_passed}");
        if (_failed > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  Failed: {_failed}");
            Console.ResetColor();
            return 1;
        }
        Console.WriteLine("  All tests passed.");
        return 0;
    }
}

/// <summary>
/// Minimal assertion helpers. Throws on failure with a descriptive message.
/// </summary>
public static class Assert
{
    public static void True(bool condition, string message = "expected true")
    {
        if (!condition) throw new AssertionException(message);
    }

    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new AssertionException($"expected {expected}, got {actual}");
    }

    public static void Throws<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new AssertionException(
                $"expected {typeof(TException).Name}, got {ex.GetType().Name}: {ex.Message}");
        }
        throw new AssertionException($"expected {typeof(TException).Name}, no exception thrown");
    }
}

public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}
