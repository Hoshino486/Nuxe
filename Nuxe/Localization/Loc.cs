using System.Globalization;
using System.IO;
using System.Windows;

namespace Nuxe;

/// <summary>
/// Runtime localization. Strings live in Localization/Strings.zh.xaml (Chinese, default)
/// and Localization/Strings.en.xaml (English) and are looked up through
/// DynamicResource so switching the language updates the live UI.
/// </summary>
internal static class Loc
{
    public const string Chinese = "zh";
    public const string English = "en";

    private const string ChineseDictionary = "Localization/Strings.zh.xaml";
    private const string EnglishDictionary = "Localization/Strings.en.xaml";

    private static readonly string LanguageFilePath = Path.Combine(AppContext.BaseDirectory, "Nuxe.language");

    private static ResourceDictionary Strings;
    private static ResourceDictionary Installed;
    private static string CurrentLanguage = Chinese;

    public static event Action LanguageChanged;

    public static string Current => CurrentLanguage;
    public static bool IsChinese => CurrentLanguage == Chinese;

    /// <summary>Loads the startup language; NUXE_LANG overrides the persisted setting.</summary>
    public static void Initialize(string persisted)
    {
        string resolved = Environment.GetEnvironmentVariable("NUXE_LANG");
        if (string.IsNullOrWhiteSpace(resolved))
            resolved = ReadLanguageFile();
        if (string.IsNullOrWhiteSpace(resolved))
            resolved = persisted;
        Apply(resolved, false);
    }

    public static void SetLanguage(string language)
    {
        string normalized = Normalize(language);
        if (normalized == CurrentLanguage && Strings != null)
            return;
        Apply(normalized, true);
    }

    /// <summary>Persists the active language; storage failures never interrupt the UI.</summary>
    public static void PersistLanguage()
    {
        try
        {
            Nuxe.Properties.Settings.Default.Language = CurrentLanguage;
            Nuxe.Properties.Settings.Default.Save();
        }
        catch
        {
            // Portable or read-only installations fall through to the side-car file below.
        }

        try
        {
            File.WriteAllText(LanguageFilePath, CurrentLanguage);
        }
        catch
        {
            // No writable location at all: the choice stays for this session only.
        }
    }

    private static string ReadLanguageFile()
    {
        try
        {
            return File.Exists(LanguageFilePath) ? File.ReadAllText(LanguageFilePath).Trim() : null;
        }
        catch
        {
            return null;
        }
    }

    public static string Normalize(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return Chinese;
        return language.Trim().ToLowerInvariant() switch
        {
            "en" or "en-us" or "en-gb" or "english" => English,
            _ => Chinese,
        };
    }

    private static void Apply(string language, bool notify)
    {
        CurrentLanguage = Normalize(language);
        string source = CurrentLanguage == English ? EnglishDictionary : ChineseDictionary;

        var loaded = new ResourceDictionary { Source = new Uri(source, UriKind.Relative) };

        var app = Application.Current;
        if (Installed == null)
            Installed = new ResourceDictionary();

        if (app != null && !app.Resources.MergedDictionaries.Contains(Installed))
            app.Resources.MergedDictionaries.Add(Installed);

        // Mutating the live dictionary in place is what makes DynamicResource
        // consumers pick up the new strings without restarting the application.
        Installed.Clear();
        foreach (object key in loaded.Keys)
            Installed[key] = loaded[key];

        Strings = Installed;

        if (notify)
            LanguageChanged?.Invoke();
    }

    private static bool IsLocalizationDictionary(ResourceDictionary dictionary)
    {
        if (dictionary.Source == null)
            return false;
        string source = dictionary.Source.OriginalString;
        return source.Contains("Localization/Strings", StringComparison.OrdinalIgnoreCase);
    }

    public static string Get(string key)
    {
        if (Strings != null && Strings.Contains(key) && Strings[key] is string text)
            return text;

        var app = Application.Current;
        if (app != null && app.Resources.Contains(key) && app.Resources[key] is string appText)
            return appText;

        return key;
    }

    public static string Format(string key, params object[] args)
    {
        string format = Get(key);
        try
        {
            return string.Format(CultureInfo.CurrentCulture, format, args);
        }
        catch (FormatException)
        {
            return format;
        }
    }

    /// <summary>
    /// Renders an elapsed duration without relying on TimeSpan format strings.
    /// TimeSpan custom formats require every literal character to be escaped, which is
    /// easy to break in translated resources, so the units are assembled by hand.
    /// </summary>
    public static string Elapsed(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
            elapsed = TimeSpan.Zero;

        long totalSeconds = (long)elapsed.TotalSeconds;
        long hours = totalSeconds / 3600;
        long minutes = totalSeconds % 3600 / 60;
        long seconds = totalSeconds % 60;

        string hourUnit = Get("Time_Hour");
        string minuteUnit = Get("Time_Minute");
        string secondUnit = Get("Time_Second");

        var builder = new System.Text.StringBuilder();
        if (hours > 0)
            builder.Append(hours).Append(hourUnit).Append(minutes).Append(minuteUnit).Append(seconds).Append(secondUnit);
        else if (minutes > 0)
            builder.Append(minutes).Append(minuteUnit).Append(seconds).Append(secondUnit);
        else
            builder.Append(seconds).Append(secondUnit);

        return builder.ToString();
    }

    public static string LanguageName(string language) =>
        Get(Normalize(language) == English ? "Language_en" : "Language_zh");

    private static readonly Dictionary<string, string> GameNames = new()
    {
        ["Armored Core V"] = "装甲核心 V",
        ["Armored Core Verdict Day"] = "装甲核心 审判日",
        ["Armored Core VI"] = "装甲核心 VI 境界天火",
        ["Dark Souls"] = "黑暗之魂",
        ["Dark Souls Remastered"] = "黑暗之魂 重制版",
        ["Dark Souls II"] = "黑暗之魂 II",
        ["Dark Souls II Scholar of the First Sin"] = "黑暗之魂 II 原罪学者",
        ["Dark Souls III"] = "黑暗之魂 III",
        ["Elden Ring"] = "艾尔登法环",
        ["Elden Ring Nightreign"] = "艾尔登法环 黑夜君临",
        ["Sekiro"] = "只狼：影逝二度",
        ["Sekiro Digital Artwork & Mini Soundtrack"] = "只狼 数字艺术集与迷你原声带",
        ["Steel Battalion Heavy Armor"] = "铁骑 重装甲",
    };

    private static readonly Dictionary<char, char> FullWidthParens = new()
    {
        ['('] = '（',
        [')'] = '）',
    };

    /// <summary>Localizes the display name of a game config, e.g. "Dark Souls III (PC)" -> "黑暗之魂 III（PC）".</summary>
    public static string GameName(string name)
    {
        if (!IsChinese || string.IsNullOrEmpty(name))
            return name;

        string title = name;
        string platform = null;

        int open = name.LastIndexOf('(');
        if (open > 0 && name.EndsWith(')'))
        {
            title = name[..open].TrimEnd();
            platform = name[(open + 1)..^1];
        }

        if (GameNames.TryGetValue(title, out string localizedTitle))
            title = localizedTitle;

        return platform == null ? title : $"{title}（{platform}）";
    }

    /// <summary>Converts ASCII parentheses to full-width ones for Chinese UI text.</summary>
    public static string LocalizeParens(string text)
    {
        if (!IsChinese || string.IsNullOrEmpty(text))
            return text;

        var builder = new System.Text.StringBuilder(text.Length);
        foreach (char c in text)
            builder.Append(FullWidthParens.TryGetValue(c, out char localized) ? localized : c);
        return builder.ToString();
    }
}
