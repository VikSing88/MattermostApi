using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MattermostApi
{
	public class Api : IDisposable
	{

		/// <summary>
		/// The Settings object to use for this Api instance.
		/// Will be Saved every time the AccessToken changes or is refreshed.
		/// </summary>
		public ISettings Settings;

		HttpClient _client;

		/// <summary>
		/// HttpClient, so you can set a timeeout for long operations
		/// </summary>
		public HttpClient Client { get { return _client; } }

		public Api(ISettings settings)
		{
			Settings = settings;
			var webProxy = new WebProxy();
			webProxy.UseDefaultCredentials = true;
			_client = new HttpClient(new HttpClientHandler() { Proxy = webProxy });
			_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
		}

		public void Dispose()
		{
			if (_client != null)
			{
				_client.Dispose();
				_client = null;
			}
		}


		/// <summary>
		/// Log messages will be passed to this handler
		/// </summary>
		public delegate void LogHandler(string message);

		/// <summary>
		/// Event receives all log messages (to, for example, save them to file or display them to the user)
		/// </summary>
		public event LogHandler LogMessage;

		/// <summary>
		/// Event receives all log messages (to, for example, save them to file or display them to the user)
		/// </summary>
		public event LogHandler ErrorMessage;

		/// <summary>
		/// The most recent requests sent
		/// </summary>
		public string LastRequest;

		/// <summary>
		/// The most recent response received
		/// </summary>
		public string LastResponse;

		/// <summary>
		/// Post to the Api, returning an object
		/// </summary>
		/// <typeparam name="T">The object type expected</typeparam>
		/// <param name="application">The part of the url after the company</param>
		/// <param name="getParameters">Any get parameters to pass (in an object or JObject)</param>
		/// <param name="postParameters">Any post parameters to pass (in an object or JObject)</param>
		public async Task<T> PostAsync<T>(string application, object getParameters = null, object postParameters = null) where T : new()
		{
			JObject j = await PostAsync(application, getParameters, postParameters);
			if (typeof(ApiList).IsAssignableFrom(typeof(T)))
			{
				JObject r = (getParameters == null ? (object)new ListRequest() : getParameters).ToJObject();
				r["PostParameters"] = postParameters.ToJToken();
				j["Request"] = r;
			}
			return convertTo<T>(j);
		}

		/// <summary>
		/// Post to the Api, returning a JObject
		/// </summary>
		/// <param name="application">The part of the url after the company</param>
		/// <param name="getParameters">Any get parameters to pass (in an object or JObject)</param>
		/// <param name="postParameters">Any post parameters to pass (in an object or JObject)</param>
		public async Task<JObject> PostAsync(string application, object getParameters = null, object postParameters = null)
		{
			string uri = MakeUri(application);
			uri = AddGetParams(uri, getParameters);
			return await SendMessageAsync(HttpMethod.Post, uri, postParameters);
		}

		/// <summary>
		/// Get from  the Api, returning an object
		/// </summary>
		/// <typeparam name="T">The object type expected</typeparam>
		/// <param name="application">The part of the url after the company</param>
		/// <param name="getParameters">Any get parameters to pass (in an object or JObject)</param>
		public async Task<T> GetAsync<T>(string application, object getParameters = null) where T : new()
		{
			JObject j = await GetAsync(application, getParameters);
			if (typeof(ApiList).IsAssignableFrom(typeof(T)))
				j["Request"] = (getParameters == null ? (object)new ListRequest() : getParameters).ToJObject();
			return convertTo<T>(j);
		}

		/// <summary>
		/// Get from  the Api, returning a Jobject
		/// </summary>
		/// <param name="application">The part of the url after the company</param>
		/// <param name="getParameters">Any get parameters to pass (in an object or JObject)</param>
		public async Task<JObject> GetAsync(string application, object getParameters = null)
		{
			string uri = MakeUri(application);
			uri = AddGetParams(uri, getParameters);
			return await SendMessageAsync(HttpMethod.Get, uri);
		}

		/// <summary>
		/// Put to  the Api, returning an object
		/// </summary>
		/// <typeparam name="T">The object type expected</typeparam>
		/// <param name="application">The part of the url after the company</param>
		/// <param name="getParameters">Any get parameters to pass (in an object or JObject)</param>
		/// <param name="postParameters">Any post parameters to pass (in an object or JObject)</param>
		public async Task<T> PutAsync<T>(string application, object getParameters = null, object postParameters = null) where T : new()
		{
			JObject j = await PutAsync(application, getParameters, postParameters);
			return convertTo<T>(j);
		}

		/// <summary>
		/// Put to  the Api, returning a JObject
		/// </summary>
		/// <param name="application">The part of the url after the company</param>
		/// <param name="getParameters">Any get parameters to pass (in an object or JObject)</param>
		/// <param name="postParameters">Any post parameters to pass (in an object or JObject)</param>
		public async Task<JObject> PutAsync(string application, object getParameters = null, object postParameters = null)
		{
			string uri = MakeUri(application);
			uri = AddGetParams(uri, getParameters);
			return await SendMessageAsync(HttpMethod.Put, uri, postParameters);
		}

		/// <summary>
		/// Delete to  the Api, returning an object
		/// </summary>
		/// <typeparam name="T">The object type expected</typeparam>
		/// <param name="application">The part of the url after the company</param>
		/// <param name="getParameters">Any get parameters to pass (in an object or JObject)</param>
		public async Task<T> DeleteAsync<T>(string application, object getParameters = null) where T : new()
		{
			JObject j = await DeleteAsync(application, getParameters);
			return convertTo<T>(j);
		}

		/// <summary>
		/// Delete to  the Api, returning a JObject
		/// </summary>
		/// <typeparam name="T">The object type expected</typeparam>
		/// <param name="application">The part of the url after the company</param>
		/// <param name="getParameters">Any get parameters to pass (in an object or JObject)</param>
		public async Task<JObject> DeleteAsync(string application, object getParameters = null)
		{
			string uri = MakeUri(application);
			uri = AddGetParams(uri, getParameters);
			return await SendMessageAsync(HttpMethod.Delete, uri);
		}

		/// <summary>
		/// API post using multipart/form-data.
		/// </summary>
		/// <param name="application">The full Uri you want to call (including any get parameters)</param>
		/// <param name="getParameters">Get parameters (or null if none)</param>
		/// <param name="postParameters">Post parameters as an  object or JObject
		/// </param>
		/// <returns>The result as a T Object.</returns>
		public async Task<T> PostFormAsync<T>(string application, object getParameters, object postParameters, params string[] fileParameterNames) where T : new()
		{
			JObject j = await PostFormAsync(application, getParameters, postParameters, fileParameterNames);
			return convertTo<T>(j);
		}

		/// <summary>
		/// API post using multipart/form-data.
		/// </summary>
		/// <param name="application">The full Uri you want to call (including any get parameters)</param>
		/// <param name="getParameters">Get parameters (or null if none)</param>
		/// <param name="postParameters">Post parameters as an  object or JObject
		/// </param>
		/// <returns>The result as a JObject, with MetaData filled in.</returns>
		public async Task<JObject> PostFormAsync(string application, object getParameters, object postParameters, params string[] fileParameterNames)
		{
			string uri = AddGetParams(MakeUri(application), getParameters);
			using (DisposableCollection objectsToDispose = new DisposableCollection())
			{
				MultipartFormDataContent content = objectsToDispose.Add(new MultipartFormDataContent());
				JObject data = postParameters.ToJObject();
				foreach (var o in data)
				{
					if (o.Value.Type == JTokenType.Null)
						continue;
					if (Array.IndexOf(fileParameterNames, o.Key) >= 0)
					{
						string filename = o.Value.ToString();
						FileStream fs = objectsToDispose.Add(new FileStream(filename, FileMode.Open));
						HttpContent v = objectsToDispose.Add(new StreamContent(fs));
						content.Add(v, o.Key, Path.GetFileName(filename));
					}
					else
					{
						HttpContent v = objectsToDispose.Add(new StringContent(o.Value.ToString()));
						content.Add(v, o.Key);
					}
				}
				return await SendMessageAsync(HttpMethod.Post, uri, content);
			}

		}

    /// <summary>
    /// Log a message to trace and, if present, to the LogMessage event handlers
    /// </summary>
    public void Log(string message)
    {
      message = "Mattermost log:" + message;
      System.Diagnostics.Trace.WriteLine(message);
      LogMessage?.Invoke(message);
    }

    /// <summary>
    /// Log a message to trace and, if present, to the ErrorMessage event handlers
    /// </summary>
    public void Error(string message)
    {
      message = "Mattermost error:" + message;
      System.Diagnostics.Trace.WriteLine(message);
      ErrorMessage?.Invoke(message);
    }

    /// <summary>
    /// Combine a list of arguments into a string, with "/" between them (escaping if required)
    /// </summary>
    public static string Combine(params object[] args)
		{
			return string.Join("/", args.Select(a => Uri.EscapeUriString(a.ToString())));
		}

		static readonly char[] argSplit = new char[] { '=' };

		/// <summary>
		/// Add or Replace Get Parameters to a uri
		/// </summary>
		/// <param name="parameters">Object whose properties are the arguments - e.g. new {
		/// 		type = "web_server",
		/// 		client_id = Settings.ClientId,
		/// 		redirect_uri = Settings.RedirectUri
		/// 	}</param>
		/// <returns>uri?arg1=value1&amp;arg2=value2...</returns>
		public static string AddGetParams(string uri, object parameters = null)
		{
			if (parameters != null)
			{
				Uri u = new Uri(uri);
				Dictionary<string, string> query = new Dictionary<string, string>();
				foreach (string arg in u.Query.Split('&', '?'))
				{
					if (string.IsNullOrEmpty(arg)) continue;
					string[] parts = arg.Split(argSplit, 2);
					if (parts.Length < 2) continue;
					query[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);

				}
				JObject j = parameters.ToJObject();
				List<string> p = new List<string>();
				foreach (var v in j)
				{
					if (v.Value.IsNullOrEmpty())
						query.Remove(v.Key);
					else
						query[v.Key] = v.Value.ToString();
				}
				uri = uri.Split('?')[0] + "?" + string.Join("&", query.Keys.Select(k => Uri.EscapeUriString(k) + "=" + Uri.EscapeUriString(query[k])));
			}
			return uri;
		}

		/// <summary>
		/// General API message sending.
		/// </summary>
		/// <param name="method">Get/Post/etc.</param>
		/// <param name="uri">The full Uri you want to call (including any get parameters)</param>
		/// <param name="postParameters">Post parameters as an :-
		/// object (converted to Json, MIME type application/json)
		/// JObject (converted to Json, MIME type application/json)
		/// string (sent as is, MIME type text/plain)
		/// FileStream (sent as stream, with Attachment file name, Content-Length, and MIME type according to file extension)
		/// </param>
		/// <returns>The result as a JObject, with MetaData filled in.</returns>
		public async Task<JObject> SendMessageAsync(HttpMethod method, string uri, object postParameters = null)
		{
			using (HttpResponseMessage result = await SendMessageAsyncAndGetResponse(method, uri, postParameters))
			{
				return await parseJObjectFromResponse(uri, result);
			}
		}

		/// <summary>
		/// Send a message and get the result.
		/// Deal with rate limiting return values and redirects.
		/// </summary>
		/// <param name="method">Get/Post/etc.</param>
		/// <param name="uri">The full Uri you want to call (including any get parameters)</param>
		/// <param name="postParameters">Post parameters as an object or JObject</param>
		public async Task<HttpResponseMessage> SendMessageAsyncAndGetResponse(HttpMethod method, string uri, object postParameters = null)
		{
			LastRequest = "";
			LastResponse = "";
			for (; ; )
			{
				string content = null;
				using (DisposableCollection disposeMe = new DisposableCollection())
				{
					var message = disposeMe.Add(new HttpRequestMessage(method, uri));
					if (!string.IsNullOrEmpty(Settings.AccessToken))
						message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Settings.AccessToken);

					message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
					message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
					message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
					if (postParameters != null)
					{
						if (postParameters is string s)
						{
							content = s;
							message.Content = disposeMe.Add(new StringContent(content, Encoding.UTF8, "text/plain"));
						}
						else if (postParameters is FileStream f)
						{
							content = Path.GetFileName(f.Name);
							f.Position = 0;
							message.Content = disposeMe.Add(new StreamContent(f));
							string contentType = MimeMapping.MimeUtility.GetMimeMapping(content);
							message.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
							message.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
							{
								FileName = content
							};
							message.Content.Headers.ContentLength = f.Length;
							content = "File: " + content;
						}
						else if (postParameters is HttpContent)
						{
							message.Content = (HttpContent)postParameters;
						}
						else
						{
							content = postParameters.ToJson();
							message.Content = disposeMe.Add(new StringContent(content, Encoding.UTF8, "application/json"));
						}
					}
					LastRequest = $"{message}:{content}";
					HttpResponseMessage result;
					int backoff = 500;
					int delay;
					if (Settings.LogRequest > 0)
						Log($"Sent -> {(Settings.LogRequest > 1 ? message.ToString() : message.RequestUri.ToString())}:{content}");
					result = await _client.SendAsync(message);
					LastResponse = result.ToString();
					if (Settings.LogResult > 1)
						Log($"Received -> {result}");
					switch (result.StatusCode)
					{
						case HttpStatusCode.Found:      // Redirect
							uri = result.Headers.Location.AbsoluteUri;
							delay = 1;
							break;
						case (HttpStatusCode)429:       // TooManyRequests
							delay = 5000;
							break;
						case HttpStatusCode.BadGateway:
						case HttpStatusCode.ServiceUnavailable:
						case HttpStatusCode.GatewayTimeout:
							backoff *= 2;
							delay = backoff;
							if (delay > 16000)
								return result;
							break;
						default:
							return result;
					}
					result.Dispose();
					Thread.Sleep(delay);
				}
			}
		}

		/// <summary>
		/// Build a JObject from a response
		/// </summary>
		/// <param name="uri">To store in the MetaData</param>
		async Task<JObject> parseJObjectFromResponse(string uri, HttpResponseMessage result)
		{
			JObject j = null;
			string message = result.ReasonPhrase;
			try
			{
				string data = await result.Content.ReadAsStringAsync();
				LastResponse += "\n" + data;
				if (data.StartsWith("{"))
				{
					j = JObject.Parse(data);
					string m = j["message"] + "";
					if (!string.IsNullOrEmpty(m))
						message = m;
				}
				else if (data.StartsWith("["))
				{
					j = new JObject
					{
						["List"] = JArray.Parse(data)
					};
				}
				else
				{
					j = new JObject();
					if (!string.IsNullOrEmpty(data))
						j["content"] = data;
				}
				JObject metadata = new JObject();
				if (!result.IsSuccessStatusCode && j.ContainsKey("status_code"))
				{
					metadata["Error"] = j;
				}
				metadata["Uri"] = uri;
				if (result.Headers.TryGetValues("Last-Modified", out IEnumerable<string> values))
					metadata["Modified"] = values.FirstOrDefault();
				j["MetaData"] = metadata;
				if (Settings.LogResult > 0)
					Log("Received Data -> " + j);
			}
			catch (Exception ex)
			{
				throw new ApiException(this, ex);
			}
			if (!result.IsSuccessStatusCode)
				throw new ApiException(this, message);
			return j;
		}

		/// <summary>
		/// Convert a JObject to an Object.
		/// If it is an ApiEntry, and error is not empty, throw an exception.
		/// </summary>
		/// <typeparam name="T">Object to convert to</typeparam>
		T convertTo<T>(JObject j) where T : new()
		{
			T t = j.ConvertToObject<T>();
			if (t is ApiEntry e && e.Error)
				throw new ApiException(this, e.MetaData.Error.message);
			return t;
		}

		/// <summary>
		/// Default <see cref="OpenBrowser"/>
		/// </summary>
		static void openBrowser(string url)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				Process.Start(new ProcessStartInfo("cmd", $"/c start {url.Replace("&", "^&")}"));
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				Process.Start("xdg-open", "'" + url + "'");
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				Process.Start("open", "'" + url + "'");
			}
			else
			{
				throw new ApplicationException("Unknown OS platform");
			}
		}

		static readonly Regex _http = new Regex("^https?://");

		/// <summary>
		/// Make the standard Uri (put BaseUri and CompanyId on the front)
		/// </summary>
		/// <param name="application">The remainder of the Uri</param>
		public string MakeUri(string application)
		{
			return _http.IsMatch(application) ? application : Settings.ServerUri + "api/v4/" + application;
		}

	}

	/// <summary>
	/// Exception to hold more information when an API call fails
	/// </summary>
	public class ApiException : ApplicationException
	{
		public ApiException(Api api, Exception ex) : base(ex.Message, ex)
		{
			Request = api.LastRequest;
			Response = api.LastResponse;
		}
		public ApiException(Api api, string message) : base(message)
		{
			Request = api.LastRequest;
			Response = api.LastResponse;
		}
		public string Request { get; private set; }
		public string Response { get; private set; }
		public override string ToString()
		{
			return base.ToString() + "\r\nRequest = " + Request + "\r\nResult = " + Response;
		}
	}

	/// <summary>
	/// Token returned from Auth call
	/// </summary>
	public class Token : ApiEntry
	{
		public string access_token;
		public string token_type;
		public string scope;
		public string refresh_token;
		public string csrf_token;
		public int expires_in;
	}


}
