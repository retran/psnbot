using PsnLib.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PsnLib.Entities;
using System.Net;
using PSNBot.Services;

namespace PSNBot
{
    public class Program
    {
        static void Main(string[] args)
        {
            var client = new PSNService();
            var task = client.Login("retran@tolkien.ru", "");
            task.Wait();

            var telegramClient = new Telegram.TelegramClient("");

            Console.OutputEncoding = System.Text.Encoding.Unicode;

            using (var poller = new MessagePoller(telegramClient, client))
            using (var imagePoller = new ImagePoller(telegramClient, client))
            using (var trophyPoller = new TrophyPoller(telegramClient, client))
            {
                poller.Start();
                imagePoller.Start();
                trophyPoller.Start();
                Console.ReadKey();
            }
        }
    }
}
