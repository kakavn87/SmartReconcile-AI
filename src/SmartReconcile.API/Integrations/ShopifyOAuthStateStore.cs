using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace SmartReconcile.API.Integrations;

public sealed class ShopifyOAuthStateStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> states = new();

    public string Create()
    {
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        states[state] = DateTimeOffset.UtcNow.AddMinutes(10);
        return state;
    }

    public bool Consume(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return false;

        if (!states.TryRemove(state, out var expiresAt))
            return false;

        return expiresAt > DateTimeOffset.UtcNow;
    }
}
