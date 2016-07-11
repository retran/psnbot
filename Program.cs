using System;
using PSNBot.Services;
using PSNBot.Process;
using Microsoft.AspNetCore.Hosting;

namespace PSNBot
{
    // test - -120625429
    // main - -1001017895589
    // TODO пасхалки
    // TODO поправить текст правил
    // TODO поправить текст помощи
    // TODO выделять жирным то что искал пользователь
    //  TODO ХОЧУ С КЕМ ТО ПОИГРАТЬ
    // 	/await ИМЯ ИГРЫ
    // TODO команда rating
    // TODO стикеры для ачивок
    // TODO Ещё полезным будет знать статы человека, который выше тебя по рейтингу на 1

    public class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            var client = new PSNService();
            var task = client.Login("retran@tolkien.ru", "");
            task.Wait();

            var telegramClient = new Telegram.TelegramClient("");
            var database = new DatabaseService("../../../psnbot.sqlite");

            var accounts = new AccountService(database);
            var timestampService = new TimeStampService(database);

            var registrationProcess = new RegistrationProcess(telegramClient, client, accounts);

            var chatId = -1001019649766;

            using (var poller = new MessagePoller(database, telegramClient, client, accounts, registrationProcess, chatId))
            using (var imagePoller = new ImagePoller(telegramClient, client, accounts, timestampService, chatId))
            using (var trophyPoller = new TrophyPoller(telegramClient, client, accounts, timestampService, chatId))
            using (var friendPoller = new FriendPoller(telegramClient, client, accounts, registrationProcess))
            {
                poller.Start();
                imagePoller.Start();
                trophyPoller.Start();
                friendPoller.Start();

                var host = new WebHostBuilder()
                    .UseKestrel()
                    .UseStartup<Startup>()
                    .Build();
                host.Run();            
            }
        }
    }
}
