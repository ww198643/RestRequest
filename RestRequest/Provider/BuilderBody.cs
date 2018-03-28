﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestRequest.interfaces;

namespace RestRequest.Provider
{
	public class BuilderBody : BuilderBase, IBuilder
	{
		public BuilderBody(string url, HttpMethod method, bool keepAlive, bool ignoreCertificateError) : base(url, method, keepAlive, ignoreCertificateError)
		{
			//var body = new DefaultBody();
			//body.SetContentType("application/x-www-form-urlencoded");
			RequestBody = new DefaultBody();
		}

		/// <summary>
		/// 设置json数据
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public IBuilderNoneBody Body(object parameters)
		{
			var body = new JsonBody();
			body.AddParameter(parameters);
			RequestBody = body;
			return this;
		}

		/// <summary>
		/// 设置表单数据
		/// content-type=application/x-www-form-urlencoded
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public IBuilderNoneBody Form(object parameters)
		{
			var body = new FormBody();
			body.AddParameter(parameters);
			RequestBody = body;
			return this;
		}

		/// <summary>
		/// 设置表单数据
		/// content-type=application/x-www-form-urlencoded
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public IBuilderNoneBody Form(Dictionary<string, string> parameters)
		{
			var body = new FormBody();
			body.AddParameter(parameters);
			RequestBody = body;
			return this;
		}

		/// <summary>
		/// 设置表单数据和上传的文件
		/// content-type=multipart/form-data
		/// </summary>
		/// <param name="files">上传的文件信息</param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public IBuilderNoneBody Form(IEnumerable<NamedFileStream> files, object parameters)
		{
			var body = new MultipartBody();
			body.AddParameters(parameters);
			body.AddFiles(files);
			RequestBody = body;
			return this;
		}

		/// <summary>
		/// 设置上传的文件
		/// content-type=multipart/form-data
		/// </summary>
		/// <param name="files">上传的文件信息</param>
		/// <returns></returns>
		public IBuilderNoneBody Form(IEnumerable<NamedFileStream> files)
		{
			var body = new MultipartBody();
			body.AddFiles(files);
			RequestBody = body;
			return this;
		}

		/// <summary>
		/// 设置提交的文本
		/// content-type=application/text
		/// </summary>
		/// <param name="text">提交文本</param>
		/// <returns></returns>
		public IBuilderNoneBody Form(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				throw new NullReferenceException("Post文本内容不能为空");
			RequestBody = new TextBody(text);
			return this;
		}

		/// <summary>
		/// 设置提交的Stream
		/// content-type=application/octet-stream
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		public IBuilderNoneBody Form(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException("the stream is null");
			RequestBody = new StreamBody(stream);
			return this;
		}

		/// <summary>
		/// 设置表单数据和上传的文件
		/// content-type=multipart/form-data
		/// </summary>
		/// <param name="files">上传的文件信息</param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public IBuilderNoneBody Form(IEnumerable<NamedFileStream> files, Dictionary<string, string> parameters)
		{
			var body = new MultipartBody();
			body.AddParameters(parameters);
			body.AddFiles(files);
			RequestBody = body;
			return this;
		}

		public IActionCallback OnSuccess(Action<HttpStatusCode, Stream> action)
		{
			SuccessAction = action;
			return this;
		}

		public IActionCallback OnSuccess(Action<HttpStatusCode, string> action)
		{
			SuccessAction = (statuscode, stream) =>
			{
				using (var reader = new StreamReader(stream))
				{
					action(statuscode, reader.ReadToEnd());
				}
			};
			return this;
		}

		public IActionCallback OnFail(Action<WebException> action)
		{
			FailAction = action;
			return this;
		}

		public IBuilderNoneBody Headers(Dictionary<string, string> headers)
		{
			if (!(headers?.Count > 0)) return this;
			foreach (var item in headers)
				RequestHeaders[item.Key] = item.Value;
			return this;
		}

		public IBuilderNoneBody Headers(object headers)
		{
			if (headers == null)
				return this;
			var properties = headers.GetType().GetProperties();
			foreach (var item in properties)
				RequestHeaders[item.Name] = item.GetValue(headers).ToString();
			return this;
		}

		public IBuilderNoneBody AddCertificate(string certificateUrl, string certificatePassword)
		{
			if (!File.Exists(certificateUrl))
				throw new FileNotFoundException($"证书文件不存在{certificateUrl}");
			if (ClientCertificates == null)
				ClientCertificates = new X509CertificateCollection();
			ClientCertificates.Add(new X509Certificate(certificateUrl, certificatePassword));
			return this;
		}

