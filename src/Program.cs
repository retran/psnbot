using PsnLib.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PsnLib.Entities;
using System.Net;

namespace PSNBot
{
    public class Program
    {
        static void Main(string[] args)
        {
            var client = new PSNClient();
            client.Login("retran@tolkien.ru", "");

            var telegramClient = new Telegram.TelegramClient("");

            Console.OutputEncoding = System.Text.Encoding.Unicode;

            using (var poller = new MessagePoller(telegramClient, client))
            {
                poller.Start();
                Console.ReadKey();
            }
        }
    }
}
