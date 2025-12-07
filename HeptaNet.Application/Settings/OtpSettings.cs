using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeptaNet.Application.Settings
{
    public class OtpSettings
    {
        public int Length { get; set; } = 6;
        public int ExpiryMinutes { get; set; } = 10;
        public int ResendCooldownSeconds { get; set; } = 60;
        public int MaxSendsPerDay { get; set; } = 5;
        public int MaxAttempts { get; set; } = 5;
        public string Salt { get; set; } = "otp-salt-v1";
    }
}
