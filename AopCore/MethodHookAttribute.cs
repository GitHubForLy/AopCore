using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AopCore
{
    public class MethodHookAttribute:Attribute
    {
        public virtual void OnMethodEnter(MethodExecuteArgs args)
        {

        }
        public virtual void OnMethodLeave(MethodExecuteArgs args)
        {

        }
    }
}
