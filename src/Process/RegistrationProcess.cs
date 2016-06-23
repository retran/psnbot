using PSNBot.Model;
using PSNBot.Services;
using PSNBot.Telegram;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSNBot.Process
{
    public class RegistrationProcess
    {
        private TelegramClient _telegramClient;
        private PSNService _psnService;
        private AccountService _accountService;

        public RegistrationProcess(TelegramClient telegramClient, PSNService psnService, AccountService accountService)
        {
            _telegramClient = telegramClient;
            _psnService = psnService;
            _accountService = accountService;
        }

        public async Task<bool> HandleCurrentStep(Account account, Message message)
        {
            try
            {
                var text = message.Text.Trim();
                switch (account.Status)
                {
                    case Status.AwaitingPSNName:
                        var user = await _psnService.GetUser(message.Text);
                        if (user == null)
                        {
                            await _telegramClient.SendMessage(new SendMessageQuery()
                            {
                                ChatId = account.Id,
                                ReplyToMessageId = message.MessageId,
                                Text = Messages.PSNNameFault,
                                ParseMode = "HTML",
                            });
                        }
                        else
                        {
                            await _telegramClient.SendMessage(new SendMessageQuery()
                            {
                                ChatId = account.Id,
                                ReplyToMessageId = message.MessageId,
                                Text = Messages.PSNNameSuccess,
                                ParseMode = "HTML",
                            });
                            await _psnService.SendFriendRequest(text);
                            account.PSNName = text;
                            account.Status = Status.AwaitingFriendRequest;
                            _accountService.Update(account);
                            await SendCurrentStep(account);
                        }
                        break;
                    case Status.AwaitingFriendRequest:
                        await _telegramClient.SendMessage(new SendMessageQuery()
                        {
                            ChatId = account.Id,
                            ReplyToMessageId = message.MessageId,
                            Text = Messages.AwaitingFriendRequest,
                            ParseMode = "HTML",
                        });

                        break;

                    case Status.AwaitingInterests:
                        account.Interests = text;
                        account.Status = Status.AwaitingTrophies;
                        _accountService.Update(account);

                        await _telegramClient.SendMessage(new SendMessageQuery()
                        {
                            ChatId = account.Id,
                            Text = Messages.SavedInterests,
                            ParseMode = "HTML",
                        });
                        await SendCurrentStep(account);
                        break;

                    case Status.AwaitingTrophies:
                        var value = text.ToLower();
                        if (value == "да" || value == "нет")
                        {
                            account.Status = Status.Ok;
                            account.ShowTrophies = value == "да";
                            _accountService.Update(account);

                            await _telegramClient.SendMessage(new SendMessageQuery()
                            {
                                ChatId = account.Id,
                                Text = value == "да" ? Messages.ShowTrophiesYes : Messages.ShowTrophiesNo,
                                ParseMode = "HTML",
                            });
                            await SendCurrentStep(account);
                        }
                        else
                        {
                            await _telegramClient.SendMessage(new SendMessageQuery()
                            {
                                ChatId = account.Id,
                                Text = Messages.YesOrNo,
                                ParseMode = "HTML",
                            });
                        }
                        break;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SendCurrentStep(Account account)
        {
            switch (account.Status)
            {
                case Status.AwaitingPSNName:
                    await _telegramClient.SendMessage(new SendMessageQuery()
                    {
                        ChatId = account.Id,
                        Text = Messages.StartAwaitingPSNName,
                        ParseMode = "HTML",
                    });
                    break;

                case Status.AwaitingFriendRequest:
                    await _telegramClient.SendMessage(new SendMessageQuery()
                    {
                        ChatId = account.Id,
                        Text = Messages.StartAwaitingFriendRequest,
                        ParseMode = "HTML",
                    });
                    break;
                case Status.AwaitingInterests:
                    await _telegramClient.SendMessage(new SendMessageQuery()
                    {
                        ChatId = account.Id,
                        Text = Messages.StartAwaitingInterests,
                        ParseMode = "HTML",
                    });
                    break;
                case Status.AwaitingTrophies:
                    await _telegramClient.SendMessage(new SendMessageQuery()
                    {
                        ChatId = account.Id,
                        Text = Messages.StartAwaitingTrophies,
                        ParseMode = "HTML",
                    });
                    break;
                case Status.Ok:
                    await _telegramClient.SendMessage(new SendMessageQuery()
                    {
                        ChatId = account.Id,
                        Text = Messages.EndRegistration,
                        ParseMode = "HTML",
                    });
                    break;

            }
            return true;
        }

        public void Abort(Account account)
        {

        }
    }
}
