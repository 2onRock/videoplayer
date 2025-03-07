using Npgsql;

namespace videoplayer1._0.Database
{
    public class DatabaseConnection
    {
        private static string ConnectionString = "Host=metro.proxy.rlwy.net;Port=35455;Database=railway;Username=postgres;Password=fyoKQNoCGyItrexFxTKvgKxIHLvrRGOd";

        public static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(ConnectionString);
        }
    }
} 