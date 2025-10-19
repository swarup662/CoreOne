using Microsoft.Data.SqlClient;
using System.Data;

namespace CoreOne.API.Infrastructure.Data
{
    public class DBContext
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DBContext(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// Get SqlConnection
        /// </summary>
        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        /// <summary>
        /// Execute SP and return DataTable
        /// </summary>
        public DataTable ExecuteSP_ReturnDataTable(string spName, Dictionary<string, object> parameters = null)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(spName, conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(cmd, parameters);

            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        /// <summary>
        /// Execute SP and return integer value (like identity or return code)
        /// </summary>
        public int ExecuteSP_ReturnInt(string spName, Dictionary<string, object> parameters = null)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(spName, conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Add input parameters
            AddParameters(cmd, parameters);

            // Add return value parameter
            var returnParameter = new SqlParameter
            {
                Direction = ParameterDirection.ReturnValue, // ✅ important
                SqlDbType = SqlDbType.Int
            };
            cmd.Parameters.Add(returnParameter);

            conn.Open();
            cmd.ExecuteNonQuery(); // Execute SP
            conn.Close();

            // Return the value
            return (int)(returnParameter.Value ?? 0);
        }

        /// <summary>
        /// Execute SP and return DataSet (for multiple tables)
        /// </summary>
        public DataSet ExecuteSP_ReturnDataSet(string spName, Dictionary<string, object> parameters = null)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(spName, conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(cmd, parameters);

            using var da = new SqlDataAdapter(cmd);
            var ds = new DataSet();
            da.Fill(ds);
            return ds;
        }

        /// <summary>
        /// Execute SP with Table-Valued Parameter
        /// </summary>
        public int ExecuteSP_WithTableType_ReturnInt(
           string spName,
           string tvpParamName,
           string tvpTypeName,
           DataTable tableTypeData,
           Dictionary<string, object> parameters = null)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(spName, conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Add scalar parameters
            AddParameters(cmd, parameters);
            // Add return value parameter
            var returnParameter = new SqlParameter
            {
                Direction = ParameterDirection.ReturnValue, // ✅ important
                SqlDbType = SqlDbType.Int
            };
            cmd.Parameters.Add(returnParameter);
            // Add TVP
            var tvpParam = new SqlParameter
            {
                ParameterName = tvpParamName.StartsWith("@") ? tvpParamName : "@" + tvpParamName,
                SqlDbType = SqlDbType.Structured,
                TypeName = tvpTypeName,
                Value = tableTypeData
            };
            cmd.Parameters.Add(tvpParam);

            conn.Open();
            var result = cmd.ExecuteNonQuery();
            return (int)(returnParameter.Value ?? 0);
            // return result;
        }

        public DataSet ExecuteSP_WithTableType_ReturnDataSet(
                    string spName,
                    string tvpParamName,
                    string tvpTypeName,
                    DataTable tableTypeData,
                    Dictionary<string, object> parameters = null)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(spName, conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Add scalar parameters
            AddParameters(cmd, parameters);

            // Add TVP
            var tvpParam = new SqlParameter
            {
                ParameterName = tvpParamName.StartsWith("@") ? tvpParamName : "@" + tvpParamName,
                SqlDbType = SqlDbType.Structured,
                TypeName = tvpTypeName,
                Value = tableTypeData
            };
            cmd.Parameters.Add(tvpParam);

            using var adapter = new SqlDataAdapter(cmd);
            var ds = new DataSet();
            adapter.Fill(ds);
            return ds;
        }



        /// <summary>
        /// Helper to add SQL parameters from Dictionary
        /// </summary>
        private void AddParameters(SqlCommand cmd, Dictionary<string, object> parameters)
        {
            if (parameters == null) return;

            foreach (var param in parameters)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }
        }
    }
}
