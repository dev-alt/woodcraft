namespace Woodcraft.Core.Interfaces;

/// <summary>
/// Provides typed access to configuration values loaded from config.lua.
/// Every getter accepts a fallback default — if the key is missing or the file
/// is absent the fallback is returned, so callers never crash.
/// </summary>
public interface IConfigService
{
    double GetDouble(string path, double fallback);
    int GetInt(string path, int fallback);
    string GetString(string path, string fallback);
    bool GetBool(string path, bool fallback);

    /// <summary>
    /// Reads an array-of-tables and maps each row through a callback.
    /// Returns <paramref name="fallback"/> when the path is missing.
    /// </summary>
    List<T> GetList<T>(string path, Func<IConfigTable, T> mapper, List<T>? fallback = null);

    /// <summary>
    /// Reads a Lua array of strings (e.g. { "plywood", "mdf" }).
    /// </summary>
    List<string> GetStringList(string path, List<string>? fallback = null);

    /// <summary>
    /// Reads a Lua table as a string→double dictionary (e.g. material costs).
    /// </summary>
    Dictionary<string, double> GetStringDoubleMap(string path, Dictionary<string, double>? fallback = null);

    /// <summary>
    /// Reads a Lua table as a raw IConfigTable for manual traversal.
    /// </summary>
    IConfigTable? GetTable(string path);
}

/// <summary>
/// Thin wrapper around a Lua table row so mappers don't depend on MoonSharp.
/// </summary>
public interface IConfigTable
{
    double GetDouble(string key, double fallback);
    int GetInt(string key, int fallback);
    string GetString(string key, string fallback);
    bool GetBool(string key, bool fallback);
}
