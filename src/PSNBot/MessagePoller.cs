using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PSNBot.Telegram;
using System.Net;
using System.IO;
using System.Globalization;
using System.Text;

namespace PSNBot
{
    public class MessagePoller : IDisposable
    {
        private TelegramClient _client;
        private long _offset = 0;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed = false;
        private PSNClient _psnClient;
        private AccountManager _accounts;
        private DateTime _lastCheckDateTime = DateTime.Now;

        public MessagePoller(TelegramClient client, PSNClient psnClient)
        {
            _client = client;
            _psnClient = psnClient;
            _accounts = new AccountManager();
        }

        private async Task Poll()
        {
            var updates = await _client.GetUpdates(new GetUpdatesQuery()
            {
                Offset = _offset
            });

            if (updates.Result.Any())
            {
                _offset = updates.Result.Last().UpdateId + 1;
            }

            foreach (var update in updates.Result)
            {
                if (update.Message != null && !string.IsNullOrEmpty(update.Message.Text))
                {
                    Trace.WriteLine(string.Format("{0}\t{1}\t{2}", DateTime.Now, update.Message.From.Username, update.Message.Text));
                    Handle(update.Message);
                }
            }
            

            var dt = DateTime.Now;
            if ((dt - _lastCheckDateTime).TotalSeconds > 60)
            {
                DateTime lastTimeStamp = LoadTimeStamp();

                _lastCheckDateTime = dt;
                var achievements = await _psnClient.GetAchievements(_accounts.GetAllActive());

                foreach (var ach in achievements.Where(a => a.TimeStamp > lastTimeStamp).OrderBy(a => a.TimeStamp))
                {
                    lastTimeStamp = ach.TimeStamp;

                    var account = _accounts.GetByPSN(ach.Source);
                    if (account == null)
                    {
                        continue;
                    }

                    if (!account.Chats.Any())
                    {
                        continue;
                    }

                    byte[] image = null;

                    if (!string.IsNullOrEmpty(ach.Image))
                    {
                        WebClient myWebClient = new WebClient();
                        image = myWebClient.DownloadData(ach.Image);
                    }

                    foreach (var id in account.Chats)
                    {
                        if (!string.IsNullOrEmpty(ach.Image))
                        {
                            var message = await _client.SendPhoto(new Telegram.SendPhotoQuery()
                            {
                                ChatId = id
                            }, image);
                        }

                        var msg = await _client.SendMessage(new Telegram.SendMessageQuery()
                        {
                            ChatId = id,
                            Text = ach.GetTelegramMessage(),
                            ParseMode = "HTML",
                        });
                        Thread.Sleep(1000);
                    }
                }
                SaveTimeStamp(lastTimeStamp);
            }
        }

        private void SaveTimeStamp(DateTime stamp)
        {
            using (var sw = new StreamWriter(".timestamp"))
            {
                sw.Write(stamp.ToString(CultureInfo.InvariantCulture));
                sw.Flush();
            }
        }

        private DateTime LoadTimeStamp()
        {
            if (File.Exists(".timestamp"))
            {
                using (var sr = new StreamReader(".timestamp"))
                {
                    var line = sr.ReadLine();
                    return DateTime.Parse(line, CultureInfo.InvariantCulture);
                }
            }

            return DateTime.Now;
        }

        private void Handle(Message message)
        {
            var acc = _accounts.GetById(message.From.Id);
            if (acc != null && acc.TelegramName != message.From.Username)
            {
                acc.TelegramName = message.From.Username;
                _accounts.Persist();
            }

            if (message.Text.StartsWith("/register@clankbot", StringComparison.OrdinalIgnoreCase))
            {
                var splitted = message.Text.Split(' ');
                if (splitted.Length > 1)
                {
                    if (_accounts.GetById(message.From.Id) == null)
                    {
                        if (_psnClient.SendFriendRequest(splitted[1]))
                        {
                            _accounts.Register(message.From.Id, message.From.Username, splitted[1]);
                            _client.SendMessage(new SendMessageQuery()
                            {
                                ChatId = message.Chat.Id,
                                ReplyToMessageId = message.MessageId,
                                Text = "Ты успешно зарегистрирован. Добавь меня в PSN, пожалуйста."
                            });
                        }
                        else
                        {
                            _client.SendMessage(new SendMessageQuery()
                            {
                                ChatId = message.Chat.Id,
                                ReplyToMessageId = message.MessageId,
                                Text = "Не могу найти тебя в PSN"
                            });
                        }
                    }
                    else
                    {
                        _client.SendMessage(new SendMessageQuery()
                        {
                            ChatId = message.Chat.Id,
                            ReplyToMessageId = message.MessageId,
                            Text = "Я тебя уже добавил."
                        });
                    }
                }
                else
                {
                    _client.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.Chat.Id,
                        ReplyToMessageId = message.MessageId,
                        Text = "Укажи свой идентификатор в PSN."
                    });
                }
            }

