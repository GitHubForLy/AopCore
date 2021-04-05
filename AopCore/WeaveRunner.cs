using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Mono.Cecil;

namespace AopCore
{
    public class WeaveRunner
    {

        public static void Weave(string AssemblyName,INotify notify)
        {
            if (!File.Exists(AssemblyName))
                notify.Notify(NotifyLevel.Error, "目标程序集不存在");

            WeaveCore core = new WeaveCore(AssemblyName, notify);
            core.Weave();
        }


    }
}
