# Local Stripe Development Guide

This guide explains how to set up and test Stripe payment integration during local development.

## Prerequisites

### Install Stripe CLI

**Windows (using Scoop):**
```bash
scoop bucket add stripe https://github.com/stripe/scoop-stripe-cli.git
scoop install stripe
```

**Or download directly:** https://github.com/stripe/stripe-cli/releases

## Setup Steps

### 1. Authenticate with Stripe

```bash
stripe login
```

This opens your browser to authenticate with your Stripe account.

**Important:** Authenticate to the **same Stripe account** whose API keys are configured in your application. Mismatched accounts are the most common cause of webhook issues.

### 2. Configure Application API Keys

Ensure your application uses the correct Stripe API keys from the same account:

**Backend (API):**

```bash
# Via user secrets (recommended for development)
dotnet user-secrets set "StripeSettings:SecretKey" "sk_test_xxxxx"

# Or add to appsettings.Development.json
```

> **Note:** User secrets are stored at `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json` (macOS/Linux) or `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` (Windows). The UserSecretsId is `56ee6ee3-b7d8-4759-ab17-87297accef46` (defined in `Api/Api.csproj` and `docker-compose.dcproj`).
>
> **For Docker:** Copy `docker-compose.override.example.yml` to `docker-compose.override.yml` and uncomment the path for your platform to mount user secrets into the container.

**Frontend (Angular):**

Configure the publishable key in `client/src/environments/environment.ts` or appropriate environment file:

```typescript
export const environment = {
  // ... other config
  stripePublishableKey: 'pk_test_xxxxx'
};
```

**Important:** Use **test mode keys** (starting with `pk_test_` and `sk_test_`) for local development.

### 3. Start Webhook Listener

```bash
stripe listen --forward-to http://localhost:44369/api/payments/webhook
```

This command:
- Opens a connection to Stripe's servers from your machine
- Creates a temporary webhook endpoint on Stripe
- Forwards webhook events to your local API
- Outputs a webhook signing secret: `whsec_xxxxx`

The Stripe CLI acts as a reverse proxy - your machine connects to Stripe, and Stripe sends events back through that connection. This means your localhost doesn't need to be publicly accessible.

### 4. Configure Webhook Secret

Copy the webhook secret from the CLI output and add it to your configuration:

```bash
dotnet user-secrets set "StripeSettings:WebHookSecret" "whsec_xxxxx"
```

Or add to `appsettings.Development.json`:

```json
{
  "StripeSettings": {
    "WebHookSecret": "whsec_xxxxx"
  }
}
```

**Note:** The webhook secret changes each time you run `stripe listen`, so you'll need to update it in each development session.

### 5. Restart Your API

After configuring the webhook secret, restart your API for changes to take effect:

```bash
dotnet run --project Api/Api.csproj
```

## Testing Payment Flow

### Complete End-to-End Test

This is the recommended way to test the full payment integration:

```bash
# Terminal 1: Start webhook listener FIRST
stripe listen --forward-to http://localhost:44369/api/payments/webhook

# Terminal 2: Start API
dotnet run --project Api/Api.csproj

# Terminal 3: Start Angular app
cd client
npm start
```

### Test Payment in Browser

