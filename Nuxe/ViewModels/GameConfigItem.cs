using CommunityToolkit.Mvvm.ComponentModel;

namespace Nuxe.ViewModels;

/// <summary>
/// Wrapper around a <see cref="GameConfig"/> so the displayed name can follow the active language.
/// </summary>
internal partial class GameConfigItem : ObservableObject
{
    public GameConfig Config { get; }

    public string DisplayName => Loc.GameName(Config.Name);

    /// <summary>Read by WPF automation/UI tooling and screen readers.</summary>
    public override string ToString() => DisplayName;

    public GameConfigItem(GameConfig config)
    {
        Config = config;
    }

    public void RefreshName() => OnPropertyChanged(nameof(DisplayName));
}
