using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileMover
{
    internal class ActionOutCome
    {
        internal ActionOutCome(bool success, string message, object dataObject = null)
        {
            Message = message;
            Success = success;
            DataObject = dataObject; 
        }
        internal string Message { get; private set; }
        internal bool Success { get; private set; }
        internal object DataObject { get; private set; }
    }
}
