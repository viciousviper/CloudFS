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
using System.Collections.Concurrent;
using System.Composition;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
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

        private static IAppCache cache = new CachingService();

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
        public static IEnumerable<GatewayElement> GetGatewayConfigurations(GatewayType type, GatewayCapabilities capability)
        {
            return testSection.Gateways?.Where(g => g.Type == type && (capability == GatewayCapabilities.None || !g.Exclusions.HasFlag(capability))) ?? Enumerable.Empty<GatewayElement>();
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

            return new RootName(config.Schema, config.UserName, config.Mount);
        }

        public IDictionary<string, string> GetParameters(GatewayElement config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var parameters = config.Parameters;
            if (string.IsNullOrEmpty(parameters))
                return null;

            var result = new Dictionary<string, string>();
            foreach (var parameter in parameters.Split('|')) {
                var components = parameter.Split(new[] { '=' }, 2);
                result.Add(components[0], components.Length == 2 ? components[1] : null);
            }

            return result;
        }

        private void CallTimedTestOnConfig<TGateway>(Action<TGateway, RootName, GatewayElement> test, GatewayElement config, Func<GatewayElement, TGateway> getGateway, IDictionary<string, Exception> failures)
        {
            try {
                var startedAt = DateTime.Now;
                var gateway = getGateway(config);
                var rootName = GetRootName(config);
                test(gateway, rootName, config);
                var completedAt = DateTime.Now;
                Log($"Test for schema '{config.Schema}' completed in {completedAt - startedAt}".ToString(CultureInfo.CurrentCulture));
            } catch (Exception ex) {
                var aggregateException = ex as AggregateException;
                var message = aggregateException != null ? string.Join(", ", aggregateException.InnerExceptions.Select(e => e.Message)) : ex.Message;
                Log($"Test for schema '{config.Schema}' failed:\n\t {message}".ToString(CultureInfo.CurrentCulture));
                failures.Add(config.Schema, ex);
            }
        }

        private void ExecuteByConfiguration<TGateway>(Action<TGateway, RootName, GatewayElement> test, GatewayType type, Func<GatewayElement, TGateway> getGateway, int maxDegreeOfParallelism)
        {
            var configurations = GetGatewayConfigurations(type, GatewayCapabilities.None);
            var failures = default(IDictionary<string, Exception>);

            if (maxDegreeOfParallelism > 1) {
                failures = new ConcurrentDictionary<string, Exception>();
                Parallel.ForEach(configurations, new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism }, config => {
                    CallTimedTestOnConfig<TGateway>(test, config, getGateway, failures);
                });
            } else if (maxDegreeOfParallelism == 1) {
                failures = new Dictionary<string, Exception>();
                foreach (var config in configurations) {
                    CallTimedTestOnConfig<TGateway>(test, config, getGateway, failures);
                }
            } else {
                throw new ArgumentException("Degree of parallelism must be positive", nameof(maxDegreeOfParallelism));
            }

            if (failures.Any())
                throw new AggregateException("Test failed in " + string.Join(", ", failures.Select(t => t.Key)), failures.Select(t => t.Value));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void ExecuteByConfiguration(Action<IAsyncCloudGateway, RootName, GatewayElement> test, int maxDegreeOfParallelism = int.MaxValue)
        {
            ExecuteByConfiguration(test, GatewayType.Async, config => GetAsyncGateway(config), maxDegreeOfParallelism);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void ExecuteByConfiguration(Action<ICloudGateway, RootName, GatewayElement> test, int maxDegreeOfParallelism = int.MaxValue)
        {
            ExecuteByConfiguration(test, GatewayType.Sync, config => GetGateway(config), maxDegreeOfParallelism);
        }

        public IProgress<ProgressValue> GetProgressReporter() => new NullProgressReporter();

        public void OnCondition(GatewayElement config, GatewayCapabilities capability, Action action)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var capabilityExcluded = config.Exclusions.HasFlag(capability);

            try {
                action();

                Assert.IsFalse(capabilityExcluded, $"Unexpected capability {capability}".ToString(CultureInfo.CurrentCulture));
            } catch (NotSupportedException) when (capabilityExcluded) {
            } catch (AggregateException ex) when (capabilityExcluded && ex.InnerExceptions.Count == 1 && ex.InnerException is NotSupportedException) {
            }
        }

        public byte[] GetArbitraryBytes(int size) => cache.GetOrAdd(size.ToString(CultureInfo.InvariantCulture), () => Enumerable.Range(0, size).Select(i => (byte)(i % 251 + 1)).ToArray());
    }
}
