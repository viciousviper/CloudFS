/*
The MIT License(MIT)

Copyright(c) 2015 IgorSoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.AppDomainResolver;

namespace IgorSoft.CloudFS.GatewayTests
{
    [TestClass]
    public partial class GenericAsyncGatewayTests
    {
        private const string smallContent = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

        private static byte[] largeContent = null;

        private Fixture fixture;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            AssemblyResolver.Initialize();
            Fixture.Initialize();

            largeContent = new byte[12 * (1 << 20)];
            for (int i = 0; i < largeContent.Length; ++i)
                largeContent[i] = (byte)(i % 251 + 1);
        }

        [TestInitialize]
        public void Initialize()
        {
            fixture = new Fixture();
            CompositionInitializer.SatisfyImports(fixture);
        }

        [TestCleanup]
        public void Cleanup()
        {
            fixture = null;
        }

        [TestMethod]
        public void Import_AsyncGateways_MatchConfigurations()
        {
            CollectionAssert.AreEquivalent(Fixture.GetAsyncGatewayConfigurations().Select(c => c.Schema).ToList(), fixture.AsyncGateways.Select(g => g.Metadata.CloudService).ToList(), "Gateway configurations do not match imported gateways");
        }

        [TestMethod]
        public void GetRootAsync_ReturnsResult()
        {
            fixture.ExecuteByConfiguration(config => {
                var gateway = fixture.GetAsyncGateway(config);
                var rootName = fixture.GetRootName(config);

                var root = gateway.GetRootAsync(rootName, config.ApiKey).Result;

                Assert.IsNotNull(root, "Root is null");
                Assert.AreEqual(Path.DirectorySeparatorChar.ToString(), root.Name, "Unexpected root name");
            }, ConfigManager.GatewayCapabilities.GetRoot, false);
        }

        [TestMethod]
        public void GetDriveAsync_ReturnsResult()
        {
            fixture.ExecuteByConfiguration(config => {
                var gateway = fixture.GetAsyncGateway(config);
                var rootName = fixture.GetRootName(config);

                var drive = gateway.GetDriveAsync(rootName, config.ApiKey).Result;

                Assert.IsNotNull(drive, $"Drive is null ({config.Schema})");
                Assert.IsNotNull(drive.Id, $"Missing drive ID ({config.Schema})");
                Assert.IsNotNull(drive.FreeSpace, $"Missing free space ({config.Schema})");
                Assert.IsNotNull(drive.UsedSpace, $"Missing used space ({config.Schema})");
            }, ConfigManager.GatewayCapabilities.GetDrive);
        }

        [TestMethod]
        public void GetChildItemAsync_ReturnsResults()
        {
            fixture.ExecuteByConfiguration(config => {
                var gateway = fixture.GetAsyncGateway(config);
                var rootName = fixture.GetRootName(config);

                using (var testDirectory = fixture.CreateTestDirectory(config)) {
                    gateway.NewDirectoryItemAsync(rootName, testDirectory.Id, "DirectoryContent").Wait();
                    gateway.NewFileItemAsync(rootName, testDirectory.Id, "File.ext", new MemoryStream(new byte[100]), fixture.GetProgressReporter()).Wait();

                    var items = gateway.GetChildItemAsync(rootName, testDirectory.Id).Result.ToList();

                    Assert.AreEqual(2, items.Count, "Unexpected number of results");
                    Assert.IsTrue(items.OfType<DirectoryInfoContract>().Any(i => i.Name == "DirectoryContent"), "Expected directory is missing");
                    Assert.IsTrue(items.OfType<FileInfoContract>().Any(i => i.Name == "File.ext" && i.Size == 100), "Expected file is missing");
                }
            }, ConfigManager.GatewayCapabilities.GetChildItem);
        }

        [TestMethod]
        public void ClearContentAsync_ExecutesClear()
        {
            fixture.ExecuteByConfiguration(config => {
                var gateway = fixture.GetAsyncGateway(config);
                var rootName = fixture.GetRootName(config);

                using (var testDirectory = fixture.CreateTestDirectory(config)) {
                    var testFile = gateway.NewFileItemAsync(rootName, testDirectory.Id, "File.ext", new MemoryStream(new byte[100]), fixture.GetProgressReporter()).Result;
                    testFile.Directory = testDirectory.ToContract();

                    gateway.ClearContentAsync(rootName, testFile.Id, () => new FileSystemInfoLocator(testFile)).Wait();

                    var items = gateway.GetChildItemAsync(rootName, testDirectory.Id).Result.ToList();

                    testFile = (FileInfoContract)items.Single();
                    Assert.AreEqual("File.ext", testFile.Name, "Expected file is missing");
                    Assert.AreEqual(0, testFile.Size, "Mismatched content size");
                }
            }, ConfigManager.GatewayCapabilities.ClearContent);
        }

        [TestMethod]
        public void GetContentAsync_ReturnsResult()
        {
            fixture.ExecuteByConfiguration(config => {
                var gateway = fixture.GetAsyncGateway(config);
                var rootName = fixture.GetRootName(config);

                using (var testDirectory = fixture.CreateTestDirectory(config)) {
                    var testFile = gateway.NewFileItemAsync(rootName, testDirectory.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter()).Result;

                    using (var result = gateway.GetContentAsync(rootName, testFile.Id).Result) {
                        Assert.AreEqual(smallContent, new StreamReader(result).ReadToEnd(), "Mismatched content");
                    }
                }
            }, ConfigManager.GatewayCapabilities.GetContent);
        }

        [TestMethod]
        public void SetContentAsync_ExecutesSet()
        {
            fixture.ExecuteByConfiguration(config => {
                var gateway = fixture.GetAsyncGateway(config);
                var rootName = fixture.GetRootName(config);

                using (var testDirectory = fixture.CreateTestDirectory(config)) {
                    var testFile = gateway.NewFileItemAsync(rootName, testDirectory.Id, "File.ext", new MemoryStream(new byte[100]), fixture.GetProgressReporter()).Result;
                    testFile.Directory = testDirectory.ToContract();

                    gateway.SetContentAsync(rootName, testFile.Id, new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter(), () => new FileSystemInfoLocator(testFile)).Wait();

                    using (var result = gateway.GetContentAsync(rootName, testFile.Id).Result) {
                        Assert.AreEqual(smallContent, new StreamReader(result).ReadToEnd(), "Mismatched content");
                    }
                }
            }, ConfigManager.GatewayCapabilities.SetContent);
        }

        [TestMethod]
        public void SetContentAsync_WhereContentIsLarge_ExecutesSet()
        {
            fixture.ExecuteByConfiguration(config => {
                var gateway = fixture.GetAsyncGateway(config);
                var rootName = fixture.GetRootName(config);

                using (var testDirectory = fixture.CreateTestDirectory(config)) {
                    var testFile = gateway.NewFileItemAsync(rootName, testDirectory.Id, "File.ext", new MemoryStream(new byte[100]), fixture.GetProgressReporter()).Result;
                    testFile.Directory = testDirectory.ToContract();

                    gateway.SetContentAsync(rootName, testFile.Id, new MemoryStream(largeContent), fixture.GetProgressReporter(), () => new FileSystemInfoLocator(testFile)).Wait();

                    using (var result = gateway.GetContentAsync(rootName, testFile.Id).Result) {
                        var buffer = new byte[largeContent.Length];
                        int position = 0, bytesRead = 0;
                        do {
                            position += bytesRead = result.Read(buffer, position, buffer.Length - position);
                        } while (bytesRead != 0);
                        Assert.AreEqual(buffer.Length, position, "Truncated result content");
                        Assert.AreEqual(-1, result.ReadByte(), "Excessive result content");
                        CollectionAssert.AreEqual(largeContent, buffer, "Mismatched result content");
                    }
                }
            }, ConfigManager.GatewayCapabilities.SetContent);
        }

        [TestMethod]
        public void CopyItemAsync_WhereItemIsDirectory_ExecutesCopy()
        {
            fixture.ExecuteByConfiguration(config => {
                var gateway = fixture.GetAsyncGateway(config);
                var rootName = fixture.GetRootName(config);

                using (var testDirectory = fixture.CreateTestDirectory(config)) {
                    var directoryOriginal = gateway.NewDirectoryItemAsync(rootName, testDirectory.Id, "Directory").Result;
                    var fileOriginal = gateway.NewFileItemAsync(rootName, directoryOriginal.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter()).Result;

                    var directoryCopy = (DirectoryInfoContract)gateway.CopyItemAsync(rootName, directoryOriginal.Id, "Directory-Copy", testDirectory.Id, true).Result;

                    var items = gateway.GetChildItemAsync(rootName, testDirectory.Id).Result;
                    Assert.AreEqual(items.Single(i => i.Name == "Directory-Copy").Id, directoryCopy.Id, "Mismatched copied directory Id");
                    Assert.IsNotNull(items.SingleOrDefault(i => i.Name == "Directory"), "Original directory is missing");
                    var copiedFile = (FileInfoContract)gateway.GetChildItemAsync(rootName, directoryCopy.Id).Result.SingleOrDefault(i => i.Name == "File.ext");
                    Assert.IsTrue(copiedFile != null, "Expected copied file is missing");
                    using (var result = gateway.GetContentAsync(rootName, copiedFile.Id).Result) {
                        Assert.AreEqual(smallContent, new StreamReader(result).ReadToEnd(), "Mismatched content");
                    }
                    Assert.AreNotEqual(fileOriginal.Id, copiedFile.Id, "Duplicate copied file Id");
                }
            }, ConfigManager.GatewayCapabilities.CopyDirectoryItem);
        }

        [TestMethod]
        public void CopyItemAsync_WhereItemIsFile_ExecutesCopy()
        {
            fixture.ExecuteByConfiguration(config => {
                var gateway = fixture.GetAsyncGateway(config);
                var rootName = fixture.GetRootName(config);

                using (var testDirectory = fixture.CreateTestDirectory(config)) {
                    var fileOriginal = gateway.NewFileItemAsync(rootName, testDirectory.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter()).Result;

                    var fileCopy = (FileInfoContract)gateway.CopyItemAsync(rootName, fileOriginal.Id, "File-Copy.ext", testDirectory.Id, false).Result;

                    var items = gateway.GetChildItemAsync(rootName, testDirectory.Id).Result;
                    Assert.AreEqual(items.Single(i => i.Name == "File-Copy.ext").Id, fileCopy.Id, "Mismatched copied file Id");
                    Assert.IsNotNull(items.SingleOrDefault(i => i.Name == "File.ext"), "Original file is missing");
                    using (var result = gateway.GetContentAsync(rootName, fileCopy.Id).Result) {
                        Assert.AreEqual(smallContent, new StreamReader(result).ReadToEnd(), "Mismatched content");
                    }
                }
            }, ConfigManager.GatewayCapabilities.CopyFileItem);
        }

        [TestMethod]
        public void MoveItemAsync_WhereItemIsDirectory_ExecutesMove()
        {
            fixture.ExecuteByConfiguration(config => {
                var gateway = fixture.GetAsyncGateway(config);
                var rootName = fixture.GetRootName(config);

                using (var testDirectory = fixture.CreateTestDirectory(config)) {
                    var directoryOriginal = gateway.NewDirectoryItemAsync(rootName, testDirectory.Id, "Directory").Result;
                    directoryOriginal.Parent = testDirectory.ToContract();
                    var directoryTarget = gateway.NewDirectoryItemAsync(rootName, testDirectory.Id, "DirectoryTarget").Result;
                    var fileOriginal = gateway.NewFileItemAsync(rootName, directoryOriginal.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter()).Result;

                    var directoryMoved = (DirectoryInfoContract)gateway.MoveItemAsync(rootName, directoryOriginal.Id, "Directory", directoryTarget.Id, () => new FileSystemInfoLocator(directoryOriginal)).Result;

                    var targetItems = gateway.GetChildItemAsync(rootName, directoryTarget.Id).Result;
                    Assert.AreEqual(targetItems.Single(i => i.Name == "Directory").Id, directoryMoved.Id, "Mismatched moved directory Id");
                    var originalItems = gateway.GetChildItemAsync(rootName, testDirectory.Id).Result;
                    Assert.IsNull(originalItems.SingleOrDefault(i => i.Name == "Directory"), "Original directory remains");
                    var movedFile = (FileInfoContract)gateway.GetChildItemAsync(rootName, directoryMoved.Id).Result.SingleOrDefault(i => i.Name == "File.ext");
                    Assert.IsTrue(movedFile != null, "Expected moved file is missing");
                    using (var result = gateway.GetContentAsync(rootName, movedFile.Id).Result) {
                        Assert.AreEqual(smallContent, new StreamReader(result).ReadToEnd(), "Mismatched content");
                    }
                    Assert.AreEqual(fileOriginal.Id, movedFile.Id, "Mismatched moved file Id");
                }
            }, ConfigManager.GatewayCapabilities.MoveDirectoryItem);
        }

        [TestMethod]
        public void MoveItemAsync_WhereItemIsFile_ExecutesMove()
        {
            fixture.ExecuteByConfiguration(config => {
                var gateway = fixture.GetAsyncGateway(config);
                var rootName = fixture.GetRootName(config);

                using (var testDirectory = fixture.CreateTestDirectory(config)) {
                    var directoryTarget = gateway.NewDirectoryItemAsync(rootName, testDirectory.Id, "DirectoryTarget").Result;
                    directoryTarget.Parent = testDirectory.ToContract();
                    var fileOriginal = gateway.NewFileItemAsync(rootName, testDirectory.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter()).Result;

                    var fileMoved = (FileInfoContract)gateway.MoveItemAsync(rootName, fileOriginal.Id, "File.ext", directoryTarget.Id, () => new FileSystemInfoLocator(directoryTarget)).Result;

                    var targetItems = gateway.GetChildItemAsync(rootName, directoryTarget.Id).Result;
                    Assert.AreEqual(targetItems.Single(i => i.Name == "File.ext").Id, fileMoved.Id, "Mismatched moved file Id");
                    var originalItems = gateway.GetChildItemAsync(rootName, testDirectory.Id).Result;
                    Assert.IsNull(originalItems.SingleOrDefault(i => i.Name == "File.ext"), "Original file remains");
                    using (var result = gateway.GetContentAsync(rootName, fileMoved.Id).Result) {
                        Assert.AreEqual(smallContent, new StreamReader(result).ReadToEnd(), "Mismatched content");
                    }
                }
            }, ConfigManager.GatewayCapabilities.MoveFileItem);
        }

        [TestMethod]
        public void NewDirectoryItemAsync_CreatesDirectory()
        {
            fixture.ExecuteByConfiguration(config => {
                var gateway = fixture.GetAsyncGateway(config);
                var rootName = fixture.GetRootName(config);

                using (var testDirectory = fixture.CreateTestDirectory(config)) {

                    var newDirectory = gateway.NewDirectoryItemAsync(rootName, testDirectory.Id, "Directory").Result;

                    var items = gateway.GetChildItemAsync(rootName, testDirectory.Id).Result;
                    Assert.AreEqual(1, items.Count(i => i.Name == "Directory"), "Expected directory is missing");
                    Assert.AreEqual(items.Single(i => i.Name == "Directory").Id, newDirectory.Id, "Mismatched directory Id");
                }
            }, ConfigManager.GatewayCapabilities.NewDirectoryItem);
        }

        [TestMethod]
        public void NewFileItemAsync_CreatesFile()
        {
            fixture.ExecuteByConfiguration(config => {
                var gateway = fixture.GetAsyncGateway(config);
                var rootName = fixture.GetRootName(config);

                using (var testDirectory = fixture.CreateTestDirectory(config)) {
                    var newFile = gateway.NewFileItemAsync(rootName, testDirectory.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter()).Result;

                    var items = gateway.GetChildItemAsync(rootName, testDirectory.Id).Result;
                    Assert.AreEqual(1, items.Count(i => i.Name == "File.ext"), "Expected file is missing");
                    Assert.AreEqual(items.Single(i => i.Name == "File.ext").Id, newFile.Id, "Mismatched file Id");
                    using (var result = gateway.GetContentAsync(rootName, newFile.Id).Result) {
                        Assert.AreEqual(smallContent, new StreamReader(result).ReadToEnd(), "Mismatched content");
                    }
                }
            }, ConfigManager.GatewayCapabilities.NewFileItem);
        }

        [TestMethod]
        public void NewFileItemAsync_WhereContentIsLarge_CreatesFile()
        {
            fixture.ExecuteByConfiguration(config => {
                var gateway = fixture.GetAsyncGateway(config);
                var rootName = fixture.GetRootName(config);

                using (var testDirectory = fixture.CreateTestDirectory(config)) {
                    var newFile = gateway.NewFileItemAsync(rootName, testDirectory.Id, "File.ext", new MemoryStream(largeContent), fixture.GetProgressReporter()).Result;

                    var items = gateway.GetChildItemAsync(rootName, testDirectory.Id).Result;
                    Assert.AreEqual(1, items.Count(i => i.Name == "File.ext"), "Expected file is missing");
                    Assert.AreEqual(items.Single(i => i.Name == "File.ext").Id, newFile.Id, "Mismatched file Id");
                    using (var result = gateway.GetContentAsync(rootName, newFile.Id).Result) {
                        var buffer = new byte[largeContent.Length];
                        int position = 0, bytesRead = 0;
                        do {
                            position += bytesRead = result.Read(buffer, position, buffer.Length - position);
                        } while (bytesRead != 0);
                        Assert.AreEqual(buffer.Length, position, "Truncated result content");
                        Assert.AreEqual(-1, result.ReadByte(), "Excessive result content");
                        CollectionAssert.AreEqual(largeContent, buffer, "Mismatched result content");
                    }
                }
            }, ConfigManager.GatewayCapabilities.NewFileItem);
        }

        [TestMethod]
        public void RemoveItemAsync_WhereItemIsDirectory_ExecutesRemove()
        {
            fixture.ExecuteByConfiguration(config => {
                var gateway = fixture.GetAsyncGateway(config);
                var rootName = fixture.GetRootName(config);

                using (var testDirectory = fixture.CreateTestDirectory(config)) {
                    var directory = gateway.NewDirectoryItemAsync(rootName, testDirectory.Id, "Directory").Result;
                    gateway.NewFileItemAsync(rootName, directory.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter()).Wait();

                    gateway.RemoveItemAsync(rootName, directory.Id, true).Wait();

                    var items = gateway.GetChildItemAsync(rootName, testDirectory.Id).Result;
                    Assert.IsFalse(items.Any(i => i.Name == "Directory"), "Excessive directory found");
                }
            }, ConfigManager.GatewayCapabilities.RemoveItem);
        }

        [TestMethod]
        public void RemoveItemAsync_WhereItemIsFile_ExecutesRemove()
        {
            fixture.ExecuteByConfiguration(config => {
                var gateway = fixture.GetAsyncGateway(config);
                var rootName = fixture.GetRootName(config);

                using (var testDirectory = fixture.CreateTestDirectory(config)) {
                    var file = gateway.NewFileItemAsync(rootName, testDirectory.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter()).Result;

                    gateway.RemoveItemAsync(rootName, file.Id, false).Wait();

                    var items = gateway.GetChildItemAsync(rootName, testDirectory.Id).Result;
                    Assert.IsFalse(items.Any(i => i.Name == "File.ext"), "Excessive file found");
                }
            }, ConfigManager.GatewayCapabilities.RemoveItem);
        }

        [TestMethod]
        public void RenameItemAsync_WhereItemIsDirectory_ExecutesRename()
        {
            fixture.ExecuteByConfiguration(config => {
                var gateway = fixture.GetAsyncGateway(config);
                var rootName = fixture.GetRootName(config);

                using (var testDirectory = fixture.CreateTestDirectory(config)) {
                    var directory = gateway.NewDirectoryItemAsync(rootName, testDirectory.Id, "Directory").Result;
                    directory.Parent = testDirectory.ToContract();

                    gateway.RenameItemAsync(rootName, directory.Id, "Directory-Renamed", () => new FileSystemInfoLocator(directory)).Wait();

                    var items = gateway.GetChildItemAsync(rootName, testDirectory.Id).Result;
                    Assert.IsTrue(items.Any(i => i.Name == "Directory-Renamed"), "Expected renamed directory is missing");
                    Assert.IsFalse(items.Any(i => i.Name == "Directory"), "Excessive directory found");
                }
            }, ConfigManager.GatewayCapabilities.RenameDirectoryItem);
        }

        [TestMethod]
        public void RenameItemAsync_WhereItemIsFile_ExecutesRename()
        {
            fixture.ExecuteByConfiguration(config => {
                var gateway = fixture.GetAsyncGateway(config);
                var rootName = fixture.GetRootName(config);

                using (var testDirectory = fixture.CreateTestDirectory(config)) {
                    var file = gateway.NewFileItemAsync(rootName, testDirectory.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter()).Result;
                    file.Directory = testDirectory.ToContract();

                    gateway.RenameItemAsync(rootName, file.Id, "File-Renamed.ext", () => new FileSystemInfoLocator(file)).Wait();

                    var items = gateway.GetChildItemAsync(rootName, testDirectory.Id).Result;
                    Assert.IsTrue(items.Any(i => i.Name == "File-Renamed.ext"), "Expected renamed file is missing");
                    using (var result = gateway.GetContentAsync(rootName, ((FileInfoContract)items.Single(i => i.Name == "File-Renamed.ext")).Id).Result) {
                        Assert.AreEqual(smallContent, new StreamReader(result).ReadToEnd(), "Mismatched content");
                    }
                    Assert.IsFalse(items.Any(i => i.Name == "File.ext"), "Excessive file found");
                }
            }, ConfigManager.GatewayCapabilities.RenameFileItem);
        }
    }
}
