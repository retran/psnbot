using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSNBot.Model
{
    public class Account
    {
        public long Id { get; set; }
        public string TelegramName { get; set; }
        public string PSNName { get; set; }
        public string Interests { get; set; }
        public bool ShowTrophies { get; set; }
        public long Status { get; set; }
    }
}
