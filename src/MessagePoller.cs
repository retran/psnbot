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
using System.Collections.Generic;
using PsnLib.Entities;
using PSNBot.Services;

namespace PSNBot
{
    public class MessagePoller : IDisposable
    {
        private TelegramClient _client;
        private long _offset = 0;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed = false;
        private PSNService _psnService;
        private AccountManager _accounts;
        private DateTime _lastCheckDateTime = DateTime.Now;

        public MessagePoller(TelegramClient client, PSNService psnService)
        {
            _client = client;
            _psnService = psnService;
            _accounts = new AccountManager();
        }

        private async Task PollTelegram()
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
        }

        private async Task Poll()
        {
            await PollTelegram();

            try
            {
                var dt = DateTime.Now;
                if ((dt - _lastCheckDateTime).TotalSeconds > 60)
                {
                    DateTime lastPhotoTimeStamp = LoadTimeStamp(".phototimestamp");
                    var msgs = (await _psnService.GetMessages(lastPhotoTimeStamp)).OrderBy(m => m.TimeStamp);
                    foreach (var msg in msgs)
                    {
                        var account = _accounts.GetByPSN(msg.Source);

                        if (account != null)
                        {
                            foreach (var id in account.Chats)
                            {
                                var tlgMsg = await _client.SendMessage(new Telegram.SendMessageQuery()
                                {
                                    ChatId = id,
                                    Text = string.Format("Пользователь <b>{0} ({1})</b> опубликовал изображение:", account.PSNName, account.TelegramName),
                                    ParseMode = "HTML",
                                });

                                var message = await _client.SendPhoto(new Telegram.SendPhotoQuery()
                                {
                                    ChatId = id
                                }, msg.Data);

                                Thread.Sleep(1000);
                            }
                            lastPhotoTimeStamp = msg.TimeStamp;
                        }
                    }
                    SaveTimeStamp(lastPhotoTimeStamp, ".phototimestamp");

                    DateTime lastTimeStamp = LoadTimeStamp(".timestamp");
                    _lastCheckDateTime = dt;
                    var trophies = await _psnService.GetTrophies(_accounts.GetAllActive());
                    foreach (var ach in trophies.Where(a => a.TimeStamp > lastTimeStamp).OrderBy(a => a.TimeStamp))
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
                    SaveTimeStamp(lastTimeStamp, ".timestamp");
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(string.Format("{0}\t{1}", DateTime.Now, e.Message));
            }
        }

        private void SaveTimeStamp(DateTime stamp, string filename)
        {
            using (var sw = new StreamWriter(filename))
            {
                sw.Write(stamp.ToString(CultureInfo.InvariantCulture));
                sw.Flush();
            }
        }

        private DateTime LoadTimeStamp(string filename)
        {
            if (File.Exists(filename))
            {
                using (var sr = new StreamReader(filename))
                {
                    var line = sr.ReadLine();
                    return DateTime.Parse(line, CultureInfo.InvariantCulture);
                }
            }

            return DateTime.UtcNow;
        }

        private async void Handle(Message message)
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
                    if (_accounts.GetById(message.From.Id) == null && _accounts.GetByPSN(splitted[1].Trim()) == null)
                    {
                        var success = await _psnService.SendFriendRequest(splitted[1].Trim());
                        if (success)
                        {
                            _accounts.Register(message.From.Id, message.From.Username, splitted[1].Trim());
                            await _client.SendMessage(new SendMessageQuery()
                            {
                                ChatId = message.Chat.Id,
                                ReplyToMessageId = message.MessageId,
                                Text = "Ты успешно зарегистрирован. Добавь меня в PSN, пожалуйста."
                            });
                        }
                        else
                        {
                            await _client.SendMessage(new SendMessageQuery()
                            {
                                ChatId = message.Chat.Id,
                                ReplyToMessageId = message.MessageId,
                                Text = "Не могу найти тебя в PSN."
                            });
                        }
                    }
                    else
                    {
                        await _client.SendMessage(new SendMessageQuery()
                        {
                            ChatId = message.Chat.Id,
                            ReplyToMessageId = message.MessageId,
                            Text = "Я тебя уже добавил."
                        });
                    }
                }
                else
                {
                    await _client.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.Chat.Id,
                        ReplyToMessageId = message.MessageId,
                        Text = "Укажи свой идентификатор в PSN (например, так: /register@clankbot <your psn id>)."
                    });
                }
            }

            if (message.Text.StartsWith("/start@clankbot", StringComparison.OrdinalIgnoreCase))
            {
                if (_accounts.GetById(message.From.Id) != null)
                {
                    _accounts.Start(message.From.Id, message.Chat.Id);
                    await _client.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.Chat.Id,
                        ReplyToMessageId = message.MessageId,
                        Text = "Начинаю трансляцию призов."
                    });
                }
                else
                {
                    await _client.SendMessage(new SendMessageQuery()
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
                    await _client.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.Chat.Id,
                        ReplyToMessageId = message.MessageId,
                        Text = "Интересы сохранены."
                    });
                }
                else
                {
                    await _client.SendMessage(new SendMessageQuery()
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
                    await _client.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.Chat.Id,
                        ReplyToMessageId = message.MessageId,
                        Text = "Останавливаю трансляцию призов."
                    });
                }
                else
                {
                    await _client.SendMessage(new SendMessageQuery()
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

                var filtered = _accounts.GetAll().Where(a => string.IsNullOrEmpty(interests)
                    || (!string.IsNullOrEmpty(a.Interests) && a.Interests.ToLower().Contains(interests.ToLower()))
                    || (!string.IsNullOrEmpty(a.TelegramName) && a.TelegramName.ToLower().Contains(interests.ToLower()))
                    || (!string.IsNullOrEmpty(a.PSNName) && a.PSNName.ToLower().Contains(interests.ToLower())));

                var lines = filtered.AsParallel().Select(async account =>
                {
                    var builder = new StringBuilder();
                    builder.AppendLine(string.Format("Telegram: <b>{0}</b>\nPSN: <b>{1}</b>", account.TelegramName, account.PSNName));

                    var userEntry = await _psnService.GetUser(account.PSNName);

                    //if (!string.IsNullOrEmpty(interests))
                    {
                        var status = userEntry.GetStatus();
                        if (!string.IsNullOrEmpty(status))
                        {
                            builder.AppendLine(string.Format("{0}", status));
                        }
                    }

                    if (!string.IsNullOrEmpty(account.Interests))
                    {
                        builder.AppendLine(string.Format("{0}", account.Interests));
                    }
                    builder.AppendLine();
                    return builder.ToString();
                }).ToArray();

                Task.WaitAll(lines);

                foreach (var line in lines)
                {
                    if (sb.Length + line.Result.Length < 4096)
                    {
                        sb.Append(line.Result);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(interests))
                        {
                            await _client.SendMessage(new SendMessageQuery()
                            {
                                ChatId = message.From.Id,
                                Text = sb.ToString(),
                                ParseMode = "HTML",
                            });
                        }
                        else
                        {
                            await _client.SendMessage(new SendMessageQuery()
                            {
                                ChatId = message.Chat.Id,
                                ReplyToMessageId = message.MessageId,
                                Text = sb.ToString(),
                                ParseMode = "HTML",
                            });
                        }
                        sb.Clear();
                        sb.Append(line.Result);
                    }
                }

                if (string.IsNullOrEmpty(interests))
                {
                    await _client.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.From.Id,
                        Text = sb.ToString(),
                        ParseMode = "HTML",
                    });
                }
                else
                {
                    await _client.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.Chat.Id,
                        ReplyToMessageId = message.MessageId,
                        Text = sb.ToString(),
                        ParseMode = "HTML",
                    });
                }
            }

            if (message.Text.StartsWith("/top@clankbot", StringComparison.OrdinalIgnoreCase))
            {
                var tasks = _accounts.GetAll().AsParallel().Select(async a =>
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
                }).ToArray();
                Task.WaitAll(tasks);
                var table = tasks.Where(t => t.Result != null).Select(t => t.Result)
                    .OrderByDescending(t => t.Rating).Take(20);

                StringBuilder sb = new StringBuilder();
                int i = 1;
                foreach (var t in table)
                {
                    sb.AppendLine(string.Format("{0}. {1} {2}", i, !string.IsNullOrEmpty(t.TelegramName) ? t.TelegramName : t.PSNName, t.ThrophyLine));
                    i++;
                }

                await _client.SendMessage(new SendMessageQuery()
                {
                    ChatId = message.Chat.Id,
                    ReplyToMessageId = message.MessageId,
                    Text = sb.ToString(),
                    ParseMode = "HTML",
                });
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
