using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class PersianTextShaperHelperWindow : EditorWindow
{
    static class WindowsClipboard
    {
        public static void SetText(string text)
        {
            OpenClipboard();

            EmptyClipboard();
            IntPtr hGlobal = default;
            try
            {
                var bytes = (text.Length + 1) * 2;
                hGlobal = Marshal.AllocHGlobal(bytes);

                if (hGlobal == default)
                {
                    ThrowWin32();
                }

                var target = GlobalLock(hGlobal);

                if (target == default)
                {
                    ThrowWin32();
                }

                try
                {
                    Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                }
                finally
                {
                    GlobalUnlock(target);
                }

                if (SetClipboardData(cfUnicodeText, hGlobal) == default)
                {
                    ThrowWin32();
                }

                hGlobal = default;
            }
            finally
            {
                if (hGlobal != default)
                {
                    Marshal.FreeHGlobal(hGlobal);
                }

                CloseClipboard();
            }
        }

        public static void OpenClipboard()
        {
            var num = 10;
            while (true)
            {
                if (OpenClipboard(default))
                {
                    break;
                }

                if (--num == 0)
                {
                    ThrowWin32();
                }

                Thread.Sleep(100);
            }
        }

        const uint cfUnicodeText = 13;

        static void ThrowWin32()
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);

        [DllImport("user32.dll")]
        static extern bool EmptyClipboard();
    }

    string RawText;
    string ShapedText;
    bool rightToLeftRender;

    [MenuItem("Window/Persian Text Shaper")]
    public static void ShowWindow()
    {
        GetWindow(typeof(PersianTextShaperHelperWindow));
    }

    void OnGUI()
    {
        if (string.IsNullOrEmpty(RawText))
        {
            ShapedText = "";
        }
        else
        {
            bool Rtl = true;
            foreach (var ch in RawText)
            {
                switch (PersianTextShaper.PersianTextShaper.GetCharType(ch))
                {
                    case PersianTextShaper.PersianTextShaper.CharType.LTR:
                        Rtl = false;
                        break;
                    case PersianTextShaper.PersianTextShaper.CharType.RTL:
                        break;
                    default:
                        continue;
                }
                break;
            }
            ShapedText = PersianTextShaper.PersianTextShaper.ShapeText(RawText, Rtl, rightToLeftRenderDirection: rightToLeftRender);
        }

        GUILayout.Label("Input (Not Fixed)", EditorStyles.boldLabel);
        RawText = EditorGUILayout.TextArea(RawText);

        GUILayout.Label("Output (Fixed)", EditorStyles.boldLabel);
        ShapedText = EditorGUILayout.TextArea(ShapedText);

        GUILayout.Label("Right to left render direction?", EditorStyles.boldLabel);
        rightToLeftRender = EditorGUILayout.Toggle(rightToLeftRender);

        if (GUILayout.Button("Copy"))
        {
            WindowsClipboard.SetText(ShapedText);
        }
    }
}