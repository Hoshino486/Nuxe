using System.IO;

namespace Nuxe;

internal class RestoreOperation : Operation
{
    private string GameDir { get; }
    private GameConfig GameConfig { get; }

    public RestoreOperation(string gameDir, GameConfig gameConfig)
    {
        Common.AssertDirExists(gameDir, Loc.Get("Err_GameDirNotFound"));
        GameDir = Path.GetFullPath(gameDir);
        GameConfig = gameConfig;
    }

    protected override void Run()
    {
        DeletePaths(Loc.Get("Step_Restore1"));
        RestoreFiles(Loc.Get("Step_Restore2"));
    }

    private void DeletePaths(string step)
    {
        string[] deletePaths = ["_unknown", .. GameConfig.DeletePaths];
        for (int i = 0; i < deletePaths.Length; i++)
        {
            CancellationToken.ThrowIfCancellationRequested();
            string deletePath = deletePaths[i];
            double progress = (double)i / deletePaths.Length;
            Progress.Report(new(progress, Loc.Format("Progress_Path", step, i, deletePaths.Length, deletePath)));

            deletePath = Path.Combine(GameDir, deletePath);
            if (File.Exists(deletePath))
                File.Delete(deletePath);
            else if (Directory.Exists(deletePath))
                Directory.Delete(deletePath, true);
        }
    }

    private void RestoreFiles(string step)
    {
        string backupDir = Path.Combine(GameDir, "_backup");
        if (!Directory.Exists(backupDir))
            return;

        string[] dirs = Directory.GetDirectories(backupDir);
        string[] files = Directory.GetFiles(backupDir);
        int totalPaths = dirs.Length + files.Length;

        for (int i = 0; i < dirs.Length; i++)
        {
            CancellationToken.ThrowIfCancellationRequested();
            string source = dirs[i];
            double progress = (double)i / totalPaths;
            Progress.Report(new(progress, Loc.Format("Progress_Path", step, i, totalPaths, source[backupDir.Length..])));

            string target = Path.Combine(GameDir, source[(backupDir.Length + 1)..]);
            if (Directory.Exists(target))
                Directory.Delete(target, true);
            Directory.Move(source, target);
        }

        for (int i = 0; i < files.Length; i++)
        {
            CancellationToken.ThrowIfCancellationRequested();
            string source = files[i];
            double progress = (double)(i + dirs.Length) / totalPaths;
            Progress.Report(new(progress, Loc.Format("Progress_Path", step, i + dirs.Length, totalPaths, source[backupDir.Length..])));

            string target = Path.Combine(GameDir, source[(backupDir.Length + 1)..]);
            if (File.Exists(target))
                File.Delete(target);
            File.Move(source, target);
        }

        Directory.Delete(backupDir);
    }
}
