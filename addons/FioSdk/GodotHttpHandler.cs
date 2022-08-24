using Godot;
using FioSharp.Secp256k1;
using FioSharp.Core.Helpers;
using FioSharp.Core.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using FioSharp.Core.Exceptions;

public class GodotHttpHandler : Node, IHttpHandler
{
	private static Dictionary<string, object> ResponseCache { get; set; } = new Dictionary<string, object>();
	private static readonly HTTPClient http = new HTTPClient(); // Create the client.
	private static readonly string[] headers = new string[] { "Content-Type: application/json" };
	private const string APPLICATION_JSON = "Content-Type: application/json";

	private const string FRAME = "physics_frame";

	public async Task<string> SetupHttpClient(string url)
	{
		int port = 443;

		// Get the base of the url
		int n = 3;
		int slash = url.TakeWhile(c => (n -= (c == '/' ? 1 : 0)) > 0).Count();

		// Get the port
		Regex r = new Regex(
			@"^(?<url>\w+:\/\/[^\/:]+)(?<port>:\d+)?",
			RegexOptions.None,
			TimeSpan.FromMilliseconds(150));
		Match m = r.Match(url);

		//GD.Print(url);
		string urlBase = "";
		if (m.Success)
		{
			//GD.Print(m.Result("${url}${port}"));
			urlBase = m.Result("${url}");
			string p = m.Result("${port}");
			if (!p.Empty())
			{
				port = int.Parse(p.Substring(1));
			}
		}
		else
		{
			throw new ApiException()
			{
				StatusCode = -1,
				Content = "Cannot connect to or resolve host: " + url
			};
		}
		//GD.Print(urlBase);

		// Connect to and wait for response
		Error err = http.ConnectToHost(urlBase, port: port);
		//GD.Print("Connection Error: ", err);
		while (http.GetStatus() == HTTPClient.Status.Connecting || http.GetStatus() == HTTPClient.Status.Resolving)
		{
			http.Poll();
			//GD.Print("Connecting...");
			await ToSignal(Engine.GetMainLoop(), FRAME);
		}

		if (http.GetStatus() == HTTPClient.Status.CantConnect || http.GetStatus() == HTTPClient.Status.CantResolve)
		{
			GD.Print("Connection Status: ", http.GetStatus());
			throw new ApiException()
			{
				StatusCode = -1,
				Content = "Cannot connect to or resolve host: " + url
			};
		}

		// Return the endpoint to call
		return url.Substring(m.Length);
	}

	/// <summary>
	/// Clear cached responses from requests called with Post/GetWithCacheAsync
	/// </summary>
	public void ClearResponseCache()
	{
		ResponseCache.Clear();
	}

	/// <summary>
	/// Make post request with data converted to json asynchronously
	/// </summary>
	/// <typeparam name="TResponseData">Response type</typeparam>
	/// <param name="url">Url to send the request</param>
	/// <param name="data">data sent in the body</param>
	/// <returns>Response data deserialized to type TResponseData</returns>
	public async Task<TResponseData> PostJsonAsync<TResponseData>(string url, object data)
	{
		// If the client isn't setup yet, use the URL to set it up
		string requestUrl = await SetupHttpClient(url);

		// Make the request and wait for the result
		//GD.Print(requestUrl);
		string jsonString = JsonConvert.SerializeObject(data);
		//GD.Print(jsonString);
		//GD.Print(headers[0]);
		//string[] headers2 = new string[] {
		//	APPLICATION_JSON,
		//	"Content-Length: " + jsonString.Length() };
		Error err = http.Request(HTTPClient.Method.Post, requestUrl, headers, jsonString);
		//GD.Print("Request Error: ", err);

		return await PollUntilResponse<TResponseData>();
	}

	/// <summary>
	/// Make post request with data converted to json asynchronously.
	/// Response is cached based on input (url, data)
	/// </summary>
	/// <typeparam name="TResponseData">Response type</typeparam>
	/// <param name="url">Url to send the request</param>
	/// <param name="data">data sent in the body</param>
	/// <param name="reload">ignore cached value and make a request caching the result</param>
	/// <returns>Response data deserialized to type TResponseData</returns>
	public async Task<TResponseData> PostJsonWithCacheAsync<TResponseData>(string url, object data, bool reload = false)
	{
		string hashKey = GetRequestHashKey(url, data);

		// Load from the cache if we don't care about reloading
		if (!reload)
		{
			if (ResponseCache.TryGetValue(hashKey, out object value))
			{
				return (TResponseData)value;
			}
		}

		// Get the new info, save it, and return it
		TResponseData responseData = await PostJsonAsync<TResponseData>(url, data);
		UpdateResponseDataCache(hashKey, responseData);

		return responseData;
	}

