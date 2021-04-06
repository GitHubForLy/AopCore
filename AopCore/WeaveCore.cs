using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AopCore
{
    public class WeaveCore
    {
        private AssemblyDefinition m_Assemby;
        private INotify m_Notify;
        private Types m_types;

        private Dictionary<FieldDefinition, MethodDefinition> replaceFields = new Dictionary<FieldDefinition, MethodDefinition>();
        private List<(TypeDefinition classdef,FieldDefinition field)> fieldinfoFields = new List<(TypeDefinition, FieldDefinition)>();
        public WeaveCore(string assembly,INotify notify)
        {
            ReaderParameters readerParameters = new ReaderParameters();
            readerParameters.ReadWrite = true;
            //readerParameters.InMemory = true;
            m_Assemby = AssemblyDefinition.ReadAssembly(assembly, readerParameters);

            this.m_Notify = notify;
            m_types = new Types(m_Assemby);
        }

        public void Weave()
        {
            if (HasWeaved())
            {
                m_Notify.Notify(NotifyLevel.Warning, "目标程序集已被编织");
                return;
            }

            WeaveFlag();
            DoWeave();

            m_Assemby.Write();
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
                m_Notify.Notify(NotifyLevel.Warning, "目标特性没有无参构造方法:"+attr.AttributeType.Name);
                return;
            }


            //添加新的静态字段以保存当前方法反射信息
            FieldDefinition newField = new FieldDefinition("method_info_" + method.Name,
                FieldAttributes.Static | FieldAttributes.SpecialName, m_types.Sys_MethodInfo);
            classDef.Fields.Add(newField);



            proces.Body.InitLocals = true;
            proces.Body.Variables.Add(new VariableDefinition(m_types.MethodHookAttrType));
            int argindex = proces.Body.Variables.Count - 1;

            var lb1 = proces.Create(OpCodes.Nop);
            //if (null == method_info_xxx)
            //   method_info_xxx = MethodBase.GetCurrentMethod();
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldnull));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldsfld, newField));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ceq));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Brfalse_S, lb1));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Call, m_types.Sys_GetCurrentMethodType));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Stsfld, newField));
            proces.InsertBefore(firstInstr, lb1);
            //生成ExecuteArgs对象
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldsfld, newField));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Newobj, m_types.MethodExecuteArgsCtor));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Stloc, argindex));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldloc,argindex));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldc_I4, method.Parameters.Count));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Newarr, m_types.ObjectType));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Callvirt, m_types.MethodExecuteArgs_ParameterValues_Set));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Nop));

            //赋值参数
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldloc,argindex));
                proces.InsertBefore(firstInstr, proces.Create(OpCodes.Callvirt, m_types.MethodExecuteArgs_ParameterValues_Get));
                proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldc_I4, i));
                proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldarg, i + 1));
                if (method.Parameters[i].ParameterType.IsValueType)
                    proces.InsertBefore(firstInstr, proces.Create(OpCodes.Box, method.Parameters[i].ParameterType));
                proces.InsertBefore(firstInstr, proces.Create(OpCodes.Stelem_Ref));
            }

            //实例化目标特性 并调用Enter方法
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Newobj, tarctor));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Ldloc,argindex));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Callvirt, m_types.MethodHookEnter));
            proces.InsertBefore(firstInstr, proces.Create(OpCodes.Nop));

            //实例化目标特性 并调用Leave方法
            proces.InsertBefore(lastInstr, proces.Create(OpCodes.Newobj, tarctor));
            proces.InsertBefore(lastInstr, proces.Create(OpCodes.Ldloc,argindex));
            proces.InsertBefore(lastInstr, proces.Create(OpCodes.Callvirt, m_types.MethodHookLeave));
            proces.InsertBefore(lastInstr, proces.Create(OpCodes.Nop));
        }

        protected virtual bool CheckFiled(FieldDefinition field)
        {
            if (field.IsStatic)
            {
                m_Notify.Notify(NotifyLevel.Warning, "目标字段不能是静态的:" + field.FullName);
                return false;
            }
            if (field.ContainsGenericParameter)
            {
                m_Notify.Notify(NotifyLevel.Warning, "目标字段不能包含泛型参数:" + field.FullName);
                return false;
            }
            if (field.FieldType.Resolve().IsInterface)
            {
                m_Notify.Notify(NotifyLevel.Warning, "目标字段不能是接口类型:" + field.FullName);
                return false;
            }
            return true;
        }

        private void WeaveFileds(TypeDefinition classType,FieldDefinition field, CustomAttribute attr)
        {
            var set_method = new MethodDefinition("hook_set_"+field.Name,
                MethodAttributes.Public|MethodAttributes.HideBySig|MethodAttributes.SpecialName, m_types.Sys_Void);

            var tarctor = attr.AttributeType.Resolve().Methods.First(m => m.Name == ".ctor");
            if (tarctor == null)
            {
                m_Notify.Notify(NotifyLevel.Warning, "目标特性没有无参构造方法:" + attr.AttributeType.Name);
                return;
            }

            FieldDefinition newfield = new FieldDefinition("field_info_" + field.Name,
                FieldAttributes.Static | FieldAttributes.SpecialName, m_types.Sys_FieldInfo);
            fieldinfoFields.Add((classType,newfield));

            var processor=set_method.Body.GetILProcessor();

            //给创建的静态字段赋值(如果为空)
            var lb1 = processor.Create(OpCodes.Nop);
            processor.Append(processor.Create(OpCodes.Ldnull));
            processor.Append(processor.Create(OpCodes.Ldsfld, newfield));
            processor.Append(processor.Create(OpCodes.Ceq));
            processor.Append(processor.Create(OpCodes.Brfalse_S, lb1));
            processor.Append(processor.Create(OpCodes.Ldarg_0));
            processor.Append(processor.Create(OpCodes.Call, m_types.Sys_GetTypeMethod));
            processor.Append(processor.Create(OpCodes.Ldstr, field.Name));
            processor.Append(processor.Create(OpCodes.Callvirt, m_types.Sys_GetFieldInfoMethod));
            processor.Append(processor.Create(OpCodes.Stsfld, newfield));
            processor.Append(lb1);

            //调用Hook特性方法
            processor.Append(processor.Create(OpCodes.Newobj, tarctor));
            processor.Append(processor.Create(OpCodes.Ldsfld, newfield));
            processor.Append(processor.Create(OpCodes.Ldarg_1));
            if (field.FieldType.IsValueType)
                processor.Append(processor.Create(OpCodes.Box, field.FieldType));
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
                    if(instruction.OpCode==OpCodes.Stfld && replaceFields.ContainsKey((FieldDefinition)instruction.Operand))
                    {                   
                        instruction.OpCode = OpCodes.Call;
                        instruction.Operand = replaceFields[(FieldDefinition)instruction.Operand];
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
