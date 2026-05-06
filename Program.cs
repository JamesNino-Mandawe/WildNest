using System.Diagnostics;
using System.Windows.Forms;
using Project.Accomodations;

namespace Project
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            foreach (var proc in Process.GetProcessesByName(
     System.IO.Path.GetFileNameWithoutExtension(Application.ExecutablePath)))
            {
                if (proc.Id != Environment.ProcessId)
                {
                    proc.Kill();
                    proc.WaitForExit(3000); 
                }
            }

            ApplicationConfiguration.Initialize();

            try
            {
                GuestPassWebServer.Start();
                Application.ApplicationExit += (s, e) => GuestPassWebServer.Stop();
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("Program", ex, "Guest pass server initialization failed during startup.");
            }

            try
            {
                using (var splash = new SplashScreenWeb())
                {
                    Application.Run(splash);
                }
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("Program", ex, "SplashScreenWeb failed during startup.");
            }

            try
            {
                if (Application.OpenForms.Count == 0)
                {
                    Application.Run(new HomePage());
                }
            }
            catch (Exception homeEx)
            {
                ProjectDiagnostics.LogError("Program", homeEx, "HomePage failed during startup.");
                MessageBox.Show(
                    $"WildNest could not open the main program.\r\n\r\n{homeEx.Message}",
                    "Startup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
