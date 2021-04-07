using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AopCore;

namespace AopWeaver
{
    class ConsoleNotify : INotify
    {
        public void Notify(NotifyLevel level, string messagae)
        {
            Console.WriteLine(level.ToString() + " : " + messagae);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length==0)
            {
                var tip = "对目标程序集进行静态织入\n\n"+
                    "AopWeaver [-b] assimeblyname\n\n"+
                    "-b : 处理其依赖项 否则不处理依赖项\n";
                Console.Write(tip);
            }
            else if(args.Length==1)
            {
                WeaveRunner.Weave(args[0], false,new ConsoleNotify());
                Console.WriteLine("weave finished!");
            }    
            else if(args.Length>1)
            {
                if(args[0]=="-b")
                {
                    WeaveRunner.Weave(args[1], true,new ConsoleNotify());
                    Console.WriteLine("weave finished!");
                }
                else
                {
                    Console.WriteLine("无效的命令!");
                }
            }


            Console.ReadKey();
        }
    }
}
