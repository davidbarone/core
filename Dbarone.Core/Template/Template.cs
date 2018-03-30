using DotLiquid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Template
{
    public static class TemplateProxy
    {
        public static string Render(string templateString, object model)
        {
            DotLiquid.Template template = DotLiquid.Template.Parse(templateString);

            Hash mod = new Hash();
            if (model != null)
            {
                mod = Hash.FromAnonymousObject(model);
            }

            return template.Render(mod);
        }
    }
}
