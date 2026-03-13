using System.Net;
using System.Net.Http.Json;

namespace RetroBoard.IntegrationTests.Shared.Extensions;

/// <summary>
/// Extension methods for <see cref="HttpClient"/> that simplify
/// common integration test patterns (POST, GET, assert status, deserialize).
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Sends a POST request with a JSON body and asserts a 201 Created response.
    /// Returns the deserialized response body.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <typeparam name="TResponse">The type of the expected response body.</typeparam>
    /// <param name="client">The HTTP client.</param>
    /// <param name="url">The request URL.</param>
    /// <param name="request">The request body to serialize as JSON.</param>
    /// <returns>The deserialized response body.</returns>
    /// <exception cref="HttpRequestException">
    /// Thrown when the response status code is not 201 Created.
    /// </exception>
    public static async Task<TResponse> PostAndExpectCreatedAsync<TRequest, TResponse>(
        this HttpClient client, string url, TRequest request)
    {
        var response = await client.PostAsJsonAsync(url, request);
        await response.EnsureStatusCode(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }

    /// <summary>
    /// Sends a GET request and asserts a 200 OK response.
    /// Returns the deserialized response body.
    /// </summary>
    /// <typeparam name="TResponse">The type of the expected response body.</typeparam>
    /// <param name="client">The HTTP client.</param>
    /// <param name="url">The request URL.</param>
    /// <returns>The deserialized response body.</returns>
    public static async Task<TResponse> GetAndExpectOkAsync<TResponse>(
        this HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        await response.EnsureStatusCode(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }

    /// <summary>
    /// Sends a POST request with a JSON body and asserts a specific
    /// expected error status code (e.g., 409 Conflict, 422).
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <param name="client">The HTTP client.</param>
    /// <param name="url">The request URL.</param>
    /// <param name="request">The request body.</param>
    /// <param name="expectedStatusCode">The expected HTTP status code.</param>
    /// <returns>The raw <see cref="HttpResponseMessage"/> for further assertions.</returns>
    public static async Task<HttpResponseMessage> PostAndExpectErrorAsync<TRequest>(
        this HttpClient client, string url, TRequest request, HttpStatusCode expectedStatusCode)
    {
        var response = await client.PostAsJsonAsync(url, request);
        await response.EnsureStatusCode(expectedStatusCode);
        return response;
    }

    /// <summary>
    /// Sends a DELETE request and asserts a 204 No Content response.
    /// </summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="url">The request URL.</param>
    public static async Task DeleteAndExpectNoContentAsync(
        this HttpClient client, string url)
    {
        var response = await client.DeleteAsync(url);
        await response.EnsureStatusCode(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Sends a PUT request with a JSON body and asserts a 200 OK response.
    /// Returns the deserialized response body.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <typeparam name="TResponse">The type of the expected response body.</typeparam>
    /// <param name="client">The HTTP client.</param>
    /// <param name="url">The request URL.</param>
    /// <param name="request">The request body.</param>
    /// <returns>The deserialized response body.</returns>
    public static async Task<TResponse> PutAndExpectOkAsync<TRequest, TResponse>(
        this HttpClient client, string url, TRequest request)
    {
        var response = await client.PutAsJsonAsync(url, request);
        await response.EnsureStatusCode(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }

    /// <summary>
    /// Asserts that an <see cref="HttpResponseMessage"/> has the expected status code.
    /// Throws with a descriptive message including the response body on failure.
    /// </summary>
    private static async Task EnsureStatusCode(
        this HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Expected {(int)expected} {expected} but got {(int)response.StatusCode} {response.StatusCode}. " +
                $"Response body: {body}");
        }
    }
}
