using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var runtime = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app");
        var executable = Path.Combine(runtime, "CrimsonDnsManager.App.exe");
        try
        {
            if (!File.Exists(executable)) throw new FileNotFoundException("Der Unterordner app fehlt. Bitte das vollständige Programmpaket verwenden.");
            Process.Start(new ProcessStartInfo(executable) { WorkingDirectory = runtime, UseShellExecute = true });
        }
        catch (Exception)
        {
            MessageBox.Show("DNS Manager konnte nicht gestartet werden. Bitte prüfen, ob der Unterordner app vollständig vorhanden ist.",
                "DNS Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
