using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace ContextCare.Domain.Utils;

[McpServerToolType]
public static class Tools
{
    [McpServerTool]

    [Description("A tool that fetches weather information for a given location.")]
    public static string GetWeather([Description("The location to get the weather for.")] string location)
    {
        Console.WriteLine($"{nameof(GetWeather)} invoked. Fetching weather for {location}...");
        // Simulate a tool that fetches weather information
        return $"The current weather in {location} is sunny with a temperature of 25°C.";
    }
}
