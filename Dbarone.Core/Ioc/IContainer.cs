using Dbarone.Proxy;
using System;

namespace Dbarone.Ioc
{
    public interface IContainer
    {
        InterceptHandler Interceptor { get; set; }
        void Register<TContract>(TContract instance);
        void Register<TImplementation>(bool isSingleton);
        void Register(Type type, bool isSingleton);
        void Register<TContract, TImplementation>(bool isSingleton);
        void Register<TContract>(Func<TContract> builder);
        TContract Resolve<TContract>(params object[] args);
        object Resolve(Type contract, params object[] args);
    }
}
