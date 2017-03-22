using System.Drawing;
using AnwConnector.Util.httpHelper.CsharpHttpHelper.Base;
using AnwConnector.Util.httpHelper.CsharpHttpHelper.Enum;
using AnwConnector.Util.httpHelper.CsharpHttpHelper.Helper;

namespace AnwConnector.Util.httpHelper.CsharpHttpHelper.BaseBll
{
	internal class HttpHelperBll
	{
		private HttphelperBase _httpbase = new HttphelperBase();

		internal HttpResult GetHtml(HttpItem item)
		{
			HttpResult result;
			if (item.Allowautoredirect && item.AutoRedirectCookie)
			{
				HttpResult httpResult = null;
				for (int i = 0; i < 100; i++)
				{
					item.Allowautoredirect = false;
					httpResult = this._httpbase.GetHtml(item);
					if (string.IsNullOrWhiteSpace(httpResult.RedirectUrl))
					{
						break;
					}
					item.Url = httpResult.RedirectUrl;
					item.Method = "GET";
					if (item.ResultCookieType == ResultCookieType.String)
					{
						item.Cookie += httpResult.Cookie;
					}
					else
					{
						item.CookieCollection.Add(httpResult.CookieCollection);
					}
				}
				result = httpResult;
			}
			else
			{
				result = this._httpbase.GetHtml(item);
			}
			return result;
		}

		internal Image GetImage(HttpItem item)
		{
			item.ResultType = ResultType.Byte;
			return ImageHelper.ByteToImage(this.GetHtml(item).ResultByte);
		}

		internal HttpResult FastRequest(HttpItem item)
		{
			return this._httpbase.FastRequest(item);
		}
	}
}