1. Navigate to http://localhost:4200
2. Add items to basket and proceed to checkout
3. Use Stripe test cards (see [Stripe Testing Documentation](https://docs.stripe.com/testing?testing-method=card-numbers))
   - **Success:** 4242 4242 4242 4242
   - **Decline:** 4000 0000 0000 0002
   - Expiry: Any future date (e.g., 12/34)
   - CVC: Any 3 digits (e.g., 123)
   - ZIP: Any 5 digits
4. Complete payment

### Monitor Webhooks

In Terminal 1 (webhook listener), you should see:

```
> payment_intent.succeeded [evt_xxxxx]
```

Check your API logs to confirm the webhook was received and the order status was updated to `PaymentReceived`.

### Understanding the Payment Flow

1. **User initiates checkout** → API creates Payment Intent via `POST /api/payments/{basketId}`
2. **API returns ClientSecret** → Angular uses it to confirm payment with Stripe.js
3. **User completes payment** → Stripe processes the payment
4. **Stripe sends webhook** → Your local listener forwards it to `POST /api/payments/webhook`
5. **API updates order status** → Order marked as `PaymentReceived` or `PaymentFailed`

## Quick Event Testing

To test webhook endpoint connectivity without a full payment:

```bash
stripe trigger payment_intent.succeeded
```

**Important:** This generates random test data and won't match any orders in your database. It's only useful for verifying:
- ✅ Webhook endpoint is reachable
- ✅ Signature verification works
- ✅ Event parsing is correct

You'll see a warning in your logs: `Order not found for PaymentIntent: pi_xxxxx` - this is expected.

## Troubleshooting

### Events Not Reaching CLI

**Symptoms:** You complete a payment but don't see webhook events in the Stripe CLI terminal.

**Solutions:**

1. **Start listener BEFORE creating payments**
   - The CLI only receives events that occur AFTER it starts listening
   - Old events visible in Stripe Dashboard won't be forwarded

2. **Verify account matching**
   ```bash
   # Check which Stripe account CLI is using
   stripe config --list
   ```
   - Compare with the API keys in your application
   - They must be from the **same Stripe account**

3. **Use test mode keys**
   - Keys should start with `pk_test_` and `sk_test_`
   - Webhook secret should start with `whsec_test_`

4. **Check firewall/proxy**
   - The CLI needs outbound HTTPS access to Stripe's servers
   - Corporate firewalls or VPNs may block the connection

### Webhook Signature Verification Fails

**Symptoms:** API returns 400 or logs "Invalid signature"

**Solutions:**

1. **Update webhook secret**
   - The secret changes each time you run `stripe listen`
   - Copy the new secret and update your configuration

2. **Restart API**
   - Configuration changes require an API restart

3. **Check the secret in logs**
   ```bash
   stripe listen --forward-to http://localhost:44369/api/payments/webhook --print-json
   ```

### Payment Succeeds But Order Not Updated

**Symptoms:** Payment completes but order status stays `Pending`

**Solutions:**

1. **Verify PaymentIntentId linkage**
   - The order must have the same `PaymentIntentId` as the payment
   - Check database: `SELECT PaymentIntentId FROM Orders WHERE Id = {orderId}`

2. **Check API logs**
   - Look for: `Order not found for PaymentIntent: pi_xxxxx`
   - This means the order wasn't created or doesn't have the correct PaymentIntentId

3. **Ensure order creation happens before payment**
   - Create order → Order gets PaymentIntentId from basket → Complete payment → Webhook updates order

## Production Configuration

For production, webhooks are configured differently:

### Stripe Dashboard Setup

1. Go to **https://dashboard.stripe.com**
2. Navigate to **Developers** → **Webhooks**
3. Click **"Add endpoint"**
4. Enter your production URL:
   ```
   https://your-production-domain.com/api/payments/webhook
   ```
5. Select events to listen to:
   - `payment_intent.succeeded`
   - `payment_intent.payment_failed`
6. Copy the **Signing Secret** (starts with `whsec_`)
7. Add to production configuration (Azure Key Vault, environment variables, etc.)

### Environment Differences

| Environment | Webhook Configuration | Secret Source |
|-------------|----------------------|---------------|
| **Local Development** | Stripe CLI (`stripe listen`) | CLI output (temporary) |
| **Test/Staging** | Stripe Dashboard (test mode) | Dashboard webhook settings |
| **Production** | Stripe Dashboard (live mode) | Dashboard webhook settings |

### Key Points

- **Different secrets per environment:** Each environment has its own webhook secret
- **Production requires public URL:** Stripe must be able to reach your endpoint over HTTPS
- **Test vs Live mode:** Use test mode for all non-production environments
- **Account matching is critical:** API keys and webhook configuration must be from the same Stripe account

## Quick Reference

### Common Commands

```bash
# Authenticate
stripe login

# Check account
stripe config --list

# Start webhook forwarding
stripe listen --forward-to http://localhost:44369/api/payments/webhook

# With verbose output
stripe listen --forward-to http://localhost:44369/api/payments/webhook --print-json

# Trigger test event
stripe trigger payment_intent.succeeded
```

### Configuration Files

| Setting | Location | Example Value |
|---------|----------|---------------|
| **SecretKey** | User secrets or appsettings | `sk_test_xxxxx` |
| **PublishableKey** | Angular environment file | `pk_test_xxxxx` |
| **WebHookSecret** | User secrets or appsettings | `whsec_xxxxx` |

### Test Cards

See full list: https://docs.stripe.com/testing?testing-method=card-numbers

| Card Number | Scenario |
|-------------|----------|
| 4242 4242 4242 4242 | Success |
| 4000 0000 0000 0002 | Card declined |
| 4000 0000 0000 9995 | Insufficient funds |

## Additional Resources

- [Stripe CLI Documentation](https://stripe.com/docs/stripe-cli)
- [Stripe Testing Guide](https://docs.stripe.com/testing)
- [Stripe Webhooks Guide](https://stripe.com/docs/webhooks)
- [Payment Intents API](https://stripe.com/docs/api/payment_intents)
