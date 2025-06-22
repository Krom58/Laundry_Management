using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace Laundry_Management.Laundry
{
    internal class DBconfig
    {
        public static SqlConnection GetConnection()
        {
            string connectionString = "Server=10.10.0.42\\SQLSET;Database=Laundry_Management;User Id=sa;Password=Wutt@1976;Trusted_Connection=False;";
            return new SqlConnection(connectionString);
            //"Server=KROM\\SQLEXPRESS;Database=Laundry_Management;Integrated Security=True;"
            //"Server=10.10.0.42\\SQLSET;Database=Laundry_Management;User Id=sa;Password=Wutt@1976;Trusted_Connection=False;"
        }
    }
}
