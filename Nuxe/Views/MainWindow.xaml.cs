using Nuxe.ViewModels;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Threading;

namespace Nuxe.Views;

public partial class MainWindow : Window
{
    private MainViewModel State { get; set; }
    private IProgress<OperationProgress> OperationProgress { get; }
    private CancellationTokenSource OperationCancellation { get; set; }
    private OperationProgress LastProgress { get; set; }
    private DispatcherTimer ProgressTimer { get; }
    private bool LanguageReady { get; set; }

    public MainWindow()
    {
        InitializeComponent();

        OperationProgress = new Progress<OperationProgress>(r => LastProgress = r);
        LastProgress = new(0, "");
        ProgressTimer = new() { Interval = TimeSpan.FromSeconds(1.0 / 30) };
        ProgressTimer.Tick += (sender, e) =>
        {
            var progress = LastProgress;
            ProgressBar.Value = progress.Value * 100;
            TextBoxStatus.Text = progress.Message;
            TaskbarItemInfo.ProgressValue = progress.Value;
        };
        ProgressTimer.Start();

        ComboBoxLanguage.ItemsSource = new[]
        {
            new LanguageOption(Loc.Chinese),
            new LanguageOption(Loc.English),
        };
        ComboBoxLanguage.SelectedItem = null;
        ComboBoxLanguage.ItemsSource = new[]
        {
            new LanguageOption(Loc.Chinese),
            new LanguageOption(Loc.English),
        };
        ComboBoxLanguage.SelectedItem = ((IEnumerable<LanguageOption>)ComboBoxLanguage.ItemsSource).First(option => option.Code == Loc.Current);
        LanguageReady = true;
    }

    internal void SelectLanguage(string code)
    {
        string normalized = Loc.Normalize(code);
        if (normalized == Loc.Current)
            return;

        Loc.SetLanguage(normalized);
        Loc.PersistLanguage();

        LanguageReady = false;
        var options = new[]
        {
            new LanguageOption(Loc.Chinese),
            new LanguageOption(Loc.English),
        };
        ComboBoxLanguage.ItemsSource = options;
        ComboBoxLanguage.SelectedItem = options.First(option => option.Code == Loc.Current);
        LanguageReady = true;
    }

    private void ComboBoxLanguage_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!LanguageReady || ComboBoxLanguage.SelectedItem is not LanguageOption option)
            return;

        SelectLanguage(option.Code);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            State = new();
            DataContext = State;
        }
        catch (Exception ex)
        {
            Common.DisplayError(ex);
            Close();
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        ProgressTimer.Stop();
        State.Save();
    }

    private void ButtonAbort_Click(object sender, RoutedEventArgs e)
    {
        ButtonAbort.IsEnabled = false;
        OperationCancellation.Cancel();
    }

    private async void ButtonBasicUnpack_Click(object sender, RoutedEventArgs e)
    {
        await RunOperation("Verb_Unpack", () =>
        {
            string gameDir = Path.GetDirectoryName(State.GameExe);
            var gameConfig = GameConfig.DetectGameConfig(State.GameConfigs, gameDir);
            return new UnpackOperation(State.ResDir, gameDir, gameConfig, null, null, false, false);
        });
    }

    private async void ButtonAdvancedUnpack_Click(object sender, RoutedEventArgs e)
    {
        await RunOperation("Verb_Unpack", () =>
        {
            if (State.SelectedGameConfig == null)
                throw new FriendlyException(Loc.Get("Err_ManualGameRequired"));

            string unpackDir = State.UseUnpackDir ? State.UnpackDir : null;
            string unpackFilter = State.UseUnpackFilter ? State.UnpackFilter : null;
            return new UnpackOperation(State.ResDir, State.GameDir, State.SelectedGameConfig, unpackDir, unpackFilter, State.UnpackOverwrite, true);
        });
    }

    private async void ButtonBasicPatch_Click(object sender, RoutedEventArgs e)
    {
        await RunOperation("Verb_Patch", () =>
        {
            string gameDir = Path.GetDirectoryName(State.GameExe);
            var gameConfig = GameConfig.DetectGameConfig(State.GameConfigs, gameDir);
            return new PatchOperation(State.GameExe, gameConfig, null);
        });
    }

    private async void ButtonAdvancedPatch_Click(object sender, RoutedEventArgs e)
    {
        await RunOperation("Verb_Patch", () =>
        {
            if (State.SelectedGameConfig == null)
                throw new FriendlyException(Loc.Get("Err_ManualGameRequired"));

            string outputPath = State.UsePatchOutputPath ? State.PatchOutputPath : null;
            return new PatchOperation(State.GameExe, State.SelectedGameConfig, outputPath);
        });
    }

    private async void ButtonBasicRestore_Click(object sender, RoutedEventArgs e)
    {
        await RunOperation("Verb_Restore", () =>
        {
            string gameDir = Path.GetDirectoryName(State.GameExe);
            var gameConfig = GameConfig.DetectGameConfig(State.GameConfigs, gameDir);
            return new RestoreOperation(gameDir, gameConfig);
        });
    }

    private async void ButtonAdvancedRestore_Click(object sender, RoutedEventArgs e)
    {
        await RunOperation("Verb_Restore", () =>
        {
            if (State.SelectedGameConfig == null)
                throw new FriendlyException(Loc.Get("Err_ManualGameRequired"));

            return new RestoreOperation(State.GameDir, State.SelectedGameConfig);
        });
    }

    private async void ButtonAdvancedDecrypt_Click(object sender, RoutedEventArgs e)
    {
        await RunOperation("Verb_Decrypt", () =>
        {
            if (State.SelectedGameConfig == null)
                throw new FriendlyException(Loc.Get("Err_ManualGameRequired"));

            return new DecryptOperation(State.ResDir, State.GameDir, State.SelectedGameConfig);
        });
    }

    private async Task RunOperation(string verbKey, Func<Operation> createOperation)
    {
        // Keep this up here so it doesn't dispose before the Abort button is disabled
        using var ctSource = new CancellationTokenSource();
        OperationCancellation = ctSource;

        TabControlSettings.IsEnabled = false;
        ButtonAbort.IsEnabled = true;
        try
        {
            var sw = new Stopwatch();
            sw.Start();

            await Task.Run(() => createOperation().Run(OperationProgress, OperationCancellation.Token));

            sw.Stop();
            var elapsed = sw.Elapsed + TimeSpan.FromSeconds(1); // :3c
            OperationProgress.Report(new(1, Loc.Format("Status_Completed", Loc.Get(verbKey), Loc.Elapsed(elapsed))));
            SystemSounds.Beep.Play();
        }
        catch (OperationCanceledException)
        {
            OperationProgress.Report(new(0, Loc.Format("Status_Aborted", Loc.Get(verbKey))));
        }
        catch (Exception ex)
        {
            OperationProgress.Report(new(0, Loc.Format("Status_Failed", Loc.Get(verbKey))));
            Common.DisplayError(ex);
        }
        TabControlSettings.IsEnabled = true;
        ButtonAbort.IsEnabled = false;
    }
}
