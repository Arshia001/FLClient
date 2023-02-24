using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class LatinIDGenerator
{
    static HashSet<char> allowedChars = new HashSet<char>()
        {
            '-',
            '_',
            '.',
            ',',
            '!',
            '?',
            '(',
            ')',
        };

    static char ConvertChar(char ch)
    {
        if (ch >= 'a' && ch <= 'z' ||
            ch >= 'A' && ch <= 'Z' ||
            ch >= '0' && ch <= '9' ||
            allowedChars.Contains(ch)
            )
            return ch;

        switch (ch)
        {
            case 'ا':
                return 'a';
            case 'آ':
                return 'a';
            case 'ئ':
                return 'a';
            case 'ب':
                return 'b';
            case 'پ':
                return 'p';
            case 'ت':
                return 't';
            case 'ث':
                return 's';
            case 'ج':
                return 'j';
            case 'چ':
                return 'C';
            case 'ح':
                return 'h';
            case 'خ':
                return 'K';
            case 'د':
                return 'd';
            case 'ذ':
                return 'z';
            case 'ر':
                return 'r';
            case 'ز':
                return 'z';
            case 'ژ':
                return 'Z';
            case 'س':
                return 's';
            case 'ش':
                return 'S';
            case 'ص':
                return 's';
            case 'ض':
                return 'z';
            case 'ط':
                return 't';
            case 'ظ':
                return 'z';
            case 'ع':
                return 'a';
            case 'غ':
                return 'G';
            case 'ف':
                return 'f';
            case 'ق':
                return 'G';
            case 'ک':
                return 'k';
            case 'گ':
                return 'g';
            case 'ل':
                return 'l';
            case 'م':
                return 'm';
            case 'ن':
                return 'n';
            case 'و':
                return 'v';
            case 'ه':
                return 'h';
            case 'ی':
                return 'y';
            case '۰':
                return '0';
            case '۱':
                return '1';
            case '۲':
                return '2';
            case '۳':
                return '3';
            case '۴':
                return '4';
            case '۵':
                return '5';
            case '۶':
                return '6';
            case '۷':
                return '7';
            case '۸':
                return '8';
            case '۹':
                return '9';
            case ' ':
                return '_';
            default:
                return '?';
        }
    }

    public static string ToLatinIdentifier(string word) =>
        new string(word.Select(ch => ConvertChar(ch)).ToArray());
}
