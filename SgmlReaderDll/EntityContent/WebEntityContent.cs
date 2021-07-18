using System;
using System.IO;
using System.Net;
using System.Text;

namespace Sgml
{
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
}
