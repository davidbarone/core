using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Repository
{
    /// <summary>
    /// Repository pattern. This interface allows for an concrete
    /// class to implement CRUD methods for a set of entities all
    /// within the single class.
    /// </summary>
    public interface IRepository
    {
        IEnumerable<T> Read<T>();
        T Find<T>(params object[] values);
        void Create<T>(T item);
        void Delete<T>(T item);
        void Update<T>(T item);
        void Upsert<T>(T item);
        DateTime Created<T>(T item);
        DateTime Updated<T>(T item);
    }
}
