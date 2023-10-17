using rUDP.Core.Interfaces;

namespace rUDP.Core;

internal class ConsoleLogger : ILogger
{
    private void Log(string level, string message, ConsoleColor? color = null)
    {
        var oldColor = Console.ForegroundColor;

        if(color is not null)
        {
            Console.ForegroundColor = color.Value;

            Console.WriteLine($"{DateTime.Now.ToString("dd/MMM/yyyy hh:mm:ss")} - [{level}]: {message}");

            Console.ForegroundColor = oldColor;
        }
        else
        {
            Console.WriteLine($"{DateTime.Now.ToString("dd/MMM/yyyy hh:mm:ss")} - [{level}]: {message}");
        }
    }

    public void Debug(string message)
    {
        Log("DEBUG", message);
    }

    public void Error(string message)
    {
        Log("ERROR", message, ConsoleColor.Red);
    }

    public void Info(string message)
    {
        Log("INFO", message);
    }

    public void Warn(string message)
    {
        Log("WARN", message, ConsoleColor.DarkYellow);
    }
}
