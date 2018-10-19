using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace App
{
    internal static class WebApi
    {
        internal static void Tweet(string key, params object[] args)
        {
            Task.Factory.StartNew(() =>
            {
                var message = Localization.GetText(key, args);
                var url = $"{Global.API_ENDPOINT}/tweet?u={Settings.TwitterAccount}&m={HttpUtility.UrlEncode(message)}&h={GetMD5Hash(message)}";

                var resp = Request(url);
                if (resp == null)
                {
                    Log.E("tweet-failed-request");
                }
                else if (resp == "1")
                {
                    Log.E("tweet-failed");
                }
                else if (resp == "0")
                {
                    Log.S("tweet-success");
                }
            });
        }

        internal static string Request(string urlfmt, params object[] args)
        {
            try
            {
                var url = string.Format(urlfmt, args);
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = "DFA";
                request.Timeout = 10000;
                request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                ServicePointManager.DefaultConnectionLimit = 9999;
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    var encoding = Encoding.GetEncoding(65001);

                    using (var responseStream = response.GetResponseStream())
                    using (var reader = new StreamReader(responseStream, encoding))
                        return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Log.Ex(ex, "l-web-request-failed");
            }

            return null;
        }


        internal static string Post(string url, string postData)
        {

            Task<string> ret = Task.Factory.StartNew(() =>
              {
                  try
                  {
                      string result = "";
                      HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                      req.Method = "POST";
                      req.ContentType = "application/json";

                      byte[] data = Encoding.UTF8.GetBytes(postData);
                      req.ContentLength = data.Length;
                      using (Stream reqStream = req.GetRequestStream())
                      {
                          reqStream.Write(data, 0, data.Length);
                          reqStream.Close();
                      }
                      HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                      Stream stream = resp.GetResponseStream();
                      using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                      {
                          result = reader.ReadToEnd();
                      }
                      return result;
                  }
                  catch (Exception ex)
                  {
                      return null;
                  }
              });
            return ret.Result;
        }

        internal static bool Download(string url, string path)
        {
            string tempPath = Path.GetDirectoryName(path) + @"\temp";
            Directory.CreateDirectory(tempPath);  //创建临时文件目录
            string tempFile = tempPath + @"\" + Path.GetFileName(path) + ".temp"; //临时文件
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);    //存在则删除
            }
            try
            {
                FileStream fs = new FileStream(tempFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                // 设置参数
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                //发送请求并获取相应回应数据
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                //直到request.GetResponse()程序才开始向目标网页发送Post请求
                Stream responseStream = response.GetResponseStream();
                //创建本地文件写入流
                //Stream stream = new FileStream(tempFile, FileMode.Create);
                byte[] bArr = new byte[1024];
                int size = responseStream.Read(bArr, 0, (int)bArr.Length);
                while (size > 0)
                {
                    //stream.Write(bArr, 0, size);
                    fs.Write(bArr, 0, size);
                    size = responseStream.Read(bArr, 0, (int)bArr.Length);
                }
                //stream.Close();
                fs.Close();
                responseStream.Close();
                File.Move(tempFile, path);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private static string GetMD5Hash(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            var textToHash = Encoding.UTF8.GetBytes(text);
            byte[] result;
            using (MD5 md5 = new MD5CryptoServiceProvider())
                result = md5.ComputeHash(textToHash);

            return BitConverter.ToString(result).Replace("-", "").ToLower();
        }
    }
}
