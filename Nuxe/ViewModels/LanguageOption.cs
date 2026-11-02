using System.Windows;
using System.Windows.Controls;

namespace Nuxe.ViewModels;

/// <summary>One entry of the language selector, e.g. "简体中文" / zh.</summary>
internal class LanguageOption
{
    public string Code { get; }
    public string DisplayName { get; }

    public LanguageOption(string code)
    {
        Code = code;
        DisplayName = Loc.LanguageName(code);
    }
}
