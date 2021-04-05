using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AopCore;

namespace AopWeaver
{
    class MyNotify : INotify
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
            WeaveRunner.Weave(@"F:\GitHub\AopCore\TestAop\bin\Debug\TestAop.exe", new MyNotify());

            Console.WriteLine("finished");
            Console.ReadKey();
        }
    }
}
