using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Mono.Cecil;

namespace AopCore
{
    public class WeaveRunner
    {

        public static void Weave(string AssemblyName,bool weaveDependency,INotify notify)
        {
            if (!File.Exists(AssemblyName))
                notify?.Notify(NotifyLevel.Error, "目标程序集不存在");
            try
            {
                WeaveCore core = new WeaveCore(AssemblyName, weaveDependency, notify);
                core.Weave();
            }
            catch(Exception e)
            {
                if (notify != null)
                    notify.Notify(NotifyLevel.Error, e.Message+"  stacktrace:"+e.StackTrace);
                else
                    throw e;
            }

        }

        public static void Weave(string AssemblyName, bool weaveDependency=false)
        {
            Weave(AssemblyName, weaveDependency, null);
        }
    }
}
