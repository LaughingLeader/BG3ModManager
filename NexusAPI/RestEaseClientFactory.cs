using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestEase;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace NexusAPI
{
    /// <summary>
	/// Class RestEase Client Factory.
	/// </summary>
	public sealed class RestEaseClientFactory<T> : IRestEaseClientFactory<T>
	{
		/// <summary>
		/// Gets the options.
		/// </summary>
		/// <value>
		/// The options.
		/// </value>
		public RestEaseClientFactoryOptions Options { get; private set; }

		/// <summary>
		/// Gets the HTTP client.
		/// </summary>
		/// <value>The HTTP client.</value>
		public HttpClient HttpClient { get; private set; }

		/// <summary>
		/// Gets the API client.
		/// </summary>
		/// <value>The API client.</value>
		public RestClient ApiClient { get; private set; }

		/// <summary>
		/// Gets the account API.
		/// </summary>
		/// <value>The account API.</value>
		public T Api { get; private set; }

		/// <summary>  Occurs before the HttpClient sends any data</summary>
		public event HttpLoggingHandler.OnSendAsyncHandler OnSendAsyncBefore;

		/// <summary>Occurs after the httpclient sends its request</summary>
		public event HttpLoggingHandler.OnSendAsyncHandler OnSendAsyncAfter;

		/// <summary>
		/// Initializes the factory.
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <param name="options">The options.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException">token - token cannot be null</exception>
		public Task<bool> InitializeAsync(string url, RestEaseClientFactoryOptions options = null)
		{
			return this.InitializeAsync(url, new HttpClientHandler(), options);
		}

		/// <summary>
		/// initialize the factory.
		/// </summary>
		/// <param name="url">The URL.</param>
		/// <param name="clientHandler">The client handler.</param>
		/// <param name="options">The options.</param>
		/// <returns>
		/// Task&lt;System.Boolean&gt;.
		/// </returns>
		public Task<bool> InitializeAsync(string url, HttpClientHandler clientHandler, RestEaseClientFactoryOptions options = null)
		{
			HttpClient httpClient;

			if (options?.EnableHttpLogging == true)
			{
				var loggingHandler = new HttpLoggingHandler(clientHandler);
				if (this.OnSendAsyncBefore != null) loggingHandler.OnSendAsyncBefore += this.OnSendAsyncBefore;
				if (this.OnSendAsyncAfter != null) loggingHandler.OnSendAsyncAfter += this.OnSendAsyncAfter;

				httpClient = new HttpClient(new HttpLoggingHandler(clientHandler))
				{
					BaseAddress = new Uri(url)
				};
			}
			else
			{
				httpClient = new HttpClient(clientHandler)
				{
					BaseAddress = new Uri(url)
				};
			}

			return this.InitializeAsync(httpClient, options);
		}

		/// <summary>
		/// Initializes the asynchronous.
		/// </summary>
		/// <param name="httpClient">The HTTP client.</param>
		/// <param name="options">The options.</param>
		/// <returns></returns>
		public Task<bool> InitializeAsync(HttpClient httpClient, RestEaseClientFactoryOptions options = null)
		{
			try
			{
				this.HttpClient = httpClient;
				this.Options = options;

				this.ApiClient = new RestClient(this.HttpClient)
				{
					ResponseDeserializer = this.Options?.ResponseDeserializer,
					RequestBodySerializer= this.Options?.JsonRequestBodySerializer,
					JsonSerializerSettings = this.Options?.JsonSerializerSettings,
					RequestQueryParamSerializer = this.Options?.RequestQueryParamSerializer
				};

				this.Api = this.ApiClient.For<T>();

				Serilog.Log.Information("Client Factory Initialized");
				return Task.FromResult(true);
			}
			catch (Exception ex)
			{
				Serilog.Log.Error(ex, "Failed to initialize API Client");
			}

			return Task.FromResult(false);
		}
	}

	public sealed class RestEaseClientFactoryOptions
	{
		/// <summary>
		/// Gets or sets a value indicating whether [enable HTTP logging].
		/// </summary>
		/// <value>
		///   <c>true</c> if [enable HTTP logging]; otherwise, <c>false</c>.
		/// </value>
		public bool EnableHttpLogging { get; set; }

		public MyResponseDeserializer ResponseDeserializer { get; set; } = new MyResponseDeserializer();

		public JsonRequestBodySerializer JsonRequestBodySerializer { get; set; } = new JsonRequestBodySerializer
		{
			JsonSerializerSettings =
				new JsonSerializerSettings
				{
					DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
					NullValueHandling = NullValueHandling.Ignore,
					Formatting = Formatting.Indented
				}
		};
		
		public MyRequestQueryParamSerializer RequestQueryParamSerializer { get; set; } = new MyRequestQueryParamSerializer();

		public JsonSerializerSettings JsonSerializerSettings { get; set; } = new JsonSerializerSettings
		{
			DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
			NullValueHandling = NullValueHandling.Ignore,
			Formatting = Formatting.Indented
		};
	}
}