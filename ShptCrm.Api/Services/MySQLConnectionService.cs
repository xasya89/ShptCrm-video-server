using MySql.Data.MySqlClient;

namespace ShptCrm.Api.Services
{
    public class MySQLConnectionService
    {
        public readonly MySqlConnection conn;
        public MySQLConnectionService(IConfiguration configuration)
        {
            conn = new MySqlConnection(configuration.GetConnectionString("MySQL"));
        }

        public MySqlConnection GetConnection()
        {
            if (conn.State == System.Data.ConnectionState.Closed)
                conn.Open();
            return conn;
        }
    }
}
