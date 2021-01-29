using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Sgml
{
    /// <summary>
    /// A special implementation of IEntityContent that can load content
    /// from embedded resources.
    /// </summary>
    public sealed class EmbeddedResourceEntityContent : IEntityContent
    {
        private readonly Assembly assembly;
        private readonly string name;

        /// <summary>
        /// Construct a new EmbeddedResourceEntityContent.
        /// </summary>
        /// <param name="assembly">The assembly to read resources from</param>
        /// <param name="name">The name of the embedded resource</param>
        public EmbeddedResourceEntityContent(Assembly assembly, string name)
        {
            this.assembly = assembly;
            this.name = name;
        }

        /// <summary>
        /// Return the encoding from HTTP header
        /// </summary>
        public Encoding Encoding => Encoding.UTF8;

        /// <summary>
        /// Return the HTTP ContentType
        /// </summary>
        public string MimeType => "text/plain";

        /// <summary>
        /// Returns the redirect Uri if an HTTP redirect happened during the fetching of this resource.
        /// </summary>
        public Uri Redirect => new Uri(name, UriKind.Relative);

        /// <summary>
        /// Return the encoding from HTTP header
        /// </summary>
        public Stream Open()
        {
            return assembly.GetManifestResourceStream(name);
        }
    }
}
