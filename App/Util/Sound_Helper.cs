using System;
using System.IO;
using System.Runtime.InteropServices;

namespace App
{
    internal static class Sound_Helper
    {
        internal static void Play(string FilePath)
        {
            mciSendString("close all", "", 0, 0);
            mciSendString("open " + FilePath + " alias media", "", 0, 0);
            mciSendString("play media", "", 0, 0);
        }

        internal static void TTS(string text, string localization="zh")
        {
            string tts = System.Web.HttpUtility.UrlEncode(text, System.Text.Encoding.UTF8);
            WebApi.Download($"https://fanyi.baidu.com/gettts?lan={localization}&text={tts}&spd=4&source=web", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Global.APPNAME,"tts_cache", $"{localization}_{tts}.mp3"));
            Play(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Global.APPNAME, "tts_cache", $"{localization}_{tts}.mp3"));
        }
        
        internal static void Pause()
        {
            mciSendString("pause media", "", 0, 0);
        }
        
        internal static void Stop()
        {
            mciSendString("close media", "", 0, 0);
        }
        
        [DllImport("winmm.dll", EntryPoint = "mciSendString", CharSet = CharSet.Auto)]
        private static extern int mciSendString(
         string lpstrCommand,
         string lpstrReturnString,
         int uReturnLength,
         int hwndCallback
        );
    }
}
