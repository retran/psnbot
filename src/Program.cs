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
    // test - -120625429
    // main - -1001017895589


    public class Program
    {
        static void Main(string[] args)
        {
            var client = new PSNService();
            var task = client.Login("retran@tolkien.ru", "");
            task.Wait();

            var telegramClient = new Telegram.TelegramClient("");
            var database = new DatabaseService("../psnbot.sqlite");

            var accounts = new AccountService(database);

            Console.OutputEncoding = System.Text.Encoding.Unicode;

            using (var poller = new MessagePoller(database, telegramClient, client, accounts, -120625429))
            using (var imagePoller = new ImagePoller(database, telegramClient, client, accounts, -120625429))
            using (var trophyPoller = new TrophyPoller(database, telegramClient, client, accounts, -120625429))
            {
                poller.Start();
                imagePoller.Start();
                trophyPoller.Start();
                Console.ReadKey();
            }
        }
    }
}
