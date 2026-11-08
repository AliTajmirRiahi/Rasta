using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasta.Text;

namespace Rasta.Tests
{
    [TestClass]
    public class BidiRunSplitterTests
    {
        private static string Describe(IReadOnlyList<BidiRun> runs)
        {
            var sb = new StringBuilder();
            foreach (BidiRun run in runs)
            {
                if (sb.Length > 0)
                {
                    sb.Append('|');
                }

                switch (run.Kind)
                {
                    case BidiRunKind.Opposite:
                        sb.Append("O:").Append(run.Text);
                        break;
                    case BidiRunKind.Code:
                        sb.Append("C:").Append(run.Text);
                        break;
                    case BidiRunKind.LineBreak:
                        sb.Append("NL");
                        break;
                    default:
                        sb.Append("T:").Append(run.Text);
                        break;
                }
            }

            return sb.ToString();
        }

        private static string Rtl(string text)
        {
            return Describe(BidiRunSplitter.Split(text, TextDirection.RightToLeft));
        }

        [TestMethod]
        public void Split_PurePersian_IsOneTextRun()
        {
            Assert.AreEqual("T:سلام دنیا", Rtl("سلام دنیا"));
        }

        [TestMethod]
        public void Split_PureEnglish_IsOneOppositeRun()
        {
            Assert.AreEqual("O:Hello world", Rtl("Hello world"));
        }

        [TestMethod]
        public void Split_MixedSentence_IsolatesEachEnglishWord()
        {
            Assert.AreEqual(
                "T:این متد |O:UserId|T: رو از |O:Cache|T: می\u200Cخونه",
                Rtl("این متد UserId رو از Cache می\u200Cخونه"));
        }

        [TestMethod]
        public void Split_Number_IsIsolated()
        {
            Assert.AreEqual("T:بیشتر از |O:30|T: ثانیه", Rtl("بیشتر از 30 ثانیه"));
        }

        [TestMethod]
        public void Split_ParenthesesStayOutsideTheRun()
        {
            Assert.AreEqual("T:(طبق |O:RFC 7230|T:)", Rtl("(طبق RFC 7230)"));
        }

        [TestMethod]
        public void Split_Url_IsOneRun()
        {
            Assert.AreEqual(
                "T:آدرس |O:https://example.com/docs|T: را ببین",
                Rtl("آدرس https://example.com/docs را ببین"));
        }

        [TestMethod]
        public void Split_WindowsPath_IsOneRun()
        {
            Assert.AreEqual(
                "T:فایل |O:C:\\Temp\\log.txt|T: را",
                Rtl("فایل C:\\Temp\\log.txt را"));
        }

        [TestMethod]
        public void Split_CallWithParentheses_KeepsParenthesesInTheRun()
        {
            Assert.AreEqual("T:تابع |O:GetUser()|T: را صدا بزن", Rtl("تابع GetUser() را صدا بزن"));
        }

        [TestMethod]
        public void Split_GenericTypeArguments_StayInTheRun()
        {
            Assert.AreEqual("T:از |O:List<int>|T: استفاده کن", Rtl("از List<int> استفاده کن"));
        }

        [TestMethod]
        public void Split_TrailingPunctuation_StaysOutsideTheRun()
        {
            Assert.AreEqual("T:از |O:Cache|T:. بعد", Rtl("از Cache. بعد"));
        }

        [TestMethod]
        public void Split_CommaSeparatedEnglishWords_AreOneRun()
        {
            Assert.AreEqual("T:از |O:Cache, Redis|T: استفاده کن", Rtl("از Cache, Redis استفاده کن"));
        }

        [TestMethod]
        public void Split_IdentifierWithUnderscoreAndDot_IsOneRun()
        {
            Assert.AreEqual("T:کلاس |O:My_Class.Method|T: را", Rtl("کلاس My_Class.Method را"));
        }

        [TestMethod]
        public void Split_LeadingDot_IsPartOfTheRun()
        {
            Assert.AreEqual("T:فریمورک |O:.NET|T: را", Rtl("فریمورک .NET را"));
        }

        [TestMethod]
        public void Split_PersianDigits_StayInTextRuns()
        {
            Assert.AreEqual("T:عدد \u06F3\u06F0 است", Rtl("عدد \u06F3\u06F0 است"));
        }

        [TestMethod]
        public void Split_Backticks_ProduceCodeRun()
        {
            Assert.AreEqual("T:مقدار |C:null|T: را", Rtl("مقدار `null` را"));
        }

        [TestMethod]
        public void Split_UnpairedBacktick_IsPlainText()
        {
            Assert.AreEqual("T:سلام ` دنیا", Rtl("سلام ` دنیا"));
        }

        [TestMethod]
        public void Split_LineBreaks_AreReported()
        {
            Assert.AreEqual("T:سلام|NL|O:Hello", Rtl("سلام\nHello"));
        }

        [TestMethod]
        public void Split_EmptyOrNull_ReturnsNoRuns()
        {
            Assert.AreEqual(0, BidiRunSplitter.Split(string.Empty, TextDirection.RightToLeft).Count);
            Assert.AreEqual(0, BidiRunSplitter.Split(null, TextDirection.RightToLeft).Count);
        }

        [TestMethod]
        public void Split_LeftToRightBase_IsolatesPersianFragments()
        {
            string result = Describe(BidiRunSplitter.Split("Use the سلام دنیا value", TextDirection.LeftToRight));
            Assert.AreEqual("T:Use the |O:سلام دنیا|T: value", result);
        }

        [TestMethod]
        public void Split_LeftToRightBase_KeepsZwnjInsideThePersianRun()
        {
            string result = Describe(BidiRunSplitter.Split("word می\u200Cخونه end", TextDirection.LeftToRight));
            Assert.AreEqual("T:word |O:می\u200Cخونه|T: end", result);
        }

        [TestMethod]
        public void Split_ConcatenatingRuns_RestoresTheInput()
        {
            const string input = "این `code` رو از Cache, Redis (طبق RFC 7230)\nخط دوم https://a.b/c";
            var sb = new StringBuilder();
            foreach (BidiRun run in BidiRunSplitter.Split(input, TextDirection.RightToLeft))
            {
                switch (run.Kind)
                {
                    case BidiRunKind.LineBreak:
                        sb.Append('\n');
                        break;
                    case BidiRunKind.Code:
                        sb.Append('`').Append(run.Text).Append('`');
                        break;
                    default:
                        sb.Append(run.Text);
                        break;
                }
            }

            Assert.AreEqual(input, sb.ToString());
        }
    }
}
