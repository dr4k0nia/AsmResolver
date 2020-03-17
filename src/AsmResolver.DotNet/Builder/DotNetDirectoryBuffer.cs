﻿using System.Collections.Generic;
using AsmResolver.PE.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace AsmResolver.DotNet.Builder
{
    /// <summary>
    /// Provides a mutable buffer for building up a .NET data directory, containing all metadata relevant for the
    /// execution of a .NET assembly.
    /// </summary>
    public partial class DotNetDirectoryBuffer
    {
        /// <summary>
        /// Creates a new .NET data directory buffer.
        /// </summary>
        /// <param name="module">The module for which this .NET directory is built.</param>
        /// <param name="methodBodySerializer">The method body serializer to use for constructing method bodies.</param>
        /// <param name="metadata">The metadata builder </param>
        public DotNetDirectoryBuffer(ModuleDefinition module, IMethodBodySerializer methodBodySerializer, IMetadataBuffer metadata)
        {
            Module = module;
            MethodBodySerializer = methodBodySerializer;
            Metadata = metadata;
            Resources = new DotNetResourcesDirectoryBuffer();
        }
        
        /// <summary>
        /// Gets the module for which this .NET directory is built.
        /// </summary>
        public ModuleDefinition Module
        {
            get;
        }

        /// <summary>
        /// Gets the method body serializer to use for constructing method bodies.
        /// </summary>
        public IMethodBodySerializer MethodBodySerializer
        {
            get;
        }

        /// <summary>
        /// Gets the metadata directory buffer, containing the metadata stream buffers.
        /// </summary>
        public IMetadataBuffer Metadata
        {
            get;
        }

        /// <summary>
        /// Gets the .NET resources data directory buffer, containing all the resources data stored in the .NET module. 
        /// </summary>
        public DotNetResourcesDirectoryBuffer Resources
        {
            get;
        }

        private void AssertIsImported(IModuleProvider member)
        {
            if (member.Module != Module)
                throw new MemberNotImportedException((IMetadataMember) member);
        }

        /// <summary>
        /// Builds the .NET data directory from the buffer. 
        /// </summary>
        /// <returns></returns>
        public IDotNetDirectory CreateDirectory()
        {
            var directory = new DotNetDirectory();
            directory.Metadata = Metadata.CreateMetadata();
            directory.DotNetResources = Resources.Size > 0 ? Resources.CreateDirectory() : null;
            directory.Entrypoint = GetEntrypoint();
            directory.Flags = Module.Attributes;
            return directory;
        }

        private uint GetEntrypoint()
        {
            if (Module.ManagedEntrypoint is null)
                return 0;
            
            var entrypointToken = MetadataToken.Zero;
            
            switch (Module.ManagedEntrypoint.MetadataToken.Table)
            {
                case TableIndex.Method:
                    entrypointToken = GetMethodDefinitionToken(Module.ManagedEntrypointMethod);
                    break;
                
                case TableIndex.File:
                    //todo:
                    break;
            }

            return entrypointToken.ToUInt32();
        }

        private MetadataToken AddAssemblyReference(AssemblyReference assembly)
        {
            AssertIsImported(assembly);
            
            var table = Metadata.TablesStream.GetTable<AssemblyReferenceRow>(TableIndex.AssemblyRef);

            var row = new AssemblyReferenceRow((ushort) assembly.Version.Major,
                (ushort) assembly.Version.Minor,
                (ushort) assembly.Version.Build,
                (ushort) assembly.Version.Revision,
                assembly.Attributes,
                Metadata.BlobStream.GetBlobIndex(assembly.PublicKeyOrToken),
                Metadata.StringsStream.GetStringIndex(assembly.Name),
                Metadata.StringsStream.GetStringIndex(assembly.Culture),
                Metadata.BlobStream.GetBlobIndex(assembly.HashValue));

            var token = table.Add(row, assembly.MetadataToken.Rid);
            AddCustomAttributes(token, assembly);
            return token;
        }

        /// <summary>
        /// Adds a type reference to the .NET data directory buffer. 
        /// </summary>
        /// <param name="type">The reference to the type to add.</param>
        /// <returns>The metadata token of the added type reference.</returns>
        /// <remarks>
        /// Depending on the implementation of the underlying metadata buffers, this might reuse an old type reference
        /// that was added before. 
        /// </remarks>
        public MetadataToken AddTypeReference(TypeReference type)
        {
            if (type == null)
                return 0;
            
            AssertIsImported(type);
            
            var table = Metadata.TablesStream.GetTable<TypeReferenceRow>(TableIndex.TypeRef);
            var row = new TypeReferenceRow(
                AddResolutionScope( type.Scope),
                Metadata.StringsStream.GetStringIndex(type.Name),
                Metadata.StringsStream.GetStringIndex(type.Namespace));

            var token = table.Add(row, type.MetadataToken.Rid);
            AddCustomAttributes(token, type);
            return token;
        }

        /// <summary>
        /// Adds a member reference to the .NET data directory buffer. 
        /// </summary>
        /// <param name="member">The reference to the member to add.</param>
        /// <returns>The metadata token of the added member reference.</returns>
        /// <remarks>
        /// Depending on the implementation of the underlying metadata buffers, this might reuse an old member reference
        /// that was added before. 
        /// </remarks>
        public MetadataToken AddMemberReference(MemberReference member)
        {
            AssertIsImported(member);
            
            var table = Metadata.TablesStream.GetTable<MemberReferenceRow>(TableIndex.MemberRef);
            var row = new MemberReferenceRow(
                AddMemberRefParent(member.Parent),
                Metadata.StringsStream.GetStringIndex(member.Name),
                Metadata.BlobStream.GetBlobIndex(this, member.Signature));
            
            var token = table.Add(row, member.MetadataToken.Rid);
            AddCustomAttributes(token, member);
            return token;
        }

        /// <summary>
        /// Adds a type specification to the .NET data directory buffer. 
        /// </summary>
        /// <param name="type">The specification of the type to add.</param>
        /// <returns>The metadata token of the added type specification.</returns>
        /// <remarks>
        /// Depending on the implementation of the underlying metadata buffers, this might reuse an old type specification
        /// that was added before. 
        /// </remarks>
        public MetadataToken AddTypeSpecification(TypeSpecification type)
        {
            AssertIsImported(type);
            
            var table = Metadata.TablesStream.GetTable<TypeSpecificationRow>(TableIndex.TypeSpec);
            var row = new TypeSpecificationRow(Metadata.BlobStream.GetBlobIndex(this, type.Signature));
            
            var token = table.Add(row, type.MetadataToken.Rid);
            AddCustomAttributes(token, type);
            return token;
        }

        /// <summary>
        /// Adds a stand-alone signature to the .NET data directory buffer. 
        /// </summary>
        /// <param name="signature">The stand-alone signature to add.</param>
        /// <returns>The metadata token of the added stand-alone signature.</returns>
        /// <remarks>
        /// Depending on the implementation of the underlying metadata buffers, this might reuse an old stand-alone
        /// signature that was added before. 
        /// </remarks>
        public MetadataToken AddStandAloneSignature(StandAloneSignature signature)
        {
            var table = Metadata.TablesStream.GetTable<StandAloneSignatureRow>(TableIndex.StandAloneSig);
            var row = new StandAloneSignatureRow(
                Metadata.BlobStream.GetBlobIndex(this, signature.Signature));
            
            var token = table.Add(row, signature.MetadataToken.Rid);
            AddCustomAttributes(token, signature);
            return token;
        }

        /// <summary>
        /// Adds a method specification to the .NET data directory buffer. 
        /// </summary>
        /// <param name="method">The specification of the method to add.</param>
        /// <returns>The metadata token of the added method specification.</returns>
        /// <remarks>
        /// Depending on the implementation of the underlying metadata buffers, this might reuse an old method
        /// specification that was added before. 
        /// </remarks>
        public MetadataToken AddMethodSpecification(MethodSpecification method)
        {
            var table = Metadata.TablesStream.GetTable<MethodSpecificationRow>(TableIndex.MethodSpec);
            var row = new MethodSpecificationRow(
                AddMethodDefOrRef(method.Method),
                Metadata.BlobStream.GetBlobIndex(this, method.Signature));
            
            var token = table.Add(row, method.MetadataToken.Rid);
            AddCustomAttributes(token, method);
            return token;
        }

        private void AddCustomAttributes(MetadataToken ownerToken, IHasCustomAttribute provider)
        {
            foreach (var attribute in provider.CustomAttributes)
                AddCustomAttribute(ownerToken, attribute);
        }

        private void AddCustomAttribute(MetadataToken ownerToken, CustomAttribute attribute)
        {
            var table = Metadata.TablesStream.GetTable<CustomAttributeRow>(TableIndex.CustomAttribute);

            var encoder = Metadata.TablesStream.GetIndexEncoder(CodedIndex.HasCustomAttribute);
            var row = new CustomAttributeRow(
                encoder.EncodeToken(ownerToken),
                AddCustomAttributeType(attribute.Constructor),
                Metadata.BlobStream.GetBlobIndex(this, attribute.Signature));

            table.Add(row, attribute.MetadataToken.Rid);
        }
        
        private void AddMethodSemantics(MetadataToken ownerToken, IHasSemantics provider)
        {
            foreach (var semantics in provider.Semantics)
                AddMethodSemantics(ownerToken, semantics);
        }

        private MetadataToken AddMethodSemantics(MetadataToken ownerToken, MethodSemantics semantics)
        {
            var table = Metadata.TablesStream.GetTable<MethodSemanticsRow>(TableIndex.MethodSemantics);
            var encoder = Metadata.TablesStream.GetIndexEncoder(CodedIndex.HasSemantics);

            var row = new MethodSemanticsRow(
                semantics.Attributes,
                GetMethodDefinitionToken(semantics.Method).Rid,
                encoder.EncodeToken(ownerToken)
            );

            var token = table.Add(row, semantics.MetadataToken.Rid);
            return token;
        }

        private void AddInterfaces(MetadataToken ownerToken, IEnumerable<InterfaceImplementation> interfaces)
        {
            var table = Metadata.TablesStream.GetTable<InterfaceImplementationRow>(TableIndex.InterfaceImpl);

            foreach (var implementation in interfaces)
            {
                var row = new InterfaceImplementationRow(ownerToken.Rid, AddTypeDefOrRef(implementation.Interface));
                var token = table.Add(row, 0);
                AddCustomAttributes(token, implementation);
            }
        }

        private void AddGenericParameters(MetadataToken ownerToken, IHasGenericParameters provider)
        {
            foreach (var parameter in provider.GenericParameters)
                AddGenericParameter(ownerToken, parameter);
        }

        private MetadataToken AddGenericParameter(MetadataToken ownerToken, GenericParameter parameter)
        {
            if (parameter is null)
                return 0;

            AssertIsImported(parameter);

            var table = Metadata.TablesStream.GetTable<GenericParameterRow>(TableIndex.GenericParam);
            var encoder = Metadata.TablesStream.GetIndexEncoder(CodedIndex.TypeOrMethodDef);
            
            var row = new GenericParameterRow(
                parameter.Number, 
                parameter.Attributes, 
                encoder.EncodeToken(ownerToken),
                Metadata.StringsStream.GetStringIndex(parameter.Name));

            var token = table.Add(row, parameter.MetadataToken.Rid);
            
            AddCustomAttributes(token, parameter);

            foreach (var constraint in parameter.Constraints)
                AddGenericParameterConstraint(token, constraint);
            
            return token;
        }

        private MetadataToken AddGenericParameterConstraint(MetadataToken ownerToken,
            GenericParameterConstraint constraint)
        {
            if (constraint is null)
                return 0;
            
            var table = Metadata.TablesStream.GetTable<GenericParameterConstraintRow>(TableIndex.GenericParamConstraint);
            
            var row = new GenericParameterConstraintRow(ownerToken.Rid, AddTypeDefOrRef(constraint.Constraint));

            var token = table.Add(row, constraint.MetadataToken.Rid);
            AddCustomAttributes(token, constraint);
            
            return token;
        }

        private MetadataToken AddClassLayout(MetadataToken ownerToken, ClassLayout layout)
        {
            var table = Metadata.TablesStream.GetTable<ClassLayoutRow>(TableIndex.ClassLayout);

            var row = new ClassLayoutRow(layout.PackingSize, layout.ClassSize, ownerToken.Rid);
            return table.Add(row, layout.MetadataToken.Rid);
        }
        
        private MetadataToken AddModuleReference(ModuleReference reference)
        {
            AssertIsImported(reference);
            
            var table = Metadata.TablesStream.GetTable<ModuleReferenceRow>(TableIndex.ModuleRef);

            var row = new ModuleReferenceRow(Metadata.StringsStream.GetStringIndex(reference.Name));
            var token = table.Add(row, reference.MetadataToken.Rid);
            AddCustomAttributes(token, reference);
            return token;
        }
    }
}