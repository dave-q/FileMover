using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileMover
{
    internal class FileMoverInternal
    {
        private string _destinationPath;
        private bool _overwriteExisting;
        private IProgressFileMover _progressFileMover;
        private string _sourcePath;
        private Action<FileMoveProgressArgs> _progressUpdater;

        public bool IsMoving { get; private set; }

        /// <summary>
        /// Creates a new instance of the FileMoverInternal class
        /// </summary>
        /// <param cref="IProgressFileMover" name="progressFileMover">An IProgressFileMover </param>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <param name="progressUpdater">A call back method that takes a FileMoverProgressArgs object to update the caller to the progress of the move. 
        /// Cancelled Should be set on the FileMoverProgressArgs object during the call back function if the move should be cancelled</param>
        /// <param name="overwriteExisting">Indicates whether to overwrite the file if the destination file already exists</param>
        internal FileMoverInternal(IProgressFileMover progressFileMover, string sourcePath, string destinationPath, Action<FileMoveProgressArgs> progressUpdater, bool overwriteExisting = true)
        {
            this._sourcePath = sourcePath;
            this._destinationPath = destinationPath;
            this._progressUpdater = progressUpdater;
            this._progressFileMover = progressFileMover;
            this._overwriteExisting = overwriteExisting;
        }

        internal void ProgressCallback(FileMoveProgressArgs fileMoveEventArgs)
        {
            _progressUpdater(fileMoveEventArgs);
        }

        internal async Task<bool> MoveAsync(FileMoveType moveType)
        {
            if (!IsMoving && ValidatePaths())
            {
                IsMoving = true;
                try
                {
                    return await _progressFileMover.MoveFile(_sourcePath, _destinationPath, moveType, ProgressCallback);
                }
                finally
                {
                    IsMoving = false;
                }
            }
            return false;
        }

        private bool ValidatePaths()
        {
            if (!File.Exists(_sourcePath))
            {
                throw new FileNotFoundException($"Cannot find file {_sourcePath}");
            }

            if (!CheckCanWrite())
            {
                throw new InvalidOperationException($"The destination file {_destinationPath} already exists and overwriteExisting is set to false");
            }

            return true;

        }

        private bool CheckCanWrite()
        {
            return _overwriteExisting || !File.Exists(_destinationPath);
        }
    }

}
