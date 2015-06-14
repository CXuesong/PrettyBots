using PrettyBots.Visitors;

namespace UnitTestProject1
{
    class InteractiveVCodeRecognizer : VerificationCodeRecognizer
    {
        protected override string RecognizeFromUrl(string imageUrl, WebSession session)
        {
            using (var ib = new VerificationCodeInputBox())
            {
                return ib.ShowDialog(imageUrl);
            }
        }
    }
}
