using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using M1.Application.Common;
using M1.Application.Interfaces;
using M1.Domain;
using M1.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace M1.Infrastructure.Payments;

/// <summary>
/// Demo gateway: instantly succeeds. Lets the full checkout → paid → fulfilment
/// flow run end-to-end without payment provider accounts. Selected by default
/// in the demo deployment.
/// </summary>
public class FakePaymentGateway : IPaymentGateway
{
    public PaymentProvider Provider => PaymentProvider.Fake;

    public Task<PaymentInitiation> InitiateAsync(Order order, string? phone, CancellationToken ct = default) =>
        Task.FromResult(new PaymentInitiation($"FAKE-{Guid.CreateVersion7():N}", null, AutoSucceeded: true));
}

/// <summary>
/// M-Pesa Daraja STK Push (sandbox or production, depending on config).
/// Requires Mpesa:ConsumerKey/ConsumerSecret/ShortCode/Passkey/CallbackUrl.
/// </summary>
public class MpesaPaymentGateway(IHttpClientFactory httpFactory, IConfiguration config, ILogger<MpesaPaymentGateway> logger)
    : IPaymentGateway
{
    public PaymentProvider Provider => PaymentProvider.Mpesa;

    public async Task<PaymentInitiation> InitiateAsync(Order order, string? phone, CancellationToken ct = default)
    {
        var key = config["Mpesa:ConsumerKey"];
        var secret = config["Mpesa:ConsumerSecret"];
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(secret))
            throw new DomainRuleException("M-Pesa payments are not configured yet — please choose another method.");
        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainRuleException("An M-Pesa phone number is required.");

        var baseUrl = config["Mpesa:BaseUrl"] ?? "https://sandbox.safaricom.co.ke";
        var client = httpFactory.CreateClient("mpesa");

        // 1. OAuth token
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Get,
            $"{baseUrl}/oauth/v1/generate?grant_type=client_credentials");
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{key}:{secret}")));
        var tokenResponse = await client.SendAsync(tokenRequest, ct);
        tokenResponse.EnsureSuccessStatusCode();
        var token = (await tokenResponse.Content.ReadFromJsonAsync<MpesaToken>(ct))?.access_token
            ?? throw new DomainRuleException("M-Pesa authentication failed.");

        // 2. STK push
        var shortCode = config["Mpesa:ShortCode"] ?? "174379";
        var passkey = config["Mpesa:Passkey"] ?? "";
        var timestamp = DateTime.UtcNow.AddHours(3).ToString("yyyyMMddHHmmss"); // EAT
        var password = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{shortCode}{passkey}{timestamp}"));
        var normalizedPhone = "254" + phone.TrimStart('+').TrimStart('0').TrimStart('2', '5', '4').PadLeft(9, '0');

        using var stkRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/mpesa/stkpush/v1/processrequest");
        stkRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        stkRequest.Content = JsonContent.Create(new
        {
            BusinessShortCode = shortCode,
            Password = password,
            Timestamp = timestamp,
            TransactionType = "CustomerPayBillOnline",
            Amount = (int)Math.Ceiling(order.Total),
            PartyA = normalizedPhone,
            PartyB = shortCode,
            PhoneNumber = normalizedPhone,
            CallBackURL = config["Mpesa:CallbackUrl"],
            AccountReference = order.OrderNumber,
            TransactionDesc = $"M1 Stores {order.OrderNumber}"
        });

        var stkResponse = await client.SendAsync(stkRequest, ct);
        var body = await stkResponse.Content.ReadFromJsonAsync<MpesaStkResponse>(ct);
        logger.LogInformation("Mpesa STK push for {Order}: {Code}", order.OrderNumber, body?.ResponseCode);

        if (body?.CheckoutRequestID is null)
            throw new DomainRuleException("M-Pesa did not accept the payment request. Try again.");

        return new PaymentInitiation(body.CheckoutRequestID, null, AutoSucceeded: false);
    }

    private record MpesaToken(string access_token);
    private record MpesaStkResponse(string? ResponseCode, string? CheckoutRequestID);
}

/// <summary>
/// Stripe Checkout Session via REST (test mode). Requires Stripe:SecretKey.
/// </summary>
public class StripePaymentGateway(IHttpClientFactory httpFactory, IConfiguration config, IAppUrls urls)
    : IPaymentGateway
{
    public PaymentProvider Provider => PaymentProvider.Stripe;

    public async Task<PaymentInitiation> InitiateAsync(Order order, string? phone, CancellationToken ct = default)
    {
        var secretKey = config["Stripe:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
            throw new DomainRuleException("Card payments are not configured yet — please choose another method.");

        var client = httpFactory.CreateClient("stripe");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/checkout/sessions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["mode"] = "payment",
            ["success_url"] = $"{urls.FrontendUrl}/orders/{order.OrderNumber}?paid=1",
            ["cancel_url"] = $"{urls.FrontendUrl}/orders/{order.OrderNumber}",
            ["client_reference_id"] = order.OrderNumber,
            ["line_items[0][price_data][currency]"] = "kes",
            ["line_items[0][price_data][product_data][name]"] = $"M1 Stores order {order.OrderNumber}",
            ["line_items[0][price_data][unit_amount]"] = ((long)(order.Total * 100)).ToString(),
            ["line_items[0][quantity]"] = "1"
        });

        var response = await client.SendAsync(request, ct);
        var session = await response.Content.ReadFromJsonAsync<StripeSession>(ct);
        if (session?.url is null)
            throw new DomainRuleException("Stripe did not accept the payment request. Try again.");

        return new PaymentInitiation(session.id ?? order.OrderNumber, session.url, AutoSucceeded: false);
    }

    private record StripeSession(string? id, string? url);
}
