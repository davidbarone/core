# core
A simple enterprise library based on .NET. The solution consists of the following projects:

# Dbarone.Core
This class library contains classes to assist in typical enterprise applications. This includes the following namespaces:

Dbarone.Data - A simple database abstraction, providing querying methods and a (very) simple ORM.
Dbarone.Ioc - A simple inversion-of-control container.
Dbarone.Logging - A simple logging library.
Dbarone.Proxy - A simple proxy class based on DynamicProxy, which can be used for basic AOP-style programming (for example logging on method entry/exit).
Dbarone.Repository - A simple repository pattern, with a default Xml file implementation.
Dbarone.Schedule - A library offering a scheduling functions based around cron syntax.
Dbarone.Template - A wrapper for the DotLiquid template engine.
Dbarone.Validation - A simple validation engine for .NET objects.

# Dbarone.Svc
This class library contains base classed to build simple service components which can be used in a SOA or Micro-service architecture.

# Dbarone.Cli
This console application can be used to interface with a TCP server built using the Dbarone.Svc library.
