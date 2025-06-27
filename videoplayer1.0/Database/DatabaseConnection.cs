using Npgsql;

namespace videoplayer1._0.Database
{
    public class DatabaseConnection
    {
        private static string ConnectionString = "Host=ballast.proxy.rlwy.net;Port=43701;Database=railway;Username=postgres;Password=BuuEdOQvXRKjsgnTcYleqqxUtRZBiYNZ";

        public static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(ConnectionString);
        }
    }
} 