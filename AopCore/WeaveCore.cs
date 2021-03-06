using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AopCore
{
    internal class WeaveCore
    {
        private AssemblyDefinition m_Assemby;
        private AssemblyDefinition m_MainAssemby;
        private INotify m_Notify;
        private Types m_types;

        private Dictionary<FieldDefinition, MethodDefinition> replaceFields = new Dictionary<FieldDefinition, MethodDefinition>();
        private List<(TypeDefinition classdef,FieldDefinition field)> fieldinfoFields = new List<(TypeDefinition, FieldDefinition)>();
        public WeaveCore(string assembly,string[] serarchPaths,INotify notify=null)
        {
            ReaderParameters readerParameters = new ReaderParameters();
            readerParameters.ReadWrite = true;
            DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver();
            if(serarchPaths!=null)
            {
                foreach (var path in serarchPaths)
                    assemblyResolver.AddSearchDirectory(path);
            }
            readerParameters.AssemblyResolver = assemblyResolver;

            //readerParameters.InMemory = true;
            m_MainAssemby = AssemblyDefinition.ReadAssembly(assembly, readerParameters);
            m_Notify = notify;
        }

        public void Weave(bool weaveDenpendceny=false)
        {
            var references = m_MainAssemby.MainModule.AssemblyReferences;
            WeaveAssembly(m_MainAssemby);

            if(weaveDenpendceny)
                m_Notify?.Notify(NotifyLevel.Message, "开始处理依赖项");
            if(weaveDenpendceny)
            {
                var myass = System.Reflection.Assembly.GetCallingAssembly();
                foreach (var ass in references)
                {
                    if (ass.Name == "mscorlib")
                        continue;
                    if (myass.FullName == ass.FullName)
                        continue;
                    WeaveAssembly(AssemblyDefinition.ReadAssembly(ass.Name));
                }
            }
        }


        public void WeaveAssembly(AssemblyDefinition assembly)
        {
            m_Assemby = assembly;
            using(m_Assemby)
            {
                try
                {
                    m_types = new Types(assembly);
                }
                catch (Exception e)
                {
                    m_Notify.Notify(NotifyLevel.Error, "构造 types错误:" + e.Message + "   ," + e.StackTrace);
                    return;
                }

                if (HasWeaved())
                {
                    m_Notify?.Notify(NotifyLevel.Warning, "目标程序集已被编织");
                    return;
                }

                WeaveFlag();
                DoWeave();

                m_Assemby.Write();
            }         
        }

        //植入标记类
        private void WeaveFlag()
        {
            TypeDefinition FlagClass = new TypeDefinition("WeaveNameSpace","WeaveFlagClass",TypeAttributes.Class,m_types.ObjectType);
            m_Assemby.MainModule.Types.Add(FlagClass);
        }

        //判断该程序集是否已被编织
        private bool HasWeaved()
        {
            return m_Assemby.MainModule.Types.Any(m => m.IsClass && m.Name == "WeaveFlagClass");
        }

        public void DoWeave()
        {
            var types = m_Assemby.MainModule.GetTypes();
            foreach(var type in types)
            {
                if (!type.IsClass)
                    continue;

                //hook方法
                foreach(var method in type.Methods)
                {
                    if (method.HasBody)
                    {
                        foreach (var attr in method.CustomAttributes)
                        {
                            if (attr.AttributeType.IsDrived(m_types.MethodHookAttrType))
                            {
                                WeaveMethod(type,method, attr);
                                break;
                            }
                        }
                    }                  
                }

                //hook字段
                foreach (var field in type.Fields)
                {
                    foreach (var attr in field.CustomAttributes)
                    {
                        if (attr.AttributeType.IsDrived(m_types.FiledHookAttrType))
                        {
                            if(CheckFiled(field))
                            {
                                WeaveFileds(type, field, attr);
                                break;
                            }
                        }
                    }
                }

                //将新加的静态字段添加到对应类中
                for(int i=fieldinfoFields.Count-1;i>=0;i--)
                {
                    fieldinfoFields[i].classdef.Fields.Add(fieldinfoFields[i].field);
                    fieldinfoFields.RemoveAt(i);
                }

                ReplaceFields(type);
            }

            SetFieldSetMethod();
        }

        private void WeaveMethod(TypeDefinition classDef,MethodDefinition method,CustomAttribute attr)
        {
            var proces = method.Body.GetILProcessor();

            var firstInstr = method.Body.Instructions[0];
            var lastInstr = method.Body.Instructions[method.Body.Instructions.Count - 1];

            var tarctor = attr.AttributeType.Resolve().Methods.First(m => m.Name == ".ctor");
            if (tarctor == null)
            {
                m_Notify?.Notify(NotifyLevel.Warning, "获取目标特性参构造方法失败:"+attr.AttributeType.Name);
                return;
            }


            //添加新的静态字段以保存当前方法反射信息
            FieldDefinition methodField = new FieldDefinition("method_info_" + method.Name,
                FieldAttributes.Static | FieldAttributes.SpecialName, m_types.Sys_MethodInfo);
            classDef.Fields.Add(methodField);

            //添加新的静态字段以保存当前方法的特性实例
            FieldDefinition attrField = new FieldDefinition("attr_method_instacne_" + method.Name,
                FieldAttributes.Static | FieldAttributes.SpecialName, attr.AttributeType);
            classDef.Fields.Add(attrField);


            proces.Body.InitLocals = true;
            proces.Body.Variables.Add(new VariableDefinition(m_types.Sys_ObjArray));
            proces.Body.Variables.Add(new VariableDefinition(m_types.MethodExecuteArgsType));
            int varObjarray = proces.Body.Variables.Count - 2;
            int varArgs = proces.Body.Variables.Count - 1;


            //if (null == method_info_xxx)
            //   method_info_xxx = MethodBase.GetCurrentMethod();
            var lb1 = proces.Create(OpCodes.Nop);
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldnull));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldsfld, methodField));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ceq));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Brfalse_S, lb1));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Call, m_types.Sys_GetCurrentMethodType));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Stsfld, methodField));
            proces.InsertBefore(firstInstr, lb1);

            //if (null == attr_instacne_xxx)
            //   attr_method_instacne_xxx = method_info_xxx.GetCustomAttribute(typeof(myattr),true);
            var lb2 = proces.Create(OpCodes.Nop);
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldnull));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldsfld, attrField));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ceq));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Brfalse_S, lb2));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldsfld, methodField));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldtoken, attr.AttributeType));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Call, m_types.Sys_GetTypeFromHandle));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldc_I4_1));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Call, m_types.Sys_GetCustomAttribute));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Stsfld, attrField));
            proces.InsertBefore(firstInstr, lb2);

            // var objarr=new object[x];
            //objarr[x]=xx;
            //objarr[x]=xx;
            //      ...
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldc_I4, method.Parameters.Count));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Newarr, m_types.ObjectType));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Stloc,varObjarray));
            //赋值参数
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldloc, varObjarray));
                proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldc_I4, i));
                proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldarg, i + 1));
                if (method.Parameters[i].ParameterType.IsValueType)
                    proces.InsertBefore(firstInstr, proces.Create(OpCodes.Box, method.Parameters[i].ParameterType));
                proces.InsertBefore(firstInstr, proces.Create(OpCodes.Stelem_Ref));
            }

            //var args=new MethodExecuteArgs(method_info_xxx,this);
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldsfld, methodField));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldarg_0));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Newobj, m_types.MethodExecuteArgsCtor));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Stloc, varArgs));
            //args.ParameterValues=objarr;
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldloc, varArgs));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldloc, varObjarray));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Callvirt, m_types.MethodExecuteArgs_ParameterValues_Set));

            //实例化目标特性 并调用Enter方法
            //attr_instacne_xxx.MethodHookEnter(args);
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldsfld, attrField));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldloc, varArgs));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Callvirt, m_types.MethodHookEnter));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Nop));

            //实例化目标特性 并调用Leave方法
            //attr_instacne_xxx.MethodHookLeave(args);
            proces.InsertBefore(lastInstr, proces.Create(OpCodes.Ldsfld, attrField));
            proces.InsertBefore(lastInstr, proces.Create(OpCodes.Ldloc, varArgs));
            proces.InsertBefore(lastInstr, proces.Create(OpCodes.Callvirt, m_types.MethodHookLeave));
            proces.InsertBefore(lastInstr, proces.Create(OpCodes.Nop));
        }

        protected virtual bool CheckFiled(FieldDefinition field)
        {
            if (field.IsStatic)
            {
                m_Notify?.Notify(NotifyLevel.Warning, "目标字段不能是静态的:" + field.FullName);
                return false;
            }
            if (field.ContainsGenericParameter)
            {
                m_Notify?.Notify(NotifyLevel.Warning, "目标字段不能包含泛型参数:" + field.FullName);
                return false;
            }
            try
            {
                if (field.FieldType.Resolve().IsInterface)
                {
                    m_Notify?.Notify(NotifyLevel.Warning, "目标字段不能是接口类型:" + field.FullName);
                    return false;
                }
            }
            catch (AssemblyResolutionException e)
            {
                m_Notify.Notify(NotifyLevel.Warning, e.Message + "  解析目标字段失败,字段:" + field.FullName);
                return false;
            }

            return true;
        }

        private void WeaveFileds(TypeDefinition classType,FieldDefinition field, CustomAttribute attr)
        {
            var set_method = new MethodDefinition("hook_set_"+field.Name,
                MethodAttributes.Public|MethodAttributes.HideBySig|MethodAttributes.SpecialName, m_types.Sys_Void);

            var tarctor = attr.AttributeType.Resolve().Methods.First(m => m.Name == ".ctor");
            if (tarctor == null )
            {
                m_Notify.Notify(NotifyLevel.Warning, "获取目标特性构造方法失败:" + attr.AttributeType.Name);
                return;
            }

            FieldDefinition infoField = new FieldDefinition("field_info_" + field.Name,
                FieldAttributes.Static | FieldAttributes.SpecialName, m_types.Sys_FieldInfo);
            fieldinfoFields.Add((classType, infoField));

            FieldDefinition attrfield = new FieldDefinition("attr_field_instance_" + field.Name,
            FieldAttributes.Static | FieldAttributes.SpecialName, attr.AttributeType);
            fieldinfoFields.Add((classType, attrfield));


            var processor=set_method.Body.GetILProcessor();
            processor.Body.InitLocals = true;
            processor.Body.Variables.Add(new VariableDefinition(m_types.FieldUpdateArgsType));
            int varindex = processor.Body.Variables.Count - 1;

            //给创建的静态字段赋值(如果为空)
            //if(field_info_xxx==null)
            //   field_info_xxx=this.GetType().GetFieldInfo(xxx);
            var lb1 = processor.Create(OpCodes.Nop);
            processor.Append(processor.Create(OpCodes.Ldnull));
            processor.Append(processor.Create(OpCodes.Ldsfld, infoField));
            processor.Append(processor.Create(OpCodes.Ceq));
            processor.Append(processor.Create(OpCodes.Brfalse_S, lb1));
            processor.Append(processor.Create(OpCodes.Ldarg_0));
            processor.Append(processor.Create(OpCodes.Call, m_types.Sys_GetTypeMethod));
            processor.Append(processor.Create(OpCodes.Ldstr, field.Name));
            processor.Append(processor.Create(OpCodes.Callvirt, m_types.Sys_GetFieldInfoMethod));
            processor.Append(processor.Create(OpCodes.Stsfld, infoField));
            processor.Append(lb1);

            //if (null == attr_instacne_xxx)
            //   attr_field_instance_xxx = field_info_xxx.GetCustomAttribute(typeof(myattr),true);
            var lb2 = processor.Create(OpCodes.Nop);
           processor.Append(processor.Create(OpCodes.Ldnull));
           processor.Append(processor.Create(OpCodes.Ldsfld, attrfield));
           processor.Append(processor.Create(OpCodes.Ceq));
           processor.Append(processor.Create(OpCodes.Brfalse_S, lb2));
           processor.Append(processor.Create(OpCodes.Ldsfld, infoField));
           processor.Append(processor.Create(OpCodes.Ldtoken, attr.AttributeType));
           processor.Append(processor.Create(OpCodes.Call, m_types.Sys_GetTypeFromHandle));
           processor.Append(processor.Create(OpCodes.Ldc_I4_1));
           processor.Append(processor.Create(OpCodes.Call, m_types.Sys_GetCustomAttribute));
           processor.Append(processor.Create(OpCodes.Stsfld, attrfield));
            processor.Append(lb2);

            //:: var xx1= new FieldUpdateArgs(newfield,this,value);
            processor.Append(processor.Create(OpCodes.Ldsfld, infoField));
            processor.Append(processor.Create(OpCodes.Ldarg_0));
            processor.Append(processor.Create(OpCodes.Ldarg_1));
            if (field.FieldType.IsValueType)
                processor.Append(processor.Create(OpCodes.Box, field.FieldType));
            processor.Append(processor.Create(OpCodes.Newobj, m_types.FieldUpdateArgsCtor));
            processor.Append(processor.Create(OpCodes.Stloc, varindex));

            //调用Hook特性方法
            //attr_field_instance_xxx.OnSetValue(args);
            processor.Append(processor.Create(OpCodes.Ldsfld, attrfield));
            processor.Append(processor.Create(OpCodes.Ldloc, varindex));
            processor.Append(processor.Create(OpCodes.Callvirt, m_types.FiledHookAttrOnSetValueMethod));
            processor.Append(processor.Create(OpCodes.Nop));

            //延迟对原字段的赋值 否则会引起死循环
            //processor.Append(processor.Create(OpCodes.Ldarg_0));
            //processor.Append(processor.Create(OpCodes.Ldarg_1));
            //processor.Append(processor.Create(OpCodes.Stfld, field));

            processor.Append(processor.Create(OpCodes.Ret));

            set_method.Parameters.Add(new ParameterDefinition("value",ParameterAttributes.In,field.FieldType));
            //set_method.SemanticsAttributes = MethodSemanticsAttributes.Setter;

            classType.Methods.Add(set_method);
            replaceFields.Add(field, set_method);
        }

        //将原来给目标字段赋值的指令 替换为调用该字段的set方法
        private void ReplaceFields(TypeDefinition classDefiniton)
        {

            foreach (var method in classDefiniton.Methods)
            {
                if (!method.HasBody || method.Body.Instructions == null)
                    continue;

                foreach(var instruction in method.Body.Instructions)
                {
                    if(instruction.OpCode==OpCodes.Stfld)
                    {
                        FieldDefinition field = instruction.Operand as FieldDefinition;
                        if(field!=null &&  replaceFields.ContainsKey(field))
                        {
                            instruction.OpCode = OpCodes.Call;
                            instruction.Operand = replaceFields[field];
                        }
                    }
                }
            }
        }

        //字段的set方法里添加 对原字段的赋值
        private void SetFieldSetMethod()
        {
            foreach(var mf in replaceFields)
            {
                var method = mf.Value;
                var field = mf.Key;

                var processor = method.Body.GetILProcessor();
                var lastins = method.Body.Instructions[method.Body.Instructions.Count - 1];
                processor.InsertBefore(lastins, processor.Create(OpCodes.Ldarg_0));
                processor.InsertBefore(lastins, processor.Create(OpCodes.Ldarg_1));
                processor.InsertBefore(lastins, processor.Create(OpCodes.Stfld, field));
            }
        }

    }
}