	/// <summary>
	/// Make get request asynchronously.
	/// </summary>
	/// <typeparam name="TResponseData">Response type</typeparam>
	/// <param name="url">Url to send the request</param>
	/// <returns>Response data deserialized to type TResponseData</returns>
	public async Task<TResponseData> GetJsonAsync<TResponseData>(string url)
	{
		// If the client isn't setup yet, use the URL to set it up
		string requestUrl = await SetupHttpClient(url);

		// Make the request and wait for the result
		//GD.Print(requestUrl);
		Error err = http.Request(HTTPClient.Method.Get, requestUrl, headers);
		//GD.Print("Request Error: ", err);

		return await PollUntilResponse<TResponseData>();
	}

	/// <summary>
	/// Upsert response data in the data store
	/// </summary>
	/// <typeparam name="TResponseData">response data type</typeparam>
	/// <param name="hashKey">data key</param>
	/// <param name="responseData">response data</param>
	public void UpdateResponseDataCache<TResponseData>(string hashKey, TResponseData responseData)
	{
		if (ResponseCache.ContainsKey(hashKey))
		{
			ResponseCache[hashKey] = responseData;
		}
		else
		{
			ResponseCache.Add(hashKey, responseData);
		}
	}

	/// <summary>
	/// Calculate request unique hash key
	/// </summary>
	/// <param name="url">Url to send the request</param>
	/// <param name="data">data sent in the body</param>
	/// <returns></returns>
	public string GetRequestHashKey(string url, object data)
	{
		var keyBytes = new List<byte[]>()
		{
			Encoding.UTF8.GetBytes(url),
			SerializationHelper.ObjectToByteArray(data)
		};
		return Encoding.Default.GetString(ShaManager.GetSha256Hash(SerializationHelper.Combine(keyBytes)));
	}

	public async Task<TResponseData> PollUntilResponse<TResponseData>()
	{
		//GD.Print("Requesting...");

		while (http.GetStatus() == HTTPClient.Status.Requesting)
		{
			http.Poll();
			await ToSignal(Engine.GetMainLoop(), FRAME);
		}

		// If there is a response...
		bool hasResponse = http.HasResponse();
		if (http.HasResponse())
		{
			int responseCode = http.GetResponseCode();
			//GD.Print("Code: ", responseCode); // Show response code.
			List<byte> rb = new List<byte>(); // List that will hold the data.

			// While there is data left to be read...
			while (http.GetStatus() == HTTPClient.Status.Body)
			{
				http.Poll();
				byte[] chunk = http.ReadResponseBodyChunk(); // Read a chunk.
				if (chunk.Length == 0)
				{
					// If nothing was read, wait for the buffer to fill.
					await ToSignal(Engine.GetMainLoop(), FRAME);
				}
				else
				{
					// Append the chunk to the read buffer.
					rb.AddRange(chunk);
				}
			}

			// Done!
			//GD.Print("Bytes Downloaded: ", rb.Count);
			string data = Encoding.UTF8.GetString(rb.ToArray());

			if (responseCode >= 200 && responseCode <= 299)
			{
				return JsonConvert.DeserializeObject<TResponseData>(data);
			}
			else
			{
				ApiErrorException apiError;
				try
				{
					apiError = JsonConvert.DeserializeObject<ApiErrorException>(data);
					apiError.code = responseCode;
				}
				catch (Exception e)
				{
					//Console.WriteLine("Other Exception: " + e.ToString());
					throw new ApiException
					{
						StatusCode = responseCode,
						Content = data
					};
				}

				Console.WriteLine("Throwing Error: " + apiError.ToString());
				throw apiError;
			}
		}

		// If we got here then we failed to connected
		throw new ApiException()
		{
			StatusCode = -1,
			Content = "Failed to connect"
		};
	}
}
