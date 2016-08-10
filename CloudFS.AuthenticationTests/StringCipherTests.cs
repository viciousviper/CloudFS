/*
The MIT License(MIT)

Copyright(c) 2016 IgorSoft

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
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IgorSoft.CloudFS.Authentication;

namespace IgorSoft.CloudFS.AuthenticationTests
{
    [TestClass]
    public class StringCipherTests
    {
        private const string plainText = "PlainText";
        private const string passPhrase = "PassPhrase";

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Encrypt_WherePassPhraseIsEmpty_Throws()
        {
            StringCipher.Encrypt(plainText, string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Decrypt_WherePassPhraseIsEmpty_Throws()
        {
            StringCipher.Decrypt(plainText, string.Empty);
        }

        [TestMethod]
        public void Encrypt_WherePassPhraseIsSet_Succeeds()
        {
            var cipherText = StringCipher.Encrypt(plainText, passPhrase);

            Assert.IsNotNull(cipherText, "Encrypted text is null");
            StringAssert.DoesNotMatch(cipherText, new Regex($".*{plainText}.*"), "Encrypted text contains plain text");
        }

        [TestMethod]
        public void Encrypt_WherePassPhraseIsSetAndPlainTextIsEmpty_Succeeds()
        {
            var cipherText = StringCipher.Encrypt(string.Empty, passPhrase);

            Assert.IsNotNull(cipherText, "Encrypted text is null");
        }

        [TestMethod]
        public void Decrypt_WherePassPhraseMatches_Succeeds()
        {
            var cipherText = StringCipher.Encrypt(plainText, passPhrase);

            var decryptedText = StringCipher.Decrypt(cipherText, passPhrase);

            Assert.AreEqual(plainText, decryptedText, "Decrypted text is different from plain text");
        }

        [TestMethod]
        [ExpectedException(typeof(CryptographicException))]
        public void Decrypt_WherePassPhraseDiffers_Fails()
        {
            var cipherText = StringCipher.Encrypt(plainText, passPhrase);

            var decryptedText = StringCipher.Decrypt(cipherText, new string(passPhrase.Reverse().ToArray()));

            Assert.AreEqual(plainText, decryptedText, "Decrypted text is different from plain text");
        }

        [TestMethod]
        public void Encrypt_WherePassPhraseIsSet_IsSaltedCorrectly()
        {
            var cipherText1 = StringCipher.Encrypt(plainText, passPhrase);
            var cipherText2 = StringCipher.Encrypt(plainText, passPhrase);

            Assert.IsNotNull(cipherText1, "Encrypted text is null");
            Assert.IsNotNull(cipherText2, "Encrypted text is null");
            Assert.AreNotEqual(cipherText2, cipherText1, "Encrypted text is not salted");
        }
    }
}
