using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FileMover
{
    /// <summary>
    /// A class containing methods to move or copy files while providing a callback to update the caller on the progress and give them the ability to cancel the move
    /// </summary>
    public class FileWithProgress
    {
        /// <summary>
        /// Moves the specified file from its source path to its destination path. 
        /// Calling back to the progress updater to update progress. The progressUpdater should return true if the 
        /// process should be cancelled
        /// </summary>
        /// <param name="sourcePath">The path of the source file</param>
        /// <param name="destinationPath">The path of where the file will be transferred to</param>
        /// <param name="progressUpdater" >A function to be called to update on the progress of the file moving process returning true to cancel</param>
        /// <param name="overwriteExisting">a boolean value describing what should be done if the destination file path already exists</param>
        /// <exception cref="ArgumentException">If the source file path or destination file path is null or empty</exception>
        /// <exception cref="FileNotFoundException">If the source file cannot be found</exception>
        /// <exception cref="InvalidOperationException">If the destination file exists and cref="overWritreExisting" is set to false</exception>
        public static Task<bool>MoveAsync(string sourcePath, string destinationPath, Action<FileMoveProgressArgs> progressUpdater = null, bool overwriteExisting = true)
        {
            FileMoverInternal filemover = CreateAndValidate(sourcePath, destinationPath, progressUpdater);

            return filemover.MoveAsync(FileMoveType.Move);
        }

        public static Task<bool> CopyAsync(string sourcePath, string destinationPath, Action<FileMoveProgressArgs> progressUpdater = null, bool overwriteExisting = true)
        {
            FileMoverInternal filemover = CreateAndValidate(sourcePath, destinationPath, progressUpdater);

            return filemover.MoveAsync(FileMoveType.Copy);
        }

        private static FileMoverInternal CreateAndValidate(string sourcePath, string destinationPath, Action<FileMoveProgressArgs> progressUpdater)
        {
            if (string.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentException($"{nameof(sourcePath)} cannot be null or empty");
            if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException($"{nameof(destinationPath)} cannot be null or empty");
            return new FileMoverInternal(new PInvokeFileX(), sourcePath, destinationPath, progressUpdater);
        }
    }

}
