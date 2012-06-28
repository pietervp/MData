using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using BLToolkit.Reflection.Emit;
using MData.Attributes;
using MData.Core.Base;
using MData.Core.Configuration;
using Ninject.Extensions.Conventions.BindingBuilder;
using TypeFilter = Ninject.Extensions.Conventions.BindingBuilder.TypeFilter;

namespace MData.Core
{
    internal class TypeCreator
    {
        private readonly MDataConfigurator _configurator;
        private readonly AssemblyBuilderHelper _assemblyBuilder;
        private readonly IDictionary<Type, Type> _domainToLogic;
        private readonly ICollection<string> _scannedAssemblies;
        
        private Assembly _previouslyGeneratedAssembly;

        /// <summary>
        /// Creates a new instance of the TypeCreator class
        /// </summary>
        /// <param name="configurator"></param>
        public TypeCreator(MDataConfigurator configurator)
        {
            _configurator = configurator;
            
            _assemblyBuilder = new AssemblyBuilderHelper(_configurator.AssemblyNameForStorage, new Version(1,0),null);
            _assemblyBuilder.AssemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(SecurityRulesAttribute).GetConstructors()[0], new object[] { SecurityRuleSet.Level1 }));
            _assemblyBuilder.AssemblyName.Flags |= AssemblyNameFlags.Retargetable;
            
