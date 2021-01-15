﻿using System;
using System.Collections.Generic;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata;
using AsmResolver.PE.DotNet.Metadata.Blob;
using AsmResolver.PE.DotNet.Metadata.Tables;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace AsmResolver.DotNet.Serialized
{
    /// <summary>
    /// Represents a lazily initialized implementation of <see cref="MethodSpecification"/>  that is read from a
    /// .NET metadata image. 
    /// </summary>
    public class SerializedMethodSpecification : MethodSpecification
    {
        private readonly ModuleReadContext _context;
        private readonly MethodSpecificationRow _row;
        
        /// <summary>
        /// Creates a method specification from a method specification metadata row.
        /// </summary>
        /// <param name="context">The reader context.</param>
        /// <param name="token">The token to initialize the method specification for.</param>
        /// <param name="row">The metadata table row to base the method specification on.</param>
        public SerializedMethodSpecification(ModuleReadContext context, MetadataToken token, in MethodSpecificationRow row)
            : base(token)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _row = row;
        }

        /// <inheritdoc />
        protected override IMethodDefOrRef GetMethod()
        {
            var encoder = _context.Image.DotNetDirectory.Metadata
                .GetStream<TablesStream>()
                .GetIndexEncoder(CodedIndex.MethodDefOrRef);
            
            var methodToken = encoder.DecodeIndex(_row.Method);
            return _context.ParentModule.TryLookupMember(methodToken, out var member)
                ? member as IMethodDefOrRef
                : null;
        }

        /// <inheritdoc />
        protected override GenericInstanceMethodSignature GetSignature()
        {
            var reader = _context.Image.DotNetDirectory.Metadata
                .GetStream<BlobStream>()
                .GetBlobReaderByIndex(_row.Instantiation);
            
            return GenericInstanceMethodSignature.FromReader(new BlobReadContext(_context), reader);
        }

        /// <inheritdoc />
        protected override IList<CustomAttribute> GetCustomAttributes() => 
            _context.ParentModule.GetCustomAttributeCollection(this);
    }
}