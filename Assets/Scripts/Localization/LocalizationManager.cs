using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Culture
{
    fa_IR,
    en_US
}

public interface ICultureUtils
{
    bool IsRightToLeft { get; }

    string SimplifyWordForMatching(string originalWord);
    string TransformWordForDisplay(string originalWord);
    char TransformCharForDisplayInGrid(char ch);
    char[] GetAllChars();
}

class PersianCultureUtils : ICultureUtils
{
    static char[] chars = new[] { 'ا', 'ب', 'پ', 'ت', 'ث', 'ج', 'چ', 'ح', 'خ', 'د', 'ذ', 'ر', 'ز', 'ژ', 'س', 'ش', 'ص', 'ض', 'ط', 'ظ', 'ع', 'غ', 'ف', 'ق', 'ک', 'گ', 'ل', 'م', 'ن', 'و', 'ه', 'ی' };

    public bool IsRightToLeft => true;

    public char[] GetAllChars()
    {
        return chars;
    }

    static char? GetSimplifiedChar(char ch)
    {
        switch (ch)
        {
            case 'آ':
            case 'أ':
            case 'إ':
                return 'ا';
            case 'ؤ':
                return 'و';
            case 'ئ':
            case 'ء':
                return 'ی';
            case 'ة':
                return 'ه';
            case 'ك':
                return 'ک';
            case 'ي':
                return 'ی';
            case 'ا':
            case 'ب':
            case 'پ':
            case 'ت':
            case 'ث':
            case 'ج':
            case 'چ':
            case 'ح':
            case 'خ':
            case 'د':
            case 'ذ':
            case 'ر':
            case 'ز':
            case 'ژ':
            case 'س':
            case 'ش':
            case 'ص':
            case 'ض':
            case 'ط':
            case 'ظ':
            case 'ع':
            case 'غ':
            case 'ف':
            case 'ق':
            case 'ک':
            case 'گ':
            case 'ل':
            case 'م':
            case 'ن':
            case 'و':
            case 'ه':
            case 'ی':
                return ch;

            default:
                return null;
        }
    }

    public string SimplifyWordForMatching(string originalWord)
    {
        return new string(originalWord
            .Select(ch => GetSimplifiedChar(ch))
            .Where(ch => ch != null)
            .Select(ch => ch.Value)
            .ToArray());
    }

    public char TransformCharForDisplayInGrid(char ch)
    {
        if (ch == 'ه')
            return 'ﻫ';

        return ch;
    }

    public string TransformWordForDisplay(string originalWord)
    {
        return originalWord;
    }
}

class EnglishCultureUtils : ICultureUtils
{
    static char[] chars = new[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };

    public bool IsRightToLeft => false;

    public char[] GetAllChars()
    {
        return chars;
    }

    public string SimplifyWordForMatching(string originalWord)
    {
        return originalWord.ToLower();
    }

    public char TransformCharForDisplayInGrid(char ch)
    {
        if (char.IsLower(ch))
            return char.ToUpper(ch);

        return ch;
    }

    public string TransformWordForDisplay(string originalWord)
    {
        if (char.IsLower(originalWord[0]))
            return char.ToUpper(originalWord[0]) + originalWord.Substring(1);

        return originalWord;
    }
}

public class LocalizationManager : SingletonBehaviour<LocalizationManager>
{
    [Serializable]
    public class FontReplacement
    {
        public Font Original;
        public Font Replacement;
    }

    [Serializable]
    public class TmpFontReplacement
    {
        public TMP_FontAsset Original;
        public TMP_FontAsset Replacement;
    }

    [Serializable]
    public class LocalizationFontInfo
    {
        public Culture Culture;
        public FontReplacement[] Replacements;
        public TmpFontReplacement[] TmpReplacements;
    }


#if en_US
    public const string CurrentCultureName = "en-US";
#else
    public const string CurrentCultureName = "fa-IR";
#endif


    public static Culture CurrentCulture => CurrentCultureName == "fa-IR" ? Culture.fa_IR : Culture.en_US;

    public static ICultureUtils CultureUtils { get; }
#if en_US
        = new EnglishCultureUtils();
#else
        = new PersianCultureUtils();
#endif

    public static bool IsRightToLeft => CurrentCulture == Culture.fa_IR; //??


    public LocalizationFontInfo[] Fonts;


    protected override bool IsGlobal => true;

    Dictionary<string, string> entries = new Dictionary<string, string>();

    Dictionary<Font, Font> fontReplacements;
    Dictionary<TMP_FontAsset, TMP_FontAsset> tmpFontReplacements;


    protected override void Awake()
    {
        base.Awake();

        var asset = Resources.Load<TextAsset>("Localization/" + CurrentCultureName + "/Localization");

        if (asset != null)
        {
            using (var reader = new StringReader(asset.text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var idx = line.IndexOf(':');
                    if (idx < 0)
                        continue;

                    entries.Add(line.Substring(0, idx).ToLower(), line.Substring(idx + 1).Replace("\\n", "\n"));
                }
            }

            Resources.UnloadAsset(asset);
        }

        var info = Fonts.FirstOrDefault(i => i.Culture == CurrentCulture);
        if (info != null)
        {
            fontReplacements = info.Replacements.ToDictionary(r => r.Original, r => r.Replacement);
            tmpFontReplacements = info.TmpReplacements.ToDictionary(r => r.Original, r => r.Replacement);
        }
        else
        {
            fontReplacements = new Dictionary<Font, Font>();
            tmpFontReplacements = new Dictionary<TMP_FontAsset, TMP_FontAsset>();
        }
    }

    public string Translate(string key)
    {
        return string.Concat(key.Split('|').Select(k => TranslateSingle(k)));
    }

    string TranslateSingle(string key)
    {
        if (key.StartsWith(":"))
            return key.Substring(1);

        string result;
        if (entries.TryGetValue(key.ToLower(), out result))
            return result;

        return key;
    }

    public Font GetReplacementFont(Font font)
    {
        Font result;
        if (fontReplacements.TryGetValue(font, out result))
            return result;

        return font;
    }

    public TMP_FontAsset GetReplacementFont(TMP_FontAsset font)
    {
        TMP_FontAsset result;
        if (tmpFontReplacements.TryGetValue(font, out result))
            return result;

        return font;
    }
}
