using System;
using System.Data.SqlClient;

namespace WindowsFormsApp1.Data
{
    public class DatabaseConnection
    {
        private static DatabaseConnection instance;
        private string connectionString;

        public static DatabaseConnection Instance
        {
            get
            {
                if (instance == null)
                    instance = new DatabaseConnection();
                return instance;
            }
        }

        private DatabaseConnection()
        {
            // Thay đổi connection string theo máy của bạn
            connectionString = @"Server=localhost;Database=QL_ebook;Integrated Security=True;";

            // Hoặc nếu dùng SQL Server Authentication:
            // connectionString = @"Server=localhost;Database=QL_ebook;User Id=sa;Password=yourpassword;";
        }

        public SqlConnection GetConnection()
        {
            try
            {
                SqlConnection conn = new SqlConnection(connectionString);
                return conn;
            }
            catch (Exception ex)
            {
                throw new Exception("Không thể kết nối database: " + ex.Message);
            }
        }

        public bool TestConnection()
        {
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    conn.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        // Cập nhật connection string nếu cần
        public void SetConnectionString(string server, string database, string username = null, string password = null)
        {
            if (string.IsNullOrEmpty(username))
            {
                // Windows Authentication
                connectionString = $"Server={server};Database={database};Integrated Security=True;";
            }
            else
            {
                // SQL Server Authentication
                connectionString = $"Server={server};Database={database};User Id={username};Password={password};";
            }
        }
    }
}