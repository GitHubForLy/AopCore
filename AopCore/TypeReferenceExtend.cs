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
        public static bool IsDrived(this TypeReference type, TypeReference typeReference)
        {
            if (type.FullName == typeReference.FullName)
                return true;

            var de = type.Resolve();
            if (de.BaseType == null)
                return false;
            return IsDrived(de.BaseType, typeReference);

        }
    }
}
