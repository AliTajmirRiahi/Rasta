using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasta.Text;

namespace Rasta.Tests
{
    [TestClass]
    public class RtlTextDetectorTests
    {
        [TestMethod]
        public void IsRtlCharacter_PersianLetter_IsTrue()
        {
            Assert.IsTrue(RtlTextDetector.IsRtlCharacter('س'));
        }

        [TestMethod]
        public void IsRtlCharacter_LatinLetter_IsFalse()
        {
            Assert.IsFalse(RtlTextDetector.IsRtlCharacter('a'));
        }

        [TestMethod]
        [DataRow('؀')]
        [DataRow('ۿ')]
        [DataRow('ݐ')]
        [DataRow('ݿ')]
        [DataRow('ࢠ')]
        [DataRow('ࣿ')]
        [DataRow('ﭐ')]
        [DataRow('﷿')]
        [DataRow('ﹰ')]
        [DataRow('﻿')]
        public void IsRtlCharacter_RangeBoundaries_AreRtl(char c)
        {
            Assert.IsTrue(RtlTextDetector.IsRtlCharacter(c));
        }

        [TestMethod]
        [DataRow('׿')]
        [DataRow('܀')]
        [DataRow('ݏ')]
        [DataRow('ހ')]
        [DataRow('࢟')]
        [DataRow('ﭏ')]
        [DataRow('﹯')]
        [DataRow('＀')]
        public void IsRtlCharacter_JustOutsideRanges_IsNotRtl(char c)
        {
            Assert.IsFalse(RtlTextDetector.IsRtlCharacter(c));
        }

        [TestMethod]
        public void CountRtlLetters_PurePersian_CountsEveryLetter()
        {
            Assert.AreEqual(8, RtlTextDetector.CountRtlLetters("سلام دنیا"));
        }

        [TestMethod]
        public void CountRtlLetters_PureEnglish_IsZero()
        {
            Assert.AreEqual(0, RtlTextDetector.CountRtlLetters("Hello world"));
        }

        [TestMethod]
        public void CountRtlLetters_Mixed_CountsOnlyPersianLetters()
        {
            Assert.AreEqual(6, RtlTextDetector.CountRtlLetters("این متد UserId"));
        }

        [TestMethod]
        public void CountRtlLetters_PersianDigits_AreNotLetters()
        {
            Assert.AreEqual(0, RtlTextDetector.CountRtlLetters("۱۲۳"));
        }

        [TestMethod]
        public void CountRtlLetters_Arabic_IsCounted()
        {
            Assert.AreEqual(12, RtlTextDetector.CountRtlLetters("مرحبا بالعالم"));
        }

        [TestMethod]
        public void CountRtlLetters_Zwnj_IsNotCounted()
        {
            Assert.AreEqual(6, RtlTextDetector.CountRtlLetters("می‌خونه"));
        }

        [TestMethod]
        public void CountRtlLetters_NullAndEmpty_AreZero()
        {
            Assert.AreEqual(0, RtlTextDetector.CountRtlLetters(null));
            Assert.AreEqual(0, RtlTextDetector.CountRtlLetters(string.Empty));
        }

        [TestMethod]
        public void ContainsRtl_HonoursMinimum()
        {
            Assert.IsTrue(RtlTextDetector.ContainsRtl("ab س", 1));
            Assert.IsFalse(RtlTextDetector.ContainsRtl("ab س", 2));
            Assert.IsTrue(RtlTextDetector.ContainsRtl("ab سل", 2));
        }

        [TestMethod]
        public void ContainsRtl_MinimumBelowOne_IsTreatedAsOne()
        {
            Assert.IsFalse(RtlTextDetector.ContainsRtl("abc", 0));
            Assert.IsTrue(RtlTextDetector.ContainsRtl("س", -5));
        }

        [TestMethod]
        public void ContainsRtl_Null_IsFalse()
        {
            Assert.IsFalse(RtlTextDetector.ContainsRtl(null, 1));
        }

        [TestMethod]
        public void GetBaseDirection_EnglishFirstWithoutForce_IsLeftToRight()
        {
            Assert.AreEqual(TextDirection.LeftToRight, RtlTextDetector.GetBaseDirection("Hello سلام", false));
        }

        [TestMethod]
        public void GetBaseDirection_EnglishFirstWithForce_IsRightToLeft()
        {
            Assert.AreEqual(TextDirection.RightToLeft, RtlTextDetector.GetBaseDirection("Hello سلام", true));
        }

        [TestMethod]
        public void GetBaseDirection_PersianFirst_IsRightToLeft()
        {
            Assert.AreEqual(TextDirection.RightToLeft, RtlTextDetector.GetBaseDirection("سلام Hello", false));
        }

        [TestMethod]
        public void GetBaseDirection_NoStrongLetters_IsLeftToRight()
        {
            Assert.AreEqual(TextDirection.LeftToRight, RtlTextDetector.GetBaseDirection("123 !!!", true));
        }

        [TestMethod]
        public void GetBaseDirection_EnglishOnlyWithForce_IsLeftToRight()
        {
            Assert.AreEqual(TextDirection.LeftToRight, RtlTextDetector.GetBaseDirection("Hello", true));
        }

        [TestMethod]
        public void GetBaseDirection_Null_IsLeftToRight()
        {
            Assert.AreEqual(TextDirection.LeftToRight, RtlTextDetector.GetBaseDirection(null, true));
        }
    }
}
