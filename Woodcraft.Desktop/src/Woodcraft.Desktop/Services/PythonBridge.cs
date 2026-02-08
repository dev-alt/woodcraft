using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Woodcraft.Desktop.Services;

/// <summary>
/// Manages communication with the Python MCP server via subprocess.
/// </summary>
public class PythonBridge : IPythonBridge
{
    private readonly ILogger<PythonBridge> _logger;
    private readonly string _pythonPath;
    private readonly string _serverModule;
    private readonly JsonSerializerOptions _jsonOptions;

    private Process? _process;
    private StreamWriter? _stdin;
    private StreamReader? _stdout;
    private int _requestId;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private bool _disposed;

    public bool IsConnected => _process != null && !_process.HasExited;

    public event EventHandler<bool>? ConnectionChanged;

    public PythonBridge(ILogger<PythonBridge> logger, string? pythonPath = null, string? serverModule = null)
    {
        _logger = logger;
        _pythonPath = pythonPath ?? "python3";
        _serverModule = serverModule ?? "woodcraft.server";

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            _logger.LogWarning("Python bridge already connected");
            return;
        }

        _logger.LogInformation("Starting Python MCP server...");

        var startInfo = new ProcessStartInfo
        {
            FileName = _pythonPath,
            Arguments = $"-m {_serverModule}",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardInputEncoding = Encoding.UTF8
        };

        // Set working directory to the Python package location
        var pythonPackagePath = FindPythonPackage();
        if (!string.IsNullOrEmpty(pythonPackagePath))
        {
            startInfo.WorkingDirectory = pythonPackagePath;
            // Add to PYTHONPATH
            var currentPath = Environment.GetEnvironmentVariable("PYTHONPATH") ?? "";
            startInfo.EnvironmentVariables["PYTHONPATH"] = $"{pythonPackagePath}:{currentPath}";
        }

        try
        {
            _process = Process.Start(startInfo);
            if (_process == null)
                throw new InvalidOperationException("Failed to start Python process");

            _stdin = _process.StandardInput;
            _stdout = _process.StandardOutput;

            // Start reading stderr for logging
            _ = Task.Run(() => ReadStderrAsync(_process), cancellationToken);

            // Wait a bit for the server to initialize
            await Task.Delay(500, cancellationToken);

            if (_process.HasExited)
            {
                var exitCode = _process.ExitCode;
                throw new InvalidOperationException($"Python process exited immediately with code {exitCode}");
            }

            _logger.LogInformation("Python MCP server started successfully");
            ConnectionChanged?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Python MCP server");
            await StopAsync();
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (_process == null) return;

        _logger.LogInformation("Stopping Python MCP server...");

        try
        {
            if (!_process.HasExited)
            {
                _stdin?.Close();
                await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));

                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error stopping Python process");
        }
        finally
        {
            _process?.Dispose();
            _process = null;
            _stdin = null;
            _stdout = null;
            ConnectionChanged?.Invoke(this, false);
        }
    }

    public async Task<T> CallToolAsync<T>(string toolName, object? arguments = null, CancellationToken cancellationToken = default)
    {
        var json = await CallToolRawAsync(toolName, arguments, cancellationToken);
        return JsonSerializer.Deserialize<T>(json, _jsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize response for tool '{toolName}'");
    }

    public async Task<string> CallToolRawAsync(string toolName, object? arguments = null, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Python bridge is not connected");

        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            var requestId = Interlocked.Increment(ref _requestId);

            // Build JSON-RPC request
            var request = new JsonRpcRequest
            {
                JsonRpc = "2.0",
                Id = requestId,
                Method = "tools/call",
                Params = new ToolCallParams
                {
                    Name = toolName,
                    Arguments = arguments != null
                        ? JsonSerializer.SerializeToElement(arguments, _jsonOptions)
                        : null
                }
            };

            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            _logger.LogDebug("Sending request: {Request}", requestJson);

            // Send request
            if (_stdin == null || _stdout == null)
                throw new InvalidOperationException("Python bridge is not connected. Streams are null.");

            await _stdin.WriteLineAsync(requestJson);
            await _stdin.FlushAsync();

            // Read response
            var responseJson = await _stdout.ReadLineAsync(cancellationToken)
                ?? throw new InvalidOperationException("No response from Python server");

            _logger.LogDebug("Received response: {Response}", responseJson);

            // Parse response
            var response = JsonSerializer.Deserialize<JsonRpcResponse>(responseJson, _jsonOptions);
            if (response == null)
                throw new InvalidOperationException("Failed to parse JSON-RPC response");

            if (response.Error != null)
                throw new PythonBridgeException(response.Error.Message, response.Error.Code);

            // Extract the result - MCP returns content array
            if (response.Result.TryGetProperty("content", out var content) &&
                content.GetArrayLength() > 0 &&
                content[0].TryGetProperty("text", out var text))
            {
                return text.GetString() ?? "{}";
            }

            return response.Result.GetRawText();
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task ReadStderrAsync(Process process)
    {
        try
        {
            while (!process.HasExited)
            {
                var line = await process.StandardError.ReadLineAsync();
                if (!string.IsNullOrEmpty(line))
                {
                    _logger.LogWarning("[Python] {Message}", line);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Stderr reading ended");
        }
    }

    private string? FindPythonPackage()
    {
        // Try to find the woodcraft Python package
        var searchPaths = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "python"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python"),
            Path.Combine(Environment.CurrentDirectory, "python"),
            Path.Combine(Environment.CurrentDirectory, "..", "src")
        };

        foreach (var path in searchPaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath))
            {
                _logger.LogDebug("Found Python package at: {Path}", fullPath);
                return fullPath;
            }
        }

        _logger.LogWarning("Could not find Python package directory");
        return null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await StopAsync();
        _sendLock.Dispose();

        GC.SuppressFinalize(this);
    }

    // JSON-RPC types
    private class JsonRpcRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("params")]
        public object? Params { get; set; }
    }

    private class ToolCallParams
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("arguments")]
        public JsonElement? Arguments { get; set; }
    }

    private class JsonRpcResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("result")]
        public JsonElement Result { get; set; }

        [JsonPropertyName("error")]
        public JsonRpcError? Error { get; set; }
    }

    private class JsonRpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}

/// <summary>
/// Exception thrown when Python bridge encounters an error.
/// </summary>
public class PythonBridgeException : Exception
{
    public int ErrorCode { get; }

    public PythonBridgeException(string message, int errorCode = 0)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
