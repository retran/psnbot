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
using PSNBot.Commands;

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
        private IEnumerable<Command> _commands;

        public MessagePoller(TelegramClient client, PSNService psnService, AccountManager accounts)
        {
            _client = client;
            _psnService = psnService;
            _accounts = accounts;

            _commands = new Command[]
            {
                new TopCommand(_psnService, client, _accounts),
                new SearchCommand(_psnService, client, _accounts),
                new ListCommand(_psnService, client, _accounts)
            };
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
        }

        private async void Handle(Message message)
        {
            var acc = _accounts.GetById(message.From.Id);
            if (acc != null && acc.TelegramName != message.From.Username)
            {
                acc.TelegramName = message.From.Username;
                _accounts.Persist();
            }

            var command = _commands.FirstOrDefault(c => c.IsApplicable(message));
            if (command != null)
            {
                await command.Handle(message);
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
