using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace AopCore
{
    public static class TypeReferenceExtend
    {
        /// <summary>
        /// 判断该类型是否等于或继承自指定的类型
        /// </summary>
        public static bool IsDrived(this TypeReference type, TypeReference typeReference)
        {
            string fullname = type.FullName;
            int index = fullname.IndexOf('<');
            if (index != -1)    //如果为泛型类则不要尖括号和泛型参数
                fullname = fullname.Substring(0, index);

            if (fullname == typeReference.FullName)
                return true;
            TypeDefinition typde;
            try
            {
                typde = type.Resolve();
            }
            catch(AssemblyResolutionException)
            {
                return false;
            }

            if (typde.BaseType == null)
                return false;

            return IsDrived(typde.BaseType, typeReference);
        }
    }
}
