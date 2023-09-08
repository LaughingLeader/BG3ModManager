using System.Net.Http;
using System.Threading.Tasks;
using RestEase;

namespace NexusAPI
{
    public interface IRestEaseClientFactory<out T>
    {
        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <value>
        /// The options.
        /// </value>
        RestEaseClientFactoryOptions Options { get; }

        /// <summary>
        /// Gets the API.
        /// </summary>
        /// <value>
        /// The API.
        /// </value>
        T Api { get; }
        /// <summary>
        /// Gets the API client.
        /// </summary>
        /// <value>
        /// The API client.
        /// </value>
        RestClient ApiClient { get; }
        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>
        /// The HTTP client.
        /// </value>
        HttpClient HttpClient { get; }

        /*
        /// <summary>
        /// Initializes the asynchronous.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="token">The token.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        Task<bool> InitializeAsync(string url, string token, RestEaseClientFactoryOptions options = null);
        */
        
        /// <summary>
        /// Initializes the asynchronous.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="clientHandler">The client handler.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        Task<bool> InitializeAsync(string url, HttpClientHandler clientHandler, RestEaseClientFactoryOptions options = null);

        /// <summary>
        /// Initializes the asynchronous.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        Task<bool> InitializeAsync(HttpClient httpClient, RestEaseClientFactoryOptions options = null);
    }
}