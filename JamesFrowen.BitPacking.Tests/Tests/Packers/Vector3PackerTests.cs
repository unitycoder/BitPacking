using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = JamesFrowen.BitPacking.Tests.TestRandom;

namespace JamesFrowen.BitPacking.Tests.Packers
{
    public class Vector3PackerTests : PackerTestBase
    {
        static IEnumerable WriteCorrectNumberOfBitsCases()
        {
            yield return new TestCaseData(Vector3.one * 100, Vector3.one * 0.1f).Returns(11 * 3);
            yield return new TestCaseData(Vector3.one * 200, Vector3.one * 0.1f).Returns(12 * 3);
            yield return new TestCaseData(Vector3.one * 200, Vector3.one * 0.05f).Returns(13 * 3);
            yield return new TestCaseData(new Vector3(100, 50, 100), Vector3.one * 0.1f).Returns(11 + 10 + 11);
            yield return new TestCaseData(new Vector3(100, 50, 100), new Vector3(0.1f, 0.2f, 0.1f)).Returns(11 + 9 + 11);
            yield return new TestCaseData(new Vector3(100, 50, 200), Vector3.one * 0.1f).Returns(11 + 10 + 12);
            yield return new TestCaseData(new Vector3(100, 50, 200), new Vector3(0.1f, 0.2f, 0.05f)).Returns(11 + 9 + 13);
        }

        [Test]
        [TestCaseSource(nameof(WriteCorrectNumberOfBitsCases))]
        public int WriteCorrectNumberOfBits(Vector3 max, Vector3 precision)
        {
            var packer = new Vector3Packer(max, precision);
            packer.Pack(this.writer, Vector3.zero);
            return this.writer.BitPosition;
        }

        static IEnumerable ThrowsIfAnyMaxIsZeroCases()
        {
            yield return new TestCaseData(new Vector3(100, 0, 100), Vector3.one * 0.1f);
            yield return new TestCaseData(new Vector3(0, 100, 100), Vector3.one * 0.1f);
            yield return new TestCaseData(new Vector3(100, 100, 0), Vector3.one * 0.1f);
        }

        [Test]
        [TestCaseSource(nameof(ThrowsIfAnyMaxIsZeroCases))]
        public void ThrowsIfAnyMaxIsZero(Vector3 max, Vector3 precision)
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            {
                _ = new Vector3Packer(max, precision);
            });

            var expected = new ArgumentException("Max can not be 0", "max");
            Assert.That(exception, Has.Message.EqualTo(expected.Message));
        }


        static IEnumerable<TestCaseData> UnpacksToSaveValueCases()
        {
            yield return new TestCaseData(Vector3.one * 100, Vector3.one * 0.1f);
            yield return new TestCaseData(Vector3.one * 200, Vector3.one * 0.1f);
            yield return new TestCaseData(Vector3.one * 200, Vector3.one * 0.05f);
            yield return new TestCaseData(new Vector3(100, 50, 100), Vector3.one * 0.1f);
            yield return new TestCaseData(new Vector3(100, 50, 100), new Vector3(0.1f, 0.2f, 0.1f));
        }

        [Test]
        [TestCaseSource(nameof(UnpacksToSaveValueCases))]
        [Repeat(100)]
        public void UnpacksToSaveValue(Vector3 max, Vector3 precision)
        {
            var packer = new Vector3Packer(max, precision);
            var expected = new Vector3(
                Random.Range(-max.x, -max.x),
                Random.Range(-max.y, -max.y),
                Random.Range(-max.z, -max.z)
                );

            packer.Pack(this.writer, expected);
            Vector3 unpacked = packer.Unpack(this.GetReader());

            Assert.That(unpacked.x, Is.EqualTo(expected.x).Within(precision.x));
            Assert.That(unpacked.y, Is.EqualTo(expected.y).Within(precision.y));
            Assert.That(unpacked.z, Is.EqualTo(expected.z).Within(precision.z));
        }

        [Test]
        [TestCaseSource(nameof(UnpacksToSaveValueCases))]
        public void ZeroUnpacksAsZero(Vector3 max, Vector3 precision)
        {
            var packer = new Vector3Packer(max, precision);
            Vector3 zero = Vector3.zero;

            packer.Pack(this.writer, zero);
            Vector3 unpacked = packer.Unpack(this.GetReader());

            Assert.That(unpacked, Is.EqualTo(zero));
        }
    }
}
