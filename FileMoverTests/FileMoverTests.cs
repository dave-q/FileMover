using System;
using FileMover;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace FileMoverTests
{
    [TestClass]
    public class FileMoverTests
    {
        public bool IsCancelled { get; private set; }

        [TestMethod]
        public async Task FileMoverSetsIsMoving()
        {
            CreateTestFile();
            var sourcePath = TestFilePath;
            var destPath = TestFilePath;

            Mock<IProgressFileMover> mockMover = new Mock<IProgressFileMover>();
            mockMover.Setup(mover => mover.MoveFile(It.IsAny<string>(), It.IsAny<string>(), FileMoveType.Move, It.IsAny<Action<FileMoveProgressArgs>>())).Returns(async () => { await Task.Delay(1000); return true; });

            var filemover = new FileMoverInternal(mockMover.Object, sourcePath, destPath, ProgressUpdater);

            var result = filemover.MoveAsync(FileMoveType.Move);

            Assert.IsTrue(filemover.IsMoving);

            await result;

        }

        [TestMethod]
        public void FileMoverCancelSetsCancelOnEventArgs()
        {
            this.IsCancelled = true;
            var fileMover = new FileMoverInternal(new Mock<IProgressFileMover>().Object, "source", "dest",ProgressUpdater);

            var fileMoverEventArgs = new FileMoveProgressArgs(1, 1);

            fileMover.ProgressCallback(fileMoverEventArgs);
            this.IsCancelled = false;
            Assert.IsTrue(fileMoverEventArgs.Cancelled);
        }

        [TestMethod]
        public void FileMoverUpdatesProgressUpdater()
        {
            var totalBytes = 0L;
            var transferredBytes = 0L;

            Action<FileMoveProgressArgs> _progressUpdated = (FileMoveProgressArgs progressArgs) =>
            {
                totalBytes = progressArgs.TotalBytes;
                transferredBytes = progressArgs.TransferredBytes;
                progressArgs.Cancelled = false;
            };

            var fileMover = new FileMoverInternal(new Mock<IProgressFileMover>().Object, "source", "dest", _progressUpdated);

            var fileMoverEventArgs = new FileMoveProgressArgs(100, 50);

            fileMover.ProgressCallback(fileMoverEventArgs);

            Assert.AreEqual(100, totalBytes);
            Assert.AreEqual(50, transferredBytes);
        }

        public void ProgressUpdater(FileMoveProgressArgs progressArgs)
        {
            var transferredBytes = progressArgs.TransferredBytes;
            var totalBytes = progressArgs.TotalBytes;
            var percent = transferredBytes / totalBytes * 100;
            progressArgs.Cancelled =  this.IsCancelled;
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public async Task FileMoveFailsIfSourceFileDoesntExist()
        {
            CreateTestFile();
            var sourcePath = "TestPath";
            var destPath = TestFilePath;

            Mock<IProgressFileMover> _mockMover = new Mock<IProgressFileMover>();

            var fileMover = new FileMoverInternal(_mockMover.Object, sourcePath, destPath, ProgressUpdater, false);

            var result = await fileMover.MoveAsync(FileMoveType.Move);
            
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task FileMoveFailsIfDestExistsAndOverwriteIsFalse()
        {
            CreateTestFile();
            var sourcePath = TestFilePath;
            var destPath = TestFilePath;

            Mock<IProgressFileMover> _mockMover = new Mock<IProgressFileMover>();

            var fileMover = new FileMoverInternal(_mockMover.Object, sourcePath, destPath, ProgressUpdater, false);

            var result = await fileMover.MoveAsync(FileMoveType.Move);
        }

        
        [TestMethod]
        public async Task FileMovePassesIfDestExistsAndOverwriteIsTrue()
        {
            CreateTestFile();
            var sourcePath = TestFilePath;
            var destPath = TestFilePath;

            Mock<IProgressFileMover> _mockMover = new Mock<IProgressFileMover>();
            _mockMover.Setup(mover => mover.MoveFile(It.IsAny<string>(), It.IsAny<string>(), FileMoveType.Move, It.IsAny<Action<FileMoveProgressArgs>>())).ReturnsAsync(true);
            

            var fileMover = new FileMoverInternal(_mockMover.Object, sourcePath, destPath, ProgressUpdater, true);

            var result = await fileMover.MoveAsync(FileMoveType.Move);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task DoAFileMove()
        {
            CreateTestFile();
            var result = await FileWithProgress.MoveAsync(TestFilePath, TestDestinationPath, ProgressUpdater);

            Assert.IsTrue(result);

            Assert.IsTrue(File.Exists(TestDestinationPath));

            Assert.IsFalse(File.Exists(TestFilePath));
        }

        [TestMethod]
        public async Task DoAFileCopy()
        {
            CreateTestFile();

            var result = await FileWithProgress.CopyAsync(TestFilePath, TestDestinationPath, ProgressUpdater);

            Assert.IsTrue(result);

            Assert.IsTrue(File.Exists(TestDestinationPath));

            Assert.IsTrue(File.Exists(TestFilePath));
        }

        [TestCleanup]
        public void DeleteDestFile()
        {
            File.Delete(TestFilePath);
            File.Delete(TestDestinationPath);
        }

        public void CreateTestFile()
        {
            using (var file = File.CreateText(TestFilePath))
            {
                file.WriteLine("SOME TESTING TEXT");
            }
        }

        string TestFilePath = "testFile.txt";

        string TestDestinationPath = "DestFile.txt";

    }
}
