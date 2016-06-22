using PSNBot.Model;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
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

        public SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(string.Format("Data Source={0};Version=3;", _filename));
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
                var command = new SQLiteCommand(sb.ToString(), connection);
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
                var command = new SQLiteCommand(sb.ToString(), connection);

                foreach (var prop in props)
                {
                    command.Parameters.Add(new SQLiteParameter(prop.Name, prop.GetValue(record)));
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
                var command = new SQLiteCommand(sb.ToString(), connection);

                foreach (var prop in props)
                {
                    command.Parameters.Add(new SQLiteParameter(prop.Name, prop.GetValue(record)));
                }

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

        public IEnumerable<T> Select<T>(string param = null, object value = null)
        {
            var type = typeof(T);
            var props = type.GetProperties();

            var sb = new StringBuilder();
            sb.Append("SELECT * FROM ");
            sb.Append(type.Name);
            if (param != null)
            {
                sb.Append(string.Format(" WHERE {0} = @param", param));
            }
            sb.Append(";");

            using (var connection = GetConnection())
            {
                connection.Open();
                var command = new SQLiteCommand(sb.ToString(), connection);

                if (param != null)
                {
                    command.Parameters.Add(new SQLiteParameter("param", value));
                }

                var reader = command.ExecuteReader();

                while (reader.Read())
                {
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
                    yield return record;
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
            SQLiteConnection.CreateFile(_filename);
            CreateTable<Account>();
            CreateTable<TimeStamp>();
        }
    }
}
