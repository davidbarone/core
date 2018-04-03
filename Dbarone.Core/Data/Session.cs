using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Collections;
using System.Data.SqlClient;
using Dbarone.Core;

namespace Dbarone.Data
{
    public class Session : ISession
    {
        private readonly string connectionString;
        private string provider;
        private Hydrater hydrater = new Hydrater();
        private int CommandTimeout = 600;

        public IDbConnection Connection { get; set; }
        public IDbTransaction Transaction { get; set; }

        // static constructor (get entity information)
        static Session()
        {
            MetaDataStore.BuildMetaDataFor(typeof(Session).Assembly);
        }

        public static void BuildMetaDataFor(Assembly assembly)
        {
            MetaDataStore.BuildMetaDataFor(assembly);
        }

        public static ISession Create(string connectionString, int commandTimeout = 60, string providerName = "System.Data.SqlClient")
        {
            return new Session(
                providerName,
                connectionString,
                false,
                commandTimeout);
        }

        public static string BatchDelimiterSql
        {
            get { return "GO\r\n"; }
        }

        public void Dispose()
        {
            if (Transaction != null) Transaction.Dispose();
            if (Connection != null) Connection.Dispose();
        }

        public void Commit()
        {
            Transaction.Commit();
        }

        private void InitializeConnection(bool createTransaction=true)
        {
            var factory = DbProviderFactories.GetFactory(provider);
            Connection = factory.CreateConnection();

            Connection.ConnectionString = connectionString;
            Connection.Open();
            if (createTransaction)
                Transaction = Connection.BeginTransaction();
        }

        public void Rollback()
        {
            Transaction.Rollback();
        }

        public Session(string provider, string connectionString, bool useTransaction, int? commandTimeout = 30)
        {
            this.connectionString = connectionString;
            this.provider = provider;
            if (commandTimeout.HasValue) this.CommandTimeout = commandTimeout.Value;
            InitializeConnection(useTransaction);
        }

        public virtual object ExecuteScalar(string sql, object param = null, CommandType commandType = CommandType.Text, int? commandTimeout = null)
        {
            using (var cmd = BuildCommand(sql, param, commandType))
            {
                return cmd.ExecuteScalar();
            }
        }

        public virtual IEnumerable<T> QueryEntity<T>(string sql = null, object param = null, CommandType commandType = CommandType.Text, bool buffered = true) where T : IEntity
        {
            var data = QueryEntityInternal<T>(sql, param, commandType);
            return (buffered) ? data.ToList() : data;
        }

