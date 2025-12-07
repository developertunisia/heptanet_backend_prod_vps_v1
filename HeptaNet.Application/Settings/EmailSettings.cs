using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeptaNet.Application.Settings
{
    public class EmailSettings
    {
        public string Host { get; set; } = default!;
        public int Port { get; set; } = 587;
        public string User { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string From { get; set; } = default!;
        public string FromName { get; set; } = "HeptaNET";
    }
}
