using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;


[assembly: InternalsVisibleTo("FileMoverTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace FileMover
{
    /// <summary>
    /// A class containing methods to move or copy files while providing a callback to update the caller on the progress and give them the ability to cancel the move
    /// </summary>
    public class FileWithProgress
    {
        /// <summary>
        /// Moves the specified file from its source path to its destination path. 
        /// Calling back to the progress updater to update progress and check the cancelledNotifier as to whether to cancel the transfer
        /// </summary>
        /// <param name="sourcePath">The path of the source file</param>
        /// <param name="destinationPath">The path of where the file will be transferred to</param>
        /// <param name="progressUpdater" >A function to be called to update on the progress of the file moving process returning true to cancel</param>
        /// <param name="overwriteExisting">a boolean value describing what should be done if the destination file path already exists</param>
        /// <exception cref="ArgumentException">If the source file path or destination file path is null or empty</exception>
        /// <exception cref="FileNotFoundException">If the source file cannot be found</exception>
        /// <exception cref="InvalidOperationException">If the destination file exists and cref="overWritreExisting" is set to false</exception>
        public static Task<bool>MoveAsync(string sourcePath, string destinationPath, Func<long, long, bool> progressUpdater = null, bool overwriteExisting = true)
        {
            FileMoverInternal filemover = CreateAndValidate(sourcePath, destinationPath, progressUpdater);


            return filemover.MoveAsync(FileMoveType.Move);
        }

        public static Task<bool>CopyAsync(string sourcePath, string destinationPath, Func<long,long, bool> progressUpdater = null, bool overwriteExisting = true)
        {
            FileMoverInternal filemover = CreateAndValidate(sourcePath, destinationPath, progressUpdater);

            return filemover.MoveAsync(FileMoveType.Copy);
        }

        private static FileMoverInternal CreateAndValidate(string sourcePath, string destinationPath, Func<long, long, bool> progressUpdater)
        {
            if (string.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentException("sourcePath cannot be null or empty");
            if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("destinationPath cannot be null or empty");
            var filemover = new FileMoverInternal(new PInvokeFileX(), sourcePath, destinationPath, progressUpdater);
            return filemover;
        }
    }

    internal enum FileMoveType
    {
        Move,
        Copy
    }

    internal class FileMoverInternal
    {
        /// <summary>
        /// Creates a new instance of the FileMoverInternal class
        /// </summary>
        /// <param cref="IProgressFileMover" name="progressFileMover">An IProgressFileMover </param>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <param name="progressUpdater">A call back method to update the caller to the progress of hte move</param>
        /// <param name="cancelledNotifier">An object that can be checked for a cancel flag</param>
        /// <param name="overwriteExisting">Indicates whether to overwrite the file if the destination file already exists</param>
        internal FileMoverInternal(IProgressFileMover progressFileMover, string sourcePath, string destinationPath, Func<long,long, bool> progressUpdater, bool overwriteExisting = true)
        {
            this._sourcePath = sourcePath;
            this._destinationPath = destinationPath;
            this.ProgressUpdater = progressUpdater;
            this._progressFileMover = progressFileMover;
            this._overwriteExisting = overwriteExisting;
        }

        private bool _overwriteExisting;

        private IProgressFileMover _progressFileMover;

        private string _destinationPath;
        private Func<long, long, bool> ProgressUpdater{ get; set; }
        private string _sourcePath;
        
        public bool IsMoving { get; private set; }

        internal void ProgressCallback(FileMoveProgressArgs fileMoveEventArgs)
        {
            var cancelled = ProgressUpdater(fileMoveEventArgs.TotalBytes, fileMoveEventArgs.TransferredBytes);
            fileMoveEventArgs.Cancelled = cancelled;
        }

        internal async Task<bool> MoveAsync(FileMoveType moveType)
        {
            if (ValidatePaths() && !IsMoving)
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
                throw new FileNotFoundException(string.Format("Cannot find file {0}", _sourcePath));
            }

            if (!CheckCanWrite())
            {
                throw new InvalidOperationException(string.Format("The destination file {0} already exists and overwriteExisting is set to false", _destinationPath));
            }

            return true;
                
        }

        private bool CheckCanWrite()
        {
            var canWrite = true;
            if (!_overwriteExisting)
            {
                canWrite = !File.Exists(_destinationPath);
            }
            return canWrite;
        }
    }
}
