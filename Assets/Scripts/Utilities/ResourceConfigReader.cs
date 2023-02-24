using System;
using System.Collections.Generic;

public static class ResourceConfigReader
{
    public static Dictionary<string, string> Read(string text)
    {
        var result = new Dictionary<string, string>();

        foreach (var line in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var split = line.Split(new[] { '=' }, 2);
            if (split.Length != 2)
                throw new Exception("Invalid resource config entry " + line);

            result.Add(split[0].Trim(), split[1].Trim());
        }

        return result;
    }
}
