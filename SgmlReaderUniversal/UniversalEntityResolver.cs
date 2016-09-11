using Sgml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Windows.Storage;
using System.Net.Http;
using System.Reflection;
using Windows.UI.Xaml;

namespace Sgml
{
    public class UniversalEntityResolver : IEntityResolver
    {
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
            StorageFile file = StorageFile.GetFileFromPathAsync(this.path).GetResults();
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
