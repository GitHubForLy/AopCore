using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AopCore
{
    public class FieldUpdateArgs
    {
        public FieldUpdateArgs(FieldInfo fieldInfo,object instance,object value)
        {
            this.Field = fieldInfo;
            this.Instance = instance;
            this.value = value;
        }
        public FieldInfo Field { get; }
        public object value { get; set; }

        public object Instance { get; }
    }
}
