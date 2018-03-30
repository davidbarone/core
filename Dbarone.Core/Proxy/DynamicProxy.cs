
using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

namespace Dbarone.Proxy
{
    /// <summary>
    /// Provides proxy functionality.
    /// See https://msdn.microsoft.com/en-us/magazine/dn574804.aspx.
    /// </summary>
    public class DynamicProxy : RealProxy
    {
        private readonly object _decorated;
        public event InterceptHandler Intercept;

        public DynamicProxy(object decorated)
          : base(decorated.GetType())
        {
            _decorated = decorated;
        }

        public override IMessage Invoke(IMessage msg)
        {
            if (Intercept != null)
                Intercept(_decorated, new InterceptorEventArgs
                {
                    Boundary = BoundaryType.BEFORE,
                    Message = msg,
                    Exception = null
                });

            var methodCall = msg as IMethodCallMessage;
            var methodInfo = methodCall.MethodBase as MethodInfo;

            try
            {
                var result = methodInfo.Invoke(_decorated, methodCall.InArgs);

                if (Intercept != null)
                    Intercept(_decorated, new InterceptorEventArgs
                    {
                        Boundary = BoundaryType.AFTER,
                        Message = msg,
                        Exception = null
                    });
                return new ReturnMessage(result, null, 0, methodCall.LogicalCallContext, methodCall);
            }
            catch (Exception e)
            {
                if (Intercept != null)
                    Intercept(_decorated, new InterceptorEventArgs
                    {
                        Boundary = BoundaryType.ERROR,
                        Message = msg,
                        // Proxy wraps real error in TargetInvocationException
                        Exception = e.InnerException ?? e
                    });
                return new ReturnMessage(e, methodCall);
            }
        }
    }
}
