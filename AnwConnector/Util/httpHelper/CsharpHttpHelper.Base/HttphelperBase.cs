using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using AnwConnector.Util.httpHelper.CsharpHttpHelper.Enum;
using AnwConnector.Util.httpHelper.CsharpHttpHelper.Static;

namespace AnwConnector.Util.httpHelper.CsharpHttpHelper.Base
{
	internal class HttphelperBase
	{
		private Encoding _encoding = Encoding.Default;

		private Encoding _postencoding = Encoding.Default;

		private HttpWebRequest _request = null;

		private HttpWebResponse _response = null;

		private IPEndPoint _ipEndPoint = null;

		internal HttpResult GetHtml(HttpItem item)
		{
			HttpResult httpResult = new HttpResult();
			HttpResult result;
			try
			{
				this.SetRequest(item);
			}
			catch (Exception ex)
			{
				result = new HttpResult
				{
					Cookie = string.Empty,
					Header = null,
					Html = ex.Message,
					StatusDescription = "配置参数时出错：" + ex.Message
				};
				return result;
			}
			try
			{
				using (this._response = (HttpWebResponse)this._request.GetResponse())
				{
					this.GetData(item, httpResult);
				}
			}
			catch (WebException ex2)
			{
				if (ex2.Response != null)
				{
					using (this._response = (HttpWebResponse)ex2.Response)
					{
						this.GetData(item, httpResult);
					}
				}
				else
				{
					httpResult.Html = ex2.Message;
				}
			}
			catch (Exception ex)
			{
				httpResult.Html = ex.Message;
			}
			if (item.IsToLower)
			{
				httpResult.Html = httpResult.Html.ToLower();
			}
			result = httpResult;
			return result;
		}

		internal HttpResult FastRequest(HttpItem item)
		{
			HttpResult httpResult = new HttpResult();
			HttpResult result;
			try
			{
				this.SetRequest(item);
			}
			catch (Exception ex)
			{
				result = new HttpResult
				{
					Cookie = (this._response.Headers["set-cookie"] != null) ? this._response.Headers["set-cookie"] : string.Empty,
					Header = null,
					Html = ex.Message,
					StatusDescription = "配置参数时出错：" + ex.Message
				};
				return result;
			}
			try
			{
				using (this._response = (HttpWebResponse)this._request.GetResponse())
				{
					result = new HttpResult
					{
						Cookie = (this._response.Headers["set-cookie"] != null) ? this._response.Headers["set-cookie"] : string.Empty,
						Header = this._response.Headers,
						StatusCode = this._response.StatusCode,
						StatusDescription = this._response.StatusDescription
					};
					return result;
				}
			}
			catch (WebException ex2)
			{
				using (this._response = (HttpWebResponse)ex2.Response)
				{
					result = new HttpResult
					{
						Cookie = (this._response.Headers["set-cookie"] != null) ? this._response.Headers["set-cookie"] : string.Empty,
						Header = this._response.Headers,
						StatusCode = this._response.StatusCode,
						StatusDescription = this._response.StatusDescription
					};
					return result;
				}
			}
			catch (Exception ex)
			{
				httpResult.Html = ex.Message;
			}
			if (item.IsToLower)
			{
				httpResult.Html = httpResult.Html.ToLower();
			}
			result = httpResult;
			return result;
		}

		private void GetData(HttpItem item, HttpResult result)
		{
			if (this._response != null)
			{
				result.StatusCode = this._response.StatusCode;
				result.ResponseUri = this._response.ResponseUri.ToString();
				result.StatusDescription = this._response.StatusDescription;
				result.Header = this._response.Headers;
				if (this._response.Cookies != null)
				{
					result.CookieCollection = this._response.Cookies;
				}
				if (this._response.Headers["set-cookie"] != null)
				{
					result.Cookie = this._response.Headers["set-cookie"];
				}
				byte[] @byte = this.GetByte();
				if (@byte != null && @byte.Length > 0)
				{
					this.SetEncoding(item, result, @byte);
					result.Html = this._encoding.GetString(@byte);
				}
				else
				{
					result.Html = string.Empty;
				}
			}
		}

		private void SetEncoding(HttpItem item, HttpResult result, byte[] responseByte)
		{
			if (item.ResultType == ResultType.Byte)
			{
				result.ResultByte = responseByte;
			}
			if (this._encoding == null)
			{
				Match match = Regex.Match(Encoding.Default.GetString(responseByte), RegexString.Enconding, RegexOptions.IgnoreCase);
				string text = string.Empty;
				if (match != null && match.Groups.Count > 0)
				{
					text = match.Groups[1].Value.ToLower().Trim();
				}
				string text2 = string.Empty;
				if (!string.IsNullOrWhiteSpace(this._response.CharacterSet))
				{
					text2 = this._response.CharacterSet.Trim().Replace("\"", "").Replace("'", "");
				}
				if (text.Length > 2)
				{
					try
					{
						this._encoding = Encoding.GetEncoding(text.Replace("\"", string.Empty).Replace("'", "").Replace(";", "").Replace("iso-8859-1", "gbk").Trim());
					}
					catch
					{
						if (string.IsNullOrEmpty(text2))
						{
							this._encoding = Encoding.UTF8;
						}
						else
						{
							this._encoding = Encoding.GetEncoding(text2);
						}
					}
				}
				else if (string.IsNullOrEmpty(text2))
				{
					this._encoding = Encoding.UTF8;
				}
				else
				{
					this._encoding = Encoding.GetEncoding(text2);
				}
			}
		}

		private byte[] GetByte()
		{
			byte[] result = null;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				if (this._response.ContentEncoding != null && this._response.ContentEncoding.Equals("gzip", StringComparison.InvariantCultureIgnoreCase))
				{
					new GZipStream(this._response.GetResponseStream(), CompressionMode.Decompress).CopyTo(memoryStream, 10240);
				}
				else
				{
					this._response.GetResponseStream().CopyTo(memoryStream, 10240);
				}
				result = memoryStream.ToArray();
			}
			return result;
		}

		private void SetRequest(HttpItem item)
		{
			ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(this.CheckValidationResult);
			this._request = (HttpWebRequest)WebRequest.Create(item.Url);
			if (item.IpEndPoint != null)
			{
				this._ipEndPoint = item.IpEndPoint;
				this._request.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint(this.BindIpEndPointCallback);
			}
			this._request.AutomaticDecompression = item.AutomaticDecompression;
			this.SetCer(item);
			this.SetCerList(item);
			if (item.Header != null && item.Header.Count > 0)
			{
				string[] allKeys = item.Header.AllKeys;
				for (int i = 0; i < allKeys.Length; i++)
				{
					string name = allKeys[i];
					this._request.Headers.Add(name, item.Header[name]);
				}
			}
			this.SetProxy(item);
			if (item.ProtocolVersion != null)
			{
				this._request.ProtocolVersion = item.ProtocolVersion;
			}
			this._request.ServicePoint.Expect100Continue = item.Expect100Continue;
			this._request.Method = item.Method;
			this._request.Timeout = item.Timeout;
			this._request.KeepAlive = item.KeepAlive;
			this._request.ReadWriteTimeout = item.ReadWriteTimeout;
			if (!string.IsNullOrWhiteSpace(item.Host))
			{
				this._request.Host = item.Host;
			}
			if (item.IfModifiedSince.HasValue)
			{
				this._request.IfModifiedSince = Convert.ToDateTime(item.IfModifiedSince);
			}
			this._request.Accept = item.Accept;
			this._request.ContentType = item.ContentType;
			this._request.UserAgent = item.UserAgent;
			this._encoding = item.Encoding;
			this._request.Credentials = item.Credentials;
			this.SetCookie(item);
			this._request.Referer = item.Referer;
			this._request.AllowAutoRedirect = item.Allowautoredirect;
			if (item.MaximumAutomaticRedirections > 0)
			{
				this._request.MaximumAutomaticRedirections = item.MaximumAutomaticRedirections;
			}
			this.SetPostData(item);
			if (item.Connectionlimit > 0)
			{
				this._request.ServicePoint.ConnectionLimit = item.Connectionlimit;
			}
		}

