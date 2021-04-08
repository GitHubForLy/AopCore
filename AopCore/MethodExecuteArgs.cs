using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace AopCore
{
    public class MethodExecuteArgs
    {
        public MethodExecuteArgs(MethodBase method,object instance)
        {
            this.Method = method;
            this.Instance = instance;
        }
        public object Instance { get; }
        public MethodBase Method { get; }
        public object[] ParameterValues { get; set; }
    }
}
