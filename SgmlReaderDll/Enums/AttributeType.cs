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
    /// Defines the different possible attribute types.
    /// </summary>
    public enum AttributeType
    {
        /// <summary>
        /// Attribute type not specified.
        /// </summary>
        Default,

        /// <summary>
        /// The attribute contains text (with no markup).
        /// </summary>
        CDATA,
        
        /// <summary>
        /// The attribute contains an entity declared in a DTD.
        /// </summary>
        ENTITY,

        /// <summary>
        /// The attribute contains a number of entities declared in a DTD.
        /// </summary>
        ENTITIES,
        
        /// <summary>
        /// The attribute is an id attribute uniquely identifie the element it appears on.
        /// </summary>
        ID,
        
        /// <summary>
        /// The attribute value can be any declared subdocument or data entity name.
        /// </summary>
        IDREF,
        
        /// <summary>
        /// The attribute value is a list of (space separated) declared subdocument or data entity names.
        /// </summary>
        IDREFS,
        
        /// <summary>
        /// The attribute value is a SGML Name.
        /// </summary>
        NAME,
        
        /// <summary>
        /// The attribute value is a list of (space separated) SGML Names.
        /// </summary>
        NAMES,
        
        /// <summary>
        /// The attribute value is an XML name token (i.e. contains only name characters, but in this case with digits and other valid name characters accepted as the first character).
        /// </summary>
        NMTOKEN,

        /// <summary>
        /// The attribute value is a list of (space separated) XML NMTokens.
        /// </summary>
        NMTOKENS,

        /// <summary>
        /// The attribute value is a number.
        /// </summary>
        NUMBER,
        
        /// <summary>
        /// The attribute value is a list of (space separated) numbers.
        /// </summary>
        NUMBERS,
        
        /// <summary>
        /// The attribute value is a number token (i.e. a name that starts with a number).
        /// </summary>
        NUTOKEN,
        
        /// <summary>
        /// The attribute value is a list of number tokens.
        /// </summary>
        NUTOKENS,
        
        /// <summary>
        /// Attribute value is a member of the bracketed list of notation names that qualifies this reserved name.
        /// </summary>
        NOTATION,
        
        /// <summary>
        /// The attribute value is one of a set of allowed names.
        /// </summary>
        ENUMERATION
    }
}
