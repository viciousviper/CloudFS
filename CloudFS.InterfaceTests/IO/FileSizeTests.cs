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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.CloudFS.InterfaceTests.IO
{
    [TestClass]
    public sealed class FileSizeTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CreateNew_WhereSizeIsZero_ReturnsFileSizeEmpty()
        {
            var sut = new FileSize(0);

            Assert.AreEqual(FileSize.Empty, sut);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void CreateNew_WhereSizeIsPositive_Succeeds()
        {
            var size = 1000;

            var sut = new FileSize(size);

            Assert.AreEqual(size, sut.Value);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentException))]
        public void CreateNew_WhereSizeIsNegative_Throws()
        {
#pragma warning disable S1481 // Unused local variables should be removed
            var sut = new FileSize(-1);
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void OperatorExplicitFileSize_Succeeds()
        {
            var size = 1000L;

            var sut = new FileSize(size);

            Assert.AreEqual((FileSize)size, sut);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void OperatorImplicitLong_Succeeds()
        {
            var size = 1000L;

            var sut = new FileSize(size);

            Assert.AreEqual(size, sut);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void OperatorPlus_ReturnsCorrectSumFileSize()
        {
            var size1 = 100;
            var size2 = 50;

            var sut = new FileSize(size1) + new FileSize(size2);

            Assert.AreEqual(new FileSize(size1 + size2), sut);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void OperatorPlus_WhereFirstOperandIsNull_Throws()
        {
#pragma warning disable S1481 // Unused local variables should be removed
            var sut = default(FileSize) + FileSize.Empty;
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void OperatorPlus_WhereSecondOperandIsNull_Throws()
        {
#pragma warning disable S1481 // Unused local variables should be removed
            var sut = FileSize.Empty + default(FileSize);
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void OperatorMinus_ReturnsCorrectDifferenceFileSize()
        {
            var size1 = 100;
            var size2 = 50;

            var sut = new FileSize(size1) - new FileSize(size2);

            Assert.AreEqual(new FileSize(size1 - size2), sut);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void OperatorMinus_WhereFirstOperandIsNull_Throws()
        {
#pragma warning disable S1481 // Unused local variables should be removed
            var sut = default(FileSize) - FileSize.Empty;
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void OperatorMinus_WhereSecondOperandIsNull_Throws()
        {
#pragma warning disable S1481 // Unused local variables should be removed
            var sut = FileSize.Empty - default(FileSize);
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void OperatorMultiplyLeft_ByLongValue_WhereOperandIsNull_Throws()
        {
            var factor = 5L;

#pragma warning disable S1481 // Unused local variables should be removed
            var sut = factor * default(FileSize);
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void OperatorMultiplyLeft_ByLongValue_ReturnsCorrectProductFileSize()
        {
            var size = 1000;
            var factor = 5L;

            var sut = new FileSize(size);

            Assert.AreEqual(new FileSize(factor * size), factor * sut);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void OperatorMultiplyLeft_ByDecimalValue_WhereOperandIsNull_Throws()
        {
            var factor = 5.5m;

#pragma warning disable S1481 // Unused local variables should be removed
            var sut = factor * default(FileSize);
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void OperatorMultiplyLeft_ByDecimalValue_ReturnsCorrectProductFileSize()
        {
            var size = 1000;
            var factor = 5.5m;

            var sut = new FileSize(size);

            Assert.AreEqual(new FileSize(factor * size), factor * sut);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void OperatorMultiplyRight_ByLongValue_WhereOperandIsNull_Throws()
        {
            var factor = 5L;

#pragma warning disable S1481 // Unused local variables should be removed
            var sut = default(FileSize) * factor;
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void OperatorMultiplyRight_ByLongValue_ReturnsCorrectProductFileSize()
        {
            var size = 1000;
            var factor = 5L;

            var sut = new FileSize(size);

            Assert.AreEqual(new FileSize(size * factor), sut * factor);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void OperatorMultiplyRight_ByDecimalValue_WhereOperandIsNull_Throws()
        {
            var factor = 5.5m;

#pragma warning disable S1481 // Unused local variables should be removed
            var sut = default(FileSize) * factor;
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void OperatorMultiplyRight_ByDecimalValue_ReturnsCorrectProductFileSize()
        {
            var size = 1000;
            var factor = 5.5m;

            var sut = new FileSize(size);

            Assert.AreEqual(new FileSize(size * factor), sut * factor);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void OperatorDivideRight_ByLongValue_WhereOperandIsNull_Throws()
        {
            var divisor = 5L;

#pragma warning disable S1481 // Unused local variables should be removed
            var sut = default(FileSize) / divisor;
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void OperatorDivideRight_ByLongValue_ReturnsCorrectQuotientFileSize()
        {
            var size = 1000;
            var divisor = 5L;

            var sut = new FileSize(size);

            Assert.AreEqual(new FileSize(size / divisor), sut / divisor);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArgumentNullException))]
        public void OperatorDivideRight_ByDecimalValue_WhereOperandIsNull_Throws()
        {
            var divisor = 2.5m;

#pragma warning disable S1481 // Unused local variables should be removed
            var sut = default(FileSize) / divisor;
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void OperatorDivideRight_ByDecimalValue_ReturnsCorrectQuotientFileSize()
        {
            var size = 1000;
            var divisor = 2.5m;

            var sut = new FileSize(size);

            Assert.AreEqual(new FileSize(size / divisor), sut / divisor);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [DeploymentItem("IO\\FileSizeTests.Configuration.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\IO\\FileSizeTests.Configuration.xml", "Invalid", DataAccessMethod.Sequential)]
        [ExpectedException(typeof(FormatException))]
        public void Parse_WhereTextIsInvalid_Throws()
        {
            var displayValue = ((string)TestContext.DataRow["DisplayValue"]).Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

#pragma warning disable S1481 // Unused local variables should be removed
            var sut = new FileSize(displayValue);
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [ExpectedException(typeof(ArithmeticException))]
        public void Parse_WhereSizeIsNonIntegral_Throws()
        {
#pragma warning disable S1481 // Unused local variables should be removed
            var sut = new FileSize("1.3kB".Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator));
#pragma warning restore S1481 // Unused local variables should be removed
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        public void Parse_Empty_Succeeds()
        {
            var sut = new FileSize(nameof(FileSize.Empty));

            Assert.AreEqual(0, sut.Value);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [DeploymentItem("IO\\FileSizeTests.Configuration.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\IO\\FileSizeTests.Configuration.xml", "Mapping", DataAccessMethod.Sequential)]
        public void Parse_ForDifferentValues_Succeeds()
        {
            var displayValue = ((string)TestContext.DataRow["DisplayValue"]).Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            var value = long.Parse((string)TestContext.DataRow["Value"]);

            var sut = new FileSize(displayValue);

            Assert.AreEqual(value, sut.Value);
        }

        [TestMethod, TestCategory(nameof(TestCategories.Offline))]
        [DeploymentItem("IO\\FileSizeTests.Configuration.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML", "|DataDirectory|\\IO\\FileSizeTests.Configuration.xml", "Mapping", DataAccessMethod.Sequential)]
        public void ToString_ForDifferentValues_Succeeds()
        {
            var value = long.Parse((string)TestContext.DataRow["Value"]);
            var displayValue = ((string)TestContext.DataRow["DisplayValue"]).Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

            var sut = new FileSize(value);

            Assert.AreEqual(displayValue, sut.ToString());
        }
    }
}
