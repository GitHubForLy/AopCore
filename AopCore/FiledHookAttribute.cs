using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Reflection.Emit;

namespace AopCore
{
    public class FiledHookAttribute:Attribute
    {
        public void SetValue(string classFullName,string filedName,object value)
        {
            var classD = Assembly.GetEntryAssembly().GetType(classFullName);
            if (classD == null)
                return;
            var filed = classD.GetField(filedName);
            if (filed == null)
                return;



             OnSetValue(filed, value);
        }
        public virtual void OnSetValue(FieldInfo field,object value)
        {

        }
    }
}
