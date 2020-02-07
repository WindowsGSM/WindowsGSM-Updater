using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WindowsGSM_Updater
{
    /// <summary>
    /// A small console program to update WindowsGSM
    /// </summary>
    static class Program
    {
        private static string _wgsmPath;

        static void Main(string[] args)
        {
            _wgsmPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "WindowsGSM.exe"));

            if (!File.Exists(_wgsmPath))
            {
                Console.WriteLine($"WindowsGSM.exe not found in ({_wgsmPath})");
                Console.ReadLine();
                Environment.Exit(-1);
            }

            #region Check Launch Param
            bool autostart = false, forceupdate = false;

            foreach (string arg in args)
            {
                if (arg == "-autostart")
                {
                    autostart = true;
                }
                else if (arg == "-forceupdate")
                {
                    forceupdate = true;
                }
            }
            #endregion

            #region Compare Version
            if (!forceupdate)
            {
                Console.WriteLine("Local version:");
                Console.ForegroundColor = ConsoleColor.Green;
                string localVersion = GetLocalVersion();
                Console.WriteLine(localVersion);
                Console.ResetColor();

                Console.WriteLine("Latest version:");
                Console.ForegroundColor = ConsoleColor.Green;
                string latestVersion = GetLatestVersion();
                Console.WriteLine(latestVersion);
                Console.ResetColor();

                Console.WriteLine();

                if (localVersion == latestVersion)
                {
                    Console.WriteLine("WindowsGSM is up to date.");
                    Console.ReadLine();
                    Environment.Exit(0);
                }
                else
                {
                    Console.Write($"{latestVersion} is available, do you want to update WindowsGSM? [Y/n]");
                    Console.Out.Flush();
                    var responce = Console.ReadLine();

                    if (!string.IsNullOrEmpty(responce) && responce.Trim().ToUpperInvariant() != "Y")
                    {
                        Environment.Exit(-1);
                    }
                }
            }
            #endregion

            #region Delete and Download

            if (forceupdate)
            {
                Console.WriteLine();
                Console.WriteLine("Waiting WindowsGSM.exe exit...");

                while (File.Exists(_wgsmPath))
                {
                    try
                    {
                        File.Delete(_wgsmPath);
                    }
                    catch
                    {
                        //ignore
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("Deleting WindowsGSM.exe...");
            DeleteWindowsGSM().Wait();

            if (File.Exists(_wgsmPath))
            {
                Console.WriteLine("Fail to delete WindowsGSM.exe. Reason: File In Use");
                Console.ReadLine();
                Environment.Exit(-1);
            }

            Console.WriteLine("Downloading WindowsGSM.exe...");
            DownloadWindowsGSM().Wait();

            if (!File.Exists(_wgsmPath))
            {
                Console.WriteLine("Fail to download WindowsGSM.exe");
                Console.ReadLine();
                Environment.Exit(-1);
            }
            #endregion

            #region Update End + Action
            Console.WriteLine();
            Console.WriteLine("WindowsGSM.exe updated successfully.");

            if (autostart)
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = _wgsmPath,
                    Verb = "runas"
                };

                Process.Start(psi);
            }
            else
            {
                Console.ReadLine();
            }

            Environment.Exit(0);
            #endregion
        }

        private static string GetLocalVersion()
        {
            string version = FileVersionInfo.GetVersionInfo(_wgsmPath).ProductVersion.ToString();
            return $"v{version.Substring(0, version.Length - 2)}";
        }

        private static string GetLatestVersion()
        {
            if (WebRequest.Create("https://api.github.com/repos/WindowsGSM/WindowsGSM/releases/latest") is HttpWebRequest webRequest)
            {
                webRequest.Method = "GET";
                webRequest.UserAgent = "Anything";
                webRequest.ServicePoint.Expect100Continue = false;

                try
                {
                    using (var responseReader = new StreamReader(webRequest.GetResponse().GetResponseStream()))
                    {
                        string json = responseReader.ReadToEnd();
                        Regex regex = new Regex("\"tag_name\":\"(.*?)\"");
                        var matches = regex.Matches(json);

                        if (matches.Count == 1 && matches[0].Groups.Count == 2)
                        {
                            return matches[0].Groups[1].Value;
                        }
                    }
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private static async Task<bool> DeleteWindowsGSM()
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        if (File.Exists(_wgsmPath))
                        {
                            File.Delete(_wgsmPath);

                            break;
                        }
                    }
                    catch
                    {
                        //ignore
                    }

                    Task.Delay(500);
                }
            });

            return File.Exists(_wgsmPath);
        }

        private static async Task<bool> DownloadWindowsGSM()
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync("https://github.com/BattlefieldDuck/WindowsGSM/releases/latest/download/WindowsGSM.exe", _wgsmPath);
                }
                
                return true;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error {e}");
                Console.ResetColor();

                return false;
            }
        }
    }
}
