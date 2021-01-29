// Copyright (c) Umbraco.
// See LICENSE for more details.

using System;
using NUnit.Framework;
using Umbraco.Infrastructure.Security;

namespace Umbraco.Tests.UnitTests.Umbraco.Infrastructure.Security
{
    public class NoOpLookupNormalizerTests
    {
        [Test]
        public void NormalizeName_Expect_Input_Returned()
        {
            var name = Guid.NewGuid().ToString();
            var sut = new NoOpLookupNormalizer();

            var normalizedName = sut.NormalizeName(name);

            Assert.AreEqual(name, normalizedName);
        }

        [Test]
        public void NormalizeEmail_Expect_Input_Returned()
        {
            var email = $"{Guid.NewGuid()}@umbraco";
            var sut = new NoOpLookupNormalizer();

            var normalizedEmail = sut.NormalizeEmail(email);

            Assert.AreEqual(email, normalizedEmail);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void NormalizeName_When_Name_Null_Or_Whitespace_Expect_Same_Returned(string name)
        {
            var sut = new NoOpLookupNormalizer();

            var normalizedName = sut.NormalizeName(name);

            Assert.AreEqual(name, normalizedName);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void NormalizeEmail_When_Name_Null_Or_Whitespace_Expect_Same_Returned(string email)
        {
            var sut = new NoOpLookupNormalizer();

            var normalizedEmail = sut.NormalizeEmail(email);

            Assert.AreEqual(email, normalizedEmail);
        }
    }
}
