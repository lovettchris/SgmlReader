using System;
using System.IO;
using System.Text;

namespace Sgml
{
    /// <summary>
    /// An interface that represents a loadable XML entity returned from IEntityResolver.
    /// </summary>
    public interface IEntityContent
    {
        /// <summary>
        /// Open the stream
        /// </summary>
        /// <returns></returns>
        Stream Open();

        /// <summary>
        /// Return the encoding from HTTP header
        /// </summary>
        Encoding Encoding { get; }

        /// <summary>
        /// Return the HTTP ContentType
        /// </summary>
        string MimeType { get; }

        /// <summary>
        /// Returns the redirect Uri if an HTTP redirect happened during the fetching of this resource.
        /// </summary>
        Uri Redirect { get; }
    }
}
