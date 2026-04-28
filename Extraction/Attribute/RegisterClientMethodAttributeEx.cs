using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.RTCommon
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RegisterClientMethodAttributeEx : Attribute
    {
        public string MethodName
        {
            get;
            private set;
        }

        public RegisterClientMethodAttributeEx(string type)
        {
            MethodName = type;
        }
    }
}
