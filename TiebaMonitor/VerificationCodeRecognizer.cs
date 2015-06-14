using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyBots.Visitors
{
    /// <summary>
    /// 封装了用于验证码识别的基本操作。
    /// </summary>
    public abstract class VerificationCodeRecognizer
    {
        /// <summary>
        /// 从指定的 URL 下载图像，并识别验证码。
        /// </summary>
        /// <returns>识别的验证码，或<c>null</c>表示取消。</returns>
        public string Recognize(string imageUrl, WebSession session)
        {
            return RecognizeFromUrl(imageUrl, session);
        }

        /// <summary>
        /// 从指定的 URL 下载图像，并识别验证码。
        /// </summary>
        /// <returns>识别的验证码，或<c>null</c>表示取消。</returns>
        public string Recognize(string imageUrl)
        {
            return RecognizeFromUrl(imageUrl, null);
        }

        protected abstract string RecognizeFromUrl(string imageUrl, WebSession session);
    }
}