		public IBuilderNoneBody AddCertificate(byte[] rawData, string certificatePassword)
		{
			if (ClientCertificates == null)
				ClientCertificates = new X509CertificateCollection();
			ClientCertificates.Add(new X509Certificate(rawData, certificatePassword));
			return this;
		}

		public IBuilderNoneBody AddCertificate(X509Certificate cert)
		{
			if (ClientCertificates == null)
				ClientCertificates = new X509CertificateCollection();
			ClientCertificates.Add(new X509Certificate(cert));
			return this;
		}

		public IBuilderNoneBody UserAgent(string userAgent)
		{
			if (!string.IsNullOrWhiteSpace(userAgent))
				base.UserAgent = userAgent;
			return this;
		}

		public IBuilderNoneBody Timeout(int timeout)
		{
			if (timeout <= 0)
				throw new ArgumentOutOfRangeException("超时时间必须大于0");
			base.Timeout = timeout;
			return this;
		}

		public IBuilderNoneBody Cookies(object cookies)
		{
			if (cookies == null) return this;
			base.Cookies = new List<Cookie>();
			var properties = cookies.GetType().GetProperties();
			foreach (var enumerator in properties)
			{
				base.Cookies.Add(new Cookie { Name = enumerator.Name, Value = enumerator.GetValue(cookies).ToString(), Domain = Url.Host });
			}
			return this;
		}

		public IBuilderNoneBody Cookies(Dictionary<string, string> cookies)
		{
			if (cookies != null && cookies.Count > 0)
			{
				base.Cookies = new List<Cookie>();
				foreach (var cookie in cookies)
				{
					base.Cookies.Add(new Cookie { Name = cookie.Key, Value = cookie.Value, Domain = Url.Host });
				}
			}
			return this;
		}

		public IBuilderNoneBody Cookies(IEnumerable<Cookie> cookies)
		{
			base.Cookies = cookies?.ToList();
			return this;
		}

		public void Start()
		{
			var builder = new BuilderRequest(this);
			builder.CreateRequest();
			builder.BuildRequestAndCallback();
		}

		public IBuilderNoneBody ContentType(string contenttype)
		{
			if (!string.IsNullOrWhiteSpace(contenttype))
				RequestBody?.SetContentType(contenttype);
			return this;
		}


		public ResponseResult<Stream> ResponseStream()
		{
			var builder = new BuilderRequest(this);
			builder.CreateRequest();
			builder.BuildRequest();
			var res = builder.GetResponse();
			return new ResponseResult<Stream>
			{
				Succeed = res.Success,
				StatusCode = res.StatusCode,
				Content = res.ResponseContent,
				Response = res.Response,
				Request = builder.Request
			};
		}

		public async Task<ResponseResult<Stream>> ResponseStreamAsync()
		{
			var builder = new BuilderRequestAsync(this);
			builder.CreateRequest();
			await builder.BuildRequestAsync();
			var res = await builder.GetResponseAsync();
			return new ResponseResult<Stream>
			{
				Succeed = res.Success,
				StatusCode = res.StatusCode,
				Content = res.ResponseContent,
				Response = res.Response,
				Request = builder.Request
			};
		}

		public ResponseResult<string> ResponseString()
		{
			var res = ResponseStream();
			using (var stream = res.Content)
			using (var reader = new StreamReader(stream))
			using (res.Response)
			{
				return new ResponseResult<string>
				{
					Succeed = res.Succeed,
					StatusCode = res.StatusCode,
					Content = reader.ReadToEnd(),
					Response = res.Response,
					Request = res.Request
				};
			}
		}

		public async Task<ResponseResult<string>> ResponseStringAsync()
		{
			var res = await ResponseStreamAsync();
			using (var stream = res.Content)
			using (var reader = new StreamReader(stream))
			using (res.Response)
			{
				return new ResponseResult<string>
				{
					Succeed = res.Succeed,
					StatusCode = res.StatusCode,
					Content = await reader.ReadToEndAsync(),
					Response = res.Response,
					Request = res.Request
				};
			}
		}

		public ResponseResult<T> ResponseValue<T>() where T : class
		{
			var res = ResponseString();
			return new ResponseResult<T>
			{
				Succeed = res.Succeed,
				StatusCode = res.StatusCode,
				Content = JsonConvert.DeserializeObject<T>(res.Content),
				Response = res.Response,
				Request = res.Request
			};
		}

		public async Task<ResponseResult<T>> ResponseValueAsync<T>() where T : class
		{
			var res = await ResponseStringAsync();
			return new ResponseResult<T>
			{
				Succeed = res.Succeed,
				StatusCode = res.StatusCode,
				Content = JsonConvert.DeserializeObject<T>(res.Content),
				Response = res.Response,
				Request = res.Request
			};
		}
	}
}
