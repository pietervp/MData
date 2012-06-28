using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using BLToolkit.Reflection.Emit;
using Ninject.Extensions.Conventions.BindingBuilder;
using TypeFilter = Ninject.Extensions.Conventions.BindingBuilder.TypeFilter;

namespace MData.Core
{
    internal class TypeCreator
    {
        private MDataConfigurator Configurator { get; set; }
        private readonly AssemblyBuilderHelper _assemblyBuilder;

        private readonly IDictionary<Type, Type> _domainToLogic;
        private readonly ICollection<string> _scannedAssemblies;
        private Assembly _previouslyGeneratedAssembly;

        public TypeCreator(MDataConfigurator configurator)
        {
            Configurator = configurator;
            _assemblyBuilder = new AssemblyBuilderHelper(Configurator.AssemblyNameForStorage, new Version(1,0),null);

            _assemblyBuilder.AssemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(SecurityRulesAttribute).GetConstructors()[0], new object[] { SecurityRuleSet.Level1 }));
            _assemblyBuilder.AssemblyName.Flags |= AssemblyNameFlags.Retargetable;
            
            _scannedAssemblies = new Collection<string>();
            _domainToLogic = new Dictionary<Type, Type>();
        }

        internal void RegisterAssembly(Assembly assembly)
        {
            if (_scannedAssemblies.Contains(assembly.GetName().ToString()))
                return;

            if (Configurator.Assemblies != null && !Configurator.Assemblies.Any(x => x.GetName().ToString() == assembly.GetName().ToString()))
                return;

            _scannedAssemblies.Add(assembly.GetName().ToString());

            var typeFilter = new TypeFilter();
            Type[] types = assembly.GetTypes();

            foreach (Type domainType in types.Where(x => typeFilter.HasAttribute(x, typeof (MDataAttribute))))
            {
                if (_domainToLogic.ContainsKey(domainType))
                    continue;

                Type logicType = GetLogicClass(domainType, types, typeFilter) ?? GetLogicClass(domainType);

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

#if !DEBUG
            if (!Configurator.ShouldAlwaysRecreate && File.Exists(Configurator.AssemblyNameForStorage))
                return GetCachedDomainInterface(domainType);
#endif

            var mDataAttribute = domainType.GetAttributes<MDataAttribute>().FirstOrDefault();

            if (mDataAttribute == null)
                return null;

            var generatedFields = new Dictionary<Type, FieldBuilder>();
            var baseClass = baseType ?? Configurator.EntityType;
            var typeBuilder = _assemblyBuilder.DefineType(GetTypeFullName(domainType, mDataAttribute.GetName()), TypeAttributes.Public, baseClass, domainType);
            
            //make sure our new class inherits interface T
            typeBuilder
                .TypeBuilder
                .AddInterfaceImplementation(domainType);

            //generate all interface implementations
            foreach (Type interfaceToImplement in GetInterfaceToImplements(domainType))
            {
                FieldBuilder logicField;

                bool isMethodData = interfaceToImplement.HasAttribute<MDataMethodAttribute>();
                Type logicClassType = GetLogicClass(interfaceToImplement);

                if (isMethodData)
                {
                    var linkedInterfaceType = interfaceToImplement.GetAttributes<MDataMethodAttribute>().First().GetLinkedInterfaceType();
                    logicField = generatedFields[linkedInterfaceType];
                    logicClassType = logicField.FieldType;
                }
                else
                    logicField = typeBuilder.DefineField("logic_" + (logicClassType.GetName() + "_" + Guid.NewGuid()),
                                                         logicClassType, FieldAttributes.FamORAssem);

                generatedFields.Add(interfaceToImplement, logicField);

                MapMethods(logicField, logicClassType, interfaceToImplement, typeBuilder);
                MapProperties(interfaceToImplement, typeBuilder, baseClass, logicField);
            }

            //initialize field
            CreateConstructorLogic(generatedFields.Select(x => x.Value), typeBuilder, baseClass);

            return typeBuilder.Create();
        }

        private Type GetCachedDomainInterface(Type domainType)
        {
            try
            {
                if(_previouslyGeneratedAssembly == null)
                    _previouslyGeneratedAssembly = Assembly.LoadFile(new FileInfo(Configurator.AssemblyNameForStorage).FullName);

                return _previouslyGeneratedAssembly.GetTypes().FirstOrDefault(x => x.GetInterface(domainType.Name) != null);
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.FirstOrDefault(x => x != null && x.GetInterface(domainType.Name) != null);
            }
        }

        private string GetTypeFullName(Type domainType, string toGenerateClassName)
        {
            return Configurator.Namespace + "." + (toGenerateClassName ?? domainType.Name + "_" + Guid.NewGuid());
        }

        private static IEnumerable<Type> GetInterfaceToImplements(Type domainType)
        {
            return domainType.GetInterfaces().Where(
                x =>
                x.GetAttributes<MDataAttribute>() != null || x.GetAttributes<MDataMethodAttribute>() != null).
                Union(new[] {domainType}).OrderBy(x => x.HasAttribute<MDataMethodAttribute>());
        }

        internal void SaveAssemblies()
        {
#if !DEBUG
            if(Configurator.ShouldAlwaysRecreate)
                _assemblyBuilder.Save();
#endif
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

            foreach (Assembly referencedAssembly in mDataInferface.Assembly.GetModules(false).Select(x => x.Assembly).Distinct())
            {
                RegisterAssembly(referencedAssembly);
            }

            return _domainToLogic.ContainsKey(mDataInferface)
                       ? _domainToLogic[mDataInferface]
                       : typeof (LogicBase<>).MakeGenericType(mDataInferface);
        }

        internal Type RegisterDomainInterface<T>(Type logicType) where T : class
        {
            return RegisterDomainInterface(typeof (T), logicType);
        }

        internal Assembly GetGeneratedAssembly()
        {
            return _assemblyBuilder.AssemblyBuilder;
        }

        private MethodInfo GetLogicMethod(Type logicClass, MethodInfo methodInfo)
        {
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();

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
            string name = domainType.Name;
            string defaultImplementationName = name.Substring(1);
            
            var candidates = types.Where(x => typeFilter.IsTypeInheritedFromAny(x, new[] {typeof (LogicBase<>).MakeGenericType(domainType)})).ToList();

            if(candidates.Count(x=> x.GetAttributes<MDataLogicAttribute>().Any())> 1)
                throw new FoundMultipleLogicImplementationsException(domainType, candidates);

            return
                candidates
                    .OrderBy( x =>
                        x.GetAttributes<MDataLogicAttribute>().Any() ? 0 : x.Name == defaultImplementationName ? 1 : 2)
                    .FirstOrDefault();
        }

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

    public class Reflect<T>
    {
        public static MethodInfo GetMethod(Expression<Action<T>> membercall)
        {
            var methodCallExpression = membercall.Body as MethodCallExpression;
            if (methodCallExpression != null)
            {
                if(methodCallExpression.Method.IsGenericMethod)
                    return methodCallExpression.Method.GetGenericMethodDefinition();

                return methodCallExpression.Method;
            }

            return  null;
        }
    }

    public class FoundMultipleLogicImplementationsException : Exception
    {
        public Type DomainType { get; set; }
        public IEnumerable<Type> Candidates { get; set; }
        
        public FoundMultipleLogicImplementationsException(Type domainType, IEnumerable<Type> candidates)
        {
            DomainType = domainType;
            Candidates = candidates;
        }

        public override string Message
        {
            get
            {
                return string.Format("Found multiple logical implementations for {0}: '{1}'", DomainType.Name, Candidates.Select(x => x.GetName()).Aggregate((x, y) => x + ", " + y));
            }
        }
    }
}