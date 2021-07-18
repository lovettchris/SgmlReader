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

namespace Sgml
{
    /// <summary>
    /// SGML is case insensitive, so here you can choose between converting
    /// to lower case or upper case tags.  "None" means that the case is left
    /// alone, except that end tags will be folded to match the start tags.
    /// </summary>
    public enum CaseFolding
    {
        /// <summary>
        /// Do not convert case, except for converting end tags to match start tags.
        /// </summary>
        None,

        /// <summary>
        /// Convert tags to upper case.
        /// </summary>
        ToUpper,

        /// <summary>
        /// Convert tags to lower case.
        /// </summary>
        ToLower
    }
}