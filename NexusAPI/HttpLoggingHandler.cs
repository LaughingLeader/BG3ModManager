using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace NexusAPI
{
    public static class KeyValuePairExtensions
    {
		// NOTE(Gavin): Added for compatibility after porting .netcore3.0 -> .net framework v4.7
        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
        {
            key = tuple.Key;
            value = tuple.Value;
        }
    }

    /// <summary>
	/// Class HttpLoggingHandler.
	/// </summary>
	/// <seealso cref="System.Net.Http.DelegatingHandler" />
	public sealed class HttpLoggingHandler : DelegatingHandler
	{
		
		public delegate void OnSendAsyncHandler(HttpLoggingHandler obj, HttpLoggingnHandlerEventArgs args);

		public event OnSendAsyncHandler OnSendAsyncBefore;
		public event OnSendAsyncHandler OnSendAsyncAfter;

		/// <summary>
		/// Initializes a new instance of the <see cref="HttpLoggingHandler"/> class.
		/// </summary>
		/// <param name="innerHandler">The inner handler.</param>
		public HttpLoggingHandler(HttpMessageHandler innerHandler = null)
			: base(innerHandler ?? new HttpClientHandler())
		{ }

		/// <summary>
		/// send as an asynchronous operation.
		/// </summary>
		/// <param name="request">The request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>Task&lt;HttpResponseMessage&gt;.</returns>
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			await Task.Delay(1).ConfigureAwait(false);

			var req = request;
			var id = Guid.NewGuid().ToString();
			var msg = $"[{id} -   Request]";

			Log.Debug($"{msg}========Start==========");
			Log.Verbose($"{msg} {req.Method} {req.RequestUri.PathAndQuery} {req.RequestUri.Scheme}/{req.Version}");
			Log.Debug($"{msg} Host: {req.RequestUri.Scheme}://{req.RequestUri.Host}");

			foreach (var (key, value) in req.Headers)
			{
				Log.Debug($"{msg} {key}: {string.Join(", ", value)}");
			}

			if (req.Content != null)
			{
				foreach (var (key, value) in req.Content.Headers)
				{
					Log.Debug($"{msg} {key}: {string.Join(", ", value)}");
				}

				if (req.Content is StringContent || req.Headers.IsTextBasedContentType() || req.Content.Headers.IsTextBasedContentType())
				{
					var result = await req.Content.ReadAsStringAsync().ConfigureAwait(false);

					Log.Debug($"{msg} Content:");
#if DEBUG
					Log.Debug($"{msg} {string.Join("", result)}");
#else
					Log.Debug($"{msg} {string.Join("", result.Cast<char>().Take(255))}...");
#endif
				}
			}

			Log.Debug($"{msg} Sending Reqest - Start");

			var start = DateTime.Now;

			this.OnSendAsyncBefore?.Invoke(this, new HttpLoggingnHandlerEventArgs(request));

			var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

			this.OnSendAsyncAfter?.Invoke(this, new HttpLoggingnHandlerEventArgs(request, response));


			var end = DateTime.Now;

			Log.Debug($"{msg} Sending Reqest - End");

			Log.Debug($"{msg} Duration: {end - start}");
			Log.Debug($"{msg}==========End==========");

			msg = $"[{id} - Response]";
			Log.Debug($"{msg}=========Start=========");

			var resp = response;

			if (!resp.IsSuccessStatusCode)
			{
				Log.Error($"{msg} {req.RequestUri.AbsoluteUri} ({req.RequestUri.Scheme.ToUpper()}/{resp.Version}) {(int)resp.StatusCode} {resp.ReasonPhrase}");
			}
			else
			{
				Log.Verbose($"{msg} {req.RequestUri.AbsoluteUri} ({req.RequestUri.Scheme.ToUpper()}/{resp.Version}) {(int)resp.StatusCode} {resp.ReasonPhrase}");
			}

			foreach (var (key, value) in resp.Headers)
			{
				Log.Debug($"{msg} {key}: {string.Join(", ", value)}");
			}

			if (resp.Content != null)
			{
				foreach (var (key, value) in resp.Content.Headers)
				{
					Log.Debug($"{msg} {key}: {string.Join(", ", value)}");
				}

				if (resp.Content is StringContent || resp.Headers.IsTextBasedContentType() || resp.Content.Headers.IsTextBasedContentType())
				{
					try
					{
						start = DateTime.Now;
						var result = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
						end = DateTime.Now;

						Log.Debug($"{msg} Content:");
#if DEBUG
						Log.Debug($"{msg} {string.Join("", result)}");
#else
					Log.Debug($"{msg} {string.Join("", result.Cast<char>().Take(255))}...");
#endif
						Log.Debug($"{msg} Duration: {end - start}");
					}
					catch (Exception ex)
					{
						Log.Debug(ex, "Failed to retrieve content");
					}
				}
			}

			Log.Debug($"{msg}==========End==========");

			return response;
		}
	}

	public sealed class HttpLoggingnHandlerEventArgs : EventArgs
	{
		public HttpLoggingnHandlerEventArgs(HttpRequestMessage request)
		{
			this.Request = request;
		}

		public HttpLoggingnHandlerEventArgs(HttpRequestMessage request, HttpResponseMessage response)
		{
			this.Request = request;
			this.Response = response;
		}

		public HttpRequestMessage Request { get; }
		public HttpResponseMessage Response { get; }
	}

	internal static class HttpHeadersExtensions
	{
		/// <summary>
		/// The types
		/// </summary>
		private static readonly string[] Types = { "html", "text", "xml", "json", "txt" };

		/// <summary>
		/// Determines whether [is text based content type] [the specified headers].
		/// </summary>
		/// <param name="headers">The headers.</param>
		/// <returns><c>true</c> if [is text based content type] [the specified headers]; otherwise, <c>false</c>.</returns>
		public static bool IsTextBasedContentType(this HttpHeaders headers)
		{
			if (!headers.TryGetValues("Content-Type", out IEnumerable<string> values))
				return false;
			var header = string.Join(" ", values).ToLowerInvariant();

			return Types.Any(t => header.Contains(t));
		}

		public static bool IsTextBasedContentType(this HttpRequestHeaders headers)
		{
			return IsTextBasedContentType(headers as HttpHeaders);
		}

		public static bool IsTextBasedContentType(this HttpResponseHeaders headers)
		{
			return IsTextBasedContentType(headers as HttpHeaders);
		}
	}
}