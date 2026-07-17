using System.Text.Json;
using M1.Application.Interfaces;
using M1.Domain;
using M1.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace M1.Api.Controllers;

/// <summary>
/// Provider webhooks (anonymous by design — providers can't send JWTs).
/// Both handlers locate the pending payment by provider reference and mark
/// the order paid. Idempotent: repeated callbacks are no-ops.
/// </summary>
[ApiController]
[Route("api/v1/payments")]
public class PaymentsController(
    IRepository<Payment> payments,
    IRepository<Order> orders,
    IRepository<Notification> notifications,
    IUnitOfWork uow,
    ILogger<PaymentsController> logger) : ControllerBase
{
    [HttpPost("mpesa/callback")]
    public async Task<IActionResult> MpesaCallback([FromBody] JsonElement body, CancellationToken ct)
    {
        try
        {
            var callback = body.GetProperty("Body").GetProperty("stkCallback");
            var checkoutId = callback.GetProperty("CheckoutRequestID").GetString();
            var resultCode = callback.GetProperty("ResultCode").GetInt32();
            await SettleAsync(checkoutId, resultCode == 0, ct);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Malformed M-Pesa callback");
        }
        // Daraja expects a success envelope regardless.
        return Ok(new { ResultCode = 0, ResultDesc = "Accepted" });
    }

    [HttpPost("stripe/webhook")]
    public async Task<IActionResult> StripeWebhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(ct);
        try
        {
            var json = JsonDocument.Parse(payload).RootElement;
            if (json.GetProperty("type").GetString() == "checkout.session.completed")
            {
                var sessionId = json.GetProperty("data").GetProperty("object").GetProperty("id").GetString();
                await SettleAsync(sessionId, succeeded: true, ct);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Malformed Stripe webhook");
        }
        return Ok();
    }

    private async Task SettleAsync(string? providerRef, bool succeeded, CancellationToken ct)
    {
        if (providerRef is null) return;
        var payment = await payments.FirstOrDefaultAsync(
            p => p.ProviderRef == providerRef && p.Status == PaymentStatus.Pending, ct);
        if (payment is null) return;

        payment.Status = succeeded ? PaymentStatus.Succeeded : PaymentStatus.Failed;

        var order = await orders.GetByIdAsync(payment.OrderId, ct);
        if (succeeded && order is { Status: OrderStatus.PendingPayment })
        {
            order.Status = OrderStatus.Paid;
            notifications.Add(new Notification
            {
                UserId = order.UserId,
                Type = NotificationType.OrderUpdate,
                Title = $"Payment received for {order.OrderNumber} ✅",
                Body = $"KES {order.Total:N0} confirmed. We're preparing your order."
            });
        }

        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Payment {Ref} settled: {Status}", providerRef, payment.Status);
    }
}
