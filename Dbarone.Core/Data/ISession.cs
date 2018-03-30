using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Data
{
    public interface ISession: IDisposable
    {
        IDbConnection Connection { get; set; }
        IDbTransaction Transaction { get; set; }
        void Commit();
        void Rollback();
        object ExecuteScalar(string sql, object param = null, CommandType commandType = CommandType.Text, int? commandTimeout = null);
        IEnumerable<T> QueryEntity<T>(string sql = null, object param = null, CommandType commandType = CommandType.Text, bool buffered = true) where T : IEntity;
        IEnumerable<Hashtable> QueryHashtable(string sql, object param = null, CommandType commandType = CommandType.Text, Action<List<string>> callbackHeaders = null);
        T Create<T>() where T : IEntity;
        T Find<T>(params object[] id) where T : IEntity;
        void Insert<T>(T entity, string destinationTable = null) where T : IEntity;
        void Update<T>(T entity) where T : IEntity;
        void Delete<T>(T entity) where T : IEntity;
        IEnumerable<T> Select<T>(T where = null, bool buffered = true) where T : class, IEntity;
        int Execute(string sql, object param = null, CommandType commandType = CommandType.Text);
        int ExecuteScript(string sql, object param = null, CommandType commandType = CommandType.Text);
        IDataReader ExecuteReader(string sql, object param = null, CommandType commandType = CommandType.Text);
        IEnumerable<Hashtable> GetSchema(string sql, object parameters = null, CommandType commandType = CommandType.Text);
        void BulkCopy(IEnumerable<Hashtable> data, Dictionary<string, Type> schema, string destination, int timeout = 60 * 60);
    }
}
