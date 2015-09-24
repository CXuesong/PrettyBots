using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PrettyBots.Visitors.WeatherService
{
    public class WeatherInfo
    {
        public string CityName { get; set; }

        public string Weather { get; set; }

        public string TemperatureDayTime { get; set; }

        public string TemperatureNightTime { get; set; }

        public string WindScale { get; set; }

        public string WindDirection { get; set; }

        public string[] Alarms { get; set; }

        public override string ToString()
        {
            var s = string.Format("{0}：{1}。白天：{2}，夜晚：{3}。",
                CityName, Weather, TemperatureDayTime, TemperatureNightTime);
            var wind = WindScale ?? "";
            if (!string.IsNullOrEmpty(WindDirection)) wind += (string.IsNullOrEmpty(wind) ? null : "，") + WindDirection;
            if (!string.IsNullOrEmpty(wind)) s += wind + "。";
            if (Alarms.Length > 0) s += string.Join(null, Alarms);
            return s;
        }

        internal WeatherInfo(string cityName, string weather, string temperatureDayTime, string temperatureNightTime,
            string windScale, string windDirection, string[] alarms)
        {
            CityName = cityName;
            Weather = weather;
            TemperatureDayTime = temperatureDayTime;
            TemperatureNightTime = temperatureNightTime;
            WindScale = windScale;
            WindDirection = windDirection;
            Alarms = alarms;
        }
    }

    public class WeatherReportVisitor : Visitor
    {
        /*
         varcityDZ101110901={
            "weatherinfo": {
                "city": "101110901",
                "cityname": "宝鸡",
                "temp": "36℃",
                "tempn": "23℃",
                "weather": "晴",
                "wd": "无持续风向",
                "ws": "微风",
                "weathercode": "d0",
                "weathercoden": "n0"
            }
        };varalarmDZ101110901={
            "w": [
                {
                    "w1": "陕西省",
                    "w2": "宝鸡市",
                    "w3": "渭滨区",
                    "w4": "07",
                    "w5": "高温",
                    "w6": "03",
                    "w7": "橙色",
                    "w8": "2015-07-26 14:08",
                    "w9": "渭滨区气象台2015年7月26日14时08分继续发布高温橙色预警信号：预计未来24小时内我区最高气温将达37℃以上，请注意防范。",
                    "w10": "201507261416999993高温橙色",
                    "w11": "101110901-20150726140800-0703.html"
                },
                {
                    "w1": "陕西省",
                    "w2": "宝鸡市",
                    "w3": "",
                    "w4": "07",
                    "w5": "高温",
                    "w6": "03",
                    "w7": "橙色",
                    "w8": "2015-07-26 14:05",
                    "w9": "宝鸡市气象台2015年7月26日14时05分继续发布高温橙色预警信号：预计未来24小时内我市陈仓、眉县、市区最高气温将达37℃以上，请注意防范。",
                    "w10": "201507261416570160高温橙色",
                    "w11": "1011109-20150726140500-0703.html"
                }
            ]
        }
         */

        // 0 : cityId
        // 1 : Unix Time
        public const string WeatherReportUrl = "http://d1.weather.com.cn/dingzhi/{0}.html?_={1}";
        // 0 : City Id
        public const string WeatherReportRefererUrl = "http://www.weather.com.cn/weather1d/{0}.shtml";
        // 0 : cityName
        // 1 : Unix Time
        public const string CityIdQueryUrl =
            "http://toy1.weather.com.cn/search?cityname={0}&callback=success_jsonpCallback&_={1}";

        public const string CityIdQueryRefererUrl = "http://www.weather.com.cn/forecast/index.shtml";

        public async Task<WeatherInfo> GetWeatherAsync(int cityId)
        {
            Logging.Enter(this, cityId);
            using (var client = Session.CreateWebClient())
            {
                client.Headers[HttpRequestHeader.Referer] = string.Format(WeatherReportRefererUrl, cityId);
                var resultStr = await client.DownloadStringTaskAsync(string.Format(WeatherReportUrl, cityId, Utility.UnixNow()));
                var redir = Utility.GetRedirectionUrl(resultStr);
                if (!string.IsNullOrEmpty(redir))
                {
                    Logging.Trace(this, "Redir -> {0}", redir);
                    // 下载一次后， Header 会被重置。
                    client.Headers[HttpRequestHeader.Referer] = string.Format(WeatherReportRefererUrl, cityId);
                    resultStr = await client.DownloadStringTaskAsync(redir);
                }
                resultStr += ";";
                var city = Utility.FindJsonAssignment(resultStr, "city.*?", false, true);
                var w = city["weatherinfo"];
                var a = Utility.FindJsonAssignment(resultStr, "alarm.*?", true, true);
                var alarms = a == null ? new string[0] : a["w"].Select(t => (string) t["w9"]).ToArray();
                return Logging.Exit(this,
                    new WeatherInfo((string) w["cityname"], (string) w["weather"],
                        (string) w["temp"], (string) w["tempn"],
                        (string) w["ws"], (string) w["wd"], alarms));
            }
        }

        private static readonly XDocument cityInfo = XDocument.Parse(Prompts.WeatherReportCityInfo);

        private static readonly Regex cityIdMatcher = new Regex(@"\d+");

        public async Task<int> QueryCityId(string cityName)
        {
            //success_jsonpCallback([{"ref":"404040100~Toronto~多伦多~Toronto~多伦多~Canada~~~dld~加拿大"},{"ref":"404167100~Torontocity Centre~多伦多城市中心~Torontocity Centre~多伦多城市中心~Canada~~~dldcszx~加拿大"}])
            Logging.Enter(this, cityName);
            if (string.IsNullOrWhiteSpace(cityName)) return Logging.Exit(this, 0);
            using (var client = Session.CreateWebClient())
            {
                client.Headers[HttpRequestHeader.Referer] = CityIdQueryRefererUrl;
                var resultStr = await client.DownloadStringTaskAsync(string.Format(CityIdQueryUrl,
                    cityName.ToLowerInvariant(), Utility.UnixNow()));
                var match = cityIdMatcher.Match(resultStr);
                return Logging.Exit(this, match.Success ? Convert.ToInt32(match.Groups[0].Value) : 0);
            }
        }

        public async Task<WeatherInfo> GetWeatherAsync(string cityName)
        {
            var xCity = cityInfo.Root.Descendants("city").FirstOrDefault(e => string.Compare((string) e, cityName, true) == 0);
            //如果本地ID库中不存在，则尝试就地查询城市。
            var id = xCity != null ? (int) xCity.Attribute("id") : await QueryCityId(cityName);
            return id > 0 ? await GetWeatherAsync(id) : null;
        }

        public WeatherInfo GetWeather(int cityId)
        {
            return Utility.WaitForResult(GetWeatherAsync(cityId));
        }

        public WeatherInfo GetWeather(string cityName)
        {
            return Utility.WaitForResult(GetWeatherAsync(cityName));
        }

        public WeatherReportVisitor(WebSession session) 
            : base(session)
        {
        }

        public WeatherReportVisitor()
            : base(null)
        {
        }
    }
}
