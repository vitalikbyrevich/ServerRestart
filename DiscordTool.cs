using System.IO;
using System.Net;

namespace DiscordTools
{
    static class DiscordTool
    {
        public static void SendMessageToDiscord(string url, string name, string message)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                var data = $"{{\"username\":\"{name}\",\"content\":\"{message}\"}}";
                streamWriter.Write(data);
            }
            httpWebRequest.GetResponse();
        }
    }
}
