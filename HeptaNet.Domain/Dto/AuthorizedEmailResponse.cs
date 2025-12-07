using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeptaNet.Domain.Dto
{

    public class AuthorizedEmailResponseDto
    {
        public int SyncId { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsImported { get; set; }
        public DateTime? SyncDate { get; set; }
    }

    public class AuthorizedEmailCheckResponseDto
    {
        public bool Exists { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    public class AuthorizedEmailStatusUpdateResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsImported { get; set; }
    }

}
