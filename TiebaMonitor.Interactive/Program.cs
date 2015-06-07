using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiebaMonitor.Kernel;
using System.Net;

namespace TiebaMonitor.Interactive
{
    class Program
    {
        static int Main(string[] args)
        {
            UI.Init();
            var visitor = new BaiduVisitor();
            visitor.Session.RequestingVerificationCode += session_RequestVerificationCode;
            while (true)
            {
            LOGIN:
                string userName, password;
                userName = UI.Input("用户名");
                password = UI.InputPassword("密码");
                try
                {
                    if (!visitor.Login(userName, password)) continue;
                    UI.Print("已作为{0}登录百度网页端。", visitor.AccountInfo.UserName);
                    while (true)
                    {
                        switch (UI.Input("操作", "E",
                            "L", "重新登录",
                            "E", "退出"))
                        {
                            case "L":
                                goto LOGIN;
                            case "E":
                                return 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UI.PrintError(ex);
                    continue;
                }
            }
            return 0;
        }

        static void session_RequestVerificationCode(object sender, RequestingVerificationCodeEventArgs e)
        {
            using (var client = new WebClient())
            {
                using (var s = client.OpenRead(e.ImageUrl))
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    using (var bmp = new Bitmap(s))
                    {
                        UI.Print(bmp.ASCIIFilter(4));
                    }
                }
            }
            var vc = UI.Input("键入验证码", "取消");
            if (!string.IsNullOrEmpty(vc)) e.VerificationCode = vc;
        }
    }
}
