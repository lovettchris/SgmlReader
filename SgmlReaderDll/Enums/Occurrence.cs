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

#if WINDOWS_DESKTOP
using System.Runtime.Serialization;
using System.Security.Permissions;
#endif

namespace Sgml
{
    /// <summary>
    /// Qualifies the occurrence of a child element within a content model group.
    /// </summary>
    public enum Occurrence
    {
        /// <summary>
        /// The element is required and must occur only once.
        /// </summary>
        Required,
        
        /// <summary>
        /// The element is optional and must occur once at most.
        /// </summary>
        Optional,
        
        /// <summary>
        /// The element is optional and can be repeated.
        /// </summary>
        ZeroOrMore,
        
        /// <summary>
        /// The element must occur at least once or more times.
        /// </summary>
        OneOrMore
    }
}
