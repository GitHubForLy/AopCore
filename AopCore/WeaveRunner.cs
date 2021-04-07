using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Mono.Cecil;

namespace AopCore
{
    public class WeavePamater
    {
        public string AssemblyName { get; set; }
        public bool WeaveDependency { get; set; } = false;
        public INotify Notify { get; set; }
        public string[] SerachPaths { get; set; }

    }

    public class WeaveRunner
    {

        public static void Weave(WeavePamater weavePamater)
        {
            if (!File.Exists(weavePamater.AssemblyName))
                weavePamater.Notify?.Notify(NotifyLevel.Error, "目标程序集不存在");
            try
            {
                WeaveCore core = new WeaveCore(weavePamater.AssemblyName,weavePamater.SerachPaths,weavePamater.Notify);
                core.Weave(weavePamater.WeaveDependency);
            }
            catch(Exception e)
            {
                if (weavePamater.Notify != null)
                    weavePamater.Notify?.Notify(NotifyLevel.Error, e.Message+"  stacktrace:"+e.StackTrace);
                else
                    throw e;
            }

        }

        public static void Weave(string AssemblyName, bool weaveDependency=false)
        {
            Weave(new WeavePamater { AssemblyName=AssemblyName,WeaveDependency=weaveDependency});
        }
    }
}
