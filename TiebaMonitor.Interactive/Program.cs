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
        static void Main(string[] args)
        {
            UI.Init();
            using (var session = new MonitorSession())
            {
                session.RequestVerificationCode += session_RequestVerificationCode;
                while (true)
                {
                    string userName, password;
                    userName = UI.Input("用户名");
                    password = UI.InputPassword("密码");
                    try
                    {
                        if (!session.Login(userName, password)) continue;
                        UI.Print("登录成功。");
                    }
                    catch (Exception ex)
                    {
                        UI.PrintError(ex);
                        continue;
                    }
                }
            }
        }

        static void session_RequestVerificationCode(object sender, RequestVerificationCodeEventArgs e)
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
