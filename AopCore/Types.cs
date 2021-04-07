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
        public Types(AssemblyDefinition assembly)
        {
            m_Assembly = assembly;

            MethodHookAttrType=assembly.MainModule.ImportReference(typeof(MethodHookAttribute));
            FiledHookAttrType = assembly.MainModule.ImportReference(typeof(FiledHookAttribute));
            MethodExecuteArgsType = assembly.MainModule.ImportReference(typeof(MethodExecuteArgs));

            MethodHookEnter = assembly.MainModule.ImportReference(typeof(MethodHookAttribute).GetMethod(nameof(MethodHookAttribute.OnMethodEnter)));
            MethodHookLeave = assembly.MainModule.ImportReference(typeof(MethodHookAttribute).GetMethod(nameof(MethodHookAttribute.OnMethodLeave)));
            MethodHookAttrCtor = assembly.MainModule.ImportReference(typeof(MethodHookAttribute).GetConstructor(Type.EmptyTypes));

            MethodExecuteArgsCtor = assembly.MainModule.ImportReference(typeof(MethodExecuteArgs).GetConstructor(new Type[] { typeof(MethodBase)}));
            MethodExecuteArgs_ParameterValues_Get = assembly.MainModule.ImportReference(typeof(MethodExecuteArgs).GetProperty(nameof(MethodExecuteArgs.ParameterValues)).GetGetMethod());
            MethodExecuteArgs_ParameterValues_Set = assembly.MainModule.ImportReference(typeof(MethodExecuteArgs).GetProperty(nameof(MethodExecuteArgs.ParameterValues)).GetSetMethod());

            FiledHookAttrCtor = assembly.MainModule.ImportReference(typeof(FiledHookAttribute).GetConstructor(Type.EmptyTypes));
            FiledHookAttrOnSetValueMethod = assembly.MainModule.ImportReference(typeof(FiledHookAttribute).GetMethod(nameof(FiledHookAttribute.OnSetValue)));

            Sys_GetCurrentMethodType = assembly.MainModule.ImportReference(typeof(MethodBase).GetMethod(nameof(MethodBase.GetCurrentMethod)));

            ObjectType = assembly.MainModule.ImportReference(typeof(object));
            Sys_Int32 = assembly.MainModule.ImportReference(typeof(int));
            Sys_Void = assembly.MainModule.ImportReference(typeof(void));
            Sys_MethodInfo = assembly.MainModule.ImportReference(typeof(MethodInfo));
            Sys_FieldInfo = assembly.MainModule.ImportReference(typeof(FieldInfo));

            Sys_GetTypeMethod = assembly.MainModule.ImportReference(typeof(object).GetMethod(nameof(object.GetType)));
            Sys_GetFieldInfoMethod = assembly.MainModule.ImportReference(typeof(Type).GetMethod(nameof(Type.GetField),new Type[] { typeof(string)}));
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
