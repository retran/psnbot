using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PSNBot.Telegram;
using PSNBot.Services;

namespace PSNBot.Commands
{
    public class OnlineCommand : Command
    {
        private Regex _regex;
        private PSNService _psnService;
        private TelegramClient _telegramClient;
        private AccountService _accounts;

        public OnlineCommand(PSNService psnService, TelegramClient telegramClient, AccountService accounts)
        {
            _psnService = psnService;
            _telegramClient = telegramClient;
            _accounts = accounts;
            _regex = new Regex("^/online", RegexOptions.IgnoreCase);
        }

        public override bool IsApplicable(Message message)
        {
            return _regex.IsMatch(message.Text.Trim());
        }

        public override async Task<bool> Handle(Message message)
        {
            var entries = await Task.WhenAll(_accounts.GetAll().AsParallel().Select(async a =>
            {
                var user = await _psnService.GetUser(a.PSNName);
                if (user == null)
                {
                    return null;
                }
                return new
                {
                    TelegramName = a.TelegramName,
                    PSNName = a.PSNName,
                    Online = user.GetOnline(),
                    StatusLine = user.GetStatusLine(),
                    HasPlus = user.HasPlus()
                };
            }).ToArray());

            var table = entries.Where(t => t != null && t.Online)
                .OrderBy(t => t.TelegramName).ToList();

            StringBuilder sb = new StringBuilder();

            foreach (var entry in table)
            {
                var name = entry.TelegramName;
                if (!string.IsNullOrEmpty(name))
                {
                    name = name + " (" + entry.PSNName + ")";
                }
                else
                {
                    name = entry.PSNName;
                }

                if (entry.HasPlus)
                {
                    sb.Append("PS+ ");
                }

                sb.Append(name);
                if (!string.IsNullOrEmpty(entry.StatusLine))
                {
                    sb.Append("\n" + entry.StatusLine);
                }

                sb.Append("\n\n");
            }

            var response = await _telegramClient.SendMessage(new SendMessageQuery()
            {
                ChatId = message.Chat.Id,
                ReplyToMessageId = message.MessageId,
                Text = sb.ToString(),
                ParseMode = "HTML",
            });

            return true;
        }
    }
}
