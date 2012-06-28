using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BLToolkit.Reflection.Emit;
using Ninject.Extensions.Conventions.BindingBuilder;

namespace MData.Core
{
    internal class TypeCreator
    {
        private readonly AssemblyBuilderHelper _assemblyBuilder;

        private readonly IDictionary<Type, Type> _domainToLogic;
        private readonly ICollection<string> _scannedAssemblies;

        public TypeCreator()
        {
            _assemblyBuilder = new AssemblyBuilderHelper(@".\MData.Generated.Entities.dll");
            _scannedAssemblies = new Collection<string>();
            _domainToLogic = new Dictionary<Type, Type>();
        }

        internal void RegisterAssembly(Assembly assembly)
        {
            if (_scannedAssemblies.Contains(assembly.GetName().ToString()))
                return;

            _scannedAssemblies.Add(assembly.GetName().ToString());

            var typeFilter = new Ninject.Extensions.Conventions.BindingBuilder.TypeFilter();
            var types = assembly.GetTypes();

            foreach (var domainType in types.Where(x => typeFilter.HasAttribute(x, typeof(MDataAttribute))))
            {
                if (_domainToLogic.ContainsKey(domainType))
                    continue;

                var logicType = GetLogicClass(domainType, types, typeFilter) ?? GetLogicClass(domainType);

                if (logicType == null)
                    continue;

                _domainToLogic.Add(domainType, logicType);
            }
        }

        internal void RegisterAssembly()
        {
            RegisterAssembly(Assembly.GetCallingAssembly());
        }

        internal Type RegisterDomainInterface(Type domainType, Type logicType, Type baseType = null)
        {
            if (domainType == null || logicType == null)
                return null;

            if (!domainType.IsInterface)
                return null;

            var generatedFields = new Dictionary<Type,FieldBuilder>();
            var baseClass = baseType ?? typeof(EntityBase);
            var mDataAttribute = domainType.GetAttributes<MDataAttribute>().FirstOrDefault();
            var toGenerateClassName = mDataAttribute == null ? null : mDataAttribute.GetName();
            var typeBuilder = _assemblyBuilder.DefineType(toGenerateClassName ?? domainType.Name + "_" + Guid.NewGuid(), TypeAttributes.Sealed | TypeAttributes.Public, baseClass, domainType);

            //make sure our new class inherits interface T
            typeBuilder
                .TypeBuilder
                .AddInterfaceImplementation(domainType);

            //generate all interface implementations
            foreach (var interfaceToImplement in domainType.GetInterfaces().Where(x => x.GetAttributes<MDataAttribute>() != null || x.GetAttributes<MDataMethodAttribute>() != null).Union(new[] { domainType }).OrderBy(x=> x.HasAttribute<MDataMethodAttribute>()))
            {
                FieldBuilder logicField;
             
                var isMethodData = interfaceToImplement.HasAttribute<MDataMethodAttribute>();
                var logicClassType = GetLogicClass(interfaceToImplement);

                if (isMethodData)
                {
                    logicField = generatedFields[interfaceToImplement.GetAttributes<MDataMethodAttribute>().First().GetLinkedInterfaceType()];
                    logicClassType = logicField.FieldType;
                }
                else
                    logicField = typeBuilder.DefineField("logic_" + (logicClassType.GetName() + "_" + Guid.NewGuid()), logicClassType, FieldAttributes.FamORAssem);

                generatedFields.Add(interfaceToImplement, logicField);

                MapMethods(logicField, logicClassType, interfaceToImplement, typeBuilder);
                MapProperties(interfaceToImplement, typeBuilder, baseClass, logicField);
            }

            //initialize field
            CreateConstructorLogic(generatedFields.Select(x=>x.Value), typeBuilder, baseClass);

            return typeBuilder.Create();
        }

        internal void SaveAssemblies()
        {
            _assemblyBuilder.Save();
        }

        internal Type GetLogicClass(Type mDataInferface)
        {
            return _domainToLogic.ContainsKey(mDataInferface)
                       ? _domainToLogic[mDataInferface]
                       : SearchLogicClass(mDataInferface);
        }

