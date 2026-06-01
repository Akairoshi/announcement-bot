# ADR-006: ICallbackHandler паттерн для обработки Callback Query

- **Статус:** Принято
- **Дата:** 2026-05-28

---

## Контекст

Telegram отправляет `CallbackQuery` когда пользователь нажимает инлайн-кнопку. По мере роста числа кнопок (подписка на категории, подтверждение удаления, выбор шаблона и т.д.) обработка всех callback'ов в одном месте становится неуправляемой.

---

## Решение

По аналогии с `IBotCommand` — каждый тип callback'а обрабатывается отдельным классом реализующим `ICallbackHandler`:

```csharp
public interface ICallbackHandler
{
    bool CanHandle(string callbackData);
    Task HandleAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct);
}
```

`UpdateHandler` получает все реализации через DI и роутит по `CanHandle`:

```csharp
var handler = callbackHandlers.FirstOrDefault(h => h.CanHandle(data));
if (handler is not null)
    await handler.HandleAsync(botClient, callbackQuery, ct);
```

Каждый handler отвечает за свой префикс callback data:

| Handler | Префикс |
|---|---|
| `SubscribeCallbackHandler` | `subscribe:` |
| `AnnouncementCallbackHandler` | `announcement_template:`, `announcement_category:`, `announcement_confirm:` |
| `AdminRequestCallbackHandler` | `admin_request:` |
| `CategoryCallbackHandler` | `category_remove_confirm:`, `category_remove_cancel` |
| `AdminRemoveCallbackHandler` | `admin_remove_confirm:`, `admin_remove_cancel` |
| `TemplateRemoveCallbackHandler` | `template_remove_confirm:`, `template_remove_cancel` |

---

## Альтернативы

### Все callback'и в UpdateHandler

Один большой `switch` по префиксу в `HandleCallbackQueryAsync`. Просто в начале, но быстро превращается в God Method при росте числа кнопок.

### Dictionary роутер

```csharp
var _routes = new Dictionary<string, Func<CallbackQuery, Task>>();
```

Быстрый lookup, но логика обработчиков всё равно где-то должна жить — либо в одном классе, либо требует отдельных методов.

---

## Последствия

- Добавление новой кнопки — новый класс, `UpdateHandler` не трогается
- Каждый handler изолирован и легко тестируется
- Префиксы callback data должны быть уникальными — конфликт префиксов приведёт к вызову неправильного handler'а
