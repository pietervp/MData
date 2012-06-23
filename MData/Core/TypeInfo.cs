using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BLToolkit.Reflection.Emit;
using Ninject;
using Ninject.Extensions.Conventions.BindingBuilder;

namespace MData.Core
{
    public class TypeInfo
    {
        private static TypeInfo _instance;
        public static TypeInfo Instance{get { return _instance ?? (_instance = new TypeInfo()); }}
        internal StandardKernel Kernel { get; set; }

        private readonly AssemblyBuilderHelper _assemblyBuilder;
        private readonly Dictionary<Type, Type> _domainToLogic = new Dictionary<Type, Type>(); 

        private TypeInfo()
        {
            _assemblyBuilder = new AssemblyBuilderHelper(@".\MData.Generated.Entities.dll");
            Kernel = new StandardKernel();
        }

        public void RegisterAssembly()
        {
            var callingAssembly = Assembly.GetCallingAssembly();
            var typeFilter = new Ninject.Extensions.Conventions.BindingBuilder.TypeFilter();
            var types = callingAssembly.GetTypes();

            foreach (var domainType in types.Where(x=> typeFilter.HasAttribute(x, typeof(MDataDataAttribute))))
            {
                var logicType = GetLogicClass(domainType, types, typeFilter) ?? GetLogicClass(domainType);

                if (logicType == null) 
                    continue;

                _domainToLogic.Add(domainType, logicType);
            }

            foreach (var type in _domainToLogic)
            {
                RegisterDomainInterface(type.Key, type.Value);
            }
        }

        public void RegisterDomainInterface(Type domainType, Type logicType, Type baseType = null)
        {
            if (domainType == null || logicType == null)
                return;

            if (!domainType.IsInterface)
                return;

            var generatedFields = new List<FieldBuilder>();
            var baseClass = baseType ?? typeof(EntityBase<>).MakeGenericType(domainType);
            var mDataAttribute = domainType.GetAttributes<MDataDataAttribute>().FirstOrDefault();
            var toGenerateClassName = mDataAttribute == null ? null : mDataAttribute.Name;
            var typeBuilder = _assemblyBuilder.DefineType(toGenerateClassName ?? domainType.Name + "_" + Guid.NewGuid(), TypeAttributes.Sealed | TypeAttributes.Public, baseClass, domainType);

            //make sure our new class inherits interface T
            typeBuilder
                .TypeBuilder
                .AddInterfaceImplementation(domainType);

            //generate all interface implementations
            foreach (var interfaceToImplement in domainType.GetInterfaces().Where(x => x.GetAttributes<MDataDataAttribute>() != null).Union(new[] { domainType }))
            {
                //skip the generation process if there is no MData Attribute
                if (interfaceToImplement.GetAttributes<MDataDataAttribute>().FirstOrDefault() == null)
                    continue;

                var logicClassType = GetLogicClass(interfaceToImplement);
                var logicField = typeBuilder.DefineField("logic_" + (logicClassType.Name + "_" + Guid.NewGuid()), logicClassType, FieldAttributes.FamORAssem);

                generatedFields.Add(logicField);

                MapMethods(logicField, logicClassType, interfaceToImplement, typeBuilder);
                MapProperties(interfaceToImplement, typeBuilder, baseClass);
            }

            //initialize field
            CreateConstructorLogic(generatedFields, typeBuilder, baseClass);

            typeBuilder.Create();

            Kernel.Bind(domainType).To(typeBuilder.Create());
        }

        public static void SaveAssemblies()
        {
            Instance._assemblyBuilder.Save();
        }

        private Type GetLogicClass(Type mDataInferface)
        {
            return _domainToLogic.ContainsKey(mDataInferface)
                       ? _domainToLogic[mDataInferface]
                       : typeof (BaseLogic<>).MakeGenericType(mDataInferface);
        }

        public static T Resolve<T>()
        {
            return Instance.Kernel.Get<T>();
        }
        
