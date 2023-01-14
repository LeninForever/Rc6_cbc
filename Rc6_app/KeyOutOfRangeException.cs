using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rc6_app
{
    internal class KeyOutOfRangeException : Exception
    {
        public KeyOutOfRangeException()
        {
        }
        public KeyOutOfRangeException(string message) : base(message)
        {

        }
        public KeyOutOfRangeException(string message, Exception inner)
       : base(message, inner)
        {
        }
    }
}
