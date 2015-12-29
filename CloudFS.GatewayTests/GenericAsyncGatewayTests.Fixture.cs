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
    public partial class GenericAsyncGatewayTests
    {
        private class TestDirectoryFixture : IDisposable
        {
            private readonly IAsyncCloudGateway gateway;

            private readonly RootName root;

            private readonly DirectoryInfoContract directory;

            public DirectoryId Id => directory.Id;

            public TestDirectoryFixture(IAsyncCloudGateway gateway, RootName root, string apiKey, string path)
            {
                this.gateway = gateway;
                this.root = root;

                var rootDirectory = gateway.GetRootAsync(root, apiKey).Result;

                var residualDirectory = gateway.GetChildItemAsync(root, rootDirectory.Id).Result.SingleOrDefault(f => f.Name == path) as DirectoryInfoContract;
                if (residualDirectory != null)
                    gateway.RemoveItemAsync(root, residualDirectory.Id, true);

                directory = gateway.NewDirectoryItemAsync(root, rootDirectory.Id, path).Result;
            }

            internal DirectoryInfoContract ToContract()
            {
                return directory;
            }

            public void Dispose()
            {
                gateway.RemoveItemAsync(root, directory.Id, true);
            }
        }

        private class Fixture
        {
            private const string COMPOSITION_DIRECTORY = "Gateways";

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Required for MEF composition")]
            [ImportMany]
            public IList<ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata>> AsyncGateways { get; set; }

            public static void Initialize()
            {
                CompositionInitializer.Preload(typeof(IAsyncCloudGateway));
                CompositionInitializer.Initialize(COMPOSITION_DIRECTORY);
            }

            private static void Log(string message)
            {
                Console.WriteLine(message);
            }

            public static IEnumerable<ConfigManager.AsyncGatewayConfigElement> GetAsyncGatewayConfigurations(ConfigManager.GatewayCapabilities capability = ConfigManager.GatewayCapabilities.None)
            {
                return ConfigManager.GetAsyncGatewayConfigurations().Where(g => capability == ConfigManager.GatewayCapabilities.None || !g.Exclusions.HasFlag(capability));
            }

            public IAsyncCloudGateway GetAsyncGateway(ConfigManager.AsyncGatewayConfigElement config)
            {
                return AsyncGateways.Single(g => g.Metadata.CloudService == config.Schema).CreateExport().Value;
            }

            public RootName GetRootName(ConfigManager.AsyncGatewayConfigElement config) => new RootName(config.Schema, config.UserName, config.Root);

            public TestDirectoryFixture CreateTestDirectory(ConfigManager.AsyncGatewayConfigElement config) => new TestDirectoryFixture(GetAsyncGateway(config), GetRootName(config), config.ApiKey, config.TestDirectory);

            public void ExecuteByConfiguration(Action<ConfigManager.AsyncGatewayConfigElement> test, ConfigManager.GatewayCapabilities capability, bool inParallel = true)
            {
                var failures = new List<Tuple<string, Exception>>();

                if (inParallel) {
                    Parallel.ForEach(GetAsyncGatewayConfigurations(capability), config => {
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
                    foreach (var config in GetAsyncGatewayConfigurations(capability)) {
                        try {
                            var startedAt = DateTime.Now;
                            test(config);
                            var completedAt = DateTime.Now;
                            Log($"Sequential test for schema '{config.Schema}' completed in {completedAt - startedAt}");
                        }
                        catch (Exception ex) {
                            Log($"Sequential test for schema '{config.Schema}' failed");
                            failures.Add(new Tuple<string, Exception>(config.Schema, ex));
                        }
                    }
                }

                if (failures.Any())
                    throw new AggregateException("Test failed in " + string.Join(", ", failures.Select(t => t.Item1)), failures.Select(t => t.Item2));
            }

            public IProgress<ProgressValue> GetProgressReporter() => new NullProgressReporter();
        }
    }
}