		private void SetCer(HttpItem item)
		{
			if (!string.IsNullOrWhiteSpace(item.CerPath))
			{
				if (!string.IsNullOrWhiteSpace(item.CerPwd))
				{
					this._request.ClientCertificates.Add(new X509Certificate(item.CerPath, item.CerPwd));
				}
				else
				{
					this._request.ClientCertificates.Add(new X509Certificate(item.CerPath));
				}
			}
		}

		private void SetCerList(HttpItem item)
		{
			if (item.ClentCertificates != null && item.ClentCertificates.Count > 0)
			{
				foreach (X509Certificate current in item.ClentCertificates)
				{
					this._request.ClientCertificates.Add(current);
				}
			}
		}

		private void SetCookie(HttpItem item)
		{
			if (!string.IsNullOrEmpty(item.Cookie))
			{
				this._request.Headers[HttpRequestHeader.Cookie] = item.Cookie;
			}
			if (item.ResultCookieType == ResultCookieType.CookieCollection)
			{
				this._request.CookieContainer = new CookieContainer();
				if (item.CookieCollection != null && item.CookieCollection.Count > 0)
				{
					this._request.CookieContainer.Add(item.CookieCollection);
				}
			}
		}

		private void SetPostData(HttpItem item)
		{
			if (!this._request.Method.Trim().ToLower().Contains("get"))
			{
				if (item.PostEncoding != null)
				{
					this._postencoding = item.PostEncoding;
				}
				byte[] array = null;
				if (item.PostDataType == PostDataType.Byte && item.PostdataByte != null && item.PostdataByte.Length > 0)
				{
					array = item.PostdataByte;
				}
				else if (item.PostDataType == PostDataType.FilePath && !string.IsNullOrWhiteSpace(item.Postdata))
				{
					StreamReader streamReader = new StreamReader(item.Postdata, this._postencoding);
					array = this._postencoding.GetBytes(streamReader.ReadToEnd());
					streamReader.Close();
				}
				else if (!string.IsNullOrWhiteSpace(item.Postdata))
				{
					array = this._postencoding.GetBytes(item.Postdata);
				}
				if (array != null)
				{
					this._request.ContentLength = (long)array.Length;
					this._request.GetRequestStream().Write(array, 0, array.Length);
				}
			}
		}

		private void SetProxy(HttpItem item)
		{
			bool flag = false;
			if (!string.IsNullOrWhiteSpace(item.ProxyIp))
			{
				flag = item.ProxyIp.ToLower().Contains("ieproxy");
			}
			if (!string.IsNullOrWhiteSpace(item.ProxyIp) && !flag)
			{
				if (item.ProxyIp.Contains(":"))
				{
					string[] array = item.ProxyIp.Split(new char[]
					{
						':'
					});
					WebProxy webProxy = new WebProxy(array[0].Trim(), Convert.ToInt32(array[1].Trim()));
					webProxy.Credentials = new NetworkCredential(item.ProxyUserName, item.ProxyPwd);
					this._request.Proxy = webProxy;
				}
				else
				{
					WebProxy webProxy = new WebProxy(item.ProxyIp, false);
					webProxy.Credentials = new NetworkCredential(item.ProxyUserName, item.ProxyPwd);
					this._request.Proxy = webProxy;
				}
			}
			else if (!flag)
			{
				this._request.Proxy = item.WebProxy;
			}
		}

		private bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
		{
			return true;
		}

		public IPEndPoint BindIpEndPointCallback(ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount)
		{
			return this._ipEndPoint;
		}
	}
}
