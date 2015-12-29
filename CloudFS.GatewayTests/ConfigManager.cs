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
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;

namespace IgorSoft.CloudFS.GatewayTests
{
    internal static class ConfigManager
    {
        private const string GATEWAY_SECTION = "gatewaySection";

        [Flags]
        public enum GatewayCapabilities
        {
            None =                  0x0000,
            GetDrive =              0x0001,
            GetRoot =               0x0002,
            GetChildItem =          0x0004,
            ClearContent =          0x0008,
            GetContent =            0x0010,
            SetContent =            0x0020,
            CopyFileItem =          0x0040,
            CopyDirectoryItem =     0x0080,
            MoveFileItem =          0x0100,
            MoveDirectoryItem =     0x0200,
            NewDirectoryItem =      0x0400,
            NewFileItem =           0x0800,
            RemoveItem =            0x1000,
            RenameDirectoryItem =   0x2000,
            RenameFileItem =        0x4000,
            All =                   0x4FFF
        }

        public abstract class GatewayConfigElementBase : ConfigurationElement
        {
            public const string TEST_DIRECTORY = "GatewayTests";

            [ConfigurationProperty("schema", IsRequired = true, IsKey = true)]
            public string Schema
            {
                get { return (string)this[nameof(Schema).ToPascalCase()]; }
                set { this[nameof(Schema).ToPascalCase()] = value; }
            }

            [ConfigurationProperty("userName")]
            public string UserName
            {
                get { return (string)this[nameof(UserName).ToPascalCase()]; }
                set { this[nameof(UserName).ToPascalCase()] = value; }
            }

            [ConfigurationProperty("root", IsRequired = true)]
            public string Root
            {
                get { return (string)this[nameof(Root).ToPascalCase()]; }
                set { this[nameof(Root).ToPascalCase()] = value; }
            }

            [ConfigurationProperty("apiKey")]
            public string ApiKey
            {
                get { return (string)this[nameof(ApiKey).ToPascalCase()]; }
                set { this[nameof(ApiKey).ToPascalCase()] = value; }
            }

            [ConfigurationProperty("exclusions", DefaultValue = GatewayCapabilities.None)]
            public GatewayCapabilities Exclusions
            {
                get { return (GatewayCapabilities)this[nameof(Exclusions).ToPascalCase()]; }
                set { this[nameof(Exclusions).ToPascalCase()] = value; }
            }

