using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using AnwConnector.Common;
using AnwConnector.Util.httpHelper.CsharpHttpHelper;
using Newtonsoft.Json.Linq;

namespace AnwConnector.Util
{
    public static class AnwHelper
    {
        private static readonly HttpHelper HpHelper = new HttpHelper();
        private static readonly string SecurityCookie;
        private static readonly string SecurityToken;
        private static readonly Regex Rg = new Regex(@"(?is)(\s+)");
        private static readonly string Username = Config.AnwUserName;
        private static readonly string Password = Config.AnwUserPassword;
        static AnwHelper()
        {
            LogHelper.WriteLog("正在登录 ANW系统化");
            //SecurityCookie = "saproute=da4350171461f2ef52e06f38c8f15478; RSESSIONID=97a5a3ab-438d-4dd0-9440-bc6cca865d35; USER_LOCALE=zh_CN; Security-Token=OK5NWB0YBWTF3LAN2SS3VXLPKGLB; JSESSIONID=3E6B967846B159C7A7D78B53D6CB4917";
            //SecurityToken = "OK5NWB0YBWTF3LAN2SS3VXLPKGLB";
            // 第一步
            var parameters = new SortedList<string, string>();
            var url = "https://accounts.sapanywhere.cn/sld/saml2/idp/ssojson#/login";
            //模拟浏览器打开https://accounts.sapanywhere.cn/sld/saml2/idp/ssojson#/login,获取Cookie
            var buffer = new StringBuilder();
            var i = 0;
            foreach (var key in parameters.Keys)
            {
                buffer.AppendFormat(i > 0 ? "&{0}={1}" : "{0}={1}", key,
                   HttpHelper.UrlEncode(parameters[key], Encoding.UTF8));
                i++;
            }
            var item = new HttpItem
            {
                Url = url,
                Method = "get",
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0",
                ContentType = "text/html"
            };
            var result = HpHelper.GetHtml(item);
            var cookie1 = result.Cookie; //获取（my.sapanywhere.cn）cookie
            //第二步
            parameters.Clear();
            parameters.Add("j_username", Username);
            parameters.Add("j_password", Password);
            parameters.Add("captcha", "");
            parameters.Add("locale", "zh_CN");
            parameters.Add("rememberMe", "");
            parameters.Add("RelayState", "https://app1.sapanywhere.cn/sbo/index.html?locale=zh_CN");
            //使用用户明密码，登录AnyWhere
            buffer = new StringBuilder();
            i = 0;
            foreach (var key in parameters.Keys)
            {
                buffer.AppendFormat(i > 0 ? "&{0}={1}" : "{0}={1}", key,
                    HttpHelper.UrlEncode(parameters[key], Encoding.UTF8));
                i++;
            }

            item = new HttpItem
            {
                Url = url,
                Method = "post",
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0",
                Accept = "application/json",
                ContentType = "application/x-www-form-urlencoded",
                Referer = "https://accounts.sapanywhere.cn/sld/saml2/idp/ssojson",
                KeepAlive = true,
                Cookie = cookie1,
                Postdata = buffer.ToString()
            };
            result = HpHelper.GetHtml(item);
            var cookie2 = result.Cookie;

            //第三步
            url = "https://my.sapanywhere.cn/mytenants?locale=zh_CN";
            item = new HttpItem
            {
                Url = url,
                Method = "get",
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0",
                ContentType = "text/html",
                Cookie = cookie1
            };
            cookie1 = cookie1.Substring(0, cookie1.IndexOf("RSESSIONID")) + cookie2; //更新（my.sapanywhere.cn）cookie
            result = HpHelper.GetHtml(item);
            var newcookies = result.Cookie;
            var html = result.Html;
            var samlRequest = "";
            var relayState = "";
            var locale = "";
            var match = Regex.Match(html, "<input type=\"hidden\" name=\"SAMLRequest\" value=\"(?<SAMLRequest>.*?)\" />");
            if (match.Success)
            {
                samlRequest = match.Groups["SAMLRequest"].Value;
            }
            match = Regex.Match(html, "<input type=\"hidden\" name=\"RelayState\" value=\"(?<RelayState>.*?)\" />");
            if (match.Success)
            {
                relayState = match.Groups["RelayState"].Value;
            }
            match = Regex.Match(html, "<input type=\"hidden\" name=\"locale\" value=\"(?<locale>.*?)\" />");
            if (match.Success)
            {
                locale = match.Groups["locale"].Value;
            }

            //第四步
            parameters.Clear();
            parameters.Add("SAMLRequest", samlRequest);
            parameters.Add("RelayState", relayState);
            parameters.Add("locale", locale);
            buffer = new StringBuilder();
            i = 0;
            foreach (var key in parameters.Keys)
            {
                buffer.AppendFormat(i > 0 ? "&{0}={1}" : "{0}={1}", key,
                   HttpHelper.UrlEncode(parameters[key], Encoding.UTF8));
                i++;
            }
            url = "https://accounts.sapanywhere.cn/sld/saml2/idp/ssojson";
            item = new HttpItem
            {
                Url = url,
                Method = "post",
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0",
                Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
                ContentType = "application/x-www-form-urlencoded",
                Referer = "https://my.sapanywhere.cn/mytenants?locale=zh_CN",
                KeepAlive = true,
                Cookie = cookie1,
                Postdata = buffer.ToString()
            };
            result = HpHelper.GetHtml(item);

            html = result.Html;
            var samlResponse = "";
            var sessionIndex = "";
            html = Rg.Replace(html, " ");
            match = Regex.Match(html, "<input type=\"hidden\" name=\"SAMLResponse\" value=\"(?<SAMLResponse>.*?)\" />");
            if (match.Success)
            {
                samlResponse = match.Groups["SAMLResponse"].Value;
            }
            match = Regex.Match(html, "<input type=\"hidden\" name=\"session_index\" value=\"(?<session_index>.*?)\" />");
            if (match.Success)
            {
                sessionIndex = match.Groups["session_index"].Value;
            }
            match = Regex.Match(html, "<input type=\"hidden\" name=\"RelayState\" value=\"(?<RelayState>.*?)\" />");
            if (match.Success)
            {
                relayState = match.Groups["RelayState"].Value;
            }

            //第五步
            parameters.Clear();
            parameters.Add("RelayState", relayState);
            parameters.Add("SAMLResponse", samlResponse);
            parameters.Add("session_index", sessionIndex);
            buffer = new StringBuilder();
            i = 0;
            foreach (var key in parameters.Keys)
            {
                buffer.AppendFormat(i > 0 ? "&{0}={1}" : "{0}={1}", key,
                  HttpHelper.UrlEncode(parameters[key], Encoding.UTF8));
                i++;
            }

            url = "https://my.sapanywhere.cn/sp/saml2/sp/acs?locale=zh_CN";
            item = new HttpItem
            {
                Url = url,
                Method = "POST",
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0",
                Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
                ContentType = "application/x-www-form-urlencoded",
                Referer = "https://accounts.sapanywhere.cn/sld/saml2/idp/ssojson",
                Cookie = newcookies,
                Postdata = buffer.ToString()
            };
            result = HpHelper.GetHtml(item);
            var cookie4 = result.Cookie;
            //第六步
            newcookies = newcookies.Substring(0, newcookies.IndexOf("JSESSIONID")) + cookie4;
            url = result.RedirectUrl;

            item = new HttpItem
            {
                Url = url,
                Method = "get",
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0",
                ContentType = "text/html",
                Referer = "https://accounts.sapanywhere.cn/sld/saml2/idp/ssojson",
                Cookie = newcookies
            };
            result = HpHelper.GetHtml(item);

            url = result.RedirectUrl;
            item = new HttpItem
            {
                Url = url,
                Method = "get",
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0",
                ContentType = "text/html",
                Referer = "https://accounts.sapanywhere.cn/sld/saml2/idp/ssojson"
            };
            result = HpHelper.GetHtml(item);
            html = result.Html;
            var cookie5 = result.Cookie;
            match = Regex.Match(html, "<input type=\"hidden\" name=\"SAMLRequest\" value=\"(?<SAMLRequest>.*?)\" />");
            if (match.Success)
            {
                samlRequest = match.Groups["SAMLRequest"].Value;
            }
            match = Regex.Match(html, "<input type=\"hidden\" name=\"RelayState\" value=\"(?<RelayState>.*?)\" />");
            if (match.Success)
            {
                relayState = match.Groups["RelayState"].Value;
            }
            match = Regex.Match(html, "<input type=\"hidden\" name=\"locale\" value=\"(?<locale>.*?)\" />");
            if (match.Success)
            {
                locale = match.Groups["locale"].Value;
            }
            //第七步
            parameters.Clear();
            parameters.Add("SAMLRequest", samlRequest);
            parameters.Add("RelayState", relayState);
            parameters.Add("locale", locale);
            buffer = new StringBuilder();
            i = 0;
            foreach (var key in parameters.Keys)
            {
                buffer.AppendFormat(i > 0 ? "&{0}={1}" : "{0}={1}", key,
                    HttpHelper.UrlEncode(parameters[key], Encoding.UTF8));
                i++;
            }
            url = "https://accounts.sapanywhere.cn/sld/saml2/idp/ssojson";
            item = new HttpItem
            {
                Url = url,
                Method = "post",
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0",
                Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
                ContentType = "application/x-www-form-urlencoded",
                Referer = result.ResponseUri,
                KeepAlive = true,
                Cookie = cookie1,
                Postdata = buffer.ToString()
            };
            result = HpHelper.GetHtml(item);
            html = result.Html;
            html = Rg.Replace(html, " ");
            match = Regex.Match(html, "<input type=\"hidden\" name=\"SAMLResponse\" value=\"(?<SAMLResponse>.*?)\" />");
            if (match.Success)
            {
                samlResponse = match.Groups["SAMLResponse"].Value;
            }
            match = Regex.Match(html, "<input type=\"hidden\" name=\"session_index\" value=\"(?<session_index>.*?)\" />");
            if (match.Success)
            {
                sessionIndex = match.Groups["session_index"].Value;
            }
            match = Regex.Match(html, "<input type=\"hidden\" name=\"RelayState\" value=\"(?<RelayState>.*?)\" />");
            if (match.Success)
            {
                relayState = match.Groups["RelayState"].Value;
            }

            //第八步
            parameters.Clear();
            parameters.Add("RelayState", relayState);
            parameters.Add("SAMLResponse", samlResponse);
            parameters.Add("session_index", sessionIndex);
            buffer = new StringBuilder();
            i = 0;
            foreach (var key in parameters.Keys)
            {
                buffer.AppendFormat(i > 0 ? "&{0}={1}" : "{0}={1}", key,
                   HttpHelper.UrlEncode(parameters[key], Encoding.UTF8));
                i++;
            }

            url = "https://app1.sapanywhere.cn/sbo/saml2/sp/acs?locale=zh_CN";
            item = new HttpItem
            {
                Url = url,
                Method = "POST",
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0",
                Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
                ContentType = "application/x-www-form-urlencoded",
                Referer = "https://accounts.sapanywhere.cn/sld/saml2/idp/ssojson",
                Cookie = cookie5,
                Postdata = buffer.ToString()
            };
            result = HpHelper.GetHtml(item);

            var cookie6 = result.Cookie;
            cookie6 = cookie5.Substring(0, cookie5.IndexOf("RSESSIONID")) + cookie6;


            ////第九步
            url = "https://app1.sapanywhere.cn/sbo/InitializationService@getInitData";
            item = new HttpItem
            {
                Url = url,
                Method = "get",
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0",
                ContentType = "text/html",
                Referer = "https://accounts.sapanywhere.cn/sld/saml2/idp/ssojson",
                Cookie = cookie6
            };
            result = HpHelper.GetHtml(item);


            //第十步
            url = "https://app1.sapanywhere.cn/sbo/InitializationService@getEndPoints";
            item = new HttpItem
            {
                Url = url,
                Method = "get",
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0",
                ContentType = "text/html",
                Referer = "https://accounts.sapanywhere.cn/sld/saml2/idp/ssojson",
                Cookie = cookie6
            };
            result = HpHelper.GetHtml(item);
            var cookie7 = result.Cookie;
            cookie6 = cookie6.Replace(" Path=/; Secure; HttpOnly", "").Replace(",", "");
            cookie7 = cookie7.Replace(" Path=/; Secure", "").Replace(",", "");
            var securityCookie = cookie6 + cookie7;
            SecurityCookie = securityCookie.Substring(0, securityCookie.Length - 1) + ";USER_LOCALE=zh_CN";
            var cookies = SecurityCookie.Split(';');
            foreach (var co in cookies)
            {
                var info = co.Split('=');
                if (info[0].Trim() == "Security-Token")
                {
                    SecurityToken = info[1];
                    break;
                }
            }
            LogHelper.WriteLog("ANW 登录完毕！");


        }

