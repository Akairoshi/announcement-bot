# ADR-005: In-Memory FSM для многошаговых диалогов

- **Статус:** Принято
- **Дата:** 2026-05-28

---

## Контекст

Ряд команд бота требует многошагового диалога — пользователь отвечает на несколько вопросов по очереди, и бот должен помнить контекст между сообщениями. Примеры: `/admin_request`, `/make_announcement`, `/add_template`.

Telegram асинхронный — каждое сообщение это отдельный `Update`. Бот не "ждёт" ответа, поэтому между шагами диалога нужно явно хранить состояние.

---

## Решение

**Паттерн FSM (Finite State Machine)** — для каждого пользователя хранится текущее состояние диалога (`IConversationState`). `UpdateHandler` проверяет наличие активного состояния перед роутингом к командам:

```csharp
var activeState = _stateStorage.Get(userId);
if (activeState is not null)
{
    await activeState.HandleAsync(botClient, message, ct);
    return;
}
```

Хранилище состояний — `ConversationStateStorage` на основе `Dictionary<long, IConversationState>`, зарегистрированный как `Singleton`.

### Почему IServiceScopeFactory вместо IServiceProvider

FSM состояния живут между запросами — дольше чем один `Scoped` lifetime. Если передать `IServiceProvider` из scope, он будет уничтожен после первого запроса, и следующий вызов `HandleAsync` упадёт с `ObjectDisposedException`.

`IServiceScopeFactory` — Singleton, живёт всё время приложения. Каждое обращение к БД или MediatR создаёт новый scope явно:

```csharp
using var scope = _scopeFactory.CreateScope();
var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
```

---

## Альтернативы

### Redis

Состояния хранятся в Redis — переживают перезапуск приложения, работают в multi-instance окружении. Избыточно для бота на одной машине, добавляет внешнюю зависимость.

### PostgreSQL

Состояния сериализуются в БД. Надёжно, но медленно для каждого шага диалога. Сложная сериализация объектов состояний.

### Хранение состояния в самом сообщении (callback data)

Передавать весь контекст в `callback_data` кнопок. Ограничение Telegram — 64 байта на callback_data. Не подходит для хранения произвольного контекста диалога.

---

## Последствия

- Состояния не переживают перезапуск приложения — при рестарте бота активные диалоги сбрасываются
- Подходит для одного инстанса на выделенном ПК
- При необходимости масштабирования — замена `ConversationStateStorage` на Redis-реализацию без изменения остальной архитектуры
