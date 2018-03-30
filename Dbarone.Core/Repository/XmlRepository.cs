using Dbarone.Core;
using Dbarone.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Repository
{
    /// <summary>
    /// Xml-based repository implementation.
    /// </summary>
    public class XmlRepository : IRepository
    {
        private static object lockObject = new Object();
        private static Dictionary<Type, List<object>> database = new Dictionary<Type, List<object>>();

        private string basePath = null;
        public XmlRepository(string basePath)
        {
            this.basePath = basePath;
        }

        private string GetPath<T>()
        {
            var path = Path.Combine(this.basePath, typeof(T).Name);
            // while we're here, check that path exists. If not, create
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        private void Initialise<T>()
        {
            var t = typeof(T);
            lock (lockObject)
            {
                if (!database.ContainsKey(t))
                {
                    var items = new List<object>();

                    var path = GetPath<T>();

                    // initialise
                    var files = Directory.GetFiles(path, "*.xml");
                    foreach (var file in files)
                    {
                        var xml = File.ReadAllText(Path.Combine(path, file));
                        items.Add(xml.XmlStringToObject<T>());
                    }
                    database.Add(t, items);
                }
            }
        }

        public IEnumerable<T> Read<T>()
        {
            lock (lockObject)
            {
                Initialise<T>();
                foreach (var item in database[typeof(T)])
                    yield return (T)item;
            }
        }

        public void Upsert<T>(T item)
        {
            lock (lockObject)
            {
                Initialise<T>();
                if (Read<T>().Contains(item))
                    Update<T>(item);
                else
                    Create<T>(item);
            }
        }

        public void Create<T>(T item)
        {
            lock (lockObject)
            {
                Initialise<T>();
                foreach (var i in database[typeof(T)])
                    if (i.Equals(item))
                        throw new Exception("Cannot add duplicate entity.");

                // save file
                var path = GetPath<T>();
                var xml = item.ObjectToXmlString();
                string fileName = string.Format("{0}.xml", item.ToString());
                string file = Path.Combine(path, fileName);
                if (File.Exists(file))
                    throw new Exception("Cannot create entity as file already exists.");

                File.WriteAllText(file, xml);
                database[typeof(T)].Add(item);
            }
        }

        public void Delete<T>(T item)
        {
            lock (lockObject)
            {
                Initialise<T>();
                if (item == null)
                    throw new Exception("Cannot delete null object.");

                // delete object / file
                var path = GetPath<T>();
                string fileName = string.Format("{0}.xml", item.ToString());
                string file = Path.Combine(path, fileName);
                if (!File.Exists(file))
                    throw new Exception("Cannot delete object as file storage does not exist.");

                File.Delete(file);
                database[typeof(T)].Remove(item);
            }
        }

        public void Update<T>(T item)
        {
            lock (lockObject)
            {
                Initialise<T>();
                // save file
                var path = GetPath<T>();
                var xml = item.ObjectToXmlString();
                string fileName = string.Format("{0}.xml", item.ToString());
                string file = Path.Combine(path, fileName);
                File.WriteAllText(file, xml);
                var oldItem = database[typeof(T)].First(i => i.Equals(item));
                database[typeof(T)].Remove(oldItem);
                database[typeof(T)].Add(item);
            }
        }

        public T Find<T>(params object[] values)
        {
            lock (lockObject)
            {
                Initialise<T>();
                List<PropertyInfo> keys = new List<PropertyInfo>();
                Type t = typeof(T);
                var properties = t
                    .GetPropertiesDecoratedBy<KeyAttribute>()
                    .OrderBy(p => ((KeyAttribute)p.GetCustomAttribute(typeof(KeyAttribute), true)).Order)
                    .ToList();

                if (values.Length != properties.Count())
                    throw new Exception("Incorrect number of values supplied to Find() method.");

                foreach (var item in database[typeof(T)])
                {
                    bool match = true;
                    for (int i = 0; i < values.Length; i++)
                    {
                        // When finding record, if property is string, do case insensitive compare
                        var type = properties[i].PropertyType;
                        var value = properties[i].GetValue(item);
                        if (type == typeof(string) && !((string)value).Equals((string)values[i], StringComparison.InvariantCultureIgnoreCase))
                        {
                            match = false;
                            break;
                        } else if (type != typeof(string) && !value.Equals(values[i]))
                        {
                            match = false;
                            break;
                        }
                    }
                    // if got here, then the item matches all values provided. Return this value.
                    if (match) return (T)item;
                }
                return default(T);
            }
        }

        public DateTime Created<T>(T item)
        {
            lock (lockObject)
            {
                Initialise<T>();
                // get created date on file
                var path = GetPath<T>();
                string fileName = string.Format("{0}.xml", item.ToString());
                string file = Path.Combine(path, fileName);
                return File.GetCreationTime(file);
            }
        }

        public DateTime Updated<T>(T item)
        {
            lock (lockObject)
            {
                Initialise<T>();
                // get created date on file
                var path = GetPath<T>();
                string fileName = string.Format("{0}.xml", item.ToString());
                string file = Path.Combine(path, fileName);
                return File.GetLastWriteTime(file);
            }
        }

    }
}
