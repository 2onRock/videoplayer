namespace videoplayer1._0.Utils
{
    public static class ApiConfig
    {
        public static string YouTubeApiKey { get; set; }
        public static string VKApiToken { get; set; }
        public static string RutubeApiKey { get; set; }

        public static void Initialize(string youtubeApiKey, string vkToken = "", string rutubeKey = "")
        {
            YouTubeApiKey = youtubeApiKey;
            VKApiToken = vkToken;
            RutubeApiKey = rutubeKey;
        }
    }
} 