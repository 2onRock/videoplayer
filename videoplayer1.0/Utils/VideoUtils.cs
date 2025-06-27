using System;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Web;
using System.Windows;

namespace videoplayer1._0.Utils
{
    public static class VideoUtils
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<(string title, string description)> GetVideoInfo(string url)
        {
            try
            {
                if (url.Contains("youtube.com") || url.Contains("youtu.be"))
                {
                    return await GetYouTubeInfo(url);
                }
                else if (url.Contains("rutube.ru"))
                {
                    return await GetRutubeInfo(url);
                }
                throw new ArgumentException("Unsupported video platform");
            }
            catch (Exception ex)
            {
                // В случае ошибки возвращаем пустые значения, которые пользователь может заполнить вручную
                return ("", "");
            }
        }

        public static string CreateIframe(string url)
        {
            string iframeContent = "";
            
            if (url.Contains("youtube.com") || url.Contains("youtu.be"))
            {
                string videoId = ExtractYouTubeId(url);
                iframeContent = $"<iframe width=\"560\" height=\"315\" src=\"https://www.youtube.com/embed/{videoId}\" frameborder=\"0\" allow=\"accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture\" allowfullscreen></iframe>";
            }
            else if (url.Contains("rutube.ru"))
            {
                var match = Regex.Match(url, @"rutube\.ru/video/([a-zA-Z0-9]+)");
                if (match.Success)
                {
                    string videoId = match.Groups[1].Value;
                    iframeContent = $"<iframe width=\"560\" height=\"315\" src=\"https://rutube.ru/play/embed/{videoId}\" frameborder=\"0\" allow=\"clipboard-write; autoplay; fullscreen\" webkitAllowFullScreen mozallowfullscreen allowFullScreen></iframe>";
                }
            }

            if (string.IsNullOrEmpty(iframeContent))
            {
                throw new ArgumentException("Failed to create iframe for the provided URL");
            }

            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{
            margin: 0;
            padding: 0;
            background-color: #1A1A1A;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
        }}
        iframe {{
            max-width: 100%;
            max-height: 80vh;
            box-shadow: 0 0 10px rgba(0,0,0,0.5);
        }}
    </style>
</head>
<body>
    {iframeContent}
</body>
</html>";
        }

        private static string ExtractYouTubeId(string url)
        {
            string videoId = "";
            
            if (url.Contains("youtu.be/"))
            {
                videoId = url.Split(new[] { "youtu.be/" }, StringSplitOptions.None)[1];
                if (videoId.Contains("?"))
                {
                    videoId = videoId.Split('?')[0];
                }
            }
            else if (url.Contains("youtube.com/watch"))
            {
                var match = Regex.Match(url, @"[?&]v=([^&]+)");
                if (match.Success)
                {
                    videoId = match.Groups[1].Value;
                }
            }
            
            if (string.IsNullOrEmpty(videoId))
                throw new ArgumentException("Invalid YouTube URL");
                
            return videoId;
        }

        private static async Task<(string title, string description)> GetYouTubeInfo(string url)
        {
            try
            {
                string videoId = ExtractYouTubeId(url);

                if (string.IsNullOrEmpty(ApiConfig.YouTubeApiKey))
                {
                    string embedUrl = $"https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v={videoId}&format=json";
                    var embedResponse = await client.GetStringAsync(embedUrl);
                    using (JsonDocument document = JsonDocument.Parse(embedResponse))
                    {
                        var root = document.RootElement;
                        string title = root.GetProperty("title").GetString();
                        return (title, $"Video ID: {videoId}\nTitle: {title}");
                    }
                }
                else
                {
                    // Используем YouTube Data API
                    string apiUrl = $"https://www.googleapis.com/youtube/v3/videos?id={videoId}&key={ApiConfig.YouTubeApiKey}&part=snippet";
                    var response = await client.GetStringAsync(apiUrl);
                    
                    using (JsonDocument document = JsonDocument.Parse(response))
                    {
                        var items = document.RootElement.GetProperty("items");
                        if (items.GetArrayLength() > 0)
                        {
                            var snippet = items[0].GetProperty("snippet");
                            string title = snippet.GetProperty("title").GetString();
                            string description = snippet.GetProperty("description").GetString();
                            return (title, description);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting YouTube info: {ex.Message}");
            }
            return ("", "");
        }

        private static async Task<(string title, string description)> GetRutubeInfo(string url)
        {
            try
            {
                var match = Regex.Match(url, @"rutube\.ru/video/([a-zA-Z0-9]+)");
                if (match.Success)
                {
                    string videoId = match.Groups[1].Value;

                    // Rutube предоставляет публичный API для получения информации о видео
                    string apiUrl = $"https://rutube.ru/api/video/{videoId}/";
                    var response = await client.GetStringAsync(apiUrl);
                    
                    using (JsonDocument document = JsonDocument.Parse(response))
                    {
                        var root = document.RootElement;
                        string title = root.GetProperty("title").GetString();
                        string description = root.GetProperty("description").GetString();
                        
                        if (string.IsNullOrEmpty(description))
                        {
                            description = $"Rutube Video: {title}";
                        }
                        
                        return (title, description);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting Rutube info: {ex.Message}");
            }
            return ("", "");
        }
    }
} 