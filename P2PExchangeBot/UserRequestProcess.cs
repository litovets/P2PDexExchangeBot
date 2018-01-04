using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using Telegram.Bot.Types.ReplyMarkups;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

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
                        InlineKeyboardButton.WithCallbackData("Купить"),
                        InlineKeyboardButton.WithCallbackData("Продать"),
                    },
                    new[] // third row
                    {
                        InlineKeyboardButton.WithCallbackData("Посмотреть мои заявки"),
                        InlineKeyboardButton.WithCallbackData("Посмотреть все заявки"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Отдать голос", "vote"),
                        InlineKeyboardButton.WithCallbackData("Забрать голос", "unvote"),
                        InlineKeyboardButton.WithCallbackData("Список гарантов", "escrowlist"),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(enabledNotifications ? "Выключить оповещения" : "Включить оповещения"),
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

            _startMessage = await SendMessageAsync(_chatId, "<b>Выбирайте</b>", ParseMode.Html, keyboard);
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
            if (msg.Equals("Продать"))
            {
                Console.WriteLine(Username + " Продать");
                _reqType = RequestType.Sell;
                CurrentStep = RequestSteps.EnterQuantityAndFee;

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // first row
                    {
                        InlineKeyboardButton.WithCallbackData("Отмена"),
                    }
                });

                await SendMessageAsync(_chatId, @"<b>Продажа</b>

Введите сумму, валюту и комиссию.
Например:
<b>1000 bitUSD 2.0</b>", ParseMode.Html, keyboard);
            }
            else if (msg.Equals("Купить"))
            {
                Console.WriteLine(Username + " Купить");
                _reqType = RequestType.Buy;
                CurrentStep = RequestSteps.EnterQuantityAndFee;

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // first row
                    {
                        InlineKeyboardButton.WithCallbackData("Отмена"),
                    }
                });

                await SendMessageAsync(_chatId, @"<b>Покупка</b>

Введите сумму, валюту и комиссию.
Например:
<b>1000 bitUSD 2.0</b>", ParseMode.Html, keyboard);
            }
            else if (msg.Contains("Удалить"))
            {
                int id;
                if (!ParseReqId(msg, out id))
                {
                    await SendMessageAsync(_chatId, @"Ошибка удаления. Попробуйте еще раз.");
                    return;
                }
                Console.WriteLine(Username + " Удалить заявку №" + id);

                Database.DeleteReqWithId(Username, id);
                await SendMessageAsync(_chatId, @"Заявка №" + id + " успешно удалена");
            }
            else if (msg.Contains("Изменить"))
            {
                int id;
                if (!ParseReqId(msg, out id))
                {
                    await SendMessageAsync(_chatId, @"Ошибка изменения. Попробуйте еще раз.");
                    return;
                }

                Console.WriteLine(Username + " Изменить заявку №" + id);

                CurrentStep = RequestSteps.ChangeQuantityAndFee;
                _reqIdForUpdate = id;

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // first row
                    {
                        InlineKeyboardButton.WithCallbackData("Отмена"),
                        InlineKeyboardButton.WithCallbackData("Пропустить"),
                    }
                });
                await SendMessageAsync(_chatId, @"<b>Изменение заявки</b>

Введите сумму, валюту и комиссию.
Например:
<b>1000 bitUSD 2.0</b>", ParseMode.Html, keyboard);
            }
            else if (msg.Equals("Посмотреть мои заявки"))
            {
                Console.WriteLine(Username + " Посмотреть мои заявки");
                await ProcessShowMy();
            }
            else if (msg.Equals("Посмотреть все заявки"))
            {
                Console.WriteLine(Username + " Посмотреть все заявки");
                await ProcessShowAll();
            }
            else if (msg.Equals("vote"))
            {
                if (Database.GetVotesCount(Username) <= 0)
                {
                    await SendMessageAsync(_chatId, "Доступное количество голосов - 0");
                    return;
                }
                CurrentStep = RequestSteps.VoteUser;

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // first row
                    {
                        InlineKeyboardButton.WithCallbackData("Отмена"),
                    }
                });
                await SendMessageAsync(_chatId, @"<b>Отдать голос</b>
Введите username пользователя, за которого требуется отдать голос.", ParseMode.Html, replyMarkup: keyboard);
            }
            else if (msg.Equals("unvote"))
            {
                var votedUsersList = Database.GetMyVotedUsers(Username);

                if (votedUsersList.Count == 0)
                {
                    await SendMessageAsync(_chatId, "Вы еще ни за кого не голосовали.");
                    return;
                }

                CurrentStep = RequestSteps.UnvoteUser;
                _unvoteMessage = await SendMessageAsync(_chatId, @"<b>Забрать голос</b>", ParseMode.Html, replyMarkup: GetMarkupForUnvote(votedUsersList));
            }
            else if (msg.Equals("escrowlist"))
            {
                await ProcessEscrowList();
            }
            else if (msg.Contains("Выключить оповещения"))
            {
                Console.WriteLine(Username + " Выключить оповещения");
                Database.DeleteUserFromNotifications(Username);

                await SendMessageAsync(_chatId, @"Оповещения выключены");
                await Start();
            }
            else if (msg.Contains("Включить оповещения"))
            {
                Console.WriteLine(Username + " Включить оповещения");

                if (!Database.IsNotificationsRowExistForUser(Username))
                    Database.AddUserForNotifications(Username, _chatId);

                await SendMessageAsync(_chatId, @"Оповещения включены");
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

            if (msg.Equals("Отмена"))
            {
                Console.WriteLine(Username + " Отмена");
                await Start();
                return;
            }

            string[] splitted = msg.Split(' ');

            if (splitted.Length < 3 || !int.TryParse(splitted[0], out _quantity) || !float.TryParse(splitted[2], out _fee))
            {
                await SendMessageAsync(_chatId, @"Ошибочный ввод. Попробуйте еще раз.");
                return;
            }

            _currency = splitted[1];

            CurrentStep = RequestSteps.EnterBank;
            var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // first row
                    {
                        InlineKeyboardButton.WithCallbackData("Отмена"),
                    }
                });
            await SendMessageAsync(_chatId, @"Введите название банка", ParseMode.Default, keyboard);
        }

        private async Task ProcessBank(string msg)
        {
            await Task.Delay(100);

            if (msg.Equals("Отмена"))
            {
                Console.WriteLine(Username + " Отмена");
                await Start();
                return;
            }

            if (string.IsNullOrEmpty(msg))
            {
                await SendMessageAsync(_chatId, @"Ошибочный ввод. Попробуйте еще раз.");
                return;
            }

            _bank = msg;

            CurrentStep = RequestSteps.EnterEndDate;
            var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // first row
                    {
                        InlineKeyboardButton.WithCallbackData("Отмена"),
                    }
                });
            await SendMessageAsync(_chatId, @"Введите длительность заявки в днях", ParseMode.Default, keyboard);
        }

        private async Task ProcessEndDate(string msg)
        {
            await Task.Delay(100);

            if (msg.Equals("Отмена"))
            {
                Console.WriteLine(Username + " Отмена");
                await Start();
                return;
            }

            if (!int.TryParse(msg, out _daysQuantity))
            {
                await SendMessageAsync(_chatId, @"Ошибочный ввод. Попробуйте еще раз.");
                return;
            }

            int reqId = Database.AddRequest(Username, _reqType, _quantity, _currency, _bank, _fee, DateTime.Now, DateTime.Now + TimeSpan.FromDays(_daysQuantity));
            
            await SendMessageAsync(_chatId, @"Ваша заявка успешно размещена");
            await Start();

            string requestString = Database.GetRequest(reqId);

            await SendNotifications(string.Format(@"<b>Новая заявка</b>
{0}", requestString));
        }

        private async Task SendNotifications(string message)
        {
            var forNotif = Database.GetUserlistForNotifications(Username);
            foreach(var chatId in forNotif)
            {
                await SendMessageAsync(chatId, message);
            }
        }

        private async Task ProcessChangeQuantityAndFee(string msg)
        {
            await Task.Delay(100);

            if (msg.Equals("Отмена"))
            {
                await Start();
                return;
            }

            if (!msg.Equals("Пропустить"))
            {
                string[] splitted = msg.Split(' ');

                if (splitted.Length < 3 || !int.TryParse(splitted[0], out _quantity) || !float.TryParse(splitted[2], out _fee))
                {
                    await SendMessageAsync(_chatId, @"Ошибочный ввод. Попробуйте еще раз.");
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
                        InlineKeyboardButton.WithCallbackData("Отмена"),
                        InlineKeyboardButton.WithCallbackData("Пропустить"),
                    }
                });
            await SendMessageAsync(_chatId, @"Введите название банка", ParseMode.Default, keyboard);
        }

        private async Task ProcessChangeBank(string msg)
        {
            await Task.Delay(100);

            if (msg.Equals("Отмена"))
            {
                await Start();
                return;
            }

            if (!msg.Equals("Пропустить"))
            {
                _bank = msg;
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
                        InlineKeyboardButton.WithCallbackData("Отмена"),
                        InlineKeyboardButton.WithCallbackData("Пропустить"),
                    }
                });
            await SendMessageAsync(_chatId, @"Введите длительность заявки в днях", ParseMode.Default, keyboard);
        }

        private async Task ProcessChangeEndDate(string msg)
        {
            await Task.Delay(100);

            if (msg.Equals("Отмена"))
            {
                await Start();
                return;
            }

            if (!msg.Equals("Пропустить"))
            {
                if (!int.TryParse(msg, out _daysQuantity))
                {
                    await SendMessageAsync(_chatId, @"Ошибочный ввод. Попробуйте еще раз.");
                    return;
                }
            }
            else
            {
                _daysQuantity = -1;
            }

            Database.UpdateRequest(_reqIdForUpdate, Username, _quantity, _currency, _bank, _fee,
                DateTime.Now, _daysQuantity > 0 ? DateTime.Now + TimeSpan.FromDays(_daysQuantity) : DateTime.MinValue);
            
            await SendMessageAsync(_chatId, @"Ваша заявка успешно изменена");
            await Start();

            string reqString = Database.GetRequest(_reqIdForUpdate);
            await SendNotifications(string.Format(@"<b>Измененная заявка</b>
{0}", reqString));
        }

        private async Task ProcessShowMy()
        {
            await Task.Delay(100);

            var myReqs = Database.GetRequestsFor(Username);

            if (myReqs.Count == 0)
            {
                await SendMessageAsync(_chatId, @"Заявок нет");
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
                            InlineKeyboardButton.WithCallbackData("Удалить " + reqId),
                            InlineKeyboardButton.WithCallbackData("Изменить " + reqId),
                        }
                    });

                    await SendMessageAsync(_chatId, req, ParseMode.Html, keyboard);
                }
            }


        }

        private async Task ProcessShowAll()
        {
            await Task.Delay(100);

            var myReqs = Database.GetAllRequests();

            if (myReqs.Count == 0)
            {
                await SendMessageAsync(_chatId, "Заявок нет");
                return;
            }

            string result = myReqs.Aggregate((current, next) => current + "\n\n" + next);

            await SendMessageAsync(_chatId, result, ParseMode.Html);
        }

        private async Task ProcessVote(string msg)
        {
            await Task.Delay(100);

            if (msg.Equals("Отмена"))
            {
                await Start();
                return;
            }

            string votedUser = msg.TrimStart('@');

            if (!Database.IsUserRegistered(votedUser))
            {
                await SendMessageAsync(_chatId, @"Пользователь " + votedUser + @" не зарегистрирован.
Введите другой username");
                return;
            }

            if (Database.IsAlreadyVotedByUser(Username, votedUser))
            {
                await SendMessageAsync(_chatId, "Вы уже голосовали за пользователя " + votedUser + @"
Введите другой username");
                return;
            }

            if (Username.Equals(votedUser))
            {
                await SendMessageAsync(_chatId, @"Вы не можете голосовать за себя.
Введите другой username");
                return;
            }

            Database.Vote(Username, votedUser);
            int voteCount = Database.GetVotesCount(Username);

            await SendMessageAsync(_chatId, @"Вы успешно отдали свой голос за " + votedUser + @".
Осталось голосов - " + voteCount);
            await Start();
        }

        private Message _unvoteMessage;
        private async Task ProcessUnvote(string msg)
        {
            await Task.Delay(100);

            if (msg.Equals("Отмена"))
            {
                await DeleteMessageAsync(_unvoteMessage);
                await Start();
                return;
            }

            string votedUser = msg.TrimStart('@');

            Database.Unvote(Username, votedUser);
            int voteCount = Database.GetVotesCount(Username);
            await DeleteMessageAsync(_unvoteMessage);
            await SendMessageAsync(_chatId, "Вы забрали свой голос у " + votedUser + @".
Доступное количество голосов - " + voteCount);
            await Start();
        }

        private async Task ProcessEscrowList()
        {
            var list = Database.GetEscrowList();

            if (list.Count == 0)
            {
                await SendMessageAsync(_chatId, "Пусто");
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

            keyboard[usernameList.Count] = new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData("Отмена") };

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
    }
}
