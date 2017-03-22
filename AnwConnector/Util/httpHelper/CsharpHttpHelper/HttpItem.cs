using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AnwConnector.Util.httpHelper.CsharpHttpHelper.Enum;

namespace AnwConnector.Util.httpHelper.CsharpHttpHelper
{
	public class HttpItem
	{
		private string _method = "GET";

		private int _timeout = 100000;

		private int _readWriteTimeout = 30000;

		private bool _keepAlive = true;

		private string _accept = "text/html, application/xhtml+xml, */*";

		private string _contentType = "text/html";

		private string _userAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";

		private bool _expect100Continue = true;

		private DateTime? _ifModifiedSince = null;

		private DecompressionMethods _automaticDecompression = DecompressionMethods.None;

		private PostDataType _postDataType = PostDataType.String;

		private bool _autoRedirectCookie = false;

		private ResultCookieType _resultCookieType = ResultCookieType.String;

		private ICredentials _iCredentials = CredentialCache.DefaultCredentials;

		private bool _isToLower = false;

		private bool _allowautoredirect = false;

		private int _connectionlimit = 1024;

		private ResultType _resulttype = ResultType.String;

		private WebHeaderCollection _header = new WebHeaderCollection();

		private IPEndPoint _ipEndPoint = null;

		public string Url
		{
			get;
			set;
		}

		public string Method
		{
			get
			{
				return this._method;
			}
			set
			{
				this._method = value;
			}
		}

		public int Timeout
		{
			get
			{
				return this._timeout;
			}
			set
			{
				this._timeout = value;
			}
		}

		public int ReadWriteTimeout
		{
			get
			{
				return this._readWriteTimeout;
			}
			set
			{
				this._readWriteTimeout = value;
			}
		}

		public string Host
		{
			get;
			set;
		}

		public bool KeepAlive
		{
			get
			{
				return this._keepAlive;
			}
			set
			{
				this._keepAlive = value;
			}
		}

		public string Accept
		{
			get
			{
				return this._accept;
			}
			set
			{
				this._accept = value;
			}
		}

		public string ContentType
		{
			get
			{
				return this._contentType;
			}
			set
			{
				this._contentType = value;
			}
		}

		public string UserAgent
		{
			get
			{
				return this._userAgent;
			}
			set
			{
				this._userAgent = value;
			}
		}

		public string Referer
		{
			get;
			set;
		}

		public Version ProtocolVersion
		{
			get;
			set;
		}

		public bool Expect100Continue
		{
			get
			{
				return this._expect100Continue;
			}
			set
			{
				this._expect100Continue = value;
			}
		}

		public int MaximumAutomaticRedirections
		{
			get;
			set;
		}

		public DateTime? IfModifiedSince
		{
			get
			{
				return this._ifModifiedSince;
			}
			set
			{
				this._ifModifiedSince = value;
			}
		}

		public Encoding Encoding
		{
			get;
			set;
		}

		public Encoding PostEncoding
		{
			get;
			set;
		}

		public DecompressionMethods AutomaticDecompression
		{
			get
			{
				return this._automaticDecompression;
			}
			set
			{
				this._automaticDecompression = value;
			}
		}

		public PostDataType PostDataType
		{
			get
			{
				return this._postDataType;
			}
			set
			{
				this._postDataType = value;
			}
		}

		public string Postdata
		{
			get;
			set;
		}

		public byte[] PostdataByte
		{
			get;
			set;
		}

		public CookieCollection CookieCollection
		{
			get;
			set;
		}

		public string Cookie
		{
			get;
			set;
		}

		public bool AutoRedirectCookie
		{
			get
			{
				return this._autoRedirectCookie;
			}
			set
			{
				this._autoRedirectCookie = value;
			}
		}

		public ResultCookieType ResultCookieType
		{
			get
			{
				return this._resultCookieType;
			}
			set
			{
				this._resultCookieType = value;
			}
		}

		public string CerPath
		{
			get;
			set;
		}

		public string CerPwd
		{
			get;
			set;
		}

		public X509CertificateCollection ClentCertificates
		{
			get;
			set;
		}

		public ICredentials Credentials
		{
			get
			{
				return this._iCredentials;
			}
			set
			{
				this._iCredentials = value;
			}
		}

		public bool IsToLower
		{
			get
			{
				return this._isToLower;
			}
			set
			{
				this._isToLower = value;
			}
		}

		public bool Allowautoredirect
		{
			get
			{
				return this._allowautoredirect;
			}
			set
			{
				this._allowautoredirect = value;
			}
		}

		public int Connectionlimit
		{
			get
			{
				return this._connectionlimit;
			}
			set
			{
				this._connectionlimit = value;
			}
		}

		public WebProxy WebProxy
		{
			get;
			set;
		}

		public string ProxyUserName
		{
			get;
			set;
		}

		public string ProxyPwd
		{
			get;
			set;
		}

		public string ProxyIp
		{
			get;
			set;
		}

		public ResultType ResultType
		{
			get
			{
				return this._resulttype;
			}
			set
			{
				this._resulttype = value;
			}
		}

		public WebHeaderCollection Header
		{
			get
			{
				return this._header;
			}
			set
			{
				this._header = value;
			}
		}

		public IPEndPoint IpEndPoint
		{
			get
			{
				return this._ipEndPoint;
			}
			set
			{
				this._ipEndPoint = value;
			}
		}
	}
}
