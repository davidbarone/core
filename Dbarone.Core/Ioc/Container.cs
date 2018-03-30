using Dbarone.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dbarone.Ioc
{
    public class Container : IContainer
    {
        protected InterceptHandler interceptor = null;

        public InterceptHandler Interceptor
        {
            get
            {
                return interceptor;
            }
            set
            {
                interceptor = value;
            }
        }

        [AttributeUsage(AttributeTargets.Constructor)]
        public class InjectionConstructorAttribute : Attribute
        { }

        private enum DependencyType
        {
            None = 0,   // Type is unset
            Delegate,   // A builder function
            Instance,   // A specific instance
            Singleton,  // Dynamically created singleton
            Transient   // Dynamically created transient object
        }

        private class DependencyInfo
        {
            public object Dependency { get; private set; }
            public DependencyType DependencyType { get; private set; }

            public DependencyInfo(DependencyType dependencyType, object dependency)
            {
                DependencyType = dependencyType;
                Dependency = dependency;
            }
        }

        private readonly IDictionary<Type, DependencyInfo> dependencies = new Dictionary<Type, DependencyInfo>();
        private readonly IDictionary<Type, object> instances = new Dictionary<Type, object>();

        #region Register Methods

        /// <summary>
        /// Registers an instance type
        /// </summary>
        /// <typeparam name="TContract"></typeparam>
        /// <param name="instance"></param>
        public void Register<TContract>(TContract instance)
        {
            if (typeof(TContract) == typeof(Type))
                throw new ArgumentException("Cannot register the Type type as a contract.");

            dependencies[typeof(TContract)] = new DependencyInfo(DependencyType.Instance, instance);
            instances[typeof(TContract)] = instance;
        }

        /// <summary>
        /// Registers a concrete type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isSingleton"></param>
        public void Register(Type type, bool isSingleton)
        {
            if (type.IsInterface || type.IsAbstract) throw new ArgumentException("Must register a concrete implementation.");

            DependencyType dependencyType = isSingleton ? DependencyType.Singleton : DependencyType.Transient;
            dependencies[type] = new DependencyInfo(dependencyType, type);
        }

        /// <summary>
        /// Register a concrete type.
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="isSingleton"></param>
        public void Register<TImplementation>(bool isSingleton)
        {
            Register<TImplementation, TImplementation>(isSingleton);
        }

        /// <summary>
        /// Registers a dependency type, and optionally specify a singleton be returned
        /// </summary>
        /// <typeparam name="TContract"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="isSingleton"></param>
        public void Register<TContract, TImplementation>(bool isSingleton)
        {
            DependencyType dependencyType = isSingleton ? DependencyType.Singleton : DependencyType.Transient;
            dependencies[typeof(TContract)] = new DependencyInfo(dependencyType, typeof(TImplementation));
        }

        /// <summary>
        /// Registers a delegate
        /// </summary>
        /// <typeparam name="TContract"></typeparam>
        /// <param name="builder"></param>
        public void Register<TContract>(Func<TContract> builder)
        {
            dependencies[typeof(TContract)] = new DependencyInfo(DependencyType.Delegate, builder);
        }

        #endregion

        #region Resolve Methods

        /// <summary>
        /// Gets an instance from the container.
        /// </summary>
        /// <typeparam name="TContract"></typeparam>
        /// <param name="args">User-defined arguments can be passed into
        /// the container. If args are passed in, these must match in type
        /// and order to the start of the parameter list for the constructor 
        /// </param>
        /// <returns></returns>
        public TContract Resolve<TContract>(params object[] args)
        {
            return (TContract)Resolve(typeof(TContract), args);
        }

        public object Resolve(Type contract, params object[] args)  
        {
            if (!dependencies.ContainsKey(contract))
                throw new InvalidOperationException(string.Format("Unable to resolve type '{0}'.", contract));
            if (instances.ContainsKey(contract))
                return instances[contract];
            var dependency = dependencies[contract];
            if (dependency.DependencyType == DependencyType.Delegate)
                return ((Delegate)dependency.Dependency).DynamicInvoke();

            var constructorInfo = ((Type)dependency.Dependency).GetConstructors()
                .OrderByDescending(o => (o.GetCustomAttributes(typeof(InjectionConstructorAttribute), false).Count()))
                .ThenByDescending(o => (o.GetParameters().Length))
                .First();
            var parameterInfos = constructorInfo.GetParameters();

            object instance;
            if (parameterInfos.Length == 0)
            {
                instance = Activator.CreateInstance((Type)dependency.Dependency);
            }
            else
            {
                var parameters = new List<object>(parameterInfos.Length);

                // get parameters, and add user-supplied args if specified
                int userArgCount = 0;
                foreach (ParameterInfo parameterInfo in parameterInfos)
                {
                    if (userArgCount < args.Length)
                        parameters.Add(args[userArgCount]);
                    else
                        parameters.Add(Resolve(parameterInfo.ParameterType));

                    userArgCount++;
                }

                // invoke
                instance = constructorInfo.Invoke(parameters.ToArray());
            }

            // Create proxy?
            if (Interceptor != null)
            {
                // set the instance to be a proxy of the instance.
                var proxy = new DynamicProxy(instance);
                proxy.Intercept += this.Interceptor;
                instance = proxy.GetTransparentProxy();
            }

            if (dependency.DependencyType == DependencyType.Singleton)
                instances[contract] = instance;

            return instance;
        }

        #endregion
    }
}
