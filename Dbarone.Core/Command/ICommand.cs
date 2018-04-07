using Dbarone.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Command
{
    public interface ICommand
    {
        IContainer Container { get; set; }

        string Execute();
    }
}
