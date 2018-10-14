using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace App
{
    internal class Updater
    {
        internal static void CheckNewVersion(MainForm mainForm)
        {
            Task.Factory.StartNew(() =>
            {
                var tempdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Global.APPNAME, Global.UPDATE_TEMP_DIRPATH);
                var batchpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Global.APPNAME, "update.bat");

                if (Directory.Exists(tempdir))
                {
                    Directory.Delete(tempdir, true);
                }
                Directory.CreateDirectory(tempdir);

                var resp = WebApi.Request("http://ffxiv.diemoe.net/DFA/check_update.php");
                if (resp == null)
                {
                    Log.E("l-updater-error-check");
                    return;
                }

                try {
                    var api = JsonConvert.DeserializeObject<dynamic>(resp);

                    var latest = api.tag_name.ToObject<string>();
                    Log.I("l-updater-current-version", Global.VERSION);
                    Log.I("l-updater-latest-version", latest);

                    var ci = new CultureInfo("en-us");
                    if (decimal.Parse(Global.VERSION.Substring(1), ci) >= decimal.Parse(latest.Substring(1), ci))
                    {
                        Log.S("l-updater-is-latest");
                    }
                    else
                    {
                        Log.S("l-updater-new-version");

                        string url = "http://ffxiv.diemoe.net/download/DFAssist?loc=cn";

                        if (url == null)
                        {
                            Log.E("l-updater-error-no-url");
                            return;
                        }

                        mainForm.Invoke(() =>
                        {
                            mainForm.Hide();
                            mainForm.overlayForm.Hide();
                        });

                        Task.Factory.StartNew(() =>
                        {
                            var updaterForm = new UpdaterForm();
                            updaterForm.SetVersion(latest);
                            updaterForm.ShowDialog();
                        });

                        //Sentry.Report("Update started");

                        var exepath = Process.GetCurrentProcess().MainModule.FileName;

                        var stream = GetDownloadStreamByUrl(url);
                        using (var zip = ZipStorer.Open(stream, FileAccess.Read))
                        {
                            var dir = zip.ReadCentralDir();
                            foreach (var entry in dir)
                            {
                                /*
                                if (entry.FilenameInZip == "README.txt")
                                {
                                    continue;
                                }
                                */

                                var filename = entry.FilenameInZip;
                                if (filename == "DFAssist.exe")
                                {
                                    filename = Path.GetFileName(exepath);
                                }

                                zip.ExtractFile(entry, Path.Combine(tempdir, filename));
                            }
                        }

                        var currentdir = Path.GetDirectoryName(exepath);

                        File.WriteAllText(batchpath,
                            "@echo off\r\n" +
                            "title DFAssist Updater\r\n" +
                            "echo Updating DFAssist...\r\n" +
                            "ping 127.0.0.1 -n 3 > nul\r\n" +
                            $"move /y \"{tempdir}*\" \"{currentdir}\" > nul\r\n" +
                            $"\"{exepath}\"\r\n" + 
                            "echo Running DFAssist...\r\n",
                        Encoding.Default);

                        var si = new ProcessStartInfo();
                        si.FileName = batchpath;
                        si.CreateNoWindow = true;
                        si.UseShellExecute = false;
                        si.WindowStyle = ProcessWindowStyle.Hidden;

                        Process.Start(si);
                        Settings.Updated = true;
                        Settings.Save();
                        Application.Exit();
                    }
                }
                catch (Exception ex)
                {
                    Log.Ex(ex, "l-updater-error-general");
                }

                try
                {
                    var json = WebApi.Request($"http://ffxiv.diemoe.net/DFA/Data/{Settings.Language}.json");
                    Data.Fill(json);
                }
                catch (Exception ex)
                {
                    Log.Ex(ex, "l-updater-error-data");
                }
            });
        }

        private static Stream GetDownloadStreamByUrl(string url)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = "DFA";
                request.Timeout = 20000;

                var response = (HttpWebResponse)request.GetResponse();
                var stream = new MemoryStream();

                response.GetResponseStream().CopyTo(stream);
                return stream;
            }
            catch (Exception ex)
            {
                Log.Ex(ex, "l-updater-error-downloading");
            }

            return null;
        }
    }
}
