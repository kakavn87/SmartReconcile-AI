# Render Static Site — Shopify embedding

The Angular frontend is deployed as a Render Static Site, so the Shopify embedding policy must be sent as an HTTP response header.

Add this custom header to the Render service **smartreconcile-ai-frontend** for path `/*`:

- Header: `Content-Security-Policy`
- Value: `frame-ancestors https://*.myshopify.com https://shopify.com;`

Do **not** add `X-Frame-Options: DENY` to this frontend, because that would prevent Shopify Admin from embedding the app.

Render Dashboard path:

1. Open **smartreconcile-ai-frontend**.
2. Open **Settings → Headers**.
3. Add the rule above for `/*`.
4. Save and redeploy if Render asks for a deploy.

The same rule can be represented in a Render Blueprint as:

```yaml
headers:
  - path: /*
    name: Content-Security-Policy
    value: "frame-ancestors https://*.myshopify.com https://shopify.com;"
```

The current service is already configured as a Static Site outside a repository Blueprint, so the Dashboard header rule is the authoritative configuration for the existing live service.
