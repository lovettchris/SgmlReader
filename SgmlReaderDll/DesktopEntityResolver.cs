using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

namespace Sgml
{
    class FileEntityContent : IEntityContent
    {
        readonly string path;

        public FileEntityContent(string path)
        {
            this.path = path;
        }

        public Encoding Encoding => Encoding.UTF8;

        public string MimeType => string.Empty;

        public Uri Redirect => new Uri(path);

        public Stream Open()
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }
    }

    class WebEntityContent : IEntityContent
    {
        readonly WebResponse response;

        public WebEntityContent(WebResponse response)
        {
            this.response = response;
        }

        public Encoding Encoding
        {
            get
            {
                string contentType = response.ContentType.ToLowerInvariant();
                int i = contentType.IndexOf("charset");
                Encoding e = Encoding.UTF8;
                if (i >= 0)
                {
                    int j = contentType.IndexOf("=", i);
                    int k = contentType.IndexOf(";", j);
                    if (k < 0)
                        k = contentType.Length;

                    if (j > 0)
                    {
                        j++;
                        string charset = contentType.Substring(j, k - j).Trim();
                        try
                        {
                            e = Encoding.GetEncoding(charset);
                        }
                        catch (ArgumentException)
                        {
                        }
                    }
                }
                return e;
            }
        }

        public string MimeType
        {
            get
            {
                string contentType = response.ContentType.ToLowerInvariant();
                string mimeType = contentType;
                int i = contentType.IndexOf(';');
                if (i >= 0)
                {
                    mimeType = contentType.Substring(0, i);
                }
                return mimeType;
            }
        }

        public Uri Redirect => response.ResponseUri;

        public Stream Open()
        {
            return response.GetResponseStream();
        }
    }

    /// <summary>
    /// A special implementation for IEntityResolver that uses System.IO to access local files
    /// and HttpWebRequest for web content.
    /// </summary>
    public class DesktopEntityResolver : IEntityResolver
    {
        /// <summary>
        /// Get or set the WebProxy to use for web requests.
        /// </summary>
        public WebProxy Proxy { get; set; }

        /// <summary>
        /// Open the given Uri.  If the Uri is relative then it could be referring to either a local file or
        /// an embedded resource.
        /// </summary>
        /// <param name="uri">the absolute or relative Uri of the resource to load</param>
        /// <returns>The stream, or throws exception if the resource is not found</returns>
        public virtual IEntityContent GetContent(Uri uri)
        {
            if (!uri.IsAbsoluteUri)
            {
                // see if it is a local file.
                string originalUri = uri.OriginalString;
                string path = System.IO.Path.GetFullPath(originalUri);
                if (!File.Exists(path))
                {
                    // See if it is an embedded resource then
                    foreach (Assembly assembly in new Assembly[]
                    {
                        Assembly.GetExecutingAssembly(),
                        typeof(SgmlReader).Assembly,
                        Assembly.GetCallingAssembly()
                    })
                    {
                        foreach (string name in assembly.GetManifestResourceNames())
                        {
                            if (name.EndsWith(originalUri, StringComparison.OrdinalIgnoreCase))
                            {
                                return new EmbeddedResourceEntityContent(assembly, name);
                            }
                        }
                    }
                    throw new Exception("Entity not found: " + originalUri);
                }
                uri = new Uri(path);
            }

            switch (uri.Scheme)
            {
                case "file":
                    return new FileEntityContent(uri.LocalPath);

                default:
                    HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(uri);
                    wr.UserAgent = "Mozilla/4.0 (compatible;);";
                    wr.Timeout = 10000; // in case this is running in an ASPX page.
                    if (Proxy != null)
                        wr.Proxy = Proxy;
                    wr.PreAuthenticate = false;
                    // Pass the credentials of the process. 
                    wr.Credentials = CredentialCache.DefaultCredentials;

                    WebResponse resp = wr.GetResponse();

                    return new WebEntityContent(resp);
            }
        }
    }
}
