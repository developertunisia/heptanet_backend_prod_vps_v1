using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeptaNet.Domain.Models
{
    public class OtpCode
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = default!;
        public string Purpose { get; set; } = "register";
        public string CodeHash { get; set; } = default!;
        public DateTime ExpiresAtUtc { get; set; }
        public bool IsUsed { get; set; }
        public int AttemptCount { get; set; }
        public int MaxAttempts { get; set; }
        public int SentCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? LastSentAtUtc { get; set; }
    }
}
