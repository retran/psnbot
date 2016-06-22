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
    public class ImagePoller : IDisposable
    {
        private TelegramClient _client;
        private long _offset = 0;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed = false;
        private PSNService _psnService;
        private AccountManager _accounts;
        private DateTime _lastCheckDateTime = DateTime.Now;

        public ImagePoller(TelegramClient client, PSNService psnService)
        {
            _client = client;
            _psnService = psnService;
            _accounts = new AccountManager();
        }

        private async Task Poll()
        {
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
