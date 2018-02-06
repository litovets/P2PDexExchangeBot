using System.Collections.Generic;

namespace P2PExchangeBot
{
    enum Languages
    {
        English,
        Russian
    }

    static class LanguageDictionary
    {
        public static Languages DefaultLanguage = Languages.Russian;

        public const string EnglishKey = "EnglishKey";
        public const string RussianKey = "RussianKey";
        public const string SetUsernameKey = "SetUsernameKey";
        public const string EmptyKey = "EmptyKey";
        public const string UsernameAlreadyRegisteredKey = "UsernameAlreadyRegisteredKey";
        public const string UserRegisteredKey = "UserRegisteredKey";
        public const string PleaseRegisterGroupChatKey = "PleaseRegisterGroupChatKey";
        public const string BuyKey = "BuyKey";
        public const string SellKey = "SellKey";
        public const string ShowMyReqKey = "ShowMyReqKey";
        public const string ShowAllReqKey = "ShowAllReqKey";
        public const string VoteKey = "VoteKey";
        public const string UnvoteKey = "UnvoteKey";
        public const string EscrowListKey = "EscrowListKey";
        public const string EnableNotifKey = "EnableNotifKey";
        public const string DisableNotifKey = "DisableNotifKey";
        public const string StartMessageKey = "StartMessageKey";
        public const string CancelKey = "CancelKey";
        public const string SkipKey = "SkipKey";
        public const string SellingMsgKey = "SellingMsgKey";
        public const string BuyingMsgKey = "BuyingMsgKey";
        public const string RemoveKey = "RemoveKey";
        public const string ChangeKey = "ChangeKey";
        public const string RemoveErrorKey = "RemoveErrorKey";
        public const string RemoveSuccessKey = "RemoveSuccessKey";
        public const string ChangeErrorKey = "ChangeErrorKey";
        public const string ChangingMsgKey = "ChangingMsgKey";
        public const string ZeroVotesKey = "ZeroVotesKey";
        public const string VotingMsgKey = "VotingMsgKey";
        public const string VoteListEmptyKey = "VoteListEmptyKey";
        public const string NotificationsDisabledKey = "NotificationsDisabledKey";
        public const string NotificationsEnabledKey = "NotificationsEnabledKey";
        public const string WrongInputKey = "WrongInputKey";
        public const string EnterBankNameKey = "EnterBankNameKey";
        public const string EnterReqDurationKey = "EnterReqDurationKey";
        public const string SuccessfulRequestKey = "SuccessfulRequestKey";
        public const string NewReqNotifKey = "NewReqNotifKey";
        public const string SuccessfulChangeKey = "SuccessfulChangeKey";
        public const string ChangedReqNotifKey = "ChangedReqNotifKey";
        public const string VotedUserNotRegisteredKey = "VotedUserNotRegisteredKey";
        public const string VotedUserAlreadyVotedKey = "VotedUserAlreadyVotedKey";
        public const string VotedUserIsMySelfKey = "VotedUserIsMySelfKey";
        public const string VoteSuccessfulKey = "VoteSuccessfulKey";
        public const string UnvoteSuccessfulKey = "UnvoteSuccessfulKey";
        public const string RequestResultStringTemplate = "RequestResultStringTemplate";

        private static List<Dictionary<string, string>> _dic = new List<Dictionary<string, string>>
        {
            new Dictionary<string, string> //English
            {
                { EnglishKey, "(EN) English" },
                { RussianKey, "(RU) Русский" },
                { SetUsernameKey, "Set your 'username' first" },
                { EmptyKey, "Empty" },
                { UsernameAlreadyRegisteredKey, "Username {0} already registered" },
                { UserRegisteredKey, "{0} registered" },
                { PleaseRegisterGroupChatKey, "For work with me you need to register in group chat" },
                { BuyKey, "Buy" },
                { SellKey, "Sell" },
                { ShowMyReqKey, "Show my requests" },
                { ShowAllReqKey, "Show all requests" },
                { VoteKey, "Vote" },
                { UnvoteKey, "Unvote" },
                { EscrowListKey, "Escrow List" },
                { EnableNotifKey, "Enable Notifications" },
                { DisableNotifKey, "Disable Notifications" },
                { StartMessageKey, "<b>Select</b>" },
                { CancelKey, "Cancel" },
                { SkipKey, "Skip" },
                { SellingMsgKey, @"<b>Selling</b>

Enter amount, currency and fee.
For example:
<b>1000 bitUSD 2.0</b>"},
                { BuyingMsgKey, @"<b>Buying</b>

Enter amount, currency and fee.
For example:
<b>1000 bitUSD 2.0</b>"},
                { RemoveKey, "Remove" },
                { ChangeKey, "Change" },
                { RemoveErrorKey, "Remove error" },
                { RemoveSuccessKey, "Request #{0} was removed" },
                { ChangeErrorKey, "Change error" },
                { ChangingMsgKey, @"<b>Changing request</b>

Enter amount, currency and fee.
For example:
<b>1000 bitUSD 2.0</b>" },
                { ZeroVotesKey, "Available votes - 0" },
                { VotingMsgKey, @"<b>Voting</b>
Enter the username you want to vote for." },
                { VoteListEmptyKey, "You have not voted for anyone yet" },
                { NotificationsEnabledKey, "Notifications are ON" },
                { NotificationsDisabledKey, "Notifications are OFF" },
                { WrongInputKey, "Wrong input" },
                { EnterBankNameKey, "Enter bank name" },
                { EnterReqDurationKey, "Enter request duration in days" },
                { SuccessfulRequestKey, "Your request was successfully added" },
                { NewReqNotifKey, @"<b>New request</b>
{0}" },
                { SuccessfulChangeKey, "Your request was successfully changed" },
                { ChangedReqNotifKey, @"<b>Changed request</b>
{0}" },
                { VotedUserNotRegisteredKey, @"User {0} is not registered.
Enter another username"},
                { VotedUserAlreadyVotedKey, @"You have already voted for {0}
Enter another username"},
                { VotedUserIsMySelfKey, @"You can't vote for yourself.
Enter another username"},
                { VoteSuccessfulKey,  @"You was successful voted for {0}
You have {1} votes left" },
                { UnvoteSuccessfulKey, @"You was successful unvote for {0}
You have {1} votes left" },
                { RequestResultStringTemplate, @"<b>({0})</b>
{1} <i>wants to {2}</i> <b>{3} {4}</b> <i>with fee</i> <b>{5}%</b>.
<b>Bank</b> - {6}. 
<b>Start:</b> {7}, <b>End:</b> {8}" },
            },
            new Dictionary<string, string> //Russian
            {
                { EnglishKey, "(EN) English" },
                { RussianKey, "(RU) Русский" },
                { SetUsernameKey, "Вам нужно установить ваш Username" },
                { EmptyKey, "Пусто" },
                { UsernameAlreadyRegisteredKey, "Юзер {0} уже зарегистрирован" },
                { UserRegisteredKey, "{0} зарегистрирован" },
                { PleaseRegisterGroupChatKey, "Вы не зарегистрированы. Для работы со мной вам нужно зарегистрироваться в групповом чате." },
                { BuyKey, "Купить" },
                { SellKey, "Продать" },
                { ShowMyReqKey, "Посмотреть мои заявки" },
                { ShowAllReqKey, "Посмотреть все заявки" },
                { VoteKey, "Отдать голос" },
                { UnvoteKey, "Забрать голос" },
                { EscrowListKey, "Список гарантов" },
                { EnableNotifKey, "Включить оповещения" },
                { DisableNotifKey, "Выключить оповещения" },
                { StartMessageKey, "<b>Выбирайте</b>" },
                { CancelKey, "Отмена" },
                { SkipKey, "Пропустить" },
                { SellingMsgKey, @"<b>Продажа</b>

Введите сумму, валюту и комиссию.
Например:
<b>1000 bitUSD 2.0</b>"},
                { BuyingMsgKey, @"<b>Покупка</b>

Введите сумму, валюту и комиссию.
Например:
<b>1000 bitUSD 2.0</b>"},
                { RemoveKey, "Удалить" },
                { ChangeKey, "Изменить" },
                { RemoveErrorKey, "Ошибка удаления" },
                { RemoveSuccessKey, "Заявка №{0} удалена" },
                { ChangeErrorKey, "Ошибка изменения" },
                { ChangingMsgKey, @"<b>Изменение заявки</b>

Введите сумму, валюту и комиссию.
Например:
<b>1000 bitUSD 2.0</b>" },
                { ZeroVotesKey, "Доступное количество голосов - 0" },
                { VotingMsgKey, @"<b>Отдать голос</b>
Введите username пользователя, за которого требуется отдать голос." },
                { VoteListEmptyKey, "Вы еще ни за кого не голосовали" },
                { NotificationsEnabledKey, "Оповещения включены" },
                { NotificationsDisabledKey, "Оповещения выключены" },
                { WrongInputKey, "Ошибочный ввод." },
                { EnterBankNameKey, "Введите название банка" },
                { EnterReqDurationKey, "Введите длительность заявки в днях" },
                { SuccessfulRequestKey, "Ваша заявка успешно добавлена" },
                { NewReqNotifKey, @"<b>Новая заявка</b>
{0}" },
                { SuccessfulChangeKey, "Ваша заявка успешно изменена" },
                { ChangedReqNotifKey, @"<b>Измененная заявка</b>
{0}" },
                { VotedUserNotRegisteredKey, @"Пользователь {0} не зарегистрирован.
Введите другой username"},
                { VotedUserAlreadyVotedKey, @"Вы уже голосовали за пользователя {0}
Введите другой username"},
                { VotedUserIsMySelfKey, @"Вы не можете голосовать за себя.
Введите другой username"},
                { VoteSuccessfulKey, @"Вы успешно проголосовали за {0}
У вас осталось голосов - {1}" },
                { UnvoteSuccessfulKey, @"Вы сняли свой голос с {0}
У вас осталось голосов - {1}" },
                { RequestResultStringTemplate, @"<b>({0})</b>
{1} <i>хочет {2}</i> <b>{3} {4}</b> <i>с комиссией</i> <b>{5}%</b>.
<b>Банк</b> - {6}. 
<b>Начало:</b> {7}, <b>Окончание:</b> {8}" },
            }
        };

        public static string GetTranslate(string username, string key)
        {
            Languages lang = Database.GetUserLanguage(username);

            var dic = _dic[(int)lang];

            if (dic.ContainsKey(key))
            {
                return dic[key];
            }

            return key;
        }
    }
}
