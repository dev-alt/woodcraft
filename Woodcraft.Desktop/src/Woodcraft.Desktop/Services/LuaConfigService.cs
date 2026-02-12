using Microsoft.Extensions.Logging;
using MoonSharp.Interpreter;
using Woodcraft.Core.Interfaces;

namespace Woodcraft.Desktop.Services;

/// <summary>
/// MoonSharp-based implementation of <see cref="IConfigService"/>.
/// Loads config.lua at construction with a sandboxed script environment.
/// All getters silently return fallback defaults on any failure.
/// </summary>
public sealed class LuaConfigService : IConfigService
{
    private readonly Script? _script;
    private readonly ILogger<LuaConfigService>? _logger;

    public LuaConfigService(ILogger<LuaConfigService>? logger = null)
    {
        _logger = logger;

        try
        {
            // Look for config.lua in base directory or Assets subdirectory
            var configPath = Path.Combine(AppContext.BaseDirectory, "config.lua");
            if (!File.Exists(configPath))
                configPath = Path.Combine(AppContext.BaseDirectory, "Assets", "config.lua");
            if (!File.Exists(configPath))
            {
                _logger?.LogInformation("config.lua not found, using all fallback defaults");
                return;
            }

            _script = new Script(CoreModules.TableIterators | CoreModules.Basic | CoreModules.String | CoreModules.Math);
            _script.DoFile(configPath);
            _logger?.LogInformation("Loaded config.lua from {Path}", configPath);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load config.lua, using all fallback defaults");
            _script = null;
        }
    }

    public double GetDouble(string path, double fallback)
    {
        try
        {
            var val = Navigate(path);
            return val is { Type: DataType.Number } ? val.Number : fallback;
        }
        catch { return fallback; }
    }

    public int GetInt(string path, int fallback)
    {
        try
        {
            var val = Navigate(path);
            return val is { Type: DataType.Number } ? (int)val.Number : fallback;
        }
        catch { return fallback; }
    }

    public string GetString(string path, string fallback)
    {
        try
        {
            var val = Navigate(path);
            return val is { Type: DataType.String } ? val.String : fallback;
        }
        catch { return fallback; }
    }

    public bool GetBool(string path, bool fallback)
    {
        try
        {
            var val = Navigate(path);
            return val is { Type: DataType.Boolean } ? val.Boolean : fallback;
        }
        catch { return fallback; }
    }

    public List<T> GetList<T>(string path, Func<IConfigTable, T> mapper, List<T>? fallback = null)
    {
        fallback ??= [];
        try
        {
            var val = Navigate(path);
            if (val is not { Type: DataType.Table }) return fallback;

            var result = new List<T>();
            var table = val.Table;
            foreach (var pair in table.Pairs)
            {
                if (pair.Value.Type == DataType.Table)
                    result.Add(mapper(new LuaConfigTable(pair.Value.Table)));
            }
            return result.Count > 0 ? result : fallback;
        }
        catch { return fallback; }
    }

    public List<string> GetStringList(string path, List<string>? fallback = null)
    {
        fallback ??= [];
        try
        {
            var val = Navigate(path);
            if (val is not { Type: DataType.Table }) return fallback;

            var result = new List<string>();
            foreach (var pair in val.Table.Pairs)
            {
                if (pair.Value.Type == DataType.String)
                    result.Add(pair.Value.String);
            }
            return result.Count > 0 ? result : fallback;
        }
        catch { return fallback; }
    }

    public Dictionary<string, double> GetStringDoubleMap(string path, Dictionary<string, double>? fallback = null)
    {
        fallback ??= new Dictionary<string, double>();
        try
        {
            var val = Navigate(path);
            if (val is not { Type: DataType.Table }) return fallback;

            var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in val.Table.Pairs)
            {
                if (pair.Key.Type == DataType.String && pair.Value.Type == DataType.Number)
                    result[pair.Key.String] = pair.Value.Number;
            }
            return result.Count > 0 ? result : fallback;
        }
        catch { return fallback; }
    }

    public IConfigTable? GetTable(string path)
    {
        try
        {
            var val = Navigate(path);
            return val is { Type: DataType.Table } ? new LuaConfigTable(val.Table) : null;
        }
        catch { return null; }
    }

    /// <summary>
    /// Navigates a dot-separated path (e.g. "materials.cost_per_bf") through nested Lua tables.
    /// </summary>
    private DynValue? Navigate(string path)
    {
        if (_script == null) return null;

        var parts = path.Split('.');
        DynValue current = _script.Globals.Get(parts[0]);

        for (int i = 1; i < parts.Length; i++)
        {
            if (current.Type != DataType.Table) return null;
            current = current.Table.Get(parts[i]);
        }

        return current.Type != DataType.Nil ? current : null;
    }
}