        public void RegisterDomainInterface<T>(Type logicType) where T : class
        {
            RegisterDomainInterface(typeof(T), logicType);
        }

        private static void MapProperties(Type mDataInferface, TypeBuilderHelper generatedTypeBuilder, Type entityBase)
        {
            foreach (var p in mDataInferface.GetProperties())
            {
                var property = generatedTypeBuilder.TypeBuilder.DefineProperty(p.Name, PropertyAttributes.None, p.PropertyType, null);

                var getDecl = p.GetGetMethod();
                var setDecl = p.GetSetMethod();

                CreatePropertyGetMethod(generatedTypeBuilder, p, property, getDecl, entityBase);
                CreatePropertySetMethod(generatedTypeBuilder, p, property, setDecl, entityBase);
            }
        }

        private static void CreatePropertyGetMethod(TypeBuilderHelper generatedTypeBuilder, PropertyInfo p,
                                                    PropertyBuilder property, MethodInfo getDecl, Type entityBase)
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
                .ldstr(p.Name)
                .call(entityBase.GetMethod("GetProperty").MakeGenericMethod(p.PropertyType))
                .ret();

            property.SetGetMethod(getBuilder);
        }

        private static void CreatePropertySetMethod(TypeBuilderHelper generatedTypeBuilder, PropertyInfo p,
                                                    PropertyBuilder property, MethodInfo setDecl, Type entityBase)
        {
            var name = setDecl == null ? "set_" + p.Name : setDecl.Name;
            var parameterTypes = setDecl == null ? new[] { p.PropertyType } : setDecl.GetParameters().Select(x => x.ParameterType).ToArray();
            var methodAttributes = setDecl == null ? MethodAttributes.Private : MethodAttributes.Public | MethodAttributes.Virtual;
            var setBuilder = generatedTypeBuilder.DefineMethod(name, methodAttributes,null,parameterTypes);

            setBuilder
                .Emitter
                .ldarg_0
                .ldstr(p.Name)
                .ldarg_1
                .call(entityBase.GetMethod("SetProperty").MakeGenericMethod(p.PropertyType))
                .nop
                .ret();

            property.SetSetMethod(setBuilder);
        }

        private static void CreateConstructorLogic(IEnumerable<FieldBuilder> fieldsToInit, TypeBuilderHelper generatedTypeBuilder, Type entityBase)
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
                    .callvirt(typeof(BaseLogic<>).MakeGenericType(fieldBuilder.FieldType).GetMethod("set_CurrentInstance"));
            }

            constructor.ret();
        }

        private static void MapMethods(FieldInfo logicField, Type logicClass, Type mDataInferface,
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

                var logicMethod = LogicMethod(logicClass, methodInfo);

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

        private static MethodInfo LogicMethod(Type logicClass, MethodInfo methodInfo)
        {
            var parameterInfos = methodInfo.GetParameters();

            if(methodInfo.IsGenericMethod)
                return (from mi in logicClass.GetMethods()
                              where mi.Name == methodInfo.Name
                              where mi.IsGenericMethodDefinition
                              where mi.GetParameters().Length == parameterInfos.Length
                              where mi.ToString() == methodInfo.ToString()
                              select mi).FirstOrDefault();

            return logicClass.GetMethod(methodInfo.Name, parameterInfos.Select(x => x.ParameterType).ToArray());
        }

        private static Type GetLogicClass(Type domainType, IEnumerable<Type> types, ITypeFilter typeFilter)
        {
            var name = domainType.Name;
            var defaultImplementationName = name.Substring(1);

            return types.Where(x => typeFilter.IsTypeInheritedFromAny(x, new[] { typeof(BaseLogic<>).MakeGenericType(domainType) })).OrderBy(x => x.GetAttributes<MDataLogicAttribute>().Any() ? 0 : x.Name == defaultImplementationName ? 1 : 2).FirstOrDefault();
        }

        public static Assembly GetGeneratedAssembly()
        {
            return Instance._assemblyBuilder.AssemblyBuilder;
        }
    }
}