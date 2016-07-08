using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSNBot.Model
{
    public static class Status
    {
        public const long AwaitingPSNName = 0;
        public const long AwaitingFriendRequest = 1;
        public const long AwaitingInterests = 2;
        public const long AwaitingTrophies = 3;

        public const long Ok = 1024;

        internal const long AwaitingNewTrophies = 2001;
        public const long AwaitingNewInterests = 2002;
        public const long AwaitingDeleteConfirmation = 2003;
    }
        public class Account
    {
        public long Id { get; set; }
        public string TelegramName { get; set; }
        public string PSNName { get; set; }
        public string Interests { get; set; }
        public bool ShowTrophies { get; set; }
        public long Status { get; set; }
        public DateTime RegisteredAt { get; set; }
    }
}
