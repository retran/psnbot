using PSNBot.Telegram;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PSNBot.Commands
{
    public abstract class Command
    {
        public virtual bool IsPrivateOnly()
        {
            return false;
        }

        public abstract bool IsApplicable(Message message);
        public abstract Task<bool> Handle(Message message);
    }
}