        internal Type SearchLogicClass(Type mDataInferface)
        {
            RegisterAssembly(mDataInferface.Assembly);

            foreach (var referencedAssembly in mDataInferface.Assembly.GetModules(false).Select(x=> x.Assembly).Distinct())
            {
                RegisterAssembly(referencedAssembly);
            }

            return _domainToLogic.ContainsKey(mDataInferface) ? _domainToLogic[mDataInferface] : typeof (LogicBase<>).MakeGenericType(mDataInferface);
        }

        internal Type RegisterDomainInterface<T>(Type logicType) where T : class
        {
             return RegisterDomainInterface(typeof(T), logicType);
        }

        internal Assembly GetGeneratedAssembly()
        {
            return _assemblyBuilder.AssemblyBuilder;
        }
        
        private MethodInfo GetLogicMethod(Type logicClass, MethodInfo methodInfo)
        {
            var parameterInfos = methodInfo.GetParameters();

            if (methodInfo.IsGenericMethod)
                return (from mi in logicClass.GetMethods()
                        where mi.Name == methodInfo.Name
                        where mi.IsGenericMethodDefinition
                        where mi.GetParameters().Length == parameterInfos.Length
                        where mi.ToString() == methodInfo.ToString()
                        select mi).FirstOrDefault();

            return logicClass.GetMethod(methodInfo.Name, parameterInfos.Select(x => x.ParameterType).ToArray());
        }

        private Type GetLogicClass(Type domainType, IEnumerable<Type> types, ITypeFilter typeFilter)
        {
            var name = domainType.Name;
            var defaultImplementationName = name.Substring(1);

            return types.Where(x => typeFilter.IsTypeInheritedFromAny(x, new[] { typeof(LogicBase<>).MakeGenericType(domainType) })).OrderBy(x => x.GetAttributes<MDataLogicAttribute>().Any() ? 0 : x.Name == defaultImplementationName ? 1 : 2).FirstOrDefault();
        }

        private void MapProperties(Type mDataInferface, TypeBuilderHelper generatedTypeBuilder, Type entityBase, FieldBuilder logicField)
        {
            foreach (var p in mDataInferface.GetProperties())
            {
                var property = generatedTypeBuilder.TypeBuilder.DefineProperty(p.Name, PropertyAttributes.None, p.PropertyType, null);

                var getDecl = p.GetGetMethod();
                var setDecl = p.GetSetMethod();

                CreatePropertyGetMethod(generatedTypeBuilder, p, property, getDecl, entityBase, logicField);
                CreatePropertySetMethod(generatedTypeBuilder, p, property, setDecl, entityBase, logicField);
            }
        }

        private void CreatePropertyGetMethod(TypeBuilderHelper generatedTypeBuilder, PropertyInfo p, PropertyBuilder property, MethodInfo getDecl, Type entityBase, FieldBuilder logicField)
        {
            var name = getDecl == null ? "get_" + p.Name : getDecl.Name;
            var returnType = getDecl == null ? p.PropertyType : getDecl.ReturnType;
            var parameterTypes = getDecl == null ? null : getDecl.GetParameters().Select(x => x.ParameterType).ToArray();
            var methodAttributes = getDecl == null ? MethodAttributes.Private  : MethodAttributes.Public | MethodAttributes.Virtual;

            var getBuilder = generatedTypeBuilder.DefineMethod(name, methodAttributes,
                                                               returnType,
                                                               parameterTypes);

            getBuilder
                .Emitter
                .nop
                .ldarg_0
                .ldfld(logicField)
                .ldstr(p.Name)
                .callvirt(logicField.FieldType.GetMethod("GetProperty").MakeGenericMethod(p.PropertyType))
                .ret();

            property.SetGetMethod(getBuilder);
        }

