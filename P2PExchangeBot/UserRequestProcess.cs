using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using LD = P2PExchangeBot.LanguageDictionary;

namespace P2PExchangeBot
{
    enum RequestSteps
    {
        Start,
        EnterQuantityAndFee,
        EnterBank,
        EnterEndDate,
        ChangeQuantityAndFee,
        ChangeBank,
        ChangeEndDate,
        VoteUser,
        UnvoteUser,
    }

    class UserRequestProcess
    {
        public string Username { get; private set; }
        public RequestSteps CurrentStep { get; private set; }

        private TelegramBotClient _bot;
        private long _chatId;

        private RequestType _reqType;
        private int _quantity;
        private string _currency;
        private float _fee;
        private string _bank;
        private int _daysQuantity;
        private int _reqIdForUpdate;

        public UserRequestProcess(TelegramBotClient bot, string username, long chatId)
        {
            Username = username;
            CurrentStep = RequestSteps.Start;
            _bot = bot;
            _chatId = chatId;
        }

        Message _startMessage;
        public async Task Start()
        {
            CurrentStep = RequestSteps.Start;

            bool enabledNotifications = Database.IsNotificationsRowExistForUser(Username);
            var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // first row
                    {
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.BuyKey), LD.BuyKey),
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.SellKey), LD.SellKey),
                    },
                    new[] // third row
                    {
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.ShowMyReqKey), LD.ShowMyReqKey),
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.ShowAllReqKey), LD.ShowAllReqKey),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.VoteKey), LD.VoteKey),
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.UnvoteKey), LD.UnvoteKey),
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.EscrowListKey), LD.EscrowListKey),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(enabledNotifications ? LD.GetTranslate(Username, LD.DisableNotifKey) : LD.GetTranslate(Username, LD.EnableNotifKey),
                        enabledNotifications ? LD.DisableNotifKey : LD.EnableNotifKey),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.EnglishKey), LD.EnglishKey),
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.RussianKey), LD.RussianKey),
                    }
                });

            try
            {
                if (_startMessage != null)
                    await _bot.DeleteMessageAsync(_chatId, _startMessage.MessageId);
            }
            catch
            {
                _startMessage = null;
            }

            _startMessage = await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.StartMessageKey), ParseMode.Html, keyboard);
        }

        public async Task ProcessMessage(string msg)
        {
            switch (CurrentStep)
            {
                case RequestSteps.Start:
                    await ProcessStartState(msg);
                    break;
                case RequestSteps.EnterQuantityAndFee:
                    await ProcessQuantityAndFee(msg);
                    break;
                case RequestSteps.EnterBank:
                    await ProcessBank(msg);
                    break;
                case RequestSteps.EnterEndDate:
                    await ProcessEndDate(msg);
                    break;
                case RequestSteps.ChangeQuantityAndFee:
                    await ProcessChangeQuantityAndFee(msg);
                    break;
                case RequestSteps.ChangeBank:
                    await ProcessChangeBank(msg);
                    break;
                case RequestSteps.ChangeEndDate:
                    await ProcessChangeEndDate(msg);
                    break;
                case RequestSteps.VoteUser:
                    await ProcessVote(msg);
                    break;
                case RequestSteps.UnvoteUser:
                    await ProcessUnvote(msg);
                    break;
            }
        }

        private async Task ProcessStartState(string msg)
        {
            await Task.Delay(100);
            if (msg.Equals(LD.SellKey))
            {
                Console.WriteLine(Username + " Продать");
                _reqType = RequestType.Sell;
                CurrentStep = RequestSteps.EnterQuantityAndFee;

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // first row
                    {
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.CancelKey), LD.CancelKey),
                    }
                });

                await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.SellingMsgKey), ParseMode.Html, keyboard);
            }
            else if (msg.Equals(LD.BuyKey))
            {
                Console.WriteLine(Username + " Купить");
                _reqType = RequestType.Buy;
                CurrentStep = RequestSteps.EnterQuantityAndFee;

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // first row
                    {
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.CancelKey), LD.CancelKey),
                    }
                });

                await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.BuyingMsgKey), ParseMode.Html, keyboard);
            }
            else if (msg.Contains(LD.RemoveKey))
            {
                int id;
                if (!ParseReqId(msg, out id))
                {
                    await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.RemoveErrorKey));
                    return;
                }
                Console.WriteLine(Username + " Удалить заявку №" + id);

                Database.DeleteReqWithId(Username, id);
                await SendMessageAsync(_chatId, string.Format(LD.GetTranslate(Username, LD.RemoveSuccessKey), id));
            }
            else if (msg.Contains(LD.ChangeKey))
            {
                int id;
                if (!ParseReqId(msg, out id))
                {
                    await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.ChangeErrorKey));
                    return;
                }

                Console.WriteLine(Username + " Изменить заявку №" + id);

                CurrentStep = RequestSteps.ChangeQuantityAndFee;
                _reqIdForUpdate = id;

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // first row
                    {
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.CancelKey), LD.CancelKey),
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.SkipKey), LD.SkipKey),
                    }
                });
                await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.ChangingMsgKey), ParseMode.Html, keyboard);
            }
            else if (msg.Equals(LD.ShowMyReqKey))
            {
                Console.WriteLine(Username + " Посмотреть мои заявки");
                await ProcessShowMy();
            }
            else if (msg.Equals(LD.ShowAllReqKey))
            {
                Console.WriteLine(Username + " Посмотреть все заявки");
                await ProcessShowAll();
            }
            else if (msg.Equals(LD.VoteKey))
            {
                if (Database.GetVotesCount(Username) <= 0)
                {
                    await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.ZeroVotesKey));
                    return;
                }
                CurrentStep = RequestSteps.VoteUser;

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // first row
                    {
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.CancelKey), LD.CancelKey),
                    }
                });
                await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.VotingMsgKey), ParseMode.Html, replyMarkup: keyboard);
            }
            else if (msg.Equals(LD.UnvoteKey))
            {
                var votedUsersList = Database.GetMyVotedUsers(Username);

                if (votedUsersList.Count == 0)
                {
                    await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.VoteListEmptyKey));
                    return;
                }

                CurrentStep = RequestSteps.UnvoteUser;
                _unvoteMessage = await SendMessageAsync(_chatId, string.Format(@"<b>{0}</b>", LD.GetTranslate(Username, LD.UnvoteKey)), ParseMode.Html, replyMarkup: GetMarkupForUnvote(votedUsersList));
            }
            else if (msg.Equals(LD.EscrowListKey))
            {
                await ProcessEscrowList();
            }
            else if (msg.Contains(LD.DisableNotifKey))
            {
                Console.WriteLine(Username + " Выключить оповещения");
                Database.DeleteUserFromNotifications(Username);

                await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.NotificationsDisabledKey));
                await Start();
            }
            else if (msg.Contains(LD.EnableNotifKey))
            {
                Console.WriteLine(Username + " Включить оповещения");

                if (!Database.IsNotificationsRowExistForUser(Username))
                    Database.AddUserForNotifications(Username, _chatId);

                await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.NotificationsEnabledKey));
                await Start();
            }
            else if (msg.Contains(LD.EnglishKey))
            {
                Database.SetUserLanguage(Username, Languages.English);
                await Start();
            }
            else if (msg.Contains(LD.RussianKey))
            {
                Database.SetUserLanguage(Username, Languages.Russian);
                await Start();
            }
        }

        private bool ParseReqId(string msg, out int id)
        {
            int idx1 = msg.IndexOf('(');
            int idx2 = msg.IndexOf(')');
            if (idx1 < 0 || idx2 < 0 || !int.TryParse(msg.Substring(idx1 + 1, idx2 - idx1 - 1), out id))
            {
                id = 0;
                return false;
            }

            return true;
        }

        private async Task ProcessQuantityAndFee(string msg)
        {
            await Task.Delay(100);

            if (msg.Equals(LD.CancelKey) || msg.Equals("/start"))
            {
                Console.WriteLine(Username + " Отмена");
                await Start();
                return;
            }

            string[] splitted = msg.Split(' ');

            if (splitted.Length < 3 || !int.TryParse(splitted[0], out _quantity) || !float.TryParse(splitted[2], out _fee))
            {
                await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.WrongInputKey));
                await ProcessStartState(_reqType == RequestType.Buy ? LD.BuyKey : LD.SellKey);
                return;
            }

            _currency = splitted[1];

            CurrentStep = RequestSteps.EnterBank;
            var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // first row
                    {
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.CancelKey), LD.CancelKey),
                    }
                });
            await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.EnterBankNameKey), ParseMode.Default, keyboard);
        }

        private async Task ProcessBank(string msg)
        {
            await Task.Delay(100);

            if (msg.Equals(LD.CancelKey) || msg.Equals("/start"))
            {
                Console.WriteLine(Username + " Отмена");
                await Start();
                return;
            }

            if (string.IsNullOrEmpty(msg))
            {
                await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.WrongInputKey));
                return;
            }

            _bank = StripTagsRegex(msg);

            CurrentStep = RequestSteps.EnterEndDate;
            var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // first row
                    {
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.CancelKey), LD.CancelKey),
                    }
                });
            await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.EnterReqDurationKey), ParseMode.Default, keyboard);
        }

        private async Task ProcessEndDate(string msg)
        {
            await Task.Delay(100);

            if (msg.Equals(LD.CancelKey) || msg.Equals("/start"))
            {
                Console.WriteLine(Username + " Отмена");
                await Start();
                return;
            }

            if (!int.TryParse(msg, out _daysQuantity))
            {
                await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.WrongInputKey));
                return;
            }

            int reqId = Database.AddRequest(Username, _reqType, _quantity, _currency, _bank, _fee, DateTime.Now, DateTime.Now + TimeSpan.FromDays(_daysQuantity));
            
            await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.SuccessfulRequestKey));
            await Start();

            string requestString = Database.GetRequest(reqId, Username);

            await SendNotifications(string.Format(LD.GetTranslate(Username, LD.NewReqNotifKey), requestString));
        }

        private async Task SendNotifications(string message)
        {
            var forNotif = Database.GetUserlistForNotifications(Username);
            foreach(var chatId in forNotif)
            {
                await SendMessageAsync(chatId, message, ParseMode.Html);
            }
        }

        private async Task ProcessChangeQuantityAndFee(string msg)
        {
            await Task.Delay(100);

            if (msg.Equals(LD.CancelKey) || msg.Equals("/start"))
            {
                await Start();
                return;
            }

            if (!msg.Equals(LD.SkipKey))
            {
                string[] splitted = msg.Split(' ');

                if (splitted.Length < 3 || !int.TryParse(splitted[0], out _quantity) || !float.TryParse(splitted[2], out _fee))
                {
                    await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.WrongInputKey));
                    return;
                }

                _currency = splitted[1];                
            }
            else
            {
                _currency = string.Empty;
                _quantity = -1;
                _fee = -1f;
            }

            CurrentStep = RequestSteps.ChangeBank;
            var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // first row
                    {
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.CancelKey), LD.CancelKey),
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.SkipKey), LD.SkipKey),
                    }
                });
            await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.EnterBankNameKey), ParseMode.Default, keyboard);
        }

        private async Task ProcessChangeBank(string msg)
        {
            await Task.Delay(100);

            if (msg.Equals(LD.CancelKey) || msg.Equals("/start"))
            {
                await Start();
                return;
            }

            if (!msg.Equals(LD.SkipKey))
            {
                _bank = StripTagsRegex(msg);
            }
            else
            {
                _bank = string.Empty;
            }

            CurrentStep = RequestSteps.ChangeEndDate;
            var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // first row
                    {
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.CancelKey), LD.CancelKey),
                        InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.SkipKey), LD.SkipKey),
                    }
                });
            await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.EnterReqDurationKey), ParseMode.Default, keyboard);
        }

        private async Task ProcessChangeEndDate(string msg)
        {
            await Task.Delay(100);

            if (msg.Equals(LD.CancelKey) || msg.Equals("/start"))
            {
                await Start();
                return;
            }

            if (!msg.Equals(LD.SkipKey))
            {
                if (!int.TryParse(msg, out _daysQuantity))
                {
                    await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.WrongInputKey));
                    return;
                }
            }
            else
            {
                _daysQuantity = -1;
            }

            Database.UpdateRequest(_reqIdForUpdate, Username, _quantity, _currency, _bank, _fee,
                DateTime.Now, _daysQuantity > 0 ? DateTime.Now + TimeSpan.FromDays(_daysQuantity) : DateTime.MinValue);
            
            await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.SuccessfulChangeKey));
            await Start();

            string reqString = Database.GetRequest(_reqIdForUpdate, Username);
            await SendNotifications(string.Format(LD.GetTranslate(Username, LD.ChangedReqNotifKey), reqString));
        }

        private async Task ProcessShowMy()
        {
            await Task.Delay(100);

            var myReqs = Database.GetRequestsFor(Username, Username);

            if (myReqs.Count == 0)
            {
                await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.EmptyKey));
                return;
            }

            foreach (var req in myReqs)
            {
                int idx1 = req.IndexOf('(');
                int idx2 = req.IndexOf(')');
                if (idx1 >= 0 && idx2 >= 0)
                {
                    string reqId = req.Substring(idx1, (idx2 - idx1) + 1);
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] // first row
                        {
                            InlineKeyboardButton.WithCallbackData(string.Format("{0} {1}", LD.GetTranslate(Username, LD.RemoveKey), reqId), string.Format("{0} {1}", LD.RemoveKey, reqId)),
                            InlineKeyboardButton.WithCallbackData(string.Format("{0} {1}", LD.GetTranslate(Username, LD.ChangeKey), reqId), string.Format("{0} {1}", LD.ChangeKey, reqId)),
                        }
                    });

                    await SendMessageAsync(_chatId, req, ParseMode.Html, keyboard);
                }
            }


        }

        private async Task ProcessShowAll()
        {
            await Task.Delay(100);

            var myReqs = Database.GetAllRequests(Username);

            if (myReqs.Count == 0)
            {
                await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.EmptyKey));
                return;
            }

            int idx = 0;
            while (idx < myReqs.Count)
            {
                int count = Math.Min(10, myReqs.Count - idx);
                var lst = myReqs.GetRange(idx, count);

                string result = lst.Count > 1 ? lst.Aggregate((current, next) => current + "\n\n" + next) : lst[0];

                await SendMessageAsync(_chatId, result, ParseMode.Html);
                idx += 10;
            }
        }

        private async Task ProcessVote(string msg)
        {
            await Task.Delay(100);

            if (msg.Equals(LD.CancelKey) || msg.Equals("/start"))
            {
                await Start();
                return;
            }

            string votedUser = msg.Trim('"').TrimStart('@');

            if (!Database.IsUserRegistered(votedUser))
            {
                await SendMessageAsync(_chatId, string.Format(LD.GetTranslate(Username, LD.VotedUserNotRegisteredKey), votedUser));
                return;
            }

            if (Database.IsAlreadyVotedByUser(Username, votedUser))
            {
                await SendMessageAsync(_chatId, string.Format(LD.GetTranslate(Username, LD.VotedUserAlreadyVotedKey), votedUser));
                return;
            }

            if (Username.Equals(votedUser))
            {
                await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.VotedUserIsMySelfKey));
                return;
            }

            Database.Vote(Username, votedUser);
            int voteCount = Database.GetVotesCount(Username);

            await SendMessageAsync(_chatId, string.Format(LD.GetTranslate(Username, LD.VoteSuccessfulKey), votedUser, voteCount));
            await Start();
        }

        private Message _unvoteMessage;
        private async Task ProcessUnvote(string msg)
        {
            await Task.Delay(100);

            if (msg.Equals(LD.CancelKey) || msg.Equals("/start"))
            {
                await DeleteMessageAsync(_unvoteMessage);
                await Start();
                return;
            }

            string votedUser = msg.TrimStart('@');

            Database.Unvote(Username, votedUser);
            int voteCount = Database.GetVotesCount(Username);
            await DeleteMessageAsync(_unvoteMessage);
            await SendMessageAsync(_chatId, string.Format(LD.GetTranslate(Username, LD.UnvoteSuccessfulKey), votedUser, voteCount));
            await Start();
        }

        private async Task ProcessEscrowList()
        {
            var list = Database.GetEscrowList();

            if (list.Count == 0)
            {
                await SendMessageAsync(_chatId, LD.GetTranslate(Username, LD.EmptyKey));
                return;
            }

            string result = list.Aggregate((current, next) => current + "\n" + next);
            await SendMessageAsync(_chatId, result, ParseMode.Html);
        }

        private async Task<Message> SendMessageAsync(long chatId, string msg, ParseMode parseMode = ParseMode.Default, IReplyMarkup replyMarkup = null)
        {
            try
            {
                return await _bot.SendTextMessageAsync(chatId, msg, parseMode, replyMarkup: replyMarkup);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Exception during sending message. User: {0} Message: {1}", Username, ex.Message));
            }

            return null;
        }

        private IReplyMarkup GetMarkupForUnvote(List<string> usernameList)
        {
            InlineKeyboardButton[][] keyboard = new InlineKeyboardButton[usernameList.Count + 1][];

            for (int i = 0; i < usernameList.Count; ++i)
            {
                keyboard[i] = new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData(usernameList[i]) };
            }

            keyboard[usernameList.Count] = new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData(LD.GetTranslate(Username, LD.CancelKey), LD.CancelKey) };

            return new InlineKeyboardMarkup(keyboard);
        }

        private async Task DeleteMessageAsync(Message message)
        {
            try
            {
                if (message != null)
                    await _bot.DeleteMessageAsync(_chatId, message.MessageId);
            }
            catch
            {
                //ignore
            }
        }

        public static string StripTagsRegex(string source)
        {
            return Regex.Replace(source, "<.*?>", string.Empty);
        }
    }
}