            _scannedAssemblies = new Collection<string>();
            _domainToLogic = new Dictionary<Type, Type>();
        }

        /// <summary>
        /// Registers an assembly. This means scanning an assembly for interfaces with the MData attribute applied.
        /// If such an interface was found in the given assembly, it gets mapped with a generic LogicBase instance,
        /// or a user specified logic class. This logic class is discovered in all loaded assemblies
        /// </summary>
        /// <param name="assembly"></param>
        internal void RegisterAssembly(Assembly assembly)
        {
            //we already scanned this assembly, no need to do it again
            if (_scannedAssemblies.Contains(assembly.GetName().ToString()))
                return;

            //if congifurator was configured with allowedAssemblies, then only scan assemblies of that collection
            if (_configurator.Assemblies != null && !_configurator.Assemblies.Any(x => x.GetName().ToString() == assembly.GetName().ToString()))
                return;

            //add the given assembly as scanned to avoid stackoverflows (GetLogicClass could do a call to this method)
            _scannedAssemblies.Add(assembly.GetName().ToString());

            //initialize variables to filter the assemblies types
            var typeFilter = new TypeFilter();
            var types = assembly.GetTypes();

            //loop all types of the assembly decortated with the MData attribute
            foreach (var domainType in types.Where(x => typeFilter.HasAttribute(x, typeof (MDataAttribute))))
            {
                //if the type was already registered, do no bother further processing
                if (_domainToLogic.ContainsKey(domainType))
                    continue;

                //discover the logic class (either userdefined or generic)
                var logicType = GetLogicClass(domainType, types, typeFilter) ?? GetLogicClass(domainType);

                //we should always get a type of the logic class, if not throw an error
                if (logicType == null)
                    throw new InvalidOperationException(string.Format("Could not find a userdefined/generic LogicBase descendant for type {0}", domainType.GetName()));
                
                //all ok, add the 'domain to logic' mapping
                _domainToLogic.Add(domainType, logicType);
            }
        }

        /// <summary>
        /// This is were the actual magic happens. A type is generated for the domaintype. The domaintype should always
        /// be an interface. 
        /// </summary>
        /// <param name="domainType">Interface decorated with MData attribute which needs a generated implementation</param>
        /// <param name="logicType">LogicBase class to be used (either userdefined or generic)</param>
        /// <returns></returns>
        internal Type RegisterDomainInterface(Type domainType, Type logicType)
        {
            //if one of the parameters is null, we cannot continue generating
            if (domainType == null || logicType == null)
                return null;

            //domaintype should always be an interface
            if (!domainType.IsInterface)
                return null;

#if !DEBUG
            //reuse of previoulsy generated assembly is only allowed in release mode
            if (!_configurator.ShouldAlwaysRecreate && File.Exists(_configurator.AssemblyNameForStorage))
                return GetCachedDomainInterface(domainType);
#endif

            //get the MData attribute of the interface domaintype
            var mDataAttribute = domainType.GetAttributes<MDataAttribute>().FirstOrDefault();

            //abort generating when no attribute found
            if (mDataAttribute == null)
                return null;

            //initialize local variables
            var generatedFields = new Dictionary<Type, FieldBuilder>();
            
            //create the new type, passing in a name, accesibility attributes, a base class and a generic type argument
            var typeBuilder = _assemblyBuilder.DefineType(GetTypeFullName(domainType, mDataAttribute.GetName()), TypeAttributes.Public, _configurator.EntityType, domainType);
            
            //make sure our new class inherits interface domainType
            typeBuilder
                .TypeBuilder
                .AddInterfaceImplementation(domainType);

            //loop all interfaces the domaintype implements, including the domaintype interface itself
            foreach (var interfaceToImplement in GetInterfaceToImplements(domainType))
            {
                //get the logical class associated with the current interface
                var logicClassType = GetLogicClass(interfaceToImplement);

                //create a new field (if necessary, see CreateLogicBaseField method)
                var logicField = CreateLogicBaseField(generatedFields, typeBuilder, interfaceToImplement, interfaceToImplement.HasAttribute<MDataMethodAttribute>(), ref logicClassType);

                //map the current interface with its logical field
                generatedFields.Add(interfaceToImplement, logicField);

                //implement all interface's methods
                MapMethods(logicField, logicClassType, interfaceToImplement, typeBuilder);

                //implement/generate all interface's properties
                MapProperties(interfaceToImplement, typeBuilder, _configurator.EntityType, logicField);
            }

            //initialize fields in the constructor
            CreateConstructorLogic(generatedFields.Select(x => x.Value), typeBuilder, _configurator.EntityType);

            //create the concrete type for domainType
            return typeBuilder.Create();
        }

        /// <summary>
        /// Creates / gets the correct field for the generated class which represents the logicbase class for a given
        /// MData interface.
        /// </summary>
        /// <param name="generatedFields"></param>
        /// <param name="typeBuilder"></param>
        /// <param name="interfaceToImplement"></param>
        /// <param name="isMethodData"></param>
        /// <param name="logicClassType"></param>
        /// <returns></returns>
        private static FieldBuilder CreateLogicBaseField(Dictionary<Type, FieldBuilder> generatedFields, TypeBuilderHelper typeBuilder, Type interfaceToImplement, bool isMethodData, ref Type logicClassType)
        {
            //not all interfaces implemented by domainType necesarly have the MData attribute
            //they could also have the MDataMethod attribute meaning that they will be implemented
            //by the same logic class as the MDataMethod attribute's parameter
            //if the interface is a MDataMethod interface, assign it to the linked logicClass
            if (isMethodData)
            {
                var linkedInterfaceType = interfaceToImplement.GetAttributes<MDataMethodAttribute>().First().GetLinkedInterfaceType();
                logicClassType = generatedFields[linkedInterfaceType].FieldType;
                return generatedFields[linkedInterfaceType];
            }

            return typeBuilder.DefineField(GetFieldFullName(logicClassType), logicClassType,FieldAttributes.FamORAssem);
        }

        /// <summary>
        /// Generates a unique fieldname to be used in the generated class
        /// </summary>
        /// <param name="logicClassType"></param>
        /// <returns></returns>
        private static string GetFieldFullName(Type logicClassType)
        {
            return "logic_" + (logicClassType.GetName() + "_" + Guid.NewGuid());
        }

        /// <summary>
        /// Generated a unique class name for the generated class.
        /// </summary>
        /// <param name="domainType"></param>
        /// <param name="userdefinedClassName"></param>
        /// <returns></returns>
        private string GetTypeFullName(Type domainType, string userdefinedClassName = null)
        {
            return _configurator.Namespace + "." + (userdefinedClassName ?? domainType.GetName() + "_" + Guid.NewGuid());
        }

        /// <summary>
        /// If in Release mode, we can make use of previously generated assembly instead of generating everything again,
        /// the loading of that pregenerated assembly is done here.
        /// </summary>
        /// <param name="domainType"></param>
        /// <returns></returns>
        private Type GetCachedDomainInterface(Type domainType)
        {
            try
            {
                //if we already have the assembly in memory, don't reload it
                if(_previouslyGeneratedAssembly == null)
                    _previouslyGeneratedAssembly = Assembly.LoadFile(new FileInfo(_configurator.AssemblyNameForStorage).FullName);

                //find the concrete type for the given domaintype
                return _previouslyGeneratedAssembly.GetTypes().FirstOrDefault(x => x.GetInterface(domainType.Name) != null);
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.FirstOrDefault(x => x != null && x.GetInterface(domainType.Name) != null);
            }
        }

        /// <summary>
        /// Retrieves all base interfaces for a given domaintype
        /// </summary>
        /// <param name="domainType"></param>
        /// <returns></returns>
        private IEnumerable<Type> GetInterfaceToImplements(Type domainType)
        {
            return from interfaceType in domainType.GetInterfaces().Union(new[] { domainType })
                   where interfaceType.GetAttributes<MDataAttribute>() != null
                         || interfaceType.GetAttributes<MDataMethodAttribute>() != null
                   orderby interfaceType.HasAttribute<MDataMethodAttribute>()
                   select interfaceType;
        }

        /// <summary>
        /// Saves the generated types to disk
        /// </summary>
        internal void SaveAssemblies()
        {
#if !DEBUG
            if(_configurator.ShouldAlwaysRecreate)
                _assemblyBuilder.Save();
#endif
        }

        /// <summary>
        /// Facilitates the search of a matching logic class with a domain interface class
        /// </summary>
        /// <param name="domainInterface"></param>
        /// <returns></returns>
        internal Type GetLogicClass(Type domainInterface)
        {
            //first check if the logic class was already loaded
            if (_domainToLogic.ContainsKey(domainInterface))
                return _domainToLogic[domainInterface];

            //we did not find the logic class, try scanning all known assemblies
            var referencedAssemblies = new[] { domainInterface.Assembly }.Union(domainInterface.Assembly.GetModules(false).Select(x => x.Assembly).Distinct());

            //loop all assemblies
            foreach (var referencedAssembly in referencedAssemblies)
            {
                RegisterAssembly(referencedAssembly);
            }

            //if there is a userdefined logic class it should now be discovered
            if (_domainToLogic.ContainsKey(domainInterface)) 
                return _domainToLogic[domainInterface];
            
            //if no userdefined logic was found, use the generic one
            return typeof (LogicBase<>).MakeGenericType(domainInterface);
        }
        
        /// <summary>
        /// Searches for a certain method (parameter methodInfo) on the logicclass. This method will look
        /// for a member definition with the same name, return type, parameters and generic arguments. If
        /// no matching method was found, the system will call UnImplementedMethodCall, etc on the LogicBase
        /// class.
        /// </summary>
        /// <param name="logicClass"></param>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        private MethodInfo GetLogicMethod(Type logicClass, MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();

            //reflection has a hard time discovering generic methods, so we have to do it ourselves
            if (methodInfo.IsGenericMethod)
                return (from mi in logicClass.GetMethods()
                        where mi.Name == methodInfo.Name
                        where mi.IsGenericMethodDefinition
                        where mi.GetParameters().Length == parameters.Length
                        where mi.ToString() == methodInfo.ToString()
                        select mi).FirstOrDefault();

            //search for the non-generic method in the given logicClass
            return logicClass.GetMethod(methodInfo.Name, parameters.Select(x => x.ParameterType).ToArray());
        }

        /// <summary>
        /// Will look for a user defined logic class in a set of types.
        /// </summary>
        /// <param name="domainType"></param>
        /// <param name="types"></param>
        /// <param name="typeFilter"></param>
        /// <returns></returns>
        private Type GetLogicClass(Type domainType, IEnumerable<Type> types, ITypeFilter typeFilter)
        {
            //we have 2 criteria to search a logic class:
            //1. look for the MDataLogic attribute
            //2. look for descendants of the LogicBase<> class
            var defaultImplementationName = domainType.Name.Substring(1);

            //filter out the list of types, and assign a score (the lowest score will win)
            //this score is used if multiple logic implementation candidates exist, then logicclasses with
            //the Mdatalogic attr go first, then logicclasses who have the default logic name convention, last all others
            var candidates = (from candidate in types
                             let score = candidate.HasAttribute<MDataLogicAttribute>() ? 0 : candidate.Name == defaultImplementationName ? 1 : 2
                             where candidate.IsAssignableFrom(typeof(LogicBase<>).MakeGenericType(domainType))
                             orderby score 
                             select candidate)
                             .ToList();

            //if multiple logic class had the MDataLogic attribute throw an exception because this is not normal
            if(candidates.Count(x=> x.HasAttribute<MDataLogicAttribute>()) > 1)
                throw new FoundMultipleLogicImplementationsException(domainType, candidates);

            //return the best candidate
            return candidates.FirstOrDefault(); 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mDataInferface"></param>
        /// <param name="generatedTypeBuilder"></param>
        /// <param name="entityBase"></param>
        /// <param name="logicField"></param>
        private void MapProperties(Type mDataInferface, TypeBuilderHelper generatedTypeBuilder, Type entityBase,
                                   FieldBuilder logicField)
        {
            foreach (PropertyInfo p in mDataInferface.GetProperties())
            {
                PropertyBuilder property = generatedTypeBuilder.TypeBuilder.DefineProperty(p.Name,
                                                                                           PropertyAttributes.None,
                                                                                           p.PropertyType, null);

                MethodInfo getDecl = p.GetGetMethod();
                MethodInfo setDecl = p.GetSetMethod();

                CreatePropertyGetMethod(generatedTypeBuilder, p, property, getDecl, entityBase, logicField);
                CreatePropertySetMethod(generatedTypeBuilder, p, property, setDecl, entityBase, logicField);
            }
        }

        private void CreatePropertyGetMethod(TypeBuilderHelper generatedTypeBuilder, PropertyInfo p,
                                             PropertyBuilder property, MethodInfo getDecl, Type entityBase,
                                             FieldBuilder logicField)
        {
            string name = getDecl == null ? "get_" + p.Name : getDecl.Name;
            Type returnType = getDecl == null ? p.PropertyType : getDecl.ReturnType;
            Type[] parameterTypes = getDecl == null
                                        ? null
                                        : getDecl.GetParameters().Select(x => x.ParameterType).ToArray();
            MethodAttributes methodAttributes = getDecl == null
                                                    ? MethodAttributes.Private
                                                    : MethodAttributes.Public | MethodAttributes.Virtual;

            MethodBuilderHelper getBuilder = generatedTypeBuilder.DefineMethod(name, methodAttributes,
                                                                               returnType,
                                                                               parameterTypes);

            getBuilder
                .Emitter
                .nop
                .ldarg_0
                .ldfld(logicField)
                .ldstr(p.Name)
                .callvirt(Reflect<LogicBase>.GetMethod(x=>x.GetProperty<object>(string.Empty)).MakeGenericMethod(p.PropertyType))
                .ret();

            property.SetGetMethod(getBuilder);
        }

        private void CreatePropertySetMethod(TypeBuilderHelper generatedTypeBuilder, PropertyInfo p,
                                             PropertyBuilder property, MethodInfo setDecl, Type entityBase,
                                             FieldBuilder logicField)
        {
            string name = setDecl == null ? "set_" + p.Name : setDecl.Name;
            Type[] parameterTypes = setDecl == null
                                        ? new[] {p.PropertyType}
                                        : setDecl.GetParameters().Select(x => x.ParameterType).ToArray();
            MethodAttributes methodAttributes = setDecl == null
                                                    ? MethodAttributes.Private
                                                    : MethodAttributes.Public | MethodAttributes.Virtual;
            MethodBuilderHelper setBuilder = generatedTypeBuilder.DefineMethod(name, methodAttributes, null,
                                                                               parameterTypes);

            setBuilder
                .Emitter
                .ldarg_0
                .ldfld(logicField)
                .ldstr(p.Name)
                .ldarg_1
                .callvirt(Reflect<LogicBase>.GetMethod(x=> x.SetProperty(string.Empty, string.Empty)).MakeGenericMethod(p.PropertyType))
                .nop
                .ret();

            property.SetSetMethod(setBuilder);
        }

        private void CreateConstructorLogic(IEnumerable<FieldBuilder> fieldsToInit,
                                            TypeBuilderHelper generatedTypeBuilder, Type entityBase)
        {
            EmitHelper constructor = generatedTypeBuilder.DefinePublicConstructor().Emitter;

            constructor = constructor
                .ldarg_0
                .call(entityBase.GetConstructor(new Type[] {}));

            foreach (FieldBuilder fieldBuilder in fieldsToInit)
            {
                constructor
                    .ldarg_0
                    .newobj(fieldBuilder.FieldType)
                    .stfld(fieldBuilder)
                    .ldarg_0
                    .ldfld(fieldBuilder)
                    .ldarg_0
                    .callvirt(
                        typeof (LogicBase<>).MakeGenericType(fieldBuilder.FieldType).GetMethod("set_CurrentInstance"));
            }

            constructor.ret();
        }

        private void MapMethods(FieldInfo logicField, Type logicClass, Type mDataInferface,
                                TypeBuilderHelper type)
        {
            //loop all methods of the interface definition
            foreach (MethodInfo methodInfo in mDataInferface.GetMethods())
            {
                //only implement Methods
                if (methodInfo.MemberType != MemberTypes.Method)
                    continue;

                //get_ and set_ methods are special cases, implement them as properties
                if (methodInfo.Name.StartsWith("get_") || methodInfo.Name.StartsWith("set_"))
                    continue;

                ParameterInfo[] methodParameters = methodInfo.GetParameters();
                Type[] methodGenericArguments = methodInfo.GetGenericArguments();
                MethodBuilderHelper methodBuilder;

                //check if we got to define a generic method or not
                if (methodGenericArguments.Length > 0)
                {
                    methodBuilder = type.DefineGenericMethod(methodInfo.Name,
                                                             MethodAttributes.Virtual | MethodAttributes.Public,
                                                             CallingConventions.Standard, methodGenericArguments,
                                                             methodInfo.ReturnType,
                                                             methodParameters.Select(x => x.ParameterType).ToArray());
                }
                else
                {
                    methodBuilder = type.DefineMethod(methodInfo.Name,
                                                      MethodAttributes.Virtual | MethodAttributes.Public,
                                                      methodInfo.ReturnType,
                                                      methodParameters.Select(x => x.ParameterType).ToArray());
                }

                //get hold of the method emitter
                EmitHelper emit = methodBuilder.Emitter;

                if (methodParameters.Any())
                    emit.DeclareLocal(typeof (object[]));

                //load 'this' instance
                emit
                    .ldarg(0)
                    .ldfld(logicField);

                MethodInfo logicMethod = GetLogicMethod(logicClass, methodInfo);

                if (logicMethod == null)
                {
                    logicMethod = methodInfo.ReturnType == typeof (void)
                                      ? Reflect<LogicBase>.GetMethod(x=>x.UnImplementedNoReturnMethodCall(string.Empty))
                                      : Reflect<LogicBase>.GetMethod(x => x.UnImplementedMethodCall<object>(string.Empty)).MakeGenericMethod(methodInfo.ReturnType);

                    emit = emit
                        .ldc_i4_(methodParameters.Count())
                        .newarr(typeof (object));

                    if (methodParameters.Any())
                    {
                        emit = emit.stloc_0;

                        for (int index = 0; index < methodParameters.Length; index++)
                        {
                            emit
                                .ldloc_0
                                .ldc_i4_(index)
                                .ldarg(index + 1);

                            if (methodParameters[index].ParameterType.IsValueType ||
                                methodParameters[index].ParameterType.IsGenericParameter)
                                emit.box(methodParameters[index].ParameterType);

                            emit = emit
                                .stelem_ref;
                        }

                        emit = emit
                            .ldstr(methodInfo.Name)
                            .ldloc_0;
                    }
                    else
                        emit.ldstr(methodInfo.Name);
                }
                else
                {
                    //load all method parameters onto stack
                    foreach (ParameterInfo parameterInfo in methodParameters)
                    {
                        emit.ldarg(parameterInfo);
                    }
                }

                //call the logic class
                emit
                    .callvirt(logicMethod)
                    .ret();
            }
        }
    }
}