# ADR-004: Паттерн Command для обработки Telegram-команд

- **Статус:** Принято
- **Дата:** 2026-05-28

---

## Контекст

`UpdateHandler` получает все входящие события от Telegram. По мере роста числа команд (`/start`, `/profile`, `/subscribe` и т.д.) обработка всех команд в одном классе превращается в God Object — сотни строк `if/else` или `switch` с разной логикой в одном файле.

---

## Решение

Каждая Telegram-команда — отдельный класс реализующий интерфейс `IBotCommand`.

```csharp
public interface IBotCommand
{
    string Command { get; } // "/start", "/profile" и т.д.
    Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct);
}
```

`UpdateHandler` получает все реализации `IBotCommand` через DI и роутит к нужной:

```csharp
var command = _commands.FirstOrDefault(c => message.Text.StartsWith(c.Command));
if (command is not null)
    await command.ExecuteAsync(botClient, message, ct);
```

Регистрация в DI:

```csharp
services.AddScoped<IBotCommand, StartCommand>();
services.AddScoped<IBotCommand, ProfileCommand>();
// ...
```

---

## Альтернативы

### Все команды в UpdateHandler

Один большой `switch` или цепочка `if/else` в `HandleMessageAsync`. Просто в начале, но быстро становится нечитаемым при росте числа команд. Каждое изменение затрагивает один большой файл.

### Router через Dictionary

```csharp
var _routes = new Dictionary<string, Func<Message, Task>>
{
    ["/start"] = HandleStart,
    ["/profile"] = HandleProfile,
};
```

Быстрый lookup, но логика команд всё равно живёт в одном классе или требует отдельных методов. Хуже тестируется.

---

## Последствия

- Каждая команда изолирована — легко найти, изменить, протестировать
- Добавление новой команды — новый класс, `UpdateHandler` не трогается
- Все команды регистрируются в DI — легко подменять в тестах
- `UpdateHandler` остаётся роутером без бизнес-логики
