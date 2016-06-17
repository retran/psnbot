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
        //-1001017895589
        // "ps4_rus"
        //

        static async void Do()
        {
            var client = new PSNClient();
            client.Login("", "");
            var achievements = await client.GetAchievements();

            var telegramClient = new Telegram.TelegramClient("");

            //var updates = telegramClient.GetUpdates(new Telegram.GetUpdatesQuery());


            foreach (var ach in achievements)
            {
                if (!string.IsNullOrEmpty(ach.Image))
                {
                    WebClient myWebClient = new WebClient();
                    var image = myWebClient.DownloadData(ach.Image);

                    var message = await telegramClient.SendPhoto(new Telegram.SendPhotoQuery()
                    {
                        ChatId = -120625429
                    }, image);
                }

                await telegramClient.SendMessage(new Telegram.SendMessageQuery()
                {
                    // ChatId = -1001017895589,
                    ChatId = -120625429,
                    Text = ach.GetTelegramMessage(),
                    ParseMode = "HTML",                    
                });
            }
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            Do();
            Console.ReadKey();
        }
    }
}
