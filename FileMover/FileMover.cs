using FileMoverWithUpdate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileMoverWithUpdate
{
    public static class FileWithProgress
    {

        public Task<bool>Move(string sourcePath, string destinationPath, Action<ulong, ulong> progressupdater, ICancelled cancelledNotifier)
        {
            var filemover = new FileMover(sourcePath, destinationPath, progressupdater, cancelledNotifier);

            return filemover.MoveAsync();
        }
    }


    public class FileMover
    {
        public FileMover(string sourcePath, string destinationPath, Action<ulong,ulong> progressUpdater, ICancelled cancelledNotifier)
        {
            this._sourcePath = sourcePath;
            this._destinationPath = destinationPath;
            this.ProgressUpdater = progressUpdater;
            this._cancelledNotifier = cancelledNotifier;
        }

        private ICancelled _cancelledNotifier;

        private string _destinationPath;
        private Action<ulong, ulong> ProgressUpdater{ get; set; }
        private string _sourcePath;
        
        public bool IsMoving { get; private set; }

        public void ProgressCallback(FileMoveEventArgs fileMoveEventArgs)
        {
            ProgressUpdater(fileMoveEventArgs.TransferredBytes, fileMoveEventArgs.TotalBytes);
            if(_cancelledNotifier.IsCancelled)
            {
                fileMoveEventArgs.Cancelled = true;
            }
        }

        public async Task<bool> MoveAsync()
        {
            IsMoving = true;
            try
            {
                var mover = new PInvokeFileMoveX(_sourcePath, _destinationPath, ProgressCallback);
                return await mover.MoveFile();
                
            }
                //ToDo Catch more exceptions
            catch (Exception )
            {
                throw ;
            }
            finally
            {
                IsMoving = false;
            }

        }

    }
}
