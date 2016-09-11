using Sgml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sgml
{
    class FileEntityContent : IEntityContent
    {
        string path;

        public FileEntityContent(string path)
        {
            this.path = path;
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
                return "";
            }
        }

        public Uri Redirect
        {
            get
            {
                return new Uri(path);
            }
        }

        public Stream Open()
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }
    }

    class WebEntityContent : IEntityContent
    {
        WebResponse response;

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

        public Uri Redirect
        {
            get
            {
                return response.ResponseUri;
            }
        }

        public Stream Open()
        {
            return response.GetResponseStream();
        }
    }

    public class DesktopEntityResolver : IEntityResolver
    {
        public DesktopEntityResolver()
        {
        }

        public WebProxy Proxy { get; set; }

        public IEntityContent GetContent(Uri uri)
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
