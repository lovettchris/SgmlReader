using System;
using System.IO;
using System.Text;

namespace Sgml
{
    /// <summary>
    /// Since .NET Portable has no FileStream and a completey differnet way to load manifest resource streams
    /// we invent an interface here to abstract out the difference from SgmlReader.  Any time SgmlReader needs
    /// to load a DTD or extern DTD entity it will use this interface instead.
    /// </summary>
    public interface IEntityResolver
    {
        /// <summary>
        /// Open the given Uri.  If the Uri is relative then it could be referring to either a local file or
        /// an embedded resource.
        /// </summary>
        /// <param name="uri">the absolute or relative Uri of the resource to load</param>
        /// <returns>The stream, or throws exception if the resource is not found</returns>
        IEntityContent GetContent(Uri uri);
    }
}
