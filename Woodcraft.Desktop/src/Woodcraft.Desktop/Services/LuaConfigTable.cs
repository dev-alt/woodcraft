using MoonSharp.Interpreter;
using Woodcraft.Core.Interfaces;

namespace Woodcraft.Desktop.Services;

/// <summary>
/// Wraps a MoonSharp <see cref="Table"/> to implement <see cref="IConfigTable"/>.
/// </summary>
public sealed class LuaConfigTable : IConfigTable
{
    private readonly Table _table;

    public LuaConfigTable(Table table)
    {
        _table = table;
    }

    internal Table Raw => _table;

    public double GetDouble(string key, double fallback)
    {
        try
        {
            var val = _table.Get(key);
            return val.Type == DataType.Number ? val.Number : fallback;
        }
        catch { return fallback; }
    }

    public int GetInt(string key, int fallback)
    {
        try
        {
            var val = _table.Get(key);
            return val.Type == DataType.Number ? (int)val.Number : fallback;
        }
        catch { return fallback; }
    }

    public string GetString(string key, string fallback)
    {
        try
        {
            var val = _table.Get(key);
            return val.Type == DataType.String ? val.String : fallback;
        }
        catch { return fallback; }
    }

    public bool GetBool(string key, bool fallback)
    {
        try
        {
            var val = _table.Get(key);
            return val.Type == DataType.Boolean ? val.Boolean : fallback;
        }
        catch { return fallback; }
    }
}
