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
            _regex = new Regex("^/top", RegexOptions.IgnoreCase);
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
                    Id = a.Id,
                    TelegramName = a.TelegramName,
                    PSNName = a.PSNName,
                    Rating = user.GetRating(),
                    ThrophyLine = user.GetTrophyLine()
                };
            }).ToArray());

            var table = entries.Where(t => t != null)
                .OrderByDescending(t => t.Rating).ToList();

            bool currentUserInList = false;
            StringBuilder sb = new StringBuilder();
            int i = 1;

            if (table.Any())
            {
                foreach (var t in table.Take(10))
                {
                    if (t.Id == message.From.Id)
                    {
                        currentUserInList = true;
                        sb.AppendLine(string.Format("<b>{0}. {1}</b>", i, !string.IsNullOrEmpty(t.TelegramName) ? t.TelegramName : t.PSNName));
                    }
                    else
                    {
                        sb.AppendLine(string.Format("{0}. {1}", i, !string.IsNullOrEmpty(t.TelegramName) ? t.TelegramName : t.PSNName));
                    }
                    i++;
                }

                if (!currentUserInList)
                {
                    i = table.FindIndex(t => t.Id == message.From.Id);
                    if (i > 0 && i < table.Count())
                    {
                        var currentUser = table[i];
                        sb.AppendLine("...");
                        sb.AppendLine(string.Format("<b>{0}. {1}</b>", i + 1, !string.IsNullOrEmpty(currentUser.TelegramName) ? currentUser.TelegramName : currentUser.PSNName));
                        if (i + 1 != table.Count)
                        {
                            sb.AppendLine("...");
                        }
                    }
                }


                var response = await _telegramClient.SendMessage(new SendMessageQuery()
                {
                    ChatId = message.Chat.Id,
                    ReplyToMessageId = message.MessageId,
                    Text = sb.ToString(),
                    ParseMode = "HTML",
                });
            }
            return true;
        }
    }
}
