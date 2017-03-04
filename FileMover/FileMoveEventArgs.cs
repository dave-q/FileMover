using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileMover
{
    internal class FileMoveEventArgs : EventArgs
    {
        internal FileMoveEventArgs(long totalBytes, long transferredBytes)
        {
            TransferredBytes = transferredBytes;
            TotalBytes = totalBytes;
        }
        internal long TransferredBytes {get; private set;}
        internal long TotalBytes { get; private set; }

        internal bool Cancelled { get; set; }
    }
}
