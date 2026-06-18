using System;
using System.IO;
using SmartEmergencyRoutePlanner.ViewModels;

namespace SmartEmergencyRoutePlanner;

public partial class Home
{
    private void ClearConsole()
    {
        ConsoleLogs.Clear();
    }

    private void LogSystem(string message) => Log(message, "system");
    private void LogSuccess(string message) => Log(message, "success");
    private void LogWarning(string message) => Log(message, "warning");
    private void LogError(string message) => Log(message, "danger");

    private void Log(string message, string cssClass)
    {
        ConsoleLogs.Insert(0, new LogEntry { Text = $"[{DateTime.Now:HH:mm:ss}] {message}", Class = cssClass });
        if (ConsoleLogs.Count > 100)
        {
            ConsoleLogs.RemoveAt(ConsoleLogs.Count - 1);
        }
    }

    private void RunCorrectnessTests()
    {
        LogSystem("[SISTEM] Memulai pengetesan keakuratan algoritma (Correctness Test Suite)...");
        var sw = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(sw);
            _ = SmartEmergencyRoutePlanner.Tests.AlgorithmCorrectnessTests.RunSuite();

            string testOutput = sw.ToString();
            var lines = testOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.Contains("[PASSED]") || line.Contains("SUCCESS"))
                {
                    LogSuccess(line);
                }
                else if (line.Contains("[FAILED]") || line.Contains("FAILED!"))
                {
                    LogError(line);
                }
                else
                {
                    LogSystem(line);
                }
            }
        }
        catch (Exception ex)
        {
            LogError("[ERROR] Gagal menjalankan suite tes: " + ex.Message);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
