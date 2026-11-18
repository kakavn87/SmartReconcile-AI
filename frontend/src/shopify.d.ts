export {};

declare global {
  interface Window {
    shopify?: {
      idToken: () => Promise<string>;
      config?: {
        apiKey?: string;
        shop?: string;
      };
    };
  }
}
