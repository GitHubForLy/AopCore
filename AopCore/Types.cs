using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace AopCore
{
    internal sealed class Types
    {
        AssemblyDefinition m_Assembly;
        AssemblyDefinition m_myAssemby;
        public Types(AssemblyDefinition assembly)
        {
            m_Assembly = assembly;
            m_myAssemby=AssemblyDefinition.ReadAssembly(Assembly.GetExecutingAssembly().Location);

            MethodHookAttrType = assembly.MainModule.ImportReference(m_myAssemby.MainModule.GetType(typeof(MethodHookAttribute).FullName));
            FiledHookAttrType = assembly.MainModule.ImportReference(m_myAssemby.MainModule.GetType(typeof(FiledHookAttribute).FullName));
            MethodExecuteArgsType = assembly.MainModule.ImportReference(m_myAssemby.MainModule.GetType(typeof(MethodExecuteArgs).FullName));

            //Resolve之后就还原成原来程序集的类型了就白导入了 所以此处设为局部变量
            var MethodHookAttrTypeDefention = MethodHookAttrType.Resolve();
            MethodHookEnter = assembly.MainModule.ImportReference(MethodHookAttrTypeDefention.Methods.First(m => m.Name == nameof(MethodHookAttribute.OnMethodEnter)));
            MethodHookLeave = assembly.MainModule.ImportReference(MethodHookAttrTypeDefention.Methods.First(m => m.Name == nameof(MethodHookAttribute.OnMethodLeave)));
            MethodHookAttrCtor = assembly.MainModule.ImportReference(MethodHookAttrTypeDefention.Methods.First(m => m.Name ==".ctor"));

            //Resolve之后就还原成原来程序集的类型了就白导入了 所以此处设为局部变量
            var MethodExecuteArgsTypeDefention = MethodExecuteArgsType.Resolve();
            MethodExecuteArgsCtor = assembly.MainModule.ImportReference(MethodExecuteArgsTypeDefention.Methods.First(m => m.Name == ".ctor"));
            MethodExecuteArgs_ParameterValues_Set = assembly.MainModule.ImportReference(MethodExecuteArgsTypeDefention.Methods.First(m => m.Name == "set_" + nameof(MethodExecuteArgs.ParameterValues)));
            MethodExecuteArgs_ParameterValues_Get = assembly.MainModule.ImportReference(MethodExecuteArgsTypeDefention.Methods.First(m => m.Name == "get_" + nameof(MethodExecuteArgs.ParameterValues)));

            var FiledHookAttrTypeDefention = FiledHookAttrType.Resolve();
            FiledHookAttrCtor=assembly.MainModule.ImportReference(FiledHookAttrTypeDefention.Methods.First(m=>m.Name==".ctor"));
            FiledHookAttrSetValueMethod= assembly.MainModule.ImportReference(FiledHookAttrTypeDefention.Methods.First(m => m.Name == nameof(FiledHookAttribute.SetValue)));
            FiledHookAttrOnSetValueMethod = assembly.MainModule.ImportReference(FiledHookAttrTypeDefention.Methods.First(m => m.Name == nameof(FiledHookAttribute.OnSetValue)));

            var MethodBaseType = assembly.MainModule.ImportReference(typeof(MethodBase)).Resolve();
            Sys_GetCurrentMethodType =assembly.MainModule.ImportReference(MethodBaseType.Methods.First(m => m.Name == nameof(MethodBase.GetCurrentMethod)));

            ObjectType = assembly.MainModule.ImportReference(typeof(object));
            Sys_Int32 = assembly.MainModule.ImportReference(typeof(Int32));
            Sys_Void = assembly.MainModule.ImportReference(typeof(void));
            Sys_MethodInfo = assembly.MainModule.ImportReference(typeof(MethodInfo));
            Sys_FieldInfo = assembly.MainModule.ImportReference(typeof(FieldInfo));

            var typedef = assembly.MainModule.ImportReference(typeof(Type)).Resolve();

            Sys_GetTypeMethod = assembly.MainModule.ImportReference(ObjectType.Resolve().Methods.First(m => m.Name == nameof(object.GetType)));
            Sys_GetFieldInfoMethod = assembly.MainModule.ImportReference(typedef.Methods.First(m => m.Name == nameof(Type.GetField)&& m.Parameters.Count==1));
        }

        /// <summary>
        /// 获取目标程序集的字段信息
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public FieldInfo GetTargetFieldInfo(FieldDefinition field)
        {
            var assembly = Assembly.ReflectionOnlyLoadFrom(m_Assembly.MainModule.FileName);
            return assembly.GetType(field.DeclaringType.FullName).GetField(field.Name);
        }

        /// <summary>
        /// <see cref="AopCore.MethodHookAttribute"/>类型
        /// </summary>
        public TypeReference MethodHookAttrType { get; }

        /// <summary>
        /// <see cref="AopCore.FiledHookAttribute"/>类型
        /// </summary>
        public TypeReference FiledHookAttrType { get; }
        /// <summary>
        /// <see cref="AopCore.FiledHookAttribute"/>构造方法
        /// </summary>
        public MethodReference FiledHookAttrCtor { get; }

        /// <summary>
        /// <see cref="AopCore.FiledHookAttribute.SetValue(string, string, object)"/>方法
        /// </summary>
        public MethodReference FiledHookAttrSetValueMethod { get; }

        /// <summary>
        /// <see cref="AopCore.FiledHookAttribute.OnSetValue(FieldInfo, object)"/>方法
        /// </summary>
        public MethodReference FiledHookAttrOnSetValueMethod { get; }

        /// <summary>
        /// <see cref="AopCore.MethodHookAttribute"/>构造方法
        /// </summary>
        public MethodReference MethodHookAttrCtor { get; }
        /// <summary>
        /// <see cref="AopCore.MethodHookAttribute.OnMethodEnter"/>方法
        /// </summary>
        public MethodReference MethodHookEnter { get; }
        /// <summary>
        /// <see cref="AopCore.MethodHookAttribute.OnMethodLeave"/>方法
        /// </summary>
        public MethodReference MethodHookLeave { get; }
        /// <summary>
        /// object类型
        /// </summary>
        public TypeReference ObjectType { get; }

        /// <summary>
        /// <see cref="AopCore.MethodExecuteArgs"/>类型
        /// </summary>
        public TypeReference MethodExecuteArgsType { get; }
        /// <summary>
        /// <see cref="AopCore.MethodExecuteArgs"/>的构造函数
        /// </summary>
        public MethodReference MethodExecuteArgsCtor { get; }
        /// <summary>
        /// <see cref="System.Reflection.MethodBase.GetCurrentMethod"/>方法
        /// </summary>
        public MethodReference Sys_GetCurrentMethodType { get; }
        /// <summary>
        /// <see cref="AopCore.MethodExecuteArgs.ParameterValues"/>的Set方法
        /// </summary>
        public MethodReference MethodExecuteArgs_ParameterValues_Set { get; }
        /// <summary>
        /// <see cref="AopCore.MethodExecuteArgs.ParameterValues"/>的Get方法
        /// </summary>
        public MethodReference MethodExecuteArgs_ParameterValues_Get { get; }
        /// <summary>
        /// <see cref="System.Int32"/>类型
        /// </summary>
        public TypeReference Sys_Int32 { get;}

        public TypeReference Sys_Void { get; }
        public TypeReference Sys_MethodInfo { get; }
        public TypeReference Sys_FieldInfo { get; }
        public MethodReference Sys_GetTypeMethod { get;}
        public MethodReference Sys_GetFieldInfoMethod { get; }
    }
}
