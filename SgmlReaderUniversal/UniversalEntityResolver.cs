using System;
using System.Text;
using System.IO;
using Windows.Storage;
using System.Net.Http;
using System.Reflection;
using Windows.UI.Xaml;

namespace Sgml
{
    /// <summary>
    /// A special implementation of IEntityResolver that uses the UWP Windows.Storage API's
    /// for local file access and HttpClient for web content.
    /// </summary>
    public class UniversalEntityResolver : IEntityResolver
    {
        /// <summary>
        /// Open the given Uri.  If the Uri is relative then it could be referring to either a local file or
        /// an embedded resource.
        /// </summary>
        /// <param name="uri">the absolute or relative Uri of the resource to load</param>
        /// <returns>The stream, or throws exception if the resource is not found</returns>
        public IEntityContent GetContent(Uri uri)
        {
            if (!uri.IsAbsoluteUri)
            {
                string originalUri = uri.OriginalString;
                try
                {
                    string file = System.IO.Path.GetFullPath(uri.OriginalString);
                    if (System.IO.File.Exists(file))
                    {
                        return new FileEntityContent(file);
                    }
                }
                catch (Exception)
                {
                    // access denied?
                }
                
                // Not found, so perhaps it is an embedded resource?
                var app = Application.Current;
                if (app != null)
                {
                    foreach (Assembly assembly in new Assembly[]
                    {
                            app.GetType().GetTypeInfo().Assembly,
                            typeof(SgmlReader).GetTypeInfo().Assembly
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
            }    
            if (uri.IsFile)
            {
                return new FileEntityContent(uri.OriginalString);
            }
            return new WebEntityContent(uri);
        }
    }

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
                return new Uri(this.path);
            }
        }

        public Stream Open()
        {
            StorageFile file = StorageFile.GetFileFromPathAsync(this.path).GetAwaiter().GetResult();
            return file.OpenStreamForReadAsync().Result;
        }
    }


    class WebEntityContent : IEntityContent
    {
        Uri uri;
        HttpResponseMessage response;

        public WebEntityContent(Uri uri)
        {
            this.uri = uri;
            HttpClient client = new HttpClient();
            response = client.GetAsync(uri).Result;
            response = response.EnsureSuccessStatusCode();
        }
        
        public Encoding Encoding
        {
            get
            {
                var contentType = response.Content.Headers.ContentType;
                if (contentType != null)
                {
                    string charSet = response.Content.Headers.ContentType.CharSet;
                    try
                    {
                        return  Encoding.GetEncoding(charSet);
                    }
                    catch (ArgumentException)
                    {
                    }
                }
                return Encoding.UTF8; 
            }
        }

        public string MimeType
        {
            get
            {
                var contentType = response.Content.Headers.ContentType;
                if (contentType != null)
                {
                    return contentType.MediaType;
                }
                return "text/plain";
            }
        }

        public Uri Redirect
        {
            get
            {
                // is there any way to get Http redirect Uri 
                return this.uri;
            }
        }

        public Stream Open()
        {
            return response.Content.ReadAsStreamAsync().Result;
        }
    }

}
