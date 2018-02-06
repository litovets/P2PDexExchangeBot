using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using Telegram.Bot.Types.ReplyMarkups;

using LD = P2PExchangeBot.LanguageDictionary;

namespace P2PExchangeBot
{  

    class Program
    {
        //private static readonly TelegramBotClient Bot = new TelegramBotClient("457479546:AAHhonN0rtZYf3Mp3mfSTuDbRlrey3KHOy8"); //Test Bot
        private static readonly TelegramBotClient Bot = new TelegramBotClient("495547845:AAHoqJ8ornC--R8TBy52aqpnRA67LGyiI0M");  //Release Bot

        private static Dictionary<string, UserRequestProcess> _requestsDic = new Dictionary<string, UserRequestProcess>();

        static Chat _masterChat;
        static ChatMember[] _masterChatAdmins;

        static System.Threading.Timer _timer;

        static void Main(string[] args)
        {
            _timer = new System.Threading.Timer(CheckTimeAndCleanDatabase, null, TimeSpan.FromSeconds(1), TimeSpan.FromHours(1));
            Task.Run(RunApiAsync);
            System.Console.ReadKey();
        }

        static void CheckTimeAndCleanDatabase(object state)
        {
            if (DateTime.Now.Hour != 0)
                return;

            Console.WriteLine("Очистка БД");
            Database.DeleteOldRequests();
        }

        static async Task RunApiAsync()
        {
            var me = await Bot.GetMeAsync();
            Console.Title = me.FirstName;
            System.Console.WriteLine("Hello! My name is " + me.FirstName);

            Bot.OnCallbackQuery += OnCallbackQueryReceived;
            Bot.OnMessage += OnMessage;
            Bot.OnMessageEdited += OnMessage;
            Bot.OnInlineQuery += OnInlineQuery;
            Bot.OnInlineResultChosen += OnInlineResultChosen;
            Bot.OnReceiveError += OnReceiveError;

            long masterChatId = Database.GetMasterChatId();
            if (masterChatId != 0)
            {
                try
                {
                    _masterChat = await Bot.GetChatAsync(new ChatId(masterChatId));
                    _masterChatAdmins = await Bot.GetChatAdministratorsAsync(masterChatId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception during setting master chat. Message: " + ex.Message);
                    _masterChat = null;
                    _masterChatAdmins = null;
                }
            }

            Bot.StartReceiving();
            Console.ReadKey();
            Bot.StopReceiving();
        }

        private static void OnReceiveError(object sender, ReceiveErrorEventArgs e)
        {
            Console.WriteLine("Error: " + e.ApiRequestException.Message);
        }

        private static void OnInlineResultChosen(object sender, ChosenInlineResultEventArgs e)
        {
            Console.WriteLine($"Received choosen inline result: {e.ChosenInlineResult.ResultId}");
        }

        private static async void OnInlineQuery(object sender, InlineQueryEventArgs e)
        {
            InlineQueryResult[] results = {
                new InlineQueryResultLocation
                {
                    Id = "1",
                    Latitude = 40.7058316f, // displayed result
                    Longitude = -74.2581888f,
                    Title = "New York",
                    InputMessageContent = new InputLocationMessageContent // message if result is selected
                    {
                        Latitude = 40.7058316f,
                        Longitude = -74.2581888f,
                    }
                },

                new InlineQueryResultLocation
                {
                    Id = "2",
                    Longitude = 52.507629f, // displayed result
                    Latitude = 13.1449577f,
                    Title = "Berlin",
                    InputMessageContent = new InputLocationMessageContent // message if result is selected
                    {
                        Longitude = 52.507629f,
                        Latitude = 13.1449577f
                    }
                }
            };

            await Bot.AnswerInlineQueryAsync(e.InlineQuery.Id, results, isPersonal: true, cacheTime: 0);
        }

        private static Message _lastMessage;

        private static async void OnMessage(object sender, MessageEventArgs e)
        {
            var message = e.Message;
                       
            if (message == null || message.Type != MessageType.TextMessage) return;

            if (message.Chat.Type == ChatType.Supergroup || message.Chat.Type == ChatType.Group)
            {
                await HandleGroupMessage(message);
            }
            else if (message.Chat.Type == ChatType.Private)
            {
                await HandlePrivateMessage(message);
            }
        }

        private static async Task HandleGroupMessage(Message message)
        {
            if (message.Text.StartsWith("/setmasterchat"))
            {
                if (_masterChat != null)
                    return;

                if (string.IsNullOrEmpty(message.From.Username))
                {
                    await SendMessageAsync(message.Chat.Id, @"Вам нужно установить ваш Username", parseMode: ParseMode.Html,
                    replyMarkup: new ReplyKeyboardRemove());
                    return;
                }

                if (_masterChat == null)
                    _masterChat = message.Chat;

                if (_masterChatAdmins == null)
                {
                    _masterChatAdmins = await Bot.GetChatAdministratorsAsync(new ChatId(_masterChat.Id));
                }

                bool isUserAdmin = false;
                foreach (var chatMember in _masterChatAdmins)
                {
                    if (message.From.Username == chatMember.User.Username)
                    {
                        isUserAdmin = true;
                        break;
                    }
                }

                if (!isUserAdmin)
                {
                    _masterChat = null;
                    _masterChatAdmins = null;
                    return;
                }

                Database.SetMasterChatId(message.Chat.Id);
                _masterChat = message.Chat;
                _masterChatAdmins = await Bot.GetChatAdministratorsAsync(_masterChat.Id);

                await SendMessageAsync(message.Chat.Id, "Успех!");
            }
            else if (message.Text.StartsWith("/list"))
            {
                if (_masterChat == null || _masterChatAdmins == null)
                    return;

                bool isUserAdmin = false;
                foreach (var chatMember in _masterChatAdmins)
                {
                    if (message.From.Username == chatMember.User.Username)
                    {
                        isUserAdmin = true;
                        break;
                    }
                }

                if (!isUserAdmin)
                    return;

                var reqList = Database.GetAllRequests(message.From.Username);

                if (reqList.Count == 0)
                {
                    await SendMessageAsync(message.Chat.Id, LD.GetTranslate(message.From.Username, LD.EmptyKey));
                }
                else
                {
                    string result = reqList.Aggregate((current, next) => current + "\n\n" + next);
                    _lastMessage = await SendMessageAsync(message.Chat.Id, result, ParseMode.Html);
                }
            }
            else if (message.Text.StartsWith("/register"))
            {
                if (_masterChat == null)
                    return;

                if (string.IsNullOrEmpty(message.From.Username))
                {
                    await SendMessageAsync(message.Chat.Id, @"Для регистрации Вам нужно установить ваш Username 
(For registration you need to set your username)", parseMode: ParseMode.Html,
                    replyMarkup: new ReplyKeyboardRemove());
                    return;
                }

                if (Database.IsUserRegistered(message.From.Username))
                {
                    await SendMessageAsync(message.Chat.Id, 
                        string.Format(LD.GetTranslate(message.From.Username, LD.UsernameAlreadyRegisteredKey), message.From.Username), parseMode: ParseMode.Html,
                    replyMarkup: new ReplyKeyboardRemove());
                    return;
                }

                Database.AddUser(message.From.Username);
                await SendMessageAsync(message.Chat.Id, string.Format(LD.GetTranslate(message.From.Username, LD.UserRegisteredKey), message.From.Username));
            }
            else if (message.Text.StartsWith("/unregister"))
            {
                if (string.IsNullOrEmpty(message.From.Username))
                {
                    await SendMessageAsync(message.Chat.Id, @"Вам нужно установить ваш Username в Телеграм (Please, set your username in Telegram first)", parseMode: ParseMode.Html,
                    replyMarkup: new ReplyKeyboardRemove());
                    return;
                }

                if (_masterChat == null || _masterChatAdmins == null)
                    return;

                bool isUserAdmin = false;
                foreach (var chatMember in _masterChatAdmins)
                {
                    if (message.From.Username == chatMember.User.Username)
                    {
                        isUserAdmin = true;
                        break;
                    }
                }

                if (!isUserAdmin)
                    return;

                string username = message.Text.Replace("/unregister", "").Trim('@', ' ');
                
                if (string.IsNullOrEmpty(username))
                {
                    await SendMessageAsync(message.Chat.Id, @"Пожалуйста, введите команду в виде: <b>/unregister 'username'</b>
Подставьте вместо '<username>' имя пользователя.", ParseMode.Html);
                    return;
                }
                
                if (!Database.IsUserRegistered(username))
                {
                    await SendMessageAsync(message.Chat.Id, @"Пользователь с таким именем не зарегистрирован - " + username);
                    return;
                }

                Database.DeleteUser(username);
                await SendMessageAsync(message.Chat.Id, "Пользователь " + username + " удален.");
            }
            else if (message.Text.StartsWith("/escrowlist"))
            {
                var escrowList = Database.GetEscrowList();

                if (escrowList.Count == 0)
                {
                    await SendMessageAsync(message.Chat.Id, "Пусто (Empty)");
                    return;
                }

                string result = escrowList.Aggregate((current, next) => current + "\n" + next);
                await SendMessageAsync(message.Chat.Id, result, ParseMode.Html);
            }
            else
            {
                var usage = @"<b>Использование:</b>
/setmasterchat - Зарегистрировать мастер-чат(админ)
/list   - Вывод списка заявок (админ)
/register - зарегистрироваться
/escrowlist - Вывод списка гарантов
/unregister 'username' - удалить юзера (админ)
";
                await SendMessageAsync(message.Chat.Id, usage, parseMode: ParseMode.Html,
                        replyMarkup: new ReplyKeyboardRemove());
            }
        }

        private static async Task HandlePrivateMessage(Message message)
        {
            if (string.IsNullOrEmpty(message.From.Username))
            {
                await SendMessageAsync(message.Chat.Id, @"Для работы со мной Вам нужно установить ваш Username в Телеграм.
Сделайте это и попробуйте еще раз введя команду /start

You need to set your username шт Telegram first.", parseMode: ParseMode.Html,
                replyMarkup: new ReplyKeyboardRemove());
                return;
            }

            if (!Database.IsUserRegistered(message.From.Username))
            {
                await SendMessageAsync(message.Chat.Id, LD.GetTranslate(message.From.Username, LD.PleaseRegisterGroupChatKey));
                return;
            }

            if (message.Text.StartsWith("/start"))
            {
                if (!_requestsDic.ContainsKey(message.From.Username))
                {
                    UserRequestProcess userReq = new UserRequestProcess(Bot, message.From.Username, message.Chat.Id);
                    _requestsDic.Add(message.From.Username, userReq);
                }

                var req = _requestsDic[message.From.Username];
                if (req.CurrentStep == RequestSteps.Start)
                {
                    await req.Start();
                }
            }/*
            else if (message.Text.StartsWith("/startgame"))
            {
                await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // first row
                    {
                        InlineKeyboardButton.WithCallbackData("Удар в голову"),
                        InlineKeyboardButton.WithCallbackData("Блок головы"),
                    },
                    new[] // second row
                    {
                        InlineKeyboardButton.WithCallbackData("Удар в грудь"),
                        InlineKeyboardButton.WithCallbackData("Блок груди"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Удар в пояс"),
                        InlineKeyboardButton.WithCallbackData("Блок пояса"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Удар в ноги"),
                        InlineKeyboardButton.WithCallbackData("Блок ног"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Test")
                    }
                });

                await Task.Delay(500); // simulate longer running task

                if (_lastMessage != null)
                    await Bot.DeleteMessageAsync(message.Chat.Id, _lastMessage.MessageId);

                _lastMessage = await Bot.SendTextMessageAsync(message.Chat.Id, "Choose",
                    replyMarkup: keyboard);
            }
            else if (message.Text.StartsWith("/keyboard")) // send custom keyboard
            {
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new [] // first row
                    {
                        new KeyboardButton("1.1"),
                        new KeyboardButton("1.2"),
                    },
                    new [] // last row
                    {
                        new KeyboardButton("2.1"),
                        new KeyboardButton("2.2"),
                    }
                });

                if (_lastMessage != null)
                    await Bot.DeleteMessageAsync(message.Chat.Id, _lastMessage.MessageId);

                _lastMessage = await Bot.SendTextMessageAsync(message.Chat.Id, "Choose",
                    replyMarkup: keyboard);
            }*/
            else if (_requestsDic.ContainsKey(message.From.Username) && _requestsDic[message.From.Username].CurrentStep != RequestSteps.Start)
            {
                await _requestsDic[message.From.Username].ProcessMessage(message.Text);
            }
            else
            {
                var usage = @"<b>Использование:</b>
/start   - Начало процесса

<b>Usage:</b>
/start - start process";
                

                await SendMessageAsync(message.Chat.Id, usage, ParseMode.Html,
                    replyMarkup: new ReplyKeyboardRemove());
            }
        }

        private async static Task<Message> SendMessageAsync(long chatId, string msg, ParseMode parseMode = ParseMode.Default, IReplyMarkup replyMarkup = null)
        {
            try
            {
                if (_lastMessage != null)
                    await Bot.DeleteMessageAsync(chatId, _lastMessage.MessageId);
            }
            catch
            {
                _lastMessage = null;
            }

            try
            {
                return await Bot.SendTextMessageAsync(chatId, msg, parseMode,
                    replyMarkup: replyMarkup);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }

            return null;
        }

        private static async void OnCallbackQueryReceived(object sender, CallbackQueryEventArgs e)
        {
            string message = e.CallbackQuery.Data;
            string username = e.CallbackQuery.From.Username;

            if (_requestsDic.ContainsKey(username))
            {
                var userReq = _requestsDic[username];
                await userReq.ProcessMessage(message);
            }

            try
            {
                await Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Exception during request {0} from {1}. Message: {2}", e.CallbackQuery.Data, username, ex.Message));
            }
        }
    }
}