        private void CreatePropertySetMethod(TypeBuilderHelper generatedTypeBuilder, PropertyInfo p, PropertyBuilder property, MethodInfo setDecl, Type entityBase, FieldBuilder logicField)
        {
            var name = setDecl == null ? "set_" + p.Name : setDecl.Name;
            var parameterTypes = setDecl == null ? new[] { p.PropertyType } : setDecl.GetParameters().Select(x => x.ParameterType).ToArray();
            var methodAttributes = setDecl == null ? MethodAttributes.Private : MethodAttributes.Public | MethodAttributes.Virtual;
            var setBuilder = generatedTypeBuilder.DefineMethod(name, methodAttributes,null,parameterTypes);

            setBuilder
                .Emitter
                .ldarg_0
                .ldfld(logicField)
                .ldstr(p.Name)
                .ldarg_1
                .callvirt(logicField.FieldType.GetMethod("SetProperty").MakeGenericMethod(p.PropertyType))
                .nop
                .ret();

            property.SetSetMethod(setBuilder);
        }

        private void CreateConstructorLogic(IEnumerable<FieldBuilder> fieldsToInit, TypeBuilderHelper generatedTypeBuilder, Type entityBase)
        {
            var constructor = generatedTypeBuilder.DefinePublicConstructor().Emitter;

            constructor = constructor
                .ldarg_0
                .call(entityBase.GetConstructor(new Type[] { }));

            foreach (var fieldBuilder in fieldsToInit)
            {
                constructor
                    .ldarg_0
                    .newobj(fieldBuilder.FieldType)
                    .stfld(fieldBuilder)
                    .ldarg_0
                    .ldfld(fieldBuilder)
                    .ldarg_0
                    .callvirt(typeof(LogicBase<>).MakeGenericType(fieldBuilder.FieldType).GetMethod("set_CurrentInstance"));
            }

            constructor.ret();
        }

        private void MapMethods(FieldInfo logicField, Type logicClass, Type mDataInferface,
                                       TypeBuilderHelper type)
        {
            //loop all methods of the interface definition
            foreach (var methodInfo in mDataInferface.GetMethods())
            {
                //only implement Methods
                if(methodInfo.MemberType != MemberTypes.Method)
                    continue;

                //get_ and set_ methods are special cases, implement them as properties
                if(methodInfo.Name.StartsWith("get_") || methodInfo.Name.StartsWith("set_"))
                    continue;
                
                var methodParameters = methodInfo.GetParameters();
                var methodGenericArguments = methodInfo.GetGenericArguments();
                MethodBuilderHelper methodBuilder;

                //check if we got to define a generic method or not
                if (methodGenericArguments.Length > 0)
                {
                    methodBuilder = type.DefineGenericMethod(methodInfo.Name, MethodAttributes.Virtual | MethodAttributes.Public, CallingConventions.Standard, methodGenericArguments,
                                                        methodInfo.ReturnType,
                                                        methodParameters.Select(x => x.ParameterType).ToArray());
                }
                else
                {
                    methodBuilder = type.DefineMethod(methodInfo.Name, MethodAttributes.Virtual | MethodAttributes.Public,
                                                        methodInfo.ReturnType,
                                                        methodParameters.Select(x => x.ParameterType).ToArray());
                }

                //get hold of the method emitter
                var emit = methodBuilder.Emitter;

                if (methodParameters.Any())
                    emit.DeclareLocal(typeof(object[]));

                //load 'this' instance
                emit
                    .ldarg(0)
                    .ldfld(logicField);

                var logicMethod = GetLogicMethod(logicClass, methodInfo);

                if (logicMethod == null)
                {
                    logicMethod = methodInfo.ReturnType == typeof (void)
                                      ? logicClass.GetMethod("UnImplementedNoReturnMethodCall")
                                      : logicClass.GetMethod("UnImplementedMethodCall").MakeGenericMethod(methodInfo.ReturnType);

                    emit = emit
                        .ldc_i4_(methodParameters.Count())
                        .newarr(typeof(object));

                    if (methodParameters.Any())
                    {
                        emit = emit.stloc_0;

                        for (int index = 0; index < methodParameters.Length; index++)
                        {
                            emit
                                .ldloc_0
                                .ldc_i4_(index)
                                .ldarg(index + 1);

                            if (methodParameters[index].ParameterType.IsValueType || methodParameters[index].ParameterType.IsGenericParameter)
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
                    foreach (var parameterInfo in methodParameters)
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