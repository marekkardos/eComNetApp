using System.ComponentModel.DataAnnotations;
using System.Net;
using Api.ApiResponses;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Order = Core.Entities.OrderAggregate.Order;

namespace Api.Controllers;

[ApiExplorerSettings(GroupName = "Payments")]
public class PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger, IConfiguration config) : BaseApiController
{
    private readonly string _whSecret = config.GetSection("StripeSettings:WebHookSecret").Value;

    [Authorize]
    [HttpPost("{basketId}")]
    public async Task<ActionResult<CustomerBasket>> CreateOrUpdatePaymentIntent([Required] string basketId)
    {
        var basket = await paymentService.CreateOrUpdatePaymentIntent(basketId);

        if (basket == null)
        {
            return BadRequest(new ApiResponse(HttpStatusCode.BadRequest, "Problem with your basket."));
        }

        return basket;
    }

    [HttpPost("webhook")]
    public async Task<ActionResult> StripeWebhook([FromHeader(Name = "Stripe-Signature")] string stripeSignature)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _whSecret);

        PaymentIntent intent;
        Order order;

        switch (stripeEvent.Type)
        {
            case "payment_intent.succeeded":
                intent = (PaymentIntent)stripeEvent.Data.Object;
                logger.LogInformation("Payment Succeeded: {PaymentIntentId}", intent.Id);

                order = await paymentService.UpdateOrderPaymentSucceeded(intent.Id);

                if (order != null)
                {
                    logger.LogInformation("Order updated to payment received: {OrderId}", order.Id);
                }
                else
                {
                    logger.LogInformation("Payment succeeded, order not found for PaymentIntentId: {PaymentIntentId}", intent.Id);
                }
                break;
            case "payment_intent.payment_failed":
                intent = (PaymentIntent)stripeEvent.Data.Object;

                logger.LogInformation("Payment Failed: {PaymentIntentId}, {LastPaymentError}", 
                                       intent.Id, intent.LastPaymentError?.Message);

                order = await paymentService.UpdateOrderPaymentFailed(intent.Id);

                if (order != null)
                {
                    logger.LogInformation("Payment Failed: {OrderId}", order.Id);
                }
                else
                {
                    logger.LogInformation("Payment Failed, order not found for PaymentIntentId: {PaymentIntentId}", intent.Id);
                }
                break;
        }

        return new EmptyResult();
    }
}