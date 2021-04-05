using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AopCore
{
    public enum NotifyLevel
    {
        Message,
        Warning,
        Error
    }

    public interface INotify
    {
        void Notify(NotifyLevel level, string messagae);       
    }
}
