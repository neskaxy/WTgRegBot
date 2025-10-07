using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

class Program
{
    const string BOT_TOKEN = "";
    static TelegramBotClient? botClient;
    static readonly ConcurrentDictionary<long, UserState> States = new();

    static async Task Main()
    {
        botClient = new TelegramBotClient(BOT_TOKEN);
        var me = await botClient.GetMe();
        Console.WriteLine($"Запустил @{me.Username}");

        Back.Notify = async (chatId, msg) =>
        {
            if (botClient != null)
            {
                try { await botClient.SendMessage(chatId, msg); } catch { }
            }
        };
        var cts = new CancellationTokenSource();
        botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
        }, cts.Token);
        Console.ReadLine();
        cts.Cancel();
    }
    static async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken ct)
    {
        if (botClient == null) return;

        if (update.Type == UpdateType.Message && update.Message?.Text != null && update.Message.From != null)
        {
            var msg = update.Message;
            var uid = msg.From.Id;
            var text = msg.Text.Trim();
            var state = States.GetOrAdd(uid, _ => new UserState());
            if (text == "/start")
            {
                var kb = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить профиль", "add_profile") },
                    new[] { InlineKeyboardButton.WithCallbackData("🧾 Мои профили", "my_profiles") }
                });
                await botClient.SendMessage(uid, "Выбери действие:", replyMarkup: kb);
                state.Mode = Mode.None;
                return;
            }
            switch (state.Mode)
            {
                case Mode.Phone:
                    state.Phone = text;
                    state.Mode = Mode.ApiId;
                    await botClient.SendMessage(uid, "📝 Введи API ID:");
                    return;
                case Mode.ApiId:
                    state.ApiId = text;
                    state.Mode = Mode.ApiHash;
                    await botClient.SendMessage(uid, "📝 Введи API HASH:");
                    return;
                case Mode.ApiHash:
                    state.ApiHash = text;
                    state.ProfileId = Guid.NewGuid();
                    var step = await Back.LoginStart(state.ProfileId, uid, state.Phone!, state.ApiId!, state.ApiHash!);
                    state.Mode = Mode.Code;
                    await botClient.SendMessage(uid, $"📝 Введи код с телеги:\n{step}");
                    return;
                case Mode.Code:
                    var result = await Back.LoginFinish(state.ProfileId, uid, text, state.Phone!, state.ApiId!, state.ApiHash!);
                    if (result.ok)
                        await botClient.SendMessage(uid, "✅ Профиль добавлен");
                    else
                        await botClient.SendMessage(uid, $"❌ Ошибка: {result.nextOrError}");
                    state.Mode = Mode.None;
                    return;
            }
        }
        else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery?.From != null)
        {
            var cq = update.CallbackQuery;
            var uid = cq.From.Id;
            var state = States.GetOrAdd(uid, _ => new UserState());
            await botClient.AnswerCallbackQuery(cq.Id);
            if (cq.Data == "add_profile")
            {
                state.Mode = Mode.Phone;
                await botClient.SendMessage(uid, "📱Введи номер телефона:");
            }
            else if (cq.Data == "my_profiles")
            {
                string list = Back.GetProfiles(uid);
                await botClient.SendMessage(uid, list);
            }
        }
    }
    static Task HandleErrorAsync(ITelegramBotClient _, Exception ex, CancellationToken __)
    {
        Console.WriteLine(ex.Message);
        return Task.CompletedTask;
    }
    class UserState
    {
        public Mode Mode { get; set; } = Mode.None;
        public string? Phone { get; set; }
        public string? ApiId { get; set; }
        public string? ApiHash { get; set; }
        public Guid ProfileId { get; set; }
    }
    enum Mode
    {
        None,
        Phone,
        ApiId,
        ApiHash,
        Code
    }
}
