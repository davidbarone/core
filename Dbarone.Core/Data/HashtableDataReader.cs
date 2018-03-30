using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections;

namespace Dbarone.Data
{
    /// <summary>
    /// A data reader over a collection of Hashtable objects.
    /// </summary>
    public class HashtableDataReader : IDataReader
    {
        IEnumerable<Hashtable> data;
        IEnumerator enumerator;
        private bool isClosed = false;
        private Dictionary<string, Type> schema;

        public HashtableDataReader(IEnumerable<Hashtable> data, Dictionary<string, Type> schema)
        {
            this.schema = schema;
            this.data = data;
            enumerator = data.GetEnumerator();
        }

        #region IDataReader Members

        public void Close()
        {
            IDisposable disposable = data as IDisposable;
            if (disposable != null)
                disposable.Dispose();

            isClosed = true;
        }

        public int Depth
        {
            get { return 0; }
        }

        public DataTable GetSchemaTable()
        {
            DataTable table = new DataTable("schema");
            table.Columns.Add("ColumnName", typeof(string));
            table.Columns.Add("ColumnOrdinal", typeof(int));
            table.Columns.Add("DataType", typeof(Type));

            // Get first row
            var keys = schema.Keys.ToList();
            for (int i=0; i<keys.Count; i++)
            {
                table.Rows.Add(
                    keys[i],
                    i,
                    schema[keys[i]]);
            }
            return table;
        }

        public bool IsClosed
        {
            get { return isClosed; }
        }

        public bool NextResult()
        {
            // only support 1 result.
            return false;
        }

        public bool Read()
        {
            return enumerator.MoveNext();
        }

        public int RecordsAffected
        {
            get { return -1; }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Close();
        }

        #endregion

        #region IDataRecord Members

        public int FieldCount
        {
            get { return schema.Keys.Count(); }
        }

        public bool GetBoolean(int i)
        {
            return (bool)GetValue(i);
        }

        public byte GetByte(int i)
        {
            return (byte)GetValue(i);
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i)
        {
            return (char)GetValue(i);
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        public string GetDataTypeName(int i)
        {
            var key = schema.Keys.ToList()[i];
            return schema[key].Name;
        }

        public DateTime GetDateTime(int i)
        {
            return (DateTime)GetValue(i);
        }

        public decimal GetDecimal(int i)
        {
            return (Decimal)GetValue(i);
        }

        public double GetDouble(int i)
        {
            return (Double)GetValue(i);
        }

        public Type GetFieldType(int i)
        {
            var key = schema.Keys.ToList()[i];
            return schema[key];
        }

        public float GetFloat(int i)
        {
            return (float)GetValue(i);
        }

        public Guid GetGuid(int i)
        {
            return (Guid)GetValue(i);
        }

        public short GetInt16(int i)
        {
            return (Int16)GetValue(i);
        }

        public int GetInt32(int i)
        {
            return (Int32)GetValue(i);
        }

        public long GetInt64(int i)
        {
            return (Int64)GetValue(i);
        }

        public string GetName(int i)
        {
            return schema.Keys.ToList()[i];
        }

        public int GetOrdinal(string name)
        {
            return schema.Keys.ToList().IndexOf(name);
        }

        public string GetString(int i)
        {
            return (string)GetValue(i);
        }

        public object GetValue(int i)
        {
            var key = schema.Keys.ToList()[i];
            return ((Hashtable)enumerator.Current)[key] ?? DBNull.Value;
        }

        public int GetValues(object[] values)
        {
            int i = 0;
            foreach (var field in schema.Keys)
            {
                values[i++] = ((Hashtable)enumerator.Current)[field];
            }
            return schema.Keys.Count();
        }

        public bool IsDBNull(int i)
        {
            return GetValue(i) == null || GetValue(i) == DBNull.Value;
        }

        public object this[string name]
        {
            get { return GetValue(GetOrdinal(name)); }
        }

        public object this[int i]
        {
            get { return GetValue(i); }
        }

        #endregion
    }
}
