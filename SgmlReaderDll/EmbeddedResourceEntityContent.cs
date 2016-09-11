using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sgml
{

    internal class EmbeddedResourceEntityContent : IEntityContent
    {
        private Assembly assembly;
        private string name;


        public EmbeddedResourceEntityContent(Assembly assembly, string name)
        {
            this.assembly = assembly;
            this.name = name;
        }

        public Encoding Encoding
        {
            get
            {
                return Encoding.UTF8;
            }
        }

        public string MimeType
        {
            get
            {
                return "text/plain";
            }
        }

        public Uri Redirect
        {
            get
            {
                return new Uri(name, UriKind.Relative);
            }
        }

        public Stream Open()
        {
            return assembly.GetManifestResourceStream(name);
        }
    }
}
