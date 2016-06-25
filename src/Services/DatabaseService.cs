using PSNBot.Model;
using System;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PSNBot.Services
{
    public class DatabaseService
    {
        private string _filename;

        public DatabaseService(string filename)
        {
            _filename = filename;

            if (!File.Exists(_filename))
            {
                Initialize();
            }
        }

		public SqliteConnection GetConnection()
        {
			return new SqliteConnection(string.Format("Data Source={0};Version=3;", _filename));
        }

        private void CreateTable<T>()
        {
            var type = typeof(T);
            var props = type.GetProperties();

            var sb = new StringBuilder();
            sb.Append("CREATE TABLE ");
            sb.Append(type.Name);
            sb.Append(" (");
            sb.Append(string.Join(", ", props.Select(p => 
                string.Format("{0} {1} {2}", p.Name, GetType(p.PropertyType), p.Name == "Id" ? "PRIMARY KEY" : string.Empty))));
            sb.Append(");");

            using (var connection = GetConnection())
            {
                connection.Open();
				var command = new SqliteCommand(sb.ToString(), connection);
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        public void Insert<T>(T record)
        {
            var type = typeof(T);
            var props = type.GetProperties();

            var sb = new StringBuilder();
            sb.Append("INSERT INTO ");
            sb.Append(type.Name);
            sb.Append(" (");
            sb.Append(string.Join(", ", props.Select(p => p.Name)));
            sb.Append(") VALUES (");
            sb.Append(string.Join(", ", props.Select(p => "@" + p.Name)));
            sb.Append(");");

            using (var connection = GetConnection())
            {
                connection.Open();
				var command = new SqliteCommand(sb.ToString(), connection);

                foreach (var prop in props)
                {
                    command.Parameters.Add(new SqliteParameter(prop.Name, prop.GetValue(record)));
                }

                command.ExecuteNonQuery();                
                connection.Close();
            }
        }

        public void Update<T>(T record)
        {
            var type = typeof(T);
            var props = type.GetProperties();

            var sb = new StringBuilder();
            sb.Append("UPDATE ");
            sb.Append(type.Name);
            sb.Append(" SET ");
            sb.Append(string.Join(", ", props.Where(p => p.Name != "Id").Select(p => string.Format("{0} = @{1}", p.Name, p.Name))));
            sb.Append(" WHERE Id = @id");

            using (var connection = GetConnection())
            {
                connection.Open();
                var command = new SqliteCommand(sb.ToString(), connection);

                foreach (var prop in props)
                {
					command.Parameters.Add(new SqliteParameter(prop.Name, prop.GetValue(record)));
                }

                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        public void Delete<T>(T record)
        {
            var type = typeof(T);
            var prop = type.GetProperties().FirstOrDefault(p => p.Name == "Id");

            var sb = new StringBuilder();
            sb.Append("DELETE FROM ");
            sb.Append(type.Name);
            sb.Append(" WHERE Id = @id");

            using (var connection = GetConnection())
            {
                connection.Open();
				var command = new SqliteCommand(sb.ToString(), connection);
                command.Parameters.Add(new SqliteParameter(prop.Name, prop.GetValue(record)));
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        public void Upsert<T>(T record)
        {
            var type = typeof(T);
            var prop = type.GetProperties().FirstOrDefault(p => p.Name == "Id");

            var r = Select<T>("Id", prop.GetValue(record)).FirstOrDefault();
            if (r == null)
            {
                Insert<T>(record);
            }
            else
            {
                Update<T>(record);
            }
        }

		private T Map<T>(SqliteDataReader reader)
        {
            var type = typeof(T);
            var props = type.GetProperties();

            var record = (T)Activator.CreateInstance(type);

            int i = 0;
            foreach (var prop in props)
            {
                var propertyType = prop.PropertyType;

                if (propertyType == typeof(long))
                {
                    prop.SetValue(record, reader.GetInt64(i));
                }

                if (propertyType == typeof(string))
                {
                    prop.SetValue(record, reader.GetString(i));
                }

                if (propertyType == typeof(bool))
                {
                    prop.SetValue(record, reader.GetBoolean(i));
                }

                if (propertyType == typeof(DateTime))
                {
                    prop.SetValue(record, reader.GetDateTime(i));
                }
                i++;
            }
            return record;
        }

        public IEnumerable<T> Select<T>(string param = null, object value = null)
        {
            var type = typeof(T);
            var props = type.GetProperties();

            var sb = new StringBuilder();
            sb.Append("SELECT * FROM ");
            sb.Append(type.Name);
            if (param != null)
            {
                if (value is string)
                {
                    sb.Append(string.Format(" WHERE UPPER({0}) = UPPER(@param) ", param));
                }
                else
                {
                    sb.Append(string.Format(" WHERE {0} = @param ", param));
                }
            }
            sb.Append(";");

            using (var connection = GetConnection())
            {
                connection.Open();
				var command = new SqliteCommand(sb.ToString(), connection);
                if (param != null)
                {
                    command.Parameters.Add(new SqliteParameter("param", value));
                }
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return Map<T>(reader);
                    }
                }
                connection.Close();
            }
        }

        public IEnumerable<T> Search<T>(string text)
        {
            var type = typeof(T);
            var props = type.GetProperties().Where(p => p.PropertyType == typeof(string));

            var sb = new StringBuilder();
            sb.Append("SELECT * FROM ");
            sb.Append(type.Name);
            if (props.Any())
            {
                sb.Append(" WHERE ");
                sb.Append(string.Join(" OR ", props.Select(p => string.Format("UPPER({0}) LIKE UPPER(@{1}) ESCAPE '\\'", p.Name, p.Name))));
            }
            sb.Append(";");

            using (var connection = GetConnection())
            {
                connection.Open();
				var command = new SqliteCommand(sb.ToString(), connection);
                foreach (var prop in props)
                {
					command.Parameters.Add(new SqliteParameter(prop.Name, string.Format("%{0}%", text.Replace("%", "\\%").Replace("_", "\\_"))));
                }
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return Map<T>(reader);
                    }
                }
                connection.Close();
            }
        }

        private string GetDbType(Type propertyType)
        {
            if (propertyType == typeof(long))
            {
                return "BIGINT";
            }

            if (propertyType == typeof(string))
            {
                return "NVARCHAR(2048)";
            }

            if (propertyType == typeof(bool))
            {
                return "BOOLEAN";
            }

            if (propertyType == typeof(DateTime))
            {
                return "DATETIME";
            }

            throw new InvalidOperationException();
        }
        
        private string GetType(Type propertyType)
        {
            if (propertyType == typeof(long))
            {
                return "BIGINT";
            }

            if (propertyType == typeof(string))
            {
                return "NVARCHAR(2048)";
            }

            if (propertyType == typeof(bool))
            {
                return "BOOLEAN";
            }

            if (propertyType == typeof(DateTime))
            {
                return "DATETIME";
            }

            throw new InvalidOperationException();
        }

        private void Initialize()
        {
			SqliteConnection.CreateFile(_filename);
            CreateTable<Account>();
            CreateTable<TimeStamp>();
        }
    }
}
