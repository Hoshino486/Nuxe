using Coremats;
using Nuxe.ViewModels;
using Nuxe.Views;
using System.IO;
using System.Media;

namespace Nuxe;

internal static class Common
{
    public static void DisplayError(Exception ex)
    {
        SystemSounds.Hand.Play();
        WriteErrorLog(ex);

        ErrorViewModel vm;
        if (ex is FriendlyException)
            vm = new(ex.Message, ex.InnerException?.ToString());
        else
            vm = new(Loc.Get("Err_Unexpected"), ex.ToString());
        new ErrorWindow() { DataContext = vm }.ShowDialog();
    }

    /// <summary>Appends the error to error.log next to the executable so failures stay diagnosable.</summary>
    private static void WriteErrorLog(Exception ex)
    {
        try
        {
            string logPath = Path.Combine(AppContext.BaseDirectory, "error.log");
            string text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}{Environment.NewLine}{Environment.NewLine}";
            File.AppendAllText(logPath, text);
        }
        catch
        {
            // Logging must never mask the original failure.
        }
    }

    public static void AssertDirExists(string dir, string message)
    {
        if (!Directory.Exists(dir))
            throw new FriendlyException($"{message}{Loc.Format("Err_PathSuffix", dir)}");
    }

    public static void AssertFileExists(string path, string message)
    {
        if (!File.Exists(path))
            throw new FriendlyException($"{message}{Loc.Format("Err_PathSuffix", path)}");
    }

    public static byte[] DecryptBinderHeader(string bhdPath, BinderKeysReader binderKeys, bool expectPems, byte[] bytes)
    {
        string key;
        if (expectPems)
        {
            string dir = Path.GetDirectoryName(bhdPath);
            string keyFile = Path.GetFileName(bhdPath).Replace("Ebl.bhd", "KeyCode.pem");
            string keyPath = Path.Combine(dir, keyFile);
            AssertFileExists(keyPath, Loc.Get("Err_EncryptionKeyNotFoundSteam"));
            key = File.ReadAllText(keyPath);
        }
        else
        {
            key = binderKeys.ReadKey(bhdPath);
        }
        return Coremats.Crypto.Binder5.DecryptBhd(bytes, key);
    }

    public static BHD5 ReadBinderHeader(string bhdPath, BHD5.Bhd5Format bhdFormat, BinderKeysReader binderKeys, bool expectPems)
    {
        byte[] bytes = File.ReadAllBytes(bhdPath);
        if (!BHD5.Is(bytes))
            bytes = DecryptBinderHeader(bhdPath, binderKeys, expectPems, bytes);
        return BHD5.Read(bytes, bhdFormat);
    }
}
