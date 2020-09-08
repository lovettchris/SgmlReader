using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Sgml
{
    internal sealed class EmbeddedResourceEntityContent : IEntityContent
    {
        private readonly Assembly assembly;
        private readonly string name;

        public EmbeddedResourceEntityContent(Assembly assembly, string name)
        {
            this.assembly = assembly;
            this.name = name;
        }

        public Encoding Encoding => Encoding.UTF8;
        
        public string MimeType => "text/plain";

        public Uri Redirect => new Uri(name, UriKind.Relative);

        public Stream Open()
        {
            return assembly.GetManifestResourceStream(name);
        }
    }
}