            if (message.Text.StartsWith("/start@clankbot", StringComparison.OrdinalIgnoreCase))
            {
                if (_accounts.GetById(message.From.Id) != null)
                {
                    _accounts.Start(message.From.Id, message.Chat.Id);
                    _client.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.Chat.Id,
                        ReplyToMessageId = message.MessageId,
                        Text = "Начинаю трансляцию призов."
                    });
                }
                else
                {
                    _client.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.Chat.Id,
                        ReplyToMessageId = message.MessageId,
                        Text = "Сначала зарегистрируйся."
                    });
                }
            }
            
            if (message.Text.StartsWith("/setinterests@clankbot", StringComparison.OrdinalIgnoreCase))
            {
                if (_accounts.GetById(message.From.Id) != null)
                {
                    var interests = message.Text.Remove(0, "/setinterests@clankbot".Length).Trim();
                    _accounts.SetInterests(message.From.Id, interests);
                    _client.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.Chat.Id,
                        ReplyToMessageId = message.MessageId,
                        Text = "Интересы сохранены."
                    });
                }
                else
                {
                    _client.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.Chat.Id,
                        ReplyToMessageId = message.MessageId,
                        Text = "Сначала зарегистрируйся."
                    });
                }
            }

            if (message.Text.StartsWith("/stop@clankbot", StringComparison.OrdinalIgnoreCase))
            {
                if (_accounts.GetById(message.From.Id) != null)
                {
                    _accounts.Stop(message.From.Id, message.Chat.Id);
                    _client.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.Chat.Id,
                        ReplyToMessageId = message.MessageId,
                        Text = "Останавливаю трансляцию призов."
                    });
                }
                else
                {
                    _client.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.Chat.Id,
                        ReplyToMessageId = message.MessageId,
                        Text = "Сначала зарегистрируйся."
                    });
                }
            }

            if (message.Text.StartsWith("/list@clankbot", StringComparison.OrdinalIgnoreCase))
            {
                var interests = message.Text.Remove(0, "/list@clankbot".Length).Trim();
                StringBuilder sb = new StringBuilder();
                foreach (var account in _accounts.GetAll().Where(a => string.IsNullOrEmpty(interests) 
                    || (!string.IsNullOrEmpty(a.Interests) && a.Interests.ToLower().Contains(interests.ToLower()))
                    || (!string.IsNullOrEmpty(a.TelegramName) && a.TelegramName.ToLower().Contains(interests.ToLower()))
                    || (!string.IsNullOrEmpty(a.PSNName) && a.PSNName.ToLower().Contains(interests.ToLower()))))
                {
                    sb.AppendLine(string.Format("Telegram: <b>{0}</b>\nPSN: <b>{1}</b>", account.TelegramName, account.PSNName));
                    if (!string.IsNullOrEmpty(account.Interests))
                    {
                        sb.AppendLine(string.Format("{0}", account.Interests));
                    }
                    sb.AppendLine();
                }
                if (string.IsNullOrEmpty(interests))
                {
                    _client.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.From.Id,                        
                        Text = sb.ToString(),
                        ParseMode = "HTML",
                    });
                }
                else
                {
                    _client.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.Chat.Id,
                        ReplyToMessageId = message.MessageId,
                        Text = sb.ToString(),
                        ParseMode = "HTML",
                    });
                }
            }
        }

        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            Task.Factory.StartNew(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var task = Poll();
                    task.Wait();
                }
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // ...
                }

                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _disposed = true;
            }
        }

        ~MessagePoller()
        {
            Dispose(false);
        }
    }
}
