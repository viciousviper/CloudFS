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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.CloudFS.Authentication;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.CloudFS.GatewayTests.Config;

namespace IgorSoft.CloudFS.GatewayTests
{
    [TestClass]
    public partial class GenericGatewayTests
    {
        private const string smallContent = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

        private static byte[] largeContent;

        private GatewayTestsFixture fixture;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            largeContent = new byte[12 * (1 << 20)];
            for (int i = 0; i < largeContent.Length; ++i)
                largeContent[i] = (byte)(i % 251 + 1);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            UIThread.Shutdown();
        }

        [TestInitialize]
        public void Initialize()
        {
            fixture = new GatewayTestsFixture();
            CompositionInitializer.SatisfyImports(fixture);
        }

        [TestCleanup]
        public void Cleanup()
        {
            fixture = null;
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void Import_Gateways_MatchConfigurations()
        {
            var configuredGateways = GatewayTestsFixture.GetGatewayConfigurations(GatewayType.Sync, GatewayCapabilities.None);
            var importedGateways = fixture.Gateways;

            CollectionAssert.AreEquivalent(configuredGateways.Select(c => c.Schema).ToList(), importedGateways.Select(g => g.Metadata.CloudService).ToList(), "Gateway configurations do not match imported gateways");
            foreach (var configuredGateway in configuredGateways) {
                var importedGateway = importedGateways.Single(g => g.Metadata.CloudService == configuredGateway.Schema);
                Assert.AreEqual(GatewayCapabilities.All ^ configuredGateway.Exclusions, importedGateway.Metadata.Capabilities, $"Gateway capabilities for '{configuredGateway.Schema}' differ".ToString(CultureInfo.CurrentCulture));
            }
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void GetDrive_ReturnsResult()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                fixture.OnCondition(config, GatewayCapabilities.GetDrive, () =>
                {
                    var drive = gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                    Assert.IsNotNull(drive, $"Drive is null ({config.Schema})".ToString(CultureInfo.CurrentCulture));
                    Assert.IsNotNull(drive.Id, $"Missing drive ID ({config.Schema})".ToString(CultureInfo.CurrentCulture));
                    Assert.IsNotNull(drive.FreeSpace, $"Missing free space ({config.Schema})".ToString(CultureInfo.CurrentCulture));
                    Assert.IsNotNull(drive.UsedSpace, $"Missing used space ({config.Schema})".ToString(CultureInfo.CurrentCulture));
                });
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void GetRoot_ReturnsResult()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                fixture.OnCondition(config, GatewayCapabilities.GetRoot, () =>
                {
                    var root = gateway.GetRoot(rootName, config.ApiKey);

                    Assert.IsNotNull(root, "Root is null");
                    Assert.AreEqual(Path.DirectorySeparatorChar.ToString(), root.Name, "Unexpected root name");
                });
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void GetChildItem_ReturnsResults()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(config, fixture)) {
                    gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                    gateway.NewDirectoryItem(rootName, testDirectory.Id, "DirectoryContent");
                    gateway.NewFileItem(rootName, testDirectory.Id, "File.ext", new MemoryStream(new byte[100]), fixture.GetProgressReporter());

                    fixture.OnCondition(config, GatewayCapabilities.GetChildItem, () =>
                    {
                        var items = gateway.GetChildItem(rootName, testDirectory.Id).ToList();

                        Assert.AreEqual(2, items.Count, "Unexpected number of results");
                        Assert.IsTrue(items.OfType<DirectoryInfoContract>().Any(i => i.Name == "DirectoryContent"), "Expected directory is missing");
                        Assert.IsTrue(items.OfType<FileInfoContract>().Any(i => i.Name == "File.ext" && i.Size == 100), "Expected file is missing");
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void ClearContent_ExecutesClear()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(config, fixture)) {
                    gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                    var testFile = gateway.NewFileItem(rootName, testDirectory.Id, "File.ext", new MemoryStream(new byte[100]), fixture.GetProgressReporter());
                    testFile.Directory = testDirectory.ToContract();

                    fixture.OnCondition(config, GatewayCapabilities.ClearContent, () =>
                    {
                        gateway.ClearContent(rootName, testFile.Id);

                        var items = gateway.GetChildItem(rootName, testDirectory.Id).ToList();

                        testFile = (FileInfoContract)items.Single();
                        Assert.AreEqual("File.ext", testFile.Name, "Expected file is missing");
                        Assert.AreEqual(0, testFile.Size, "Mismatched content size");
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void GetContent_ReturnsResult()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(config, fixture)) {
                    gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                    fixture.OnCondition(config, GatewayCapabilities.GetContent, () =>
                    {
                        var testFile = gateway.NewFileItem(rootName, testDirectory.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter());

                        using (var result = gateway.GetContent(rootName, testFile.Id)) {
                            Assert.AreEqual(smallContent, new StreamReader(result).ReadToEnd(), "Mismatched content");
                        }
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void SetContent_ExecutesSet()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(config, fixture)) {
                    gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                    var testFile = gateway.NewFileItem(rootName, testDirectory.Id, "File.ext", new MemoryStream(new byte[100]), fixture.GetProgressReporter());
                    testFile.Directory = testDirectory.ToContract();

                    fixture.OnCondition(config, GatewayCapabilities.SetContent, () =>
                    {
                        gateway.SetContent(rootName, testFile.Id, new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter());

                        using (var result = gateway.GetContent(rootName, testFile.Id)) {
                            Assert.AreEqual(smallContent, new StreamReader(result).ReadToEnd(), "Mismatched content");
                        }
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online)), Timeout(300000)]
        public void SetContent_WhereContentIsLarge_ExecutesSet()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(config, fixture)) {
                    gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                    var testFile = gateway.NewFileItem(rootName, testDirectory.Id, "File.ext", new MemoryStream(new byte[100]), fixture.GetProgressReporter());
                    testFile.Directory = testDirectory.ToContract();

                    fixture.OnCondition(config, GatewayCapabilities.SetContent, () =>
                    {
                        gateway.SetContent(rootName, testFile.Id, new MemoryStream(largeContent), fixture.GetProgressReporter());

                        using (var result = gateway.GetContent(rootName, testFile.Id)) {
                            var buffer = new byte[largeContent.Length];
                            int position = 0, bytesRead = 0;
                            do {
                                position += bytesRead = result.Read(buffer, position, buffer.Length - position);
                            } while (bytesRead != 0);
                            Assert.AreEqual(buffer.Length, position, "Truncated result content");
                            Assert.AreEqual(-1, result.ReadByte(), "Excessive result content");
                            CollectionAssert.AreEqual(largeContent, buffer, "Mismatched result content");
                        }
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void CopyItem_WhereItemIsDirectory_ToSameDirectoryExecutesCopy()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(config, fixture)) {
                    gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                    var directoryOriginal = gateway.NewDirectoryItem(rootName, testDirectory.Id, "Directory");
                    var fileOriginal = gateway.NewFileItem(rootName, directoryOriginal.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter());

                    fixture.OnCondition(config, GatewayCapabilities.CopyDirectoryItem, () =>
                    {
                        var directoryCopy = (DirectoryInfoContract)gateway.CopyItem(rootName, directoryOriginal.Id, "Directory-Copy", testDirectory.Id, true);

                        var items = gateway.GetChildItem(rootName, testDirectory.Id);
                        Assert.AreEqual(items.Single(i => i.Name == "Directory-Copy").Id, directoryCopy.Id, "Mismatched copied directory Id");
                        Assert.IsNotNull(items.SingleOrDefault(i => i.Name == "Directory"), "Original directory is missing");
                        var copiedFile = (FileInfoContract)gateway.GetChildItem(rootName, directoryCopy.Id).SingleOrDefault(i => i.Name == "File.ext");
                        Assert.IsTrue(copiedFile != null, "Expected copied file is missing");
                        using (var result = gateway.GetContent(rootName, copiedFile.Id)) {
                            Assert.AreEqual(smallContent, new StreamReader(result).ReadToEnd(), "Mismatched content");
                        }
                        Assert.AreNotEqual(fileOriginal.Id, copiedFile.Id, "Duplicate copied file Id");
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void CopyItem_WhereItemIsDirectory_ToDifferentDirectoryExecutesCopy()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(config, fixture)) {
                    gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                    var directoryOriginal = gateway.NewDirectoryItem(rootName, testDirectory.Id, "Directory");
                    var fileOriginal = gateway.NewFileItem(rootName, directoryOriginal.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter());
                    var directoryTarget = gateway.NewDirectoryItem(rootName, testDirectory.Id, "Target");

                    fixture.OnCondition(config, GatewayCapabilities.CopyDirectoryItem, () =>
                    {
                        var directoryCopy = (DirectoryInfoContract)gateway.CopyItem(rootName, directoryOriginal.Id, "Directory-Copy", directoryTarget.Id, true);

                        var items = gateway.GetChildItem(rootName, testDirectory.Id);
                        var targetItems = gateway.GetChildItem(rootName, directoryTarget.Id);
                        Assert.AreEqual(targetItems.Single(i => i.Name == "Directory-Copy").Id, directoryCopy.Id, "Mismatched copied directory Id");
                        Assert.IsNotNull(items.SingleOrDefault(i => i.Name == "Directory"), "Original directory is missing");
                        var copiedFile = (FileInfoContract)gateway.GetChildItem(rootName, directoryCopy.Id).SingleOrDefault(i => i.Name == "File.ext");
                        Assert.IsTrue(copiedFile != null, "Expected copied file is missing");
                        using (var result = gateway.GetContent(rootName, copiedFile.Id)) {
                            Assert.AreEqual(smallContent, new StreamReader(result).ReadToEnd(), "Mismatched content");
                        }
                        Assert.AreNotEqual(fileOriginal.Id, copiedFile.Id, "Duplicate copied file Id");
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void CopyItem_WhereItemIsFile_ToSameDirectory_ExecutesCopy()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(config, fixture)) {
                    gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                    var fileOriginal = gateway.NewFileItem(rootName, testDirectory.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter());

                    fixture.OnCondition(config, GatewayCapabilities.CopyFileItem, () =>
                    {
                        var fileCopy = (FileInfoContract)gateway.CopyItem(rootName, fileOriginal.Id, "File-Copy.ext", testDirectory.Id, false);

                        var items = gateway.GetChildItem(rootName, testDirectory.Id);
                        Assert.AreEqual(items.Single(i => i.Name == "File-Copy.ext").Id, fileCopy.Id, "Mismatched copied file Id");
                        Assert.IsNotNull(items.SingleOrDefault(i => i.Name == "File.ext"), "Original file is missing");
                        using (var result = gateway.GetContent(rootName, fileCopy.Id)) {
                            Assert.AreEqual(smallContent, new StreamReader(result).ReadToEnd(), "Mismatched content");
                        }
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void CopyItem_WhereItemIsFile_ToDifferentDirectory_ExecutesCopy()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(config, fixture)) {
                    gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                    var fileOriginal = gateway.NewFileItem(rootName, testDirectory.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter());
                    var directoryTarget = gateway.NewDirectoryItem(rootName, testDirectory.Id, "Target");

                    fixture.OnCondition(config, GatewayCapabilities.CopyFileItem, () =>
                    {
                        var fileCopy = (FileInfoContract)gateway.CopyItem(rootName, fileOriginal.Id, "File-Copy.ext", directoryTarget.Id, false);

                        var items = gateway.GetChildItem(rootName, testDirectory.Id);
                        var targetItems = gateway.GetChildItem(rootName, directoryTarget.Id);
                        Assert.AreEqual(targetItems.Single(i => i.Name == "File-Copy.ext").Id, fileCopy.Id, "Mismatched copied file Id");
                        Assert.IsNotNull(items.SingleOrDefault(i => i.Name == "File.ext"), "Original file is missing");
                        using (var result = gateway.GetContent(rootName, fileCopy.Id)) {
                            Assert.AreEqual(smallContent, new StreamReader(result).ReadToEnd(), "Mismatched content");
                        }
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void MoveItem_WhereItemIsDirectory_ExecutesMove()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(config, fixture)) {
                    gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                    var directoryOriginal = gateway.NewDirectoryItem(rootName, testDirectory.Id, "Directory");
                    directoryOriginal.Parent = testDirectory.ToContract();
                    var directoryTarget = gateway.NewDirectoryItem(rootName, testDirectory.Id, "DirectoryTarget");
                    var fileOriginal = gateway.NewFileItem(rootName, directoryOriginal.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter());

                    fixture.OnCondition(config, GatewayCapabilities.MoveDirectoryItem, () =>
                    {
                        var directoryMoved = (DirectoryInfoContract)gateway.MoveItem(rootName, directoryOriginal.Id, "Directory", directoryTarget.Id);

                        var targetItems = gateway.GetChildItem(rootName, directoryTarget.Id);
                        Assert.AreEqual(targetItems.Single(i => i.Name == "Directory").Id, directoryMoved.Id, "Mismatched moved directory Id");
                        var originalItems = gateway.GetChildItem(rootName, testDirectory.Id);
                        Assert.IsNull(originalItems.SingleOrDefault(i => i.Name == "Directory"), "Original directory remains");
                        var fileMoved = (FileInfoContract)gateway.GetChildItem(rootName, directoryMoved.Id).SingleOrDefault(i => i.Name == "File.ext");
                        Assert.IsTrue(fileMoved != null, "Expected moved file is missing");
                        using (var result = gateway.GetContent(rootName, fileMoved.Id)) {
                            Assert.AreEqual(smallContent, new StreamReader(result).ReadToEnd(), "Mismatched content");
                        }
                        if (!config.Exclusions.HasFlag(GatewayCapabilities.ItemId)) {
                            Assert.AreEqual(directoryOriginal.Id, directoryMoved.Id, "Mismatched moved directory Id");
                            Assert.AreEqual(fileOriginal.Id, fileMoved.Id, "Mismatched moved file Id");
                        }
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void MoveItem_WhereItemIsFile_ExecutesMove()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(config, fixture)) {
                    gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                    var directoryTarget = gateway.NewDirectoryItem(rootName, testDirectory.Id, "DirectoryTarget");
                    directoryTarget.Parent = testDirectory.ToContract();
                    var fileOriginal = gateway.NewFileItem(rootName, testDirectory.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter());

                    fixture.OnCondition(config, GatewayCapabilities.MoveFileItem, () =>
                    {
                        var fileMoved = (FileInfoContract)gateway.MoveItem(rootName, fileOriginal.Id, "File.ext", directoryTarget.Id);

                        var targetItems = gateway.GetChildItem(rootName, directoryTarget.Id);
                        Assert.AreEqual(targetItems.Single(i => i.Name == "File.ext").Id, fileMoved.Id, "Mismatched moved file Id");
                        var originalItems = gateway.GetChildItem(rootName, testDirectory.Id);
                        Assert.IsNull(originalItems.SingleOrDefault(i => i.Name == "File.ext"), "Original file remains");
                        using (var result = gateway.GetContent(rootName, fileMoved.Id)) {
                            Assert.AreEqual(smallContent, new StreamReader(result).ReadToEnd(), "Mismatched content");
                        }
                        if (!config.Exclusions.HasFlag(GatewayCapabilities.ItemId))
                            Assert.AreEqual(fileOriginal.Id, fileMoved.Id, "Mismatched moved file Id");
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void NewDirectoryItem_CreatesDirectory()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(config, fixture)) {
                    gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                    fixture.OnCondition(config, GatewayCapabilities.NewDirectoryItem, () =>
                    {
                        var newDirectory = gateway.NewDirectoryItem(rootName, testDirectory.Id, "Directory");

                        var items = gateway.GetChildItem(rootName, testDirectory.Id);
                        Assert.AreEqual(1, items.Count(i => i.Name == "Directory"), "Expected directory is missing");
                        Assert.AreEqual(items.Single(i => i.Name == "Directory").Id, newDirectory.Id, "Mismatched directory Id");
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void NewFileItem_CreatesFile()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(config, fixture)) {
                    gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                    fixture.OnCondition(config, GatewayCapabilities.NewFileItem, () =>
                    {
                        var newFile = gateway.NewFileItem(rootName, testDirectory.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter());

                        var items = gateway.GetChildItem(rootName, testDirectory.Id);
                        Assert.AreEqual(1, items.Count(i => i.Name == "File.ext"), "Expected file is missing");
                        Assert.AreEqual(items.Single(i => i.Name == "File.ext").Id, newFile.Id, "Mismatched file Id");
                        using (var result = gateway.GetContent(rootName, newFile.Id)) {
                            Assert.AreEqual(smallContent, new StreamReader(result).ReadToEnd(), "Mismatched content");
                        }
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online)), Timeout(300000)]
        public void NewFileItem_WhereContentIsLarge_CreatesFile()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(config, fixture)) {
                    gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                    fixture.OnCondition(config, GatewayCapabilities.NewFileItem, () =>
                    {
                        var newFile = gateway.NewFileItem(rootName, testDirectory.Id, "File.ext", new MemoryStream(largeContent), fixture.GetProgressReporter());

                        var items = gateway.GetChildItem(rootName, testDirectory.Id);
                        Assert.AreEqual(1, items.Count(i => i.Name == "File.ext"), "Expected file is missing");
                        Assert.AreEqual(items.Single(i => i.Name == "File.ext").Id, newFile.Id, "Mismatched file Id");
                        using (var result = gateway.GetContent(rootName, newFile.Id)) {
                            var buffer = new byte[largeContent.Length];
                            int position = 0, bytesRead = 0;
                            do {
                                position += bytesRead = result.Read(buffer, position, buffer.Length - position);
                            } while (bytesRead != 0);
                            Assert.AreEqual(buffer.Length, position, "Truncated result content");
                            Assert.AreEqual(-1, result.ReadByte(), "Excessive result content");
                            CollectionAssert.AreEqual(largeContent, buffer, "Mismatched result content");
                        }
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void RemoveItem_WhereItemIsDirectory_ExecutesRemove()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(config, fixture)) {
                    gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                    var directory = gateway.NewDirectoryItem(rootName, testDirectory.Id, "Directory");
                    gateway.NewFileItem(rootName, directory.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter());

                    fixture.OnCondition(config, GatewayCapabilities.RemoveItem, () =>
                    {
                        gateway.RemoveItem(rootName, directory.Id, true);

                        var items = gateway.GetChildItem(rootName, testDirectory.Id);
                        Assert.IsFalse(items.Any(i => i.Name == "Directory"), "Excessive directory found");
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void RemoveItem_WhereItemIsFile_ExecutesRemove()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(config, fixture)) {
                    gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                    var file = gateway.NewFileItem(rootName, testDirectory.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter());

                    fixture.OnCondition(config, GatewayCapabilities.RemoveItem, () =>
                    {
                        gateway.RemoveItem(rootName, file.Id, false);

                        var items = gateway.GetChildItem(rootName, testDirectory.Id);
                        Assert.IsFalse(items.Any(i => i.Name == "File.ext"), "Excessive file found");
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void RenameItem_WhereItemIsDirectory_ExecutesRename()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(config, fixture)) {
                    gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                    var directory = gateway.NewDirectoryItem(rootName, testDirectory.Id, "Directory");
                    directory.Parent = testDirectory.ToContract();

                    fixture.OnCondition(config, GatewayCapabilities.RenameDirectoryItem, () =>
                    {
                        gateway.RenameItem(rootName, directory.Id, "Directory-Renamed");

                        var items = gateway.GetChildItem(rootName, testDirectory.Id);
                        Assert.IsTrue(items.Any(i => i.Name == "Directory-Renamed"), "Expected renamed directory is missing");
                        Assert.IsFalse(items.Any(i => i.Name == "Directory"), "Excessive directory found");
                    });
                }
            });
        }

        [TestMethod, TestCategory(nameof(TestCategories.Online))]
        public void RenameItem_WhereItemIsFile_ExecutesRename()
        {
            fixture.ExecuteByConfiguration((gateway, rootName, config) => {
                using (var testDirectory = TestDirectoryFixture.CreateTestDirectory(config, fixture)) {
                    gateway.GetDrive(rootName, config.ApiKey, fixture.GetParameters(config));

                    var file = gateway.NewFileItem(rootName, testDirectory.Id, "File.ext", new MemoryStream(Encoding.ASCII.GetBytes(smallContent)), fixture.GetProgressReporter());
                    file.Directory = testDirectory.ToContract();

                    fixture.OnCondition(config, GatewayCapabilities.RenameFileItem, () =>
                    {
                        gateway.RenameItem(rootName, file.Id, "File-Renamed.ext");

                        var items = gateway.GetChildItem(rootName, testDirectory.Id);
                        Assert.IsTrue(items.Any(i => i.Name == "File-Renamed.ext"), "Expected renamed file is missing");
                        using (var result = gateway.GetContent(rootName, ((FileInfoContract)items.Single(i => i.Name == "File-Renamed.ext")).Id)) {
                            Assert.AreEqual(smallContent, new StreamReader(result).ReadToEnd(), "Mismatched content");
                        }
                        Assert.IsFalse(items.Any(i => i.Name == "File.ext"), "Excessive file found");
                    });
                }
            });
        }
    }
}
