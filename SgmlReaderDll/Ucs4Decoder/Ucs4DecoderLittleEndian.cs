/*
 * Copyright (c) 2020 Microsoft Corporation. All rights reserved.
 * Modified work Copyright (c) 2008 MindTouch. All rights reserved. 
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 */

using System;
#if WINDOWS_DESKTOP
using System.Runtime.Serialization;
using System.Security.Permissions;
#endif

namespace Sgml
{
    internal class Ucs4DecoderLittleEndian : Ucs4Decoder
    {
        internal override int GetFullChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) 
        {
            UInt32 code;
            int i, j;
            byteCount += byteIndex;
            for (i = byteIndex, j = charIndex; i + 3 < byteCount; )
            {
                code = (UInt32)(((bytes[i]) << 24) | (bytes[i + 1] << 16) | (bytes[i + 2] << 8) | (bytes[i + 3]));
                if (code > 0x10FFFF) 
                {
                    throw new SgmlParseException($"Invalid character 0x{code:x} in encoding");
                } 
                else if (code > 0xFFFF) 
                {
                    chars[j] = UnicodeToUTF16(code);
                    j++;
                } 
                else if (code >= 0xD800 && code <= 0xDFFF) 
                {
                    throw new SgmlParseException($"Invalid character 0x{code:x} in encoding");
                } 
                else 
                {
                    chars[j] = (char)code;
                }                
                j++;
                i += 4;
            }
            return j - charIndex;
        }
    }
}
