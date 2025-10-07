WTgRegBot
TelegramBot + WTelegram + .NET 8 + SqLite Управление профилями через TG API.

Получить данные API тут — https://my.telegram.org/auth

Работа:

Добавление профиля TG через номер телефона и API-ключи. Авторизация по коду из Телеги Сохранение профиля в SqLite Можно посмотреть все добавленные профили Структура: Program.cs(UI бота) + Bake.cs(Внутренность бота)

Чтобы запустить:

Получаем данные API ID и API Hash на https://my.telegram.org/auth

Устанавливаем пакеты NuGet в проект: dotnet add package Telegram.Bot dotnet add package WTelegramClient dotnet add package Microsoft.Data.Sqlite

Указываем токен бота в коде внутри ковычека BOT_TOKEN="твой_токен"

Запускаем проект, заходим в бота и проверяем.

Данный проект является простой модификацией, в которой можно хранить несколько профилей в базе данных SqLite, а позже эти профили использовать для асинхронной работы с Telegram API.
