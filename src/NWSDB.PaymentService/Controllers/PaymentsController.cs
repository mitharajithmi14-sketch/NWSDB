using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NWSDB.PaymentService.Services;
using NWSDB.Shared;
using NWSDB.Shared.Dtos;

namespace NWSDB.PaymentService.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentProcessor _processor;
    private readonly IPaymentRepository _payments;

    public PaymentsController(IPaymentProcessor processor, IPaymentRepository payments)
    {
        _processor = processor;
        _payments = payments;
    }

    /// <summary>Pays the current bill of an account and returns a receipt number.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentReceiptDto), 200)]
    [ProducesResponseType(typeof(MessageResponse), 404)]
    [ProducesResponseType(typeof(MessageResponse), 409)]
    public async Task<IActionResult> Pay(PaymentRequestDto request)
    {
        if (!User.CanAccess(request.AccountNo)) return Forbid();

        var result = await _processor.PayAsync(request, Request.BearerToken());
        return result.Receipt is not null
            ? Ok(result.Receipt)
            : StatusCode(result.StatusCode, new MessageResponse(result.Error!));
    }

    /// <summary>Payment history for an account.</summary>
    [HttpGet("{accountNo}")]
    public async Task<ActionResult<IEnumerable<PaymentReceiptDto>>> GetByAccount(string accountNo)
    {
        if (!User.CanAccess(accountNo)) return Forbid();
        var list = await _payments.GetByAccountAsync(accountNo);
        return Ok(list.Select(p => new PaymentReceiptDto(p.ReceiptNo, p.AccountNo, p.BillId, p.Amount, p.Channel, p.PaidAt)));
    }
}
