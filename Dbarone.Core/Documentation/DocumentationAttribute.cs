using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Documentation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = true)]
    public class DocumentationAttribute : Attribute
    {
        public DocumentationAttribute(string description)
        {
            this.Description = description;
        }

        public string Description { get; set; }
    }
}