            [ConfigurationProperty("testDirectory", DefaultValue = TEST_DIRECTORY)]
            public string TestDirectory
            {
                get { return (string)this[nameof(TestDirectory).ToPascalCase()]; }
                set { this[nameof(TestDirectory).ToPascalCase()] = value; }
            }
        }

        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
        public sealed class AsyncGatewayConfigElement : GatewayConfigElementBase
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object[])", Justification = "Debugger Display")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            private string DebuggerDisplay => $"{nameof(AsyncGatewayConfigElement)} {UserName}@{Schema} ({Root}) ApiKey={ApiKey}";
        }

        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
        public sealed class GatewayConfigElement : GatewayConfigElementBase
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object[])", Justification = "Debugger Display")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            private string DebuggerDisplay => $"{nameof(GatewayConfigElement)} {UserName}@{Schema} ({Root}) ApiKey={ApiKey}";
        }

        public abstract class ConfigElementCollection<TElement> : ConfigurationElementCollection
            where TElement : GatewayConfigElementBase, new()
        { 
            protected override ConfigurationElement CreateNewElement() => new TElement();

            protected override object GetElementKey(ConfigurationElement element) => ((TElement)element).Schema;

            public TElement Add(string schema, string userName, string root, string apiKey)
            {
                if (string.IsNullOrEmpty(schema))
                    throw new ArgumentNullException(nameof(schema));
                if (string.IsNullOrEmpty(root))
                    throw new ArgumentNullException(nameof(root));

                var result = CreateNewElement() as TElement;
                result.Schema = schema;
                result.UserName = userName;
                result.Root = root;
                result.ApiKey = apiKey;
                BaseAdd(result);
                return result;
            }

            public void Remove(string key)
            {
                BaseRemove(key);
            }

            public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMapAlternate;
        }

        [ConfigurationCollection(typeof(AsyncGatewayConfigElementCollection), CollectionType = ConfigurationElementCollectionType.BasicMapAlternate, AddItemName = ITEM_NAME)]
        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
        public sealed class AsyncGatewayConfigElementCollection : ConfigElementCollection<AsyncGatewayConfigElement>, IEnumerable<AsyncGatewayConfigElement>
        {
            private const string ITEM_NAME = "AsyncGateway";

            protected override string ElementName => ITEM_NAME;

            IEnumerator<AsyncGatewayConfigElement> IEnumerable<AsyncGatewayConfigElement>.GetEnumerator()
            {
                foreach (var item in this)
                    yield return (AsyncGatewayConfigElement)item;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object)", Justification = "Debugger Display")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            private string DebuggerDisplay => $"{nameof(AsyncGatewayConfigElementCollection)}[{Count}]";
        }

        [ConfigurationCollection(typeof(GatewayConfigElementBase), CollectionType = ConfigurationElementCollectionType.BasicMapAlternate, AddItemName = ITEM_NAME)]
        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
        public sealed class GatewayConfigElementCollection : ConfigElementCollection<GatewayConfigElement>, IEnumerable<GatewayConfigElement>
        {
            private const string ITEM_NAME = "Gateway";

            protected override string ElementName => ITEM_NAME;

            IEnumerator<GatewayConfigElement> IEnumerable<GatewayConfigElement>.GetEnumerator()
            {
                foreach (var item in this)
                    yield return (GatewayConfigElement)item;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object)", Justification = "Debugger Display")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            private string DebuggerDisplay => $"{nameof(GatewayConfigElementCollection)}[{Count}]";
        }

        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
        public sealed class GatewayConfigSection : ConfigurationSection
        {
            private const string ASYNC_GATEWAYS_NAME = "asyncGateways";

            private const string GATEWAYS_NAME = "gateways";

            [ConfigurationProperty(ASYNC_GATEWAYS_NAME)]
            public AsyncGatewayConfigElementCollection AsyncGateways => base[ASYNC_GATEWAYS_NAME] as AsyncGatewayConfigElementCollection;

            [ConfigurationProperty(GATEWAYS_NAME)]
            public GatewayConfigElementCollection Gateways => base[GATEWAYS_NAME] as GatewayConfigElementCollection;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)", Justification = "Debugger Display")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            private static string DebuggerDisplay => $"{nameof(GatewayConfigSection)}";
        }

        public static IList<AsyncGatewayConfigElement> GetAsyncGatewayConfigurations()
        {
            var assemblyLocation = Path.Combine(Environment.CurrentDirectory, Assembly.GetExecutingAssembly().Location);
            var configuration = ConfigurationManager.OpenExeConfiguration(assemblyLocation);

            return ((GatewayConfigSection)configuration.Sections[GATEWAY_SECTION]).AsyncGateways.ToList();
        }

        public static IList<GatewayConfigElement> GetGatewayConfigurations()
        {
            var assemblyLocation = Path.Combine(Environment.CurrentDirectory, Assembly.GetExecutingAssembly().Location);
            var configuration = ConfigurationManager.OpenExeConfiguration(assemblyLocation);

            return ((GatewayConfigSection)configuration.Sections[GATEWAY_SECTION]).Gateways.ToList();
        }

        public static string ToPascalCase(this string value)
        {
            return !string.IsNullOrEmpty(value) && char.IsUpper(value[0])
                ? value.Substring(1).Insert(0, char.ToLowerInvariant(value[0]).ToString())
                : value;
        }
    }
}