        public static HttpResult GetHttpResult(string url, IDictionary<string, string> parameters)
        {
            var buffer = new StringBuilder();
            if (parameters != null)
            {
                var i = 0;
                foreach (var key in parameters.Keys)
                {
                    buffer.AppendFormat(i > 0 ? "&{0}={1}" : "{0}={1}", key, parameters[key]);
                    i++;
                }
            }
            url = url + (buffer.Length > 0 ? "?" + buffer.ToString() : "");
            var item = new HttpItem
            {
                Url = url,
                Method = "get",
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0",
                ContentType = "text/html",
                Accept = "application/json",
                Referer = "https://app1.sapanywhere.cn/sbo/index.html",
                Cookie = SecurityCookie
            };
            item.Header.Add("SecurityToken", SecurityToken);
            var result = HpHelper.GetHtml(item);
            result.Html = Rg.Replace(result.Html, " ");
            return result;
        }

        public static HttpResult UpdateObjectHttpResult(string url, JObject jObject)
        {
            var putData = jObject.ToString();

            var item = new HttpItem
            {
                Host = "app1.sapanywhere.cn",
                Url = url,
                Method = "PUT",
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0",
                Accept = "application/json, text/javascript, */*; q=0.01",
                ContentType = "application/json",
                Referer = "https://app1.sapanywhere.cn/sbo/index.html",
                KeepAlive = true,
                Cookie = SecurityCookie,
                Postdata = putData,
                Encoding = Encoding.UTF8,
                PostEncoding = Encoding.UTF8
            };
            item.Header.Add("Security-Token", SecurityToken);
            item.Header.Add("X-Requested-With", "XMLHttpRequest");
            return HpHelper.GetHtml(item);

        }
        public static HttpResult AddObjectHttpResult(string url, JObject jObject, string xBusinessToken)
        {
            var putData = jObject.ToString();
            var item = new HttpItem
            {
                Host = "app1.sapanywhere.cn",
                Url = url,
                Method = "POST",
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0",
                Accept = "application/json, text/javascript, */*; q=0.01",
                ContentType = "application/json",
                Referer = "https://app1.sapanywhere.cn/sbo/index.html",
                KeepAlive = true,
                Cookie = SecurityCookie,
                Postdata = putData,
                Encoding = Encoding.UTF8,
                PostEncoding = Encoding.UTF8
            };
            item.Header.Add("Security-Token", SecurityToken);
            item.Header.Add("X-Business-Token", xBusinessToken);
            item.Header.Add("X-Requested-With", "XMLHttpRequest");
            return HpHelper.GetHtml(item);
        }
        public static HttpResult PostHttpResult(string url, string putData)
        {
            var item = new HttpItem
            {
                Host = "app1.sapanywhere.cn",
                Url = url,
                Method = "post",
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0",
                Accept = "application/json, text/javascript, */*; q=0.01",
                ContentType = "application/json",
                Referer = "https://app1.sapanywhere.cn/sbo/index.html",
                KeepAlive = true,
                Cookie = SecurityCookie,
                Postdata = putData,
                Encoding = Encoding.UTF8,
                PostEncoding = Encoding.UTF8
            };
            item.Header.Add("Security-Token", SecurityToken);
            item.Header.Add("X-Requested-With", "XMLHttpRequest");
            var result = HpHelper.GetHtml(item);
            return result;
        }
        public static HttpResult PostHttpResult(string url, SortedList<string, string> parameters, string referer)
        {
            var buffer = new StringBuilder();
            if (parameters != null)
            {
                var i = 0;
                foreach (var key in parameters.Keys)
                {
                    buffer.AppendFormat(i > 0 ? "&{0}={1}" : "{0}={1}", key,
                        HttpHelper.UrlEncode(parameters[key], Encoding.UTF8));
                    i++;
                }
            }
            var item = new HttpItem
            {
                Url = url,
                Method = "post",
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0",
                Accept = "application/json",
                ContentType = "application/x-www-form-urlencoded",
                Referer = referer,
                KeepAlive = true,
                Cookie = SecurityCookie,
                Postdata = buffer.ToString()
            };
            item.Header.Add("SecurityToken", SecurityToken);
            return HpHelper.GetHtml(item);
        }
    }
}