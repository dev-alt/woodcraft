namespace Woodcraft.Desktop.Services;

/// <summary>
/// Bridge for communicating with the Python MCP server.
/// </summary>
public interface IPythonBridge : IAsyncDisposable
{
    /// <summary>
    /// Whether the bridge is connected to Python.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Event raised when connection status changes.
    /// </summary>
    event EventHandler<bool>? ConnectionChanged;

    /// <summary>
    /// Start the Python server and establish connection.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop the Python server.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Call a tool on the Python server.
    /// </summary>
    /// <typeparam name="T">Expected response type.</typeparam>
    /// <param name="toolName">Name of the tool to call.</param>
    /// <param name="arguments">Arguments for the tool.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tool result.</returns>
    Task<T> CallToolAsync<T>(string toolName, object? arguments = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Call a tool and get the raw JSON response.
    /// </summary>
    Task<string> CallToolRawAsync(string toolName, object? arguments = null, CancellationToken cancellationToken = default);
}