        private IEnumerable<T> QueryEntityInternal<T>(string sql, object param = null, CommandType commandType = CommandType.Text, int? commandTimeout = null) where T : IEntity
        {
            using (var cmd = BuildCommand(sql, param, commandType))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        yield return hydrater.GetEntity<T>(reader);
                }
            }
        }

        /// <summary>
        /// Creates a new instance of an entity.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Create<T>() where T : IEntity
        {
            return Activator.CreateInstance<T>();
        }

        private Hashtable GetParametersAsHashtableFor<T>(T entity, IEnumerable<ColumnInfo> columns)
        {
            Hashtable ht = new Hashtable();

            // Get the parameters
            foreach (var column in columns)
            {
                if (column.PropertyInfo.PropertyType.IsEnum && column.EnumColumnBehaviour == EnumColumnBehaviourEnum.STRING)
                {
                    // TO DO: ADD THIS CODE TO UPDATE METHOD ETC.
                    // If Enum + set to string format, output string representation
                    var enumValue = (Enum)Convert.ChangeType(column.PropertyInfo.GetValue(entity), typeof(Enum));
                    ht.Add(column.Name, enumValue.ToString());
                }
                else
                    // otherwise, just the value.
                    ht.Add(column.Name, column.PropertyInfo.GetValue(entity));
            }
            return ht;
        }

        public virtual void Insert<T>(T entity, string destinationTable = null) where T : IEntity
        {
            var info = MetaDataStore.GetTableInfoFor<T>();
            var columns = info.Columns.ToList().Where(c => c.DatabaseGenerated == false);
            var ht = GetParametersAsHashtableFor<T>(entity, columns);

            // Execute
            var sql = string.Format(
                "INSERT INTO {0} ({1}) SELECT {2}",
                destinationTable ?? info.TableName[CrudOperation.CREATE],
                string.Join(",", columns.Select(c => c.Name)),
                string.Join(",", columns.Select(c => string.Format("@{0}",c.Name)))
            );
            Execute(sql, ht);
        }

        public virtual T Find<T>(params object[] id) where T : IEntity
        {
            Hashtable ht = new Hashtable();

            var info = MetaDataStore.GetTableInfoFor<T>();
            var columns = info.Columns.ToList();
            var parameters = id.ToList();
            var keys = columns.Where(c => c.Key).OrderBy(c => c.Order).ToList();
            if (parameters.Count()!=keys.Count())
            {
                throw new Exception("Cannot find entity. Incorrect number of key values passed in to method.");
            }

            // Get the parameters
            for (int i = 0; i < keys.Count(); i++)
            {
                ht.Add(keys[i].Name, parameters[i]);
            }

            // Execute
            var sql = string.Format(
                "SELECT * FROM {0} WHERE {1}",
                info.TableName[CrudOperation.READ],
                string.Join(" AND ", keys.Select(k=>string.Format("{0} = @{0}", k.Name)))
            );
            return QueryEntity<T>(sql, ht).FirstOrDefault();
        }

        /// <summary>
        /// Updates an entity using the entity metadata.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        public virtual void Update<T>(T entity) where T : IEntity
        {
            if (entity == null)
            {
                throw new Exception("Error in Update() method. Entity cannot be null!");
            }

            var info = MetaDataStore.GetTableInfoFor<T>();
            var columns = info.Columns.ToList();
            var keys = columns.Where(c => c.Key).ToList();

            if (!keys.Any())
            {
                throw new Exception("Cannot update Entity - no keys defined.");
            }

            var ht = GetParametersAsHashtableFor<T>(entity, columns);

            // Execute
            var sql = string.Format(
                "UPDATE {0} SET {1} WHERE {2}",
                info.TableName[CrudOperation.UPDATE],
                string.Join(",", columns.Where(c => c.DatabaseGenerated == false).Select(c => string.Format("{0} = @{0}", c.Name))),
                string.Join(" AND ", keys.Select(k => string.Format("{0} = @{0}", k.Name)))
            );
            Execute(sql, ht);
        }

        /// <summary>
        /// Deletes an entity using the entity metadata.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        public virtual void Delete<T>(T entity) where T : IEntity
        {
            if (entity == null)
            {
                throw new Exception("Error in Delete() method. Entity cannot be null!");
            }

            var info = MetaDataStore.GetTableInfoFor<T>();
            var keys = info.Columns.Where(c => c.Key).ToList();

            if (!keys.Any())
            {
                throw new Exception("Cannot delete Entity - no keys defined.");
            }

            // Get the parameters
            var ht = GetParametersAsHashtableFor<T>(entity, keys);

            // Execute
            var sql = string.Format(
                "DELETE FROM {0} WHERE {1}",
                info.TableName[CrudOperation.DELETE],
                string.Join(" AND ", keys.Select(k => string.Format("{0} = @{0}", k.Name)))
            );
            Execute(sql, ht);
        }

        /// <summary>
        /// Selects all the entities in a set.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual IEnumerable<T> Select<T>(T where = null, bool buffered=true) where T : class, IEntity
        {
            var info = MetaDataStore.GetTableInfoFor<T>();
            Dictionary<string, object> whereItems = new Dictionary<string, object>();
            if (where!=null)
            {
                var columns = MetaDataStore.GetTableInfoFor<T>().Columns;
                foreach (var column in columns)
                {
                    var value = column.PropertyInfo.GetValue(where);
                    var dflt = column.PropertyInfo.PropertyType.Default();
                    
                    if (value!=null && !value.Equals(dflt) ) {
                        whereItems.Add(column.Name, column.PropertyInfo.GetValue(where));
                    }
                }
            }

            if (whereItems.Keys.Count == 0)
            {
                var sql = string.Format(
                    "SELECT * FROM {0}",
                    info.TableName[CrudOperation.READ]
                );
                return QueryEntity<T>(sql, null, CommandType.Text, buffered);
            }
            else
            {
                var sql = string.Format(
                    "SELECT * FROM {0} WHERE {1}",
                    info.TableName[CrudOperation.READ],
                    string.Join(" AND ", whereItems.Keys.Select(v => string.Format("{0} = @{1}", v, v)))
                );
                return QueryEntity<T>(sql,  whereItems, CommandType.Text, buffered);
            }

        }

        /// <summary>
        /// Return a list of Hashtable objects, reader is closed after the call
        /// </summary>
        public virtual IEnumerable<Hashtable> QueryHashtable(string sql, object param = null, CommandType commandType = CommandType.Text, Action<List<string>> callbackHeaders = null)
        {
            using (var cmd = BuildCommand(sql, param, commandType))
            {
                bool processedHeader = false;
                using (var reader = cmd.ExecuteReader())
                {
                    // Headers callback can be used for caller to do something
                    // with header information. The hashtable returned
                    // does not guarantee the order of the fields.
                    if (!processedHeader && callbackHeaders!=null)
                    {
                        List<string> headers = new List<string>();
                        for (int i=0; i < reader.FieldCount; i++)
                        {
                            headers.Add(reader.GetName(i));
                        }
                        callbackHeaders(headers);
                    }
                    while (reader.Read())
                        yield return hydrater.GetHashTable(reader);

                    reader.NextResult();
                }
            }
        }

        public virtual int Execute(string sql, object param=null, CommandType commandType=CommandType.Text)
        {
            using (var cmd = BuildCommand(sql, param, commandType))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        public virtual int ExecuteScript(string sql, object param=null, CommandType commandType = CommandType.Text)
        {
            int rowsReturned=0;

            // Try both variations of new line.
            var batches = sql.Split(new string[] { "GO\r\n", "GO\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var batch in batches)
            {
                rowsReturned += Execute(batch, param, commandType);
            }
            return rowsReturned;
        }

        public IDataReader ExecuteReader(string sql, object param=null, CommandType commandType = CommandType.Text)
        {
            using (var cmd = BuildCommand(sql, param, commandType))
            {
                return cmd.ExecuteReader(CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo | CommandBehavior.CloseConnection);
            }
        }

        public IEnumerable<Hashtable> GetSchema(string sql, object parameters=null, CommandType commandType = CommandType.Text)
        {
            using (var reader = ExecuteReader(sql, parameters, commandType))
            {
                //var sc = reader.GetSchemaTable();
                var schema_reader = reader.GetSchemaTable().CreateDataReader();

                while (schema_reader.Read())
                    // Only return columns in the output (ignore internal join columns not in SELECT)
                    if (schema_reader["IsHidden"] == null || (bool)schema_reader["IsHidden"]==false)
                        yield return hydrater.GetHashTable(schema_reader);
            }
        }

        public void BulkCopy(IEnumerable<Hashtable> data, Dictionary<string, Type> schema, string destination, int timeout = 60 * 60)
        {
            SqlBulkCopy bcp = new SqlBulkCopy(
                (SqlConnection)this.Connection,
                SqlBulkCopyOptions.Default,
                (SqlTransaction)this.Transaction);

            foreach (string column in schema.Keys)
                bcp.ColumnMappings.Add(column, column);

            bcp.DestinationTableName = destination;
            bcp.BulkCopyTimeout = timeout;

            HashtableDataReader adapter = new HashtableDataReader(data, schema);
            bcp.WriteToServer(adapter);

        }

        private IDbCommand BuildCommand(string sql, object parameters=null, CommandType commandType = CommandType.Text)
        {
            if (Connection.State == ConnectionState.Closed)
                Connection.Open();

            var command = Connection.CreateCommand();
            command.Transaction = Transaction;
            command.CommandText = sql;
            command.CommandType = commandType;
            command.CommandTimeout = this.CommandTimeout;
            IDictionary<string, object> param_dict = new Dictionary<string, object>();

            // Parameters can either be passed in as an object, or as a Dyanamic
            if (parameters != null)
            {
                if (typeof(System.Dynamic.IDynamicMetaObjectProvider).IsAssignableFrom(parameters.GetType()))
                {
                    // dynamic type
                    if (parameters.GetType() == typeof(System.Dynamic.ExpandoObject))
                    {
                        param_dict = (IDictionary<string, object>)parameters;
                    }
                }
                else if (typeof(IDictionary).IsAssignableFrom(parameters.GetType()))
                {
                    // dictionary
                    var p = parameters as IDictionary;
                    foreach (var item in p.Keys)
                    {
                        param_dict.Add(item.ToString(), p[item]);
                    }
                }
                else
                {
                    foreach (PropertyInfo pi in parameters.GetType().GetProperties())
                    {
                        param_dict.Add(pi.Name, pi.GetValue(parameters, null) ?? DBNull.Value);
                    }
                }
            }

            // If stored proc used and connection is SqlServer, then automatically derive + populate the parameters
            var sqlcmd = command as SqlCommand;
            if (commandType == CommandType.StoredProcedure && sqlcmd != null)
            {
                SqlCommandBuilder.DeriveParameters(sqlcmd);
                foreach (SqlParameter param in sqlcmd.Parameters)
                    if (param.Direction == ParameterDirection.Input || param.Direction == ParameterDirection.InputOutput)
                    {
                        // the derived sql parameter names starts with '@' symbol.
                        string name = param.ParameterName.Substring(1);
                        if (param_dict.ContainsKey(name))
                        {
                            param.Value = param_dict[name];
                        }
                    }
            } else
            {
                // otherwise, create + populate parameter
                foreach (var item in param_dict)
                {
                    var p = command.CreateParameter();
                    p.ParameterName = item.Key;
                    p.Value = param_dict[item.Key] ?? DBNull.Value;
                    command.Parameters.Add(p);
                }
            }
            return command;
        }
    }
}