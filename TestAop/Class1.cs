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
        object[] xcc;
        public MyAttrAttribute(object[] xc)
        {
            Console.WriteLine("构造");
            xcc = xc;

        }
        public override void OnMethodEnter(MethodExecuteArgs arg)
        {
            Console.WriteLine("enter:"+arg.Method.Name+" instacne:"+(arg.Instance as Class1).GetType().FullName);
            foreach (var par in arg.ParameterValues)
            {
                Console.WriteLine("value:" + par?.ToString());
            }
            Console.WriteLine("xcc:");
            foreach (var o in xcc)
                Console.WriteLine(o.ToString());
        }
        public override void OnMethodLeave(MethodExecuteArgs arg)
        {
            Console.WriteLine("leave:"+arg.Method.Name);
            foreach (var par in arg.ParameterValues)
            {
                Console.WriteLine("value:" + par?.ToString());
            }
            Console.WriteLine("xcc:");
            foreach (var o in xcc)
                Console.WriteLine(o.ToString());
        }
    }

    public class myFiledAttribute : FiledHookAttribute
    {
        string xcv;
        public myFiledAttribute(string a)
        {
            xcv = a;
        }
        public override void OnSetValue(FieldUpdateArgs args)
        {
            Console.WriteLine("field Enter! filed:"+ args.Field?.Name+"  value:"+args.value+" instacne:"+(args.Instance as Class1).GetType().FullName);
            Console.WriteLine("this.xcv:" + xcv);
        }
    }


    public class Class1
    {
        [myFiled("tst field")]
        public string tst;

        [myFiled("vc field")]
        public int vc;

        public void TestMethod()
        {
            tst = "abc";
            vc = 123;

            tst = "jhk";
            vc = 789;


            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
            test11("xcv",234);
        }

        //static MethodInfo method_info_test11;
        //static MyAttrAttribute attr_instacne_MyAttr;

        [MyAttr(new object[] { "asdfasd",23423,typeof(string)} )]
        public void test11(string s,int a)
        {



            //if(method_info_test11==null)
            //    method_info_test11 = (MethodInfo)MethodBase.GetCurrentMethod();

            //if(attr_instacne_MyAttr==null)
            //    attr_instacne_MyAttr = (method_info_test11.GetCustomAttribute(typeof(MethodHookAttribute), true) as MyAttrAttribute);

            //attr_instacne_MyAttr.OnMethodEnter(new MethodExecuteArgs(method_info_test11, this) {  ParameterValues=new object[] { s, a } });
            ///**xxx**/
            //attr_instacne_MyAttr.OnMethodLeave(new MethodExecuteArgs(method_info_test11, this) { ParameterValues = new object[] { s, a } });




            Console.WriteLine("test11 called");




            //Console.WriteLine("");
            //Console.WriteLine("test11 called");

            //string sdf = "cvx";
            //int vx = 456;
            //Console.WriteLine(sdf + vx);
            //Console.WriteLine("");
        }
    }
}
