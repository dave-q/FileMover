using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileMoverWithUpdate
{
    public interface ICancelled
    {
        bool IsCancelled { get; }
    }
}
