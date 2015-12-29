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
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;
using System.Threading.Tasks;

namespace IgorSoft.CloudFS.GatewayTests
{
    public partial class GenericGatewayTests
    {
        private class TestDirectoryFixture : IDisposable
        {
            private readonly ICloudGateway gateway;

            private readonly RootName root;

            private readonly DirectoryInfoContract directory;

            public DirectoryId Id => directory.Id;

            public TestDirectoryFixture(ICloudGateway gateway, RootName root, string apiKey, string path)
            {
                this.gateway = gateway;
                this.root = root;

                var rootDirectory = gateway.GetRoot(root, apiKey);

                var residualDirectory = gateway.GetChildItem(root, rootDirectory.Id).SingleOrDefault(f => f.Name == path) as DirectoryInfoContract;
                if (residualDirectory != null)
                    gateway.RemoveItem(root, residualDirectory.Id, true);

                directory = gateway.NewDirectoryItem(root, rootDirectory.Id, path);
            }

            public void Dispose()
            {
                gateway.RemoveItem(root, directory.Id, true);
            }
        }

        private class Fixture
        {
            private const string COMPOSITION_DIRECTORY = "Gateways";

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Required for MEF composition")]
            [ImportMany]
            public IList<ExportFactory<ICloudGateway, CloudGatewayMetadata>> Gateways { get; set; }

            public static void Initialize()
            {
                CompositionInitializer.Preload(typeof(ICloudGateway));
                CompositionInitializer.Initialize(COMPOSITION_DIRECTORY);
            }

            private static void Log(string message)
            {
                Console.WriteLine(message);
            }

            public static IEnumerable<ConfigManager.GatewayConfigElement> GetGatewayConfigurations(ConfigManager.GatewayCapabilities capability = ConfigManager.GatewayCapabilities.None)
            {
                return ConfigManager.GetGatewayConfigurations().Where(g => capability == ConfigManager.GatewayCapabilities.None || !g.Exclusions.HasFlag(capability));
            }

            public ICloudGateway GetGateway(ConfigManager.GatewayConfigElement config)
            {
                return Gateways.Single(g => g.Metadata.CloudService == config.Schema).CreateExport().Value;
            }

            public RootName GetRootName(ConfigManager.GatewayConfigElement config) => new RootName(config.Schema, config.UserName, config.Root);

            public TestDirectoryFixture CreateTestDirectory(ConfigManager.GatewayConfigElement config) => new TestDirectoryFixture(GetGateway(config), GetRootName(config), config.ApiKey, config.TestDirectory);

            public void ExecuteByConfiguration(Action<ConfigManager.GatewayConfigElement> test, ConfigManager.GatewayCapabilities capability, bool inParallel = true)
            {
                var failures = new List<Tuple<string, Exception>>();

                if (inParallel) {
                    Parallel.ForEach(GetGatewayConfigurations(capability), config => {
                        try {
                            var startedAt = DateTime.Now;
                            test(config);
                            var completedAt = DateTime.Now;
                            Log($"Parallel test for schema '{config.Schema}' completed in {completedAt - startedAt}");
                        } catch (Exception ex) {
                            Log($"Parallel test for schema '{config.Schema}' failed");
                            lock (failures) {
                                failures.Add(new Tuple<string, Exception>(config.Schema, ex));
                            }
                        }
                    });
                } else {
                    foreach (var config in GetGatewayConfigurations(capability)) {
                        try {
                            var startedAt = DateTime.Now;
                            test(config);
                            var completedAt = DateTime.Now;
                            Log($"Sequential test for schema '{config.Schema}' completed in {completedAt - startedAt}");
                        } catch (Exception ex) {
                            Log($"Sequential test for schema '{config.Schema}' failed");
                            failures.Add(new Tuple<string, Exception>(config.Schema, ex));
                        }
                    };
                }

                if (failures.Any())
                    throw new AggregateException("Test failed in " + string.Join(", ", failures.Select(t => t.Item1)), failures.Select(t => t.Item2));
            }

            public IProgress<ProgressValue> GetProgressReporter() => new NullProgressReporter();
        }
    }
}
