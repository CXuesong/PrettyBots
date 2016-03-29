# PrettyBots

A bot.

去年的作品。虽然还没有完成，但目前已经为百度贴吧的浏览、签到、发帖、通知检索和帖子搜索提供了统一的接口。在此基础上，提供了一些简单的任务，例如自动回复、天气查询、简单的纯数字倒数接龙、贴吧迎新和嘲讽来自百度知道的重定向问题等。

例如，在百度允许的情况下，你可以使用如下的语法来完成签到：

```c#
[TestMethod]
public void SignInTest()
{
  var visitor = Utility.CreateBaiduVisitor();
  Utility.LoginVisitor(visitor);
  //由于每天每贴吧只能签到一次，因此需要指定一个贴吧列表。
  var destList = new[] {"化学", "化学2", "物理", "生物", "汉服"};
  //抽取第一个没有签到的贴吧。
  var f = destList.Select(fn => visitor.Tieba.Forum(fn))
    .FirstOrDefault(f1 => !f1.HasSignedIn);
  if (f == null)
    Assert.Inconclusive();
  else
  {
    //进行签到。
    Trace.WriteLine("Sign in : " + f.Name);
    f.SignIn();
    Assert.IsTrue(f.HasSignedIn);
    Trace.WriteLine("Rank : " + f.SignInRank);
  }
  visitor.AccountInfo.Logout();
}
```

在登录后，可以使用如下的方法来检索来自贴吧的通知消息

```c#
[TestMethod]
public void MessageNotifierTest()
{
  var visitor = Utility.CreateBaiduVisitor();
  Utility.LoginVisitor(visitor);
  visitor.Messages.Update();
  Trace.WriteLine(visitor.Messages.Counter);
  visitor.Tieba.Messages.Update();
  Trace.WriteLine(visitor.Tieba.Messages.Counters);
  visitor.AccountInfo.Logout();
}
```

关于其它应用方法，请参阅测试项目。

## 已知限制

- 目前每个帖子只能看前十层的楼中楼。
  - 只是因为我犯懒了……
- 发帖时很容易被百度要求输入验证码。

## 测试项目

测试项目为 UnitTestProject1。在使用前，你需要在项目中添加一个源文件，用以实现`Utility.LoginBaidu`函数。例如：

```c#
using PrettyBots.Visitors;

namespace UnitTestProject1
{
    partial class Utility
    {
        static partial void LoginBaidu(IAccountInfo account)
        {
            account.Login("username", "password");
        }

        static partial void LoginNetEase(IAccountInfo account)
        {
            account.Login("username", "password");
        }
    }
}

```
