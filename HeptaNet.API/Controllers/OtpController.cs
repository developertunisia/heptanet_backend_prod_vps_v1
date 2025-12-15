using HeptaNet.Application.Services.Otp;
using HeptaNet.Application.Services;
using HeptaNet.Domain.Request;
using HeptaNet.Domain.Dto;
using Microsoft.AspNetCore.Mvc;

namespace HeptaNet.API.Controllers;

[ApiController]
[Route("api/auth/otp")]
public class OtpController : ControllerBase
{
    private readonly IOtpService _otp;
    private readonly IAuthorizedEmailService _authorized;

    public OtpController(IOtpService otp, IAuthorizedEmailService authorized)
    { _otp = otp; _authorized = authorized; }

    [HttpPost("send")]
    public async Task<ActionResult<SendOtpResponseDto>> Send([FromBody] SendOtpRequestDto dto, CancellationToken ct)
    {
       // var check = await _authorized.CheckEmailExistsAsync(new CheckEmailExistsRequest { Email = dto.Email });
       // if (check is null || !check.Exists)
           // return BadRequest(new SendOtpResponseDto { Status = "error", Message = "Email non autorisé. Cet email n'est pas dans la liste des emails autorisés." });

        var res = await _otp.SendAsync(dto.Email, dto.Purpose ?? "register", ct);
        return Ok(new SendOtpResponseDto
        {
            Status = res.Success ? "sent" : (res.IsCooldown ? "cooldown" : "limit"),
            CooldownSeconds = res.CooldownSeconds,
            NextAllowedAtUtc = res.NextAllowedAtUtc,
            Message = res.Message
        });
    }

    [HttpPost("verify")]
    public async Task<ActionResult<VerifyOtpResponseDto>> Verify([FromBody] VerifyOtpRequestDto dto, CancellationToken ct)
    {
        var res = await _otp.VerifyAsync(dto.Email, dto.Purpose ?? "register", dto.Code, ct);
        if (!res.Valid)
            return Unauthorized(new VerifyOtpResponseDto { Valid = false, Expired = res.Expired, RemainingAttempts = res.RemainingAttempts, Message = res.Message });

        return Ok(new VerifyOtpResponseDto { Valid = true, Expired = false, RemainingAttempts = res.RemainingAttempts });
    }

    [HttpGet("status")]
    public async Task<ActionResult<OtpStatusResponseDto>> Status([FromQuery] OtpStatusQueryDto query, CancellationToken ct)
    {
        var res = await _otp.GetStatusAsync(query.Email, query.Purpose ?? "register", ct);
        return Ok(new OtpStatusResponseDto
        {
            CanResend = res.CanResend,
            CooldownSeconds = res.CooldownSeconds,
            NextAllowedAtUtc = res.NextAllowedAtUtc,
            RemainingAttempts = res.RemainingAttempts,
            ExpiresAtUtc = res.ExpiresAtUtc
        });
    }
}