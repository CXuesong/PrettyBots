using System;
using System.Threading;
using PrettyBots.Visitors;

namespace UnitTestProject1
{
    class InteractiveVCodeRecognizer : VerificationCodeRecognizer
    {
        protected override string RecognizeFromUrl(string imageUrl, WebSession session)
        {
            //Func<string> proc = () =>
            //{
            //    using (var ib = new VerificationCodeInputBox())
            //    {
            //        return ib.ShowDialog(imageUrl);
            //    }
            //};
            //if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            //    return proc();
            ////var result = new {value = (string)null};
            //string result = null;
            //var t = new Thread(() => { result = proc(); });
            //t.SetApartmentState(ApartmentState.STA);
            //t.Start();
            //t.Join();
            //return result;
            using (var ib = new VerificationCodeInputBox())
            {
                return ib.ShowDialog(imageUrl);
            }
        }
    }
}
