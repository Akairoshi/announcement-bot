<p align="center">
  <image width="600" src="assets/Frame.png"/>
<p/>
  
# Бот для объявлений (Announcement Bot)

## Описание

Telegram-бот для рассылки объявлений по категориям. Администраторы создают объявления для своих категорий, пользователи подписываются и получают уведомления через очередь доставки.

## Роли

### Пользователь

Может подписываться на категории и получать уведомления. Подать заявку на роль администратора.

| Команда | Описание |
|---------|----------|
| `/start` | Активация бота, отображение главного меню |
| `/profile` | Просмотр профиля и списка подписок |
| `/subscribe` | Подписка / отписка от категорий через инлайн-кнопки |
| `/list_announcement` | Просмотр последних объявлений по своим подпискам |
| `/admin_request` | Подача заявки на роль администратора |

### Администратор

Назначается SuperAdmin'ом. Создаёт объявления для назначенных категорий, управляет личными шаблонами. Может передать права другому пользователю через заявку на переназначение.

| Команда | Описание |
|---------|----------|
| `/profile` | Просмотр профиля, назначенных категорий и подписок |
| `/make_announcement` | Создать объявление (выбор категории → шаблон → текст → превью → отправка) |
| `/list_announcement` | Просмотр объявлений своих категорий со статистикой доставки |
| `/list_template` | Список своих шаблонов |
| `/add_template` | Создать шаблон с поддержкой плейсхолдеров `{Переменная}` |
| `/update_template` | Изменить название и/или текст шаблона |
| `/remove_template` | Удалить шаблон с подтверждением |
| `/admin_request` | Подать заявку на переназначение прав другому пользователю |

### Супер Администратор

Назначается через конфиг (`SuperAdmin.UserId`) при старте приложения — не может быть разжалован через бота. Управляет категориями, администраторами и заявками. Видит все объявления.

| Команда | Описание |
|---------|----------|
| `/profile` | Просмотр профиля и подписок |
| `/make_announcement` | Создать объявление для любой категории |
| `/list_announcement` | Просмотр всех объявлений со статистикой доставки |
| `/list_category` | Список всех категорий |
| `/add_category` | Добавить категорию (с проверкой уникальности) |
| `/update_category` | Переименовать категорию через инлайн-кнопки |
| `/remove_category` | Удалить категорию — подписчики уведомляются, объявления теряют привязку (CategoryId → null) |
| `/list_admin` | Список всех администраторов |
| `/remove_admin` | Понизить администратора до роли User через инлайн-кнопки |
| `/list_admin_request` | Просмотр и обработка входящих заявок |
| `/list_template` | Список своих шаблонов |
| `/add_template` | Создать шаблон |
| `/update_template` | Изменить шаблон |
| `/remove_template` | Удалить шаблон |

## Технологии

| Компонент | Технология |
|-----------|------------|
| Язык | C# / .NET 10 |
| ORM | Entity Framework Core 10 |
| База данных | PostgreSQL (Npgsql) |
| Telegram | Telegram.Bot 22.x |
| CQRS | MediatR 12.5 |
| Валидация | FluentValidation 12 |
| Хостинг | Worker Service (Long Polling, без HTTP) |


## Архитектура

Чистая архитектура (Clean Architecture) + CQRS через MediatR.

```
AnnouncementBot/
├── src/
│   ├── AnnouncementBot.Domain/              # Сущности, перечисления, интерфейсы репозиториев
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── Interfaces/
│   │
│   ├── AnnouncementBot.Application/         # Бизнес-логика, команды, запросы, DTOs
│   │   ├── Commands/
│   │   │   ├── AdminRequests/
│   │   │   ├── Announcements/
│   │   │   ├── Categories/
│   │   │   ├── Subscriptions/
│   │   │   ├── Templates/
│   │   │   └── Users/
│   │   ├── Common/
│   │   │   ├── Behaviors/                   # ValidationBehavior, AuditLogBehavior
│   │   │   └── Interfaces/                  # IAuditableRequest, IConditionalAudit
│   │   ├── DTOs/
│   │   ├── Queries/
│   │   └── Validators/
│   │
│   ├── AnnouncementBot.Infrastructure/      # БД, репозитории, фоновые сервисы
│   │   ├── BackgroundServices/
│   │   │   ├── AnnouncementDeliveryWorker   # Рассылка (каждые N минут, до 3 попыток)
│   │   │   └── AnnouncementCleanerWorker    # Удаление устаревших объявлений (30 дней)
│   │   ├── Configurations/                  # BotConfiguration, SuperAdminConfiguration
│   │   └── Persistence/
│   │       ├── Configurations/              # EF Core entity configurations
│   │       ├── Migrations/
│   │       ├── Repositories/
│   │       ├── AppDbContext.cs
│   │       └── UnitOfWork.cs
│   │
│   └── AnnouncementBot.Presentation/        # Telegram-слой, точка входа
│       ├── Middlewares/                      # ExceptionHandling, UserRegistration, Authorization
│       └── Telegram/
│           ├── Callbacks/                    # ICallbackHandler реализации
│           ├── Commands/                     # IBotCommand реализации
│           ├── FSM/                          # Состояния диалогов (In-Memory FSM)
│           │   └── States/
│           ├── Keyboards/
│           ├── UpdateHandler.cs
│           └── TelegramBotWorker.cs
│
├── docs/
│   ├── specification.md
│   ├── database.md
│   └── database.dbml
│
├── assets/
├── appsettings.example.json
├── .gitignore
├── README.md
└── Announcementbot.slnx
```

### Ключевые паттерны

**CQRS + MediatR** — каждая операция — отдельный Command или Query класс. Pipeline behaviors (`ValidationBehavior` → `AuditLogBehavior`) оборачивают все `IRequest<TResponse>` команды.

**IBotCommand / ICallbackHandler** — каждая Telegram-команда и каждый тип callback'а — отдельный класс. `UpdateHandler` роутит входящие события к нужному обработчику.

**In-Memory FSM** — многошаговые диалоги (создание объявления, заявки, шаблонов) хранятся как `IConversationState` в `ConversationStateStorage` (Singleton). Scope-зависимости получаются через `IServiceScopeFactory`.

**AuditLog через pipeline** — все команды, реализующие `IAuditableRequest` и `IRequest<TResponse>`, автоматически пишут запись в `AuditLogs` через `AuditLogBehavior` после выполнения. Важно: команды должны наследовать `IRequest<Unit>` (явный generic), а не `IRequest` (non-generic) — иначе MediatR не включает их в pipeline behaviors.


## Flow команд

### `/start`
- Регистрирует пользователя (если не существует) через `UserRegistrationMiddleware`
- Показывает главное меню с кнопками в зависимости от роли

### `/profile`
- Отображает ID, username, роль
- Для User: список подписанных категорий
- Для Admin: назначенные категории + подписки
- Для SuperAdmin: подписки + обновляет Reply-клавиатуру

### `/subscribe`
- Показывает все категории инлайн-кнопками
- 🔔 отмечены уже подписанные
- Нажатие переключает подписку (`ToggleSubscriptionCommand`) и обновляет кнопки на месте

### `/make_announcement`
1. Выбор категории инлайн-кнопками (доступные администратору/все для SA)
2. Выбор шаблона инлайн-кнопками или "Без шаблона"
3. Поочерёдный ввод значений для плейсхолдеров `{Переменная}`
4. Предпросмотр готового текста с кнопками "✅ Отправить" / "❌ Отмена"
5. `CreateAnnouncementCommand` — сохраняет объявление и создаёт `DeliveryStatus` для всех подписчиков

### `/remove_category`
1. Выбор категории инлайн-кнопками
2. Диалог подтверждения показывает количество подписчиков
3. При подтверждении — категория удаляется, `CategoryId` в объявлениях становится `null`, все подписчики получают уведомление в личку

### `/admin_request`
- **User** → подаёт заявку на назначение (Assignment): вводит причину
- **Admin** → подаёт заявку на переназначение (Reassignment): вводит целевого пользователя (ID или @username) и причину

### `/list_admin_request`
- SuperAdmin видит список ожидающих заявок
- **Assignment (одобрить)** → вводит название существующей или новой категории → пользователь получает роль Admin + доступ к категории
- **Reassignment (одобрить)** → категории переходят к новому пользователю, подавший теряет роль Admin
- **Отклонить** → заявитель получает уведомление об отказе

## Pipeline behaviors (MediatR)

| Behavior | Порядок | Назначение |
|----------|---------|------------|
| `ValidationBehavior<,>` | 1 (внешний) | Запускает FluentValidation перед хендлером |
| `AuditLogBehavior<,>` | 2 (внутренний) | Пишет AuditLog после успешного выполнения хендлера |

`AuditLogBehavior` срабатывает для команд, реализующих `IAuditableRequest`. Условная запись — через `IConditionalAudit.ShouldAudit(response)` (например, `EnsureUserExistsCommand` пишет лог только при первой регистрации).

## Фоновые сервисы

### AnnouncementDeliveryWorker
Запускается с интервалом, заданным в `BotConfiguration.SenderInterval` (минуты). Выбирает записи `DeliveryStatus` со статусом `Pending`/`Failed` и `RetryCount < 3`. Пытается отправить сообщение через Telegram API. Обрабатывает коды ошибок Telegram (400/401/403/404/429/500) и сетевые ошибки (`NetworkError`) — последние не увеличивают `RetryCount`.

### AnnouncementCleanerWorker
Запускается с тем же интервалом. Удаляет объявления старше 30 дней. `DeliveryStatus` удаляются каскадно.
