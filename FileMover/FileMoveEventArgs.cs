using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileMoverWithUpdate
{
    internal class FileMoveEventArgs : EventArgs
    {
        internal FileMoveEventArgs(ulong totalBytes, ulong transferredBytes)
        {
            TransferredBytes = transferredBytes;
            TotalBytes = totalBytes;
        }
        internal ulong TransferredBytes {get; private set;}
        internal ulong TotalBytes { get; private set; }

        internal bool Cancelled { get; set; }
    }
}
