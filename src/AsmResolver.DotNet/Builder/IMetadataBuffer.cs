﻿using AsmResolver.DotNet.Builder.Blob;
using AsmResolver.DotNet.Builder.Guid;
using AsmResolver.DotNet.Builder.Strings;
using AsmResolver.DotNet.Builder.Tables;
using AsmResolver.DotNet.Builder.UserStrings;
using AsmResolver.PE.DotNet.Metadata;
using AsmResolver.PE.DotNet.Metadata.UserStrings;

namespace AsmResolver.DotNet.Builder
{
    /// <summary>
    /// Provides access to various metadata stream buffers, including tables, strings, user-strings, GUID and blob
    /// streams.  
    /// </summary>
    public interface IMetadataBuffer
    {
        /// <summary>
        /// Gets the mutable blob stream (#Blob) buffer of this metadata directory.  
        /// </summary>
        BlobStreamBuffer BlobStream
        {
            get;
        }

        /// <summary>
        /// Gets the mutable strings stream (#Strings) buffer of this metadata directory.  
        /// </summary>
        StringsStreamBuffer StringsStream
        {
            get;
        }

        /// <summary>
        /// Gets the mutable user-strings stream (#US) buffer of this metadata directory.  
        /// </summary>
        UserStringsStreamBuffer UserStringsStream
        {
            get;
        }
        
        /// <summary>
        /// Gets the mutable GUID stream (#GUID) buffer of this metadata directory.  
        /// </summary>
        GuidStreamBuffer GuidStream
        {
            get;
        }
        
        /// <summary>
        /// Gets the mutable tables stream (#~ or #-) buffer of this metadata directory.  
        /// </summary>
        TablesStreamBuffer TablesStream
        {
            get;
        }

        /// <summary>
        /// Flushes all metadata stream buffers and builds up a new metadata directory. 
        /// </summary>
        /// <returns>The constructed metadata directory.</returns>
        IMetadata CreateMetadata();
    }
}