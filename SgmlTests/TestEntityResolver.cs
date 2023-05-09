/*
 * Modified Work Copyright (c) 2021 Microsoft Corporation. All rights reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 */

using Sgml;
using System;
using System.IO;
using System.Text;

namespace SgmlTests
{
    internal class TestEntityResolver : DesktopEntityResolver
    {
        public override IEntityContent GetContent(Uri uri)
        {
            var literal = uri.OriginalString;
            if (literal == "htmlinline.dtd")
            {
                return new EmbeddedResourceEntityContent(this.GetType().Assembly, literal) { MimeType = "text/html" };
            }
            else if (literal == "ofx160.dtd")
            {
                return new EmbeddedResourceEntityContent(this.GetType().Assembly, literal) { MimeType = "text/ofx" };
            }
            else if (literal == "wikipedia.html")
            {
                return new EmbeddedResourceEntityContent(this.GetType().Assembly, "wikipedia.html") { MimeType = "text /html" };
            }

            return base.GetContent(uri);
        }
    }
}
