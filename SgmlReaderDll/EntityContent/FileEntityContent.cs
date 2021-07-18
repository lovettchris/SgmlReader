using System;
using System.IO;
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
}
