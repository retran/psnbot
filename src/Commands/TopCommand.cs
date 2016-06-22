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
    public class TopCommand : Command
    {
        private Regex _regex;
        private PSNService _psnService;
        private TelegramClient _telegramClient;
        private AccountService _accounts;

        public TopCommand(PSNService psnService, TelegramClient telegramClient, AccountService accounts)
        {
            _psnService = psnService;
            _telegramClient = telegramClient;
            _accounts = accounts;
            _regex = new Regex("/top@clankbot", RegexOptions.IgnoreCase);
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
                    Rating = user.GetRating(),
                    ThrophyLine = user.GetTrophyLine()
                };
            }).ToArray());

            var table = entries.Where(t => t != null)
                .OrderByDescending(t => t.Rating).Take(20);

            StringBuilder sb = new StringBuilder();
            int i = 1;
            foreach (var t in table)
            {
                sb.AppendLine(string.Format("{0}. {1} {2}", i, !string.IsNullOrEmpty(t.TelegramName) ? t.TelegramName : t.PSNName, t.ThrophyLine));
                i++;
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
