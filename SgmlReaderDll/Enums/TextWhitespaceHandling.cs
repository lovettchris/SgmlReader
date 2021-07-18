/*
 * Copyright (c) 2020 Microsoft Corporation. All rights reserved.
 * Modified work Copyright (c) 2008 MindTouch. All rights reserved. 
 * s
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 */

using System;

namespace Sgml
{
    /// <summary>
    /// Due to the nature of adjacent self-closing and auto-closing SGML elements the <see cref="SgmlReader"/>'s default behavior may cause unwanted whitespace to appear at either end of the element's text content. This enum instructs <see cref="SgmlReader"/> that such whitespace is not significant-whitespace.<br />
    /// Note that this is a <see cref="FlagsAttribute"/>-enum.
    /// </summary>
    /// <remarks>Internally strings are not actually trimmed using <see cref="String.Trim(char[])"/>, but instead by skipping leading and trailing whitespace during the read-phase - this avoids unnecessary string copying and heap-allocation.</remarks>
    [Flags]
    public enum TextWhitespaceHandling
    {
        /// <summary>
        /// <c>0x00</c> - All leading and trailing whitespace in <c>InnerText</c> and <c>Value</c> properties is kept verbatim. This is the default behavior.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// <c>0x01</c> - Leading whitespace will not be present in <c>TextContent</c> and <c>Value</c> properties.
        /// </summary>
        TrimLeading  = 1,

        /// <summary>
        /// <c>0x02</c> - Trailing whitespace will not be present in <c>TextContent</c> and <c>Value</c> properties.
        /// </summary>
        TrimTrailing = 2,

        /// <summary>
        /// <c>0x04</c> - This flag only takes effect when either or both <see cref="TrimLeading"/> and <see cref="TrimTrailing"/> are set. When this flag is set, only line-break characters <c>\r</c> (CR) and <c>\n</c> (LF) will be trimmed.
        /// </summary>
        OnlyLineBreaks = 4,

        /// <summary>
        /// <c>0x03</c> - (This is a combination of flags). Neither leading nor trailing whitespace will be be present in <c>TextContent</c> and <c>Value</c> properties.
        /// </summary>
        TrimBoth  = TrimLeading | TrimTrailing,

        /// <summary>
        /// <c>0x06</c> - (This is a combination of flags). Only trailing <c>\r</c> (CR) and <c>\n</c> (LF) characters will be trimmed from trailing whitespace in in <c>InnerText</c> and <c>Value</c> properties.
        /// </summary>
        OnlyTrailingLineBreaks = OnlyLineBreaks | TrimTrailing
    }
}