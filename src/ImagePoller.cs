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
using PSNBot.Model;

namespace PSNBot
{
    public class ImagePoller : IDisposable
    {
        private TelegramClient _telegramClient;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed = false;
        private PSNService _psnService;
        private AccountService _accounts;
        private DateTime _lastCheckDateTime = DateTime.Now;
        private long _chatId;
        private DatabaseService _databaseService;

        public ImagePoller(DatabaseService databaseService, TelegramClient telegramClient, PSNService psnService, AccountService accounts, long chatId)
        {
            _chatId = chatId;
            _telegramClient = telegramClient;
            _psnService = psnService;
            _accounts = accounts;
            _databaseService = databaseService;
        }

        private async Task Poll()
        {
            try
            {
                var dt = DateTime.Now;
                if ((dt - _lastCheckDateTime).TotalSeconds > 60)
                {
                    DateTime lastPhotoTimeStamp = LoadTimeStamp(".phototimestamp");
                    var msgs = (await _psnService.GetImages(lastPhotoTimeStamp)).OrderBy(m => m.TimeStamp);
                    foreach (var msg in msgs)
                    {
                        var account = _accounts.GetByPSN(msg.Source);

                        if (account != null)
                        {
                            var tlgMsg = await _telegramClient.SendMessage(new Telegram.SendMessageQuery()
                            {
                                ChatId = _chatId,
                                Text = string.Format("Пользователь <b>{0} ({1})</b> опубликовал изображение:", account.PSNName, account.TelegramName),
                                ParseMode = "HTML",
                            });

                            var message = await _telegramClient.SendPhoto(new Telegram.SendPhotoQuery()
                            {
                                ChatId = _chatId
                            }, msg.Data);

                            Thread.Sleep(1000);
                        }
                        lastPhotoTimeStamp = msg.TimeStamp;
                    }
                    SaveTimeStamp(lastPhotoTimeStamp, ".phototimestamp");
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(string.Format("{0}\t{1}", DateTime.Now, e.Message));
            }
        }

        private void SaveTimeStamp(DateTime stamp, string id)
        {
            _databaseService.Upsert<TimeStamp>(new TimeStamp()
            {
                Id = id,
                Stamp = stamp
            });
        }

        private DateTime LoadTimeStamp(string id)
        {
            var timestamp = _databaseService.Select<TimeStamp>("id", id).FirstOrDefault();
            if (timestamp != null)
            {
                return timestamp.Stamp.ToUniversalTime();
            }

            return DateTime.UtcNow;
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

        ~ImagePoller()
        {
            Dispose(false);
        }
    }
}
