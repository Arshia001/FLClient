using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class Screenshot
{
    static string FindFileName()
    {
        var dir = new DirectoryInfo("Screenshots");
        if (!dir.Exists)
            dir.Create();

        for (int i = 0; ; ++i)
        {
            var name = Path.Combine(dir.FullName, $"{i:000}.png");
            if (!File.Exists(name))
                return name;
        }
    }

    [MenuItem("Edit/Take screenshot")]
    public static void TakeScreenshot()
    {
        ScreenCapture.CaptureScreenshot(FindFileName(), 4);
    }
}
