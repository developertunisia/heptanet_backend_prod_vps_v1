public class SendOtpRequestDto
{
    public string Email { get; set; } = default!;
    public string? Purpose { get; set; } = "register";
}

public class VerifyOtpRequestDto
{
    public string Email { get; set; } = default!;
    public string? Purpose { get; set; } = "register";
    public string Code { get; set; } = default!;
}