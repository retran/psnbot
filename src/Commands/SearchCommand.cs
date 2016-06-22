using PSNBot.Services;
using PSNBot.Telegram;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace PSNBot.Commands
{
    public class SearchCommand : Command
    {
        private Regex _regex;
        private PSNService _psnService;
        private TelegramClient _telegramClient;
        private AccountService _accounts;

        public SearchCommand(PSNService psnService, TelegramClient telegramClient, AccountService accounts)
        {
            _psnService = psnService;
            _telegramClient = telegramClient;
            _accounts = accounts;
            _regex = new Regex("/search(\\s+(?<param>.+))?", RegexOptions.IgnoreCase);
        }

        public override bool IsApplicable(Message message)
        {
            return _regex.IsMatch(message.Text.Trim());
        }

        public override async Task<bool> Handle(Message message)
        {
            var match = _regex.Match(message.Text.Trim());
            if (!match.Groups["param"].Success)
            {
                await _telegramClient.SendMessage(new SendMessageQuery()
                {
                    ChatId = message.Chat.Id,
                    ReplyToMessageId = message.MessageId,
                    Text = "Пожалуйста, укажи что ты хочешь найти. Например: /search@clankbot Dark Souls",
                    ParseMode = "HTML",
                });
                return false; 
            }

            var text = match.Groups["param"].Value;
            var filtered = _accounts.Search(text);
            var lines = filtered.AsParallel().Select(async account =>
            {
                var builder = new StringBuilder();
                builder.AppendLine(string.Format("Telegram: <b>{0}</b>\nPSN: <b>{1}</b>", account.TelegramName, account.PSNName));
                var userEntry = await _psnService.GetUser(account.PSNName);
                var status = userEntry.GetStatus();
                if (!string.IsNullOrEmpty(status))
                {
                    builder.AppendLine(string.Format("{0}", status));
                }
                if (!string.IsNullOrEmpty(account.Interests))
                {
                    builder.AppendLine(string.Format("{0}", account.Interests));
                }
                builder.AppendLine();
                return builder.ToString();
            }).ToArray();

            Task.WaitAll(lines);

            StringBuilder sb = new StringBuilder();
            foreach (var line in lines)
            {
                if (sb.Length + line.Result.Length < 4096)
                {
                    sb.Append(line.Result);
                }
                else
                {
                    await _telegramClient.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.Chat.Id,
                        ReplyToMessageId = message.MessageId,
                        Text = sb.ToString(),
                        ParseMode = "HTML",
                    });
                    sb.Clear();
                    sb.Append(line.Result);
                }
            }

            await _telegramClient.SendMessage(new SendMessageQuery()
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
