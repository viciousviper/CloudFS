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
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.AppDomainResolver;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;
using IgorSoft.CloudFS.GatewayTests.Config;

namespace IgorSoft.CloudFS.GatewayTests
{
    [TestClass]
    public class GatewayTestsFixture
    {
        private static TestSection testSection;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Required for MEF composition")]
        [ImportMany]
        public IList<ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata>> AsyncGateways { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Required for MEF composition")]
        [ImportMany]
        public IList<ExportFactory<ICloudGateway, CloudGatewayMetadata>> Gateways { get; set; }

        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            AssemblyResolver.Initialize();
            CompositionInitializer.Preload(typeof(IAsyncCloudGateway));

            testSection = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).Sections[TestSection.Name] as TestSection;
            if (testSection == null)
                throw new ConfigurationErrorsException("Test configuration missing");
            CompositionInitializer.Initialize(testSection.LibPath);
        }

        private static void Log(string message)
        {
            Console.WriteLine(message);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public static IEnumerable<GatewayElement> GetGatewayConfigurations(GatewayType type, GatewayCapabilities capability = GatewayCapabilities.None)
        {
            return testSection.Gateways.Where(g => g.Type == type && (capability == GatewayCapabilities.None || !g.Exclusions.HasFlag(capability)));
        }

        public IAsyncCloudGateway GetAsyncGateway(GatewayElement config)
        {
            return AsyncGateways.Single(g => g.Metadata.CloudService == config.Schema).CreateExport().Value;
        }

        public ICloudGateway GetGateway(GatewayElement config)
        {
            return Gateways.Single(g => g.Metadata.CloudService == config.Schema).CreateExport().Value;
        }

        public RootName GetRootName(GatewayElement config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return new RootName(config.Schema, config.UserName, config.Root);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void ExecuteByConfiguration(Action<GatewayElement> test, GatewayType type, GatewayCapabilities capability, bool inParallel = true)
        {
            var failures = new List<Tuple<string, Exception>>();

            if (inParallel) {
                Parallel.ForEach(GetGatewayConfigurations(type, capability), config => {
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
                foreach (var config in GetGatewayConfigurations(type, capability)) {
                    try {
                        var startedAt = DateTime.Now;
                        test(config);
                        var completedAt = DateTime.Now;
                        Log($"Sequential test for schema '{config.Schema}' completed in {completedAt - startedAt}");
                    } catch (Exception ex) {
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
