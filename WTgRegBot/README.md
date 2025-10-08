# WTgRegBot

- TelegramBot + WTelegram + .NET 8 + SqLite
- Управление профилями через TG API. 

---

- Получить API данные тут - https://my.telegram.org/auth

---
# Работа:

- Добавление TG профиля через номер телефона и API ключей.
- Авторизация по коду из Телеги
- Сохранение профилей в SqLite
- Можно посмотреть все добавленные профили
- Структура: Program.cs(UI бота) + Bake.cs(Внутренность бота) 

---

# Чтобы запустить:

1. Получаем данные API ID и API Hash на https://my.telegram.org/auth

2. Устанавливаем пакеты NuGet в проект:
dotnet add package Telegram.Bot
dotnet add package WTelegramClient
dotnet add package Microsoft.Data.Sqlite

3. Указываем токен бота в коде внутри ковычек
BOT_TOKEN="твой_токен"

4. Запускаем проект заходим в бота и проверяем. 

---

- Данный проект просто является примером авторизации, в которой можно хранить несколько профилей в базе данных SqLite, а позже эти профили использовать для асинхронной работы с Telegram API.

---
