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
        [TestMethod]
        public async Task FileMoverSetsIsMoving()
        {
            CreateTestFile();
            var sourcePath = TestFilePath;
            var destPath = TestFilePath;

            Mock<IProgressFileMover> mockMover = new Mock<IProgressFileMover>();
            mockMover.Setup(mover => mover.MoveFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<FileMoveEventArgs>>())).Returns(async () => { await Task.Delay(1000); return true; });


            Mock<ICancelled> mockCancelled = new Mock<ICancelled>();

            var filemover = new FileMoverInternal(mockMover.Object, sourcePath, destPath, ProgressUpdater, mockCancelled.Object);

            var result = filemover.MoveAsync();

            Assert.IsTrue(filemover.IsMoving);

            await result;

        }

        [TestMethod]
        public void FileMoverCancelSetsCancelOnEventArgs()
        {
            var _mockCancelledNotifier = new Mock<ICancelled>();
            _mockCancelledNotifier.SetupGet(canc => canc.IsCancelled).Returns(true);

            var fileMover = new FileMoverInternal(new Mock<IProgressFileMover>().Object, "source", "dest",ProgressUpdater, _mockCancelledNotifier.Object);

            var fileMoverEventArgs = new FileMoveEventArgs(1, 1);

            fileMover.ProgressCallback(fileMoverEventArgs);

            Assert.IsTrue(fileMoverEventArgs.Cancelled);
        }

        [TestMethod]
        public void FileMoverUpdatesProgressUpdater()
        {
            var _mockCancelledNotifier = new Mock<ICancelled>();

            _mockCancelledNotifier.SetupAllProperties();

            var totalBytes = 0L;
            var transferredBytes = 0L;

            Action<long, long> _progressUpdated = (long _totalBytes, long _transferredBytes) =>
            {
                totalBytes = _totalBytes;
                transferredBytes = _transferredBytes;
            };

            var fileMover = new FileMoverInternal(new Mock<IProgressFileMover>().Object, "source", "dest", _progressUpdated, _mockCancelledNotifier.Object);

            var fileMoverEventArgs = new FileMoveEventArgs(100, 50);

            fileMover.ProgressCallback(fileMoverEventArgs);

            Assert.AreEqual(100, totalBytes);
            Assert.AreEqual(50, transferredBytes);
        }

        public void ProgressUpdater(long totalBytes, long transferredBytes)
        {
            var percent = transferredBytes / totalBytes * 100;
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public async Task FileMoveFailsIfSourceFileDoesntExist()
        {
            CreateTestFile();
            var sourcePath = "TestPath";
            var destPath = TestFilePath;

            Mock<IProgressFileMover> _mockMover = new Mock<IProgressFileMover>();
            Mock<ICancelled> _mockCancelled = new Mock<ICancelled>();

            var fileMover = new FileMoverInternal(_mockMover.Object, sourcePath, destPath, ProgressUpdater, _mockCancelled.Object, false);

            var result = await fileMover.MoveAsync();
            
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task FileMoveFailsIfDestExistsAndOverwriteIsFalse()
        {
            CreateTestFile();
            var sourcePath = TestFilePath;
            var destPath = TestFilePath;

            Mock<IProgressFileMover> _mockMover = new Mock<IProgressFileMover>();
            Mock<ICancelled> _mockCancelled = new Mock<ICancelled>();

            var fileMover = new FileMoverInternal(_mockMover.Object, sourcePath, destPath, ProgressUpdater, _mockCancelled.Object, false);

            var result = await fileMover.MoveAsync();
        }

        [TestMethod]
        public async Task FileMovePassesIfDestExistsAndOverwriteIsTrue()
        {
            CreateTestFile();
            var sourcePath = TestFilePath;
            var destPath = TestFilePath;

            Mock<IProgressFileMover> _mockMover = new Mock<IProgressFileMover>();
            _mockMover.Setup(mover => mover.MoveFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<FileMoveEventArgs>>())).ReturnsAsync(true);
            Mock<ICancelled> _mockCancelled = new Mock<ICancelled>();

            var fileMover = new FileMoverInternal(_mockMover.Object, sourcePath, destPath, ProgressUpdater, _mockCancelled.Object, true);

            var result = await fileMover.MoveAsync();

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task DoAFileMove()
        {
            CreateTestFile();
            var result = await FileWithProgress.Move(TestFilePath, TestDestinationPath, ProgressUpdater);

            Assert.IsTrue(result);

            Assert.IsTrue(File.Exists(TestDestinationPath));

            Assert.IsFalse(File.Exists(TestFilePath));
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
