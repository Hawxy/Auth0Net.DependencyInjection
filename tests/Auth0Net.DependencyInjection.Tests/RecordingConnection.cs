using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Auth0.AuthenticationApi;

namespace Auth0Net.DependencyInjection.Tests;

/// <summary>
/// Records the requests sent to Auth0 and answers each with an empty response.
/// </summary>
internal sealed class RecordingConnection : IAuthenticationConnection
{
    public List<(Uri Uri, IDictionary<string, string> Body)> Requests { get; } = new();

    public Task<T> GetAsync<T>(Uri uri, IDictionary<string, string> headers = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<T> SendAsync<T>(HttpMethod method, Uri uri, object body, IDictionary<string, string> headers = null, CancellationToken cancellationToken = default)
    {
        Requests.Add((uri, (IDictionary<string, string>)body));
        return Task.FromResult(Activator.CreateInstance<T>());
    }
}
