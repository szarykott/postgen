using System.Collections.Generic;
using System.IO;

namespace Postgen;

public static class Logger
{
    private static readonly List<string> _logs = new();

    public static void Log(string message)
    {
        _logs.Add(message);
    }

    public static void Flush()
    {
        File.WriteAllLines("logs.g.nocommit.txt", _logs);
    }
}