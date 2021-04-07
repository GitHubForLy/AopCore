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
        public virtual void OnSetValue(FieldInfo field,object value)
        {

        }
    }
}
