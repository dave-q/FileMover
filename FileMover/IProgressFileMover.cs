using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileMover
{
    internal interface IProgressFileMover
    {
        Task<bool> MoveFile(string sourcePath, string destinationPath, FileMoveType moveType, Action<FileMoveProgressArgs> progressUpdateCallback);
    }
}
