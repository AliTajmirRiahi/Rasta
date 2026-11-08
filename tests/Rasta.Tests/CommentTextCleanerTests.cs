using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasta.Text;

namespace Rasta.Tests
{
    [TestClass]
    public class CommentTextCleanerTests
    {
        [TestMethod]
        public void Clean_DoubleSlash_StripsMarker()
        {
            Assert.AreEqual("این یک تست است", CommentTextCleaner.Clean("// این یک تست است"));
        }

        [TestMethod]
        public void Clean_TripleSlash_StripsMarker()
        {
            Assert.AreEqual("این یک تست است", CommentTextCleaner.Clean("/// این یک تست است"));
        }

        [TestMethod]
        public void Clean_SingleLineBlock_StripsBothMarkers()
        {
            Assert.AreEqual("سلام دنیا", CommentTextCleaner.Clean("/* سلام دنیا */"));
        }

        [TestMethod]
        public void Clean_DocBlockStart_StripsMarkers()
        {
            Assert.AreEqual("doc", CommentTextCleaner.Clean("/** doc */"));
        }

        [TestMethod]
        public void Clean_MultiLineBlock_StripsStarsAndBlankEdges()
        {
            Assert.AreEqual("خط اول\nخط دوم", CommentTextCleaner.Clean("/*\n * خط اول\n * خط دوم\n */"));
        }

        [TestMethod]
        public void Clean_ConsecutiveLineComments_KeepLineBreaks()
        {
            Assert.AreEqual("خط اول\nخط دوم", CommentTextCleaner.Clean("// خط اول\n    // خط دوم"));
        }

        [TestMethod]
        public void Clean_WindowsLineEndings_AreNormalised()
        {
            Assert.AreEqual("خط اول\nخط دوم", CommentTextCleaner.Clean("// خط اول\r\n// خط دوم"));
        }

        [TestMethod]
        public void Clean_Hash_StripsMarker()
        {
            Assert.AreEqual("سلام", CommentTextCleaner.Clean("# سلام"));
        }

        [TestMethod]
        public void Clean_SqlDashes_StripsMarker()
        {
            Assert.AreEqual("سلام", CommentTextCleaner.Clean("-- سلام"));
        }

        [TestMethod]
        public void Clean_XmlComment_StripsBothMarkers()
        {
            Assert.AreEqual("سلام", CommentTextCleaner.Clean("<!-- سلام -->"));
        }

        [TestMethod]
        public void Clean_VisualBasicApostrophes_StripsMarker()
        {
            Assert.AreEqual("سلام", CommentTextCleaner.Clean("' سلام"));
            Assert.AreEqual("سلام", CommentTextCleaner.Clean("''' سلام"));
        }

        [TestMethod]
        public void Clean_RazorComment_StripsBothMarkers()
        {
            Assert.AreEqual("سلام", CommentTextCleaner.Clean("@* سلام *@"));
        }

        [TestMethod]
        public void Clean_SummaryTag_IsStrippedAndZwnjKept()
        {
            string result = CommentTextCleaner.Clean("/// <summary>محاسبه‌ی مالیات برای Invoice</summary>");
            Assert.AreEqual("محاسبه‌ی مالیات برای Invoice", result);
        }

        [TestMethod]
        public void Clean_ParamTag_BecomesLabelledLine()
        {
            Assert.AreEqual("x — مقدار ورودی", CommentTextCleaner.Clean("/// <param name=\"x\">مقدار ورودی</param>"));
        }

        [TestMethod]
        public void Clean_FullXmlDoc_SeparatesSections()
        {
            string raw = "/// <summary>\n/// توضیح\n/// </summary>\n/// <param name=\"id\">شناسه</param>\n/// <returns>نتیجه</returns>";
            Assert.AreEqual("توضیح\n\nid — شناسه\n\nنتیجه", CommentTextCleaner.Clean(raw));
        }

        [TestMethod]
        public void Clean_ExceptionTag_UsesCrefAsLabel()
        {
            string raw = "/// <exception cref=\"ArgumentNullException\">اگر null باشد</exception>";
            Assert.AreEqual("ArgumentNullException — اگر null باشد", CommentTextCleaner.Clean(raw));
        }

        [TestMethod]
        public void Clean_SeeCref_BecomesTypeName()
        {
            Assert.AreEqual("از UserService استفاده کن", CommentTextCleaner.Clean("/// از <see cref=\"UserService\"/> استفاده کن"));
        }

        [TestMethod]
        public void Clean_SeeCrefWithPrefix_DropsPrefix()
        {
            Assert.AreEqual("Ns.Foo سلام", CommentTextCleaner.Clean("/// <see cref=\"T:Ns.Foo\"/> سلام"));
        }

        [TestMethod]
        public void Clean_SeeLangword_BecomesKeyword()
        {
            Assert.AreEqual("null بود", CommentTextCleaner.Clean("/// <see langword=\"null\"/> بود"));
        }

        [TestMethod]
        public void Clean_InlineCodeTag_BecomesBackticks()
        {
            Assert.AreEqual("مقدار `null` را برگردان", CommentTextCleaner.Clean("/// مقدار <c>null</c> را برگردان"));
        }

        [TestMethod]
        public void Clean_ParamRef_BecomesName()
        {
            Assert.AreEqual("مقدار x بزرگ است", CommentTextCleaner.Clean("/// مقدار <paramref name=\"x\"/> بزرگ است"));
        }

        [TestMethod]
        public void Clean_GenericTypeArguments_AreNotTreatedAsTags()
        {
            Assert.AreEqual("از List<int> استفاده کن", CommentTextCleaner.Clean("// از List<int> استفاده کن"));
        }

        [TestMethod]
        public void Clean_Entities_AreDecodedOnlyInsideXmlDoc()
        {
            Assert.AreEqual("a < b", CommentTextCleaner.Clean("/// <summary>a &lt; b</summary>"));
            Assert.AreEqual("a &lt; b", CommentTextCleaner.Clean("// a &lt; b"));
        }

        [TestMethod]
        public void Clean_RedundantWhitespace_IsCollapsed()
        {
            Assert.AreEqual("سلام دنیا", CommentTextCleaner.Clean("//   سلام     دنیا   "));
        }

        [TestMethod]
        public void Clean_Zwnj_IsPreserved()
        {
            string result = CommentTextCleaner.Clean("// می‌خونه");
            Assert.AreEqual("می‌خونه", result);
        }

        [TestMethod]
        public void Clean_BlankLineBetweenParagraphs_IsKeptOnce()
        {
            Assert.AreEqual("الف\n\nب", CommentTextCleaner.Clean("// الف\n//\n//\n// ب"));
        }

        [TestMethod]
        public void Clean_EmptyComments_ReturnEmptyString()
        {
            Assert.AreEqual(string.Empty, CommentTextCleaner.Clean("//"));
            Assert.AreEqual(string.Empty, CommentTextCleaner.Clean("/* */"));
            Assert.AreEqual(string.Empty, CommentTextCleaner.Clean("   "));
            Assert.AreEqual(string.Empty, CommentTextCleaner.Clean(string.Empty));
            Assert.AreEqual(string.Empty, CommentTextCleaner.Clean(null));
        }

        [TestMethod]
        public void Clean_MixedExampleFromProblemStatement_KeepsEveryWord()
        {
            const string raw = "// مقدار Timeout باید بیشتر از 30 ثانیه باشه (طبق RFC 7230)";
            Assert.AreEqual("مقدار Timeout باید بیشتر از 30 ثانیه باشه (طبق RFC 7230)", CommentTextCleaner.Clean(raw));
        }
    }
}
