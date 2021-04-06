using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AopCore;

namespace TestAop
{

    public class MyAttrAttribute:MethodHookAttribute
    {
        public override void OnMethodEnter(MethodExecuteArgs arg)
        {
            Console.WriteLine("enter:"+arg.Method.Name);
            foreach(var par in arg.ParameterValues)
            {
                Console.WriteLine("value:" + par?.ToString());
            }
        }
        public override void OnMethodLeave(MethodExecuteArgs arg)
        {
            Console.WriteLine("leave:"+arg.Method.Name);
            foreach (var par in arg.ParameterValues)
            {
                Console.WriteLine("value:" + par?.ToString());
            }
        }
    }

    public class myFiledAttribute : FiledHookAttribute
    {
        public override void OnSetValue(FieldInfo field, object value)
        {
            Console.WriteLine("field Enter! filed:"+field?.Name+"  value:"+value);
        }
    }


    public class Class1
    {
        [myFiled]
        public string tst;

        [myFiled]
        public int vc;

        public void TestMethod()
        {
            tst = "sdf";
            vc = 324;


            Console.WriteLine("testMethod");
            test11("xcv",234);
        }

        [MyAttr]
        public void test11(string s,int a)
        {
            Console.WriteLine("");
            Console.WriteLine("test11 called");

            string sdf = "cvx";
            int vx = 456;
            Console.WriteLine(sdf + vx);


            Console.WriteLine("");
        }
    }
}
