using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FileMover
{

    internal class PInvokeFileX : IProgressFileMover
    {
        /// <summary>
        /// Describes the action that should be taken once control is returned back to FileMoveX in Win32Dll method after the update progress has been called
        /// </summary>
        enum CopyProgressResult : uint
        {
            PROGRESS_CONTINUE = 0,
            PROGRESS_CANCEL = 1,
            PROGRESS_STOP = 2,
            PROGRESS_QUIET = 3
        }

        /// <summary>
        /// The reason the callback was called from the FileMoveX method
        /// </summary>
        enum CopyProgressCallbackReason : uint
        {
            CALLBACK_CHUNK_FINISHED = 0x00000000,
            CALLBACK_STREAM_SWITCH = 0x00000001
        }

        /// <summary>
        /// Move Flags required for interaction with Win32 method
        /// </summary>
        [Flags]
        enum MoveFileFlags : uint
        {
            MOVE_FILE_REPLACE_EXISTSING = 0x00000001,
            MOVE_FILE_COPY_ALLOWED = 0x00000002,
            MOVE_FILE_DELAY_UNTIL_REBOOT = 0x00000004,
            MOVE_FILE_WRITE_THROUGH = 0x00000008,
            MOVE_FILE_CREATE_HARDLINK = 0x00000010,
            MOVE_FILE_FAIL_IF_NOT_TRACKABLE = 0x00000020
        }

        [Flags]
        enum CopyFileFlags
        {
            COPY_FILE_ALLOW_DECRYPTED_DESTINATION = 0x00000008,
            COPY_FILE_COPY_SYMLINK = 0x00000800,
            COPY_FILE_FAIL_IF_EXISTS = 0x00000001,
            COPY_FILE_NO_BUFFERING = 0x00001000,
            COPY_FILE_OPEN_SOURCE_FOR_WRITE = 0x00000004,
            COPY_FILE_RESTARTABLE = 0x00000002
        }

        /// <summary>
        /// The error code returned when the method is cancelled
        /// </summary>
        const int CANCELLED_ERROR_CODE = 1235;

        /// <summary>
        /// A method that is provider by the creator of the class and is called during the call back from the win32 method
        /// </summary>
        Action<FileMoveProgressArgs> ProgressCallback { get; set; }

        private long _totalFileSize = 0;

        /// <summary>
        /// A delegate type that describes the method signature required by the win32 FileMoveX 
        /// for a method to be used a call back from the unmanged code back into here, our managed code
        /// </summary>
        /// <param name="TotalFileSize"></param>
        /// <param name="TotalBytesTransferred"></param>
        /// <param name="StreamSize"></param>
        /// <param name="StreamBytesTransferred"></param>
        /// <param name="notUsed"></param>
        /// <param name="CallbackReason"></param>
        /// <param name="sourceFileHandle"></param>
        /// <param name="destinationFileHandle"></param>
        /// <param name="notUsed2"></param>
        /// <returns></returns>
        delegate CopyProgressResult CopyProgressRoutine(
            long TotalFileSize, 
            long TotalBytesTransferred, 
            long StreamSize, 
            long StreamBytesTransferred, 
            uint notUsed, 
            CopyProgressCallbackReason CallbackReason, 
            IntPtr sourceFileHandle, 
            IntPtr destinationFileHandle, 
            IntPtr notUsed2);

        internal PInvokeFileX() { }

        /// <summary>
        /// Moves a file from the source path to the destination path, calling hte progress callback to update the progress and check if it should cancel the move
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <param name="progressCallback">A method to be called to update progress and also set cancelled on the event args if the move should be cancelled</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">If source or destination path is null or empty</exception>
        /// <exception cref="ArgumentException">If progressCallback is null</exception>
        public async Task<bool> MoveFile(string sourcePath, string destinationPath, FileMoveType moveType, Action<FileMoveProgressArgs> progressCallback)
        {
            if (string.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentException("sourcePath cannot be null or empty");
            if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("destinationPath cannot be null or empty");
            if (progressCallback == null) throw new ArgumentNullException("progressCallback");

            ProgressCallback = progressCallback;

            _totalFileSize = new FileInfo(sourcePath).Length;

            Tuple<bool, int> success;

            switch (moveType)
            {
                case FileMoveType.Move:
                    success = await StartFileMove(sourcePath, destinationPath);
                    break;
                case FileMoveType.Copy:
                    success = await StartFileCopy(sourcePath, destinationPath);
                    break;
                default:
                    throw new NotImplementedException($"File Move Type not implemenented. {moveType.ToString()}");
            }

            HandleResult(sourcePath, success);
            return success.Item1;
        }


        /// <summary>
        /// Begins the FileMove process
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        private async Task<Tuple<bool, int>> StartFileMove(string sourcePath, string destinationPath)
        {
            return await Task.Run(() =>
            {
                var _success = MoveFileWithProgress(sourcePath,
                    destinationPath,
                    new CopyProgressRoutine(CopyProgressFunc),
                    IntPtr.Zero,
                    MoveFileFlags.MOVE_FILE_COPY_ALLOWED | MoveFileFlags.MOVE_FILE_REPLACE_EXISTSING | MoveFileFlags.MOVE_FILE_WRITE_THROUGH);

                var errorCode = Marshal.GetLastWin32Error();

                return new Tuple<bool, int>(_success, errorCode);
            });
        }

        private async Task<Tuple<bool,int>> StartFileCopy(string sourcePath, string destinationPath)
        {
            return await Task.Run(() =>
            {
                var _success = CopyFileEx(sourcePath,
                    destinationPath,
                    new CopyProgressRoutine(CopyProgressFunc),
                    IntPtr.Zero,
                    false,
                    CopyFileFlags.COPY_FILE_ALLOW_DECRYPTED_DESTINATION | CopyFileFlags.COPY_FILE_COPY_SYMLINK);

                var errorCode = Marshal.GetLastWin32Error();
                return new Tuple<bool, int>(_success, errorCode);
            });
        }

        /// <summary>
        /// Handles the the returned boolean of the win32 method, along with the processing of the LastWin32 error if it failed
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="success"></param>
        private void HandleResult(string sourcePath, Tuple<bool, int> success)
        {
            FileMoveProgressArgs moveEventArgs;
            if (!success.Item1)
            {
                moveEventArgs = HandleFailed(success, _totalFileSize);
            }
            else
            {
                //doing this as small files, never get called back from the Win32 method and hence the progress is never updated, so this is make sure 100% is returned once it is complete
                moveEventArgs = new FileMoveProgressArgs(_totalFileSize, _totalFileSize);
            }
            ProgressCallback(moveEventArgs);
        }

        /// <summary>
        /// Handle a non success
        /// </summary>
        /// <param name="success"></param>
        /// <param name="fileSize"></param>
        /// <returns></returns>
        private FileMoveProgressArgs HandleFailed(Tuple<bool, int> success, long fileSize)
        {
            if (success.Item2 == CANCELLED_ERROR_CODE) //ie it failed because the user cancelled the execution we dont to thrown an exception
            {
                return new FileMoveProgressArgs(fileSize, 0);
            }
            else
            {
                throw new Win32Exception(success.Item2);
            }
        }

        /// <summary>
        /// A function that can be used as CopyProgressRoutine delegate and is passed into the win32 method
        /// </summary>
        /// <param name="TotalFileSize"></param>
        /// <param name="TotalBytesTransferred"></param>
        /// <param name="StreamSize"></param>
        /// <param name="StreamBytesTransferred"></param>
        /// <param name="notUsed"></param>
        /// <param name="CallbackReason"></param>
        /// <param name="sourceFileHandle"></param>
        /// <param name="destinationFileHandle"></param>
        /// <param name="notUsed2"></param>
        /// <returns></returns>
        private CopyProgressResult CopyProgressFunc(
                long TotalFileSize,
                long TotalBytesTransferred,
                long StreamSize,
                long StreamBytesTransferred,
                uint notUsed,
                CopyProgressCallbackReason CallbackReason,
                IntPtr sourceFileHandle,
                IntPtr destinationFileHandle,
                IntPtr notUsed2
            )
        {
            var streamSize = StreamSize;
            var streamBytesTrans = StreamBytesTransferred;
            _totalFileSize = TotalFileSize;
            var progressResult = CopyProgressResult.PROGRESS_CONTINUE;
            if (CallbackReason == CopyProgressCallbackReason.CALLBACK_CHUNK_FINISHED)
            {
                var fileMoveEventArgs = new FileMoveProgressArgs(TotalFileSize, TotalBytesTransferred);
                ProgressCallback(fileMoveEventArgs);

                if (fileMoveEventArgs.Cancelled)
                {
                    progressResult = CopyProgressResult.PROGRESS_CANCEL;
                }

            }
            return progressResult;
        }

        /// <summary>
        /// the signature of the method in the unmanaged code we will be calling into
        /// </summary>
        /// <param name="existingFileName"></param>
        /// <param name="newFileName"></param>
        /// <param name="progressRoutine"></param>
        /// <param name="notNeeded"></param>
        /// <param name="CopyFlags"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool MoveFileWithProgress(
            string existingFileName,
            string destinationFileName,
            CopyProgressRoutine progressRoutine,
            IntPtr notNeeded,
            MoveFileFlags CopyFlags);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool CopyFileEx(
            string existingFileName,
            string destinationFileName,
            CopyProgressRoutine progressRoutine,
            IntPtr notNeeded,
            bool cancelled,
            CopyFileFlags CopyFlags);

    }
}
