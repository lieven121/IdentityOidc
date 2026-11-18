using OpenIddict.Abstractions;

namespace Identity.App.Services;

/// <summary>
/// Origins of the redirect URIs of every application in the store, for the OIDC CORS policy.
/// Cached; call <see cref="Invalidate"/> after an application is created, changed or deleted.
/// </summary>
[RegisterSingleton]
public class ApplicationOrigins(IServiceScopeFactory scopeFactory)
{
    private volatile IReadOnlySet<string>? origins;

    public async ValueTask<IReadOnlySet<string>> GetAsync(CancellationToken cancellationToken = default)
    {
        var cached = origins;
        if (cached != null)
            return cached;

        using var scope = scopeFactory.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await foreach (var app in manager.ListAsync(cancellationToken: cancellationToken))
        {
            foreach (var uri in await manager.GetRedirectUrisAsync(app, cancellationToken))
            {
                if (TryGetOrigin(uri, out var origin))
                    result.Add(origin);
            }
        }

        origins = result;
        return result;
    }

    public void Invalidate() => origins = null;

    public static bool TryGetOrigin(string uri, out string origin)
    {
        origin = "";
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed) || parsed.Scheme is not ("http" or "https"))
            return false;

        origin = parsed.GetLeftPart(UriPartial.Authority);
        return true;
    }
}
