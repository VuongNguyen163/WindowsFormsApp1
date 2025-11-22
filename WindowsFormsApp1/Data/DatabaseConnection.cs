using System;
using System.Data.SqlClient;

namespace WindowsFormsApp1.Data
{
    public class DatabaseConnection
    {
        private static DatabaseConnection instance;
        // Chuỗi kết nối đến database QL_ebook
        private string connectionString = @"Server=localhost;Database=QL_ebook;Integrated Security=True;MultipleActiveResultSets=true";

        public static DatabaseConnection Instance
        {
            get
            {
                if (instance == null) instance = new DatabaseConnection();
                return instance;
            }
        }

        private DatabaseConnection() { }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }

        public bool TestConnection()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}