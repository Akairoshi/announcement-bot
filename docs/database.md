# База данных

PostgreSQL. Схема в формате DBML — `database.dbml`.

---

## Таблицы

- [Users](#users)
- [Categories](#categories)
- [AdminCategoryAccess](#admincategoryaccess)
- [Subscriptions](#subscriptions)
- [Templates](#templates)
- [Announcements](#announcements)
- [DeliveryStatuses](#deliverystatuses)
- [AdminRequests](#adminrequests)
- [AuditLogs](#auditlogs)

---

## Users

Пользователи бота. Создаётся автоматически при первом обращении (`UserRegistrationMiddleware`).

| Поле | Тип | Nullable | Описание |
|------|-----|----------|----------|
| `Id` | bigint | NO | Telegram User ID — PK, не генерируется БД |
| `UserName` | varchar(32) | YES | Telegram @username. Обновляется при каждом обращении |
| `Role` | int | NO | `0` = User, `1` = Admin, `2` = SuperAdmin |
| `JoinedAt` | timestamp | NO | Дата первой регистрации в боте |

**Примечание:** SuperAdmin создаётся при старте приложения на основе `SuperAdmin.UserId` из конфига. Роль SuperAdmin восстанавливается при каждом запуске, если была изменена вне бота.

---

## Categories

Категории объявлений. Управляются только SuperAdmin'ом.

| Поле | Тип | Nullable | Описание |
|------|-----|----------|----------|
| `Id` | uuid | NO | PK, генерируется приложением |
| `Name` | varchar(100) | NO | Название. Уникальный индекс |
| `CreatedById` | bigint | NO | FK → `Users.Id` (Restrict) |
| `CreatedAt` | timestamp | NO | Дата создания |

При удалении категории: `AdminCategoryAccess` и `Subscriptions` удаляются каскадно, `Announcements.CategoryId` → `null` (SetNull), подписчики получают уведомление в личку.

---

## AdminCategoryAccess

Связь Admin ↔ Category. Определяет, какими категориями управляет администратор.

| Поле | Тип | Nullable | Описание |
|------|-----|----------|----------|
| `AdminId` | bigint | NO | FK → `Users.Id` (Cascade) |
| `CategoryId` | uuid | NO | FK → `Categories.Id` (Cascade) |

PK составной: `(AdminId, CategoryId)`.

---

## Subscriptions

Подписки пользователей на категории.

| Поле | Тип | Nullable | Описание |
|------|-----|----------|----------|
| `UserId` | bigint | NO | FK → `Users.Id` (Cascade) |
| `CategoryId` | uuid | NO | FK → `Categories.Id` (Cascade) |
| `SubscribedAt` | timestamp | NO | Дата подписки |

PK составной: `(UserId, CategoryId)`. При отписке запись удаляется. При удалении категории записи удаляются каскадно.

---

## Templates

Шаблоны объявлений. Принадлежат конкретному администратору — другие их не видят. Поддерживают плейсхолдеры вида `{Переменная}`.

| Поле | Тип | Nullable | Описание |
|------|-----|----------|----------|
| `Id` | uuid | NO | PK |
| `Name` | varchar(100) | NO | Название шаблона |
| `Text` | text | NO | Текст с плейсхолдерами |
| `CreatedById` | bigint | NO | FK → `Users.Id` (Restrict) |
| `CreatedAt` | timestamp | NO | Дата создания |

При удалении шаблона: `Announcements.TemplateId` → `null` (SetNull).

---

## Announcements

Объявления, созданные администраторами.

| Поле | Тип | Nullable | Описание |
|------|-----|----------|----------|
| `Id` | uuid | NO | PK |
| `Text` | text | NO | Итоговый текст объявления (плейсхолдеры уже заполнены) |
| `CategoryId` | uuid | **YES** | FK → `Categories.Id` (SetNull). `null` если категория удалена |
| `TemplateId` | uuid | **YES** | FK → `Templates.Id` (SetNull). `null` если без шаблона или шаблон удалён |
| `CreatedById` | bigint | NO | FK → `Users.Id` (Restrict) |
| `CreatedAt` | timestamp | NO | Дата создания |

**Жизненный цикл:** `AnnouncementCleanerWorker` удаляет записи старше 30 дней. `DeliveryStatuses` удаляются каскадно.

---

## DeliveryStatuses

Статус доставки конкретного объявления конкретному пользователю. Одна запись = одна очередь на доставку.

| Поле | Тип | Nullable | Описание |
|------|-----|----------|----------|
| `Id` | uuid | NO | PK |
| `AnnouncementId` | uuid | NO | FK → `Announcements.Id` (Cascade) |
| `UserId` | bigint | NO | FK → `Users.Id` (Cascade). Получатель |
| `Status` | int | NO | `0` = Pending, `1` = Sent, `2` = Failed |
| `ErrorStatus` | int | NO | `0` = None, `-1` = NetworkError, `400/401/403/404/429/500` = HTTP-коды Telegram API |
| `RetryCount` | int | NO | Число попыток отправки. По умолчанию `0` |
| `LastAttemptAt` | timestamp | YES | Время последней попытки. `null` если попыток не было |
| `SentAt` | timestamp | YES | Время успешной доставки |

**Retry-логика (`AnnouncementDeliveryWorker`):** запускается каждые N минут (из конфига). Выбирает записи со статусом `Pending` или `Failed` и `RetryCount < 3`. `NetworkError` не увеличивает `RetryCount` — повтор при следующем цикле. После 3 неуспешных попыток запись остаётся в `Failed`.

---

## AdminRequests

Заявки на назначение или переназначение администратора.

| Поле | Тип | Nullable | Описание |
|------|-----|----------|----------|
| `Id` | uuid | NO | PK |
| `RequesterId` | bigint | NO | FK → `Users.Id` (Restrict). Кто подаёт заявку |
| `TargetId` | bigint | YES | FK → `Users.Id` (Restrict). Целевой пользователь (только для Reassignment) |
| `Type` | int | NO | `0` = Assignment, `1` = Reassignment |
| `Details` | text | YES | Причина заявки |
| `Status` | int | NO | `0` = Pending, `1` = Approved, `2` = Rejected |
| `CreatedAt` | timestamp | NO | Дата подачи |
| `ReviewedAt` | timestamp | YES | Дата рассмотрения |
| `ReviewedById` | bigint | YES | FK → `Users.Id` (Restrict). Кто рассмотрел |

---

## AuditLogs

Журнал действий. Заполняется автоматически через `AuditLogBehavior` (MediatR pipeline) для всех команд, реализующих `IAuditableRequest`.

| Поле | Тип | Nullable | Описание |
|------|-----|----------|----------|
| `Id` | uuid | NO | PK |
| `UserId` | bigint | NO | FK → `Users.Id` (Restrict). Инициатор действия |
| `Action` | varchar(100) | NO | Тип действия: `CategoryCreated`, `CategoryUpdated`, `CategoryDeleted`, `TemplateCreated`, `TemplateUpdated`, `TemplateDeleted`, `AnnouncementCreated`, `AdminAppointed`, `AdminRemoved`, `CategorySubscribed`, `CategoryUnsubscribed`, `UserRegistered` |
| `EntityName` | varchar(100) | NO | Название сущности: `Category`, `Template`, `Announcement`, `User`, `Subscription`, `AdminRequest` |
| `EntityId` | varchar(100) | NO | ID изменённой записи |
| `Details` | text | YES | Дополнительный контекст (например, новое название при переименовании) |
| `CreatedAt` | timestamp | NO | Время события (UTC) |

---

## Связи

```
Users ──< AdminCategoryAccess >── Categories
Users ──< Subscriptions >── Categories
Users ──< Templates
Users ──< Announcements
Users ──< DeliveryStatuses
Users ──< AdminRequests (Requester)
Users ──< AdminRequests (Target, nullable)
Users ──< AdminRequests (ReviewedBy, nullable)
Users ──< AuditLogs
Announcements ──< DeliveryStatuses
Categories ──o Announcements   (nullable, SetNull при удалении категории)
Templates ──o Announcements    (nullable, SetNull при удалении шаблона)
```

---

## OnDelete поведение

| Связь | Поведение |
|-------|-----------|
| User → AdminCategoryAccess | Cascade |
| Category → AdminCategoryAccess | Cascade |
| User → Subscriptions | Cascade |
| Category → Subscriptions | Cascade |
| Category → Announcements (CategoryId) | **SetNull** |
| Template → Announcements (TemplateId) | SetNull |
| Announcement → DeliveryStatuses | Cascade |
| User → DeliveryStatuses | Cascade |
| User → Categories (CreatedById) | Restrict |
| User → Templates (CreatedById) | Restrict |
| User → Announcements (CreatedById) | Restrict |
| User → AuditLogs | Restrict |
| User → AdminRequests | Restrict |
