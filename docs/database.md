# База данных

PostgreSQL. Схема в формате DBML и PDF - database.dbml/pdf.

---

## Таблицы


- [Users](#users)
- [Categories](#categories)
- [AdminCategoryAccess](#admincategoryaccess)
- [Subscriptions](#subscriptions)
- [Templates](#templates)
- [Announcements](#announcements)
- [DeliveryStatus](#deliverystatus)
- [AdminRequests](#adminrequests)
- [AuditLog](#auditlog)

---

## Users

Пользователи бота. Создаётся автоматически при первом обращении к боту (`/start`).

| Поле | Тип | Nullable | Описание |
|------|-----|----------|----------|
| `Id` | bigint | NO | Telegram User ID. Используется как PK — Telegram гарантирует уникальность |
| `UserName` | varchar | **YES** | Telegram username (`@username`). Может отсутствовать — в Telegram необязателен |
| `Role` | int | NO | Роль пользователя: `0` = User, `1` = Admin, `2` = Супер Админ |
| `JoinAt` | timestamp | NO | Дата первого обращения к боту |

**Примечание:** Основной Супер Админ создаётся при старте приложения на основе `SuperAdmin.TelegramUserId` из конфига. Его роль нельзя изменить через бота.

---

## Categories

Категории объявлений. Управляются только Супер Админ'ом.

| Поле | Тип | Nullable | Описание |
|------|-----|----------|----------|
| `Id` | uuid | NO | Первичный ключ |
| `Name` | varchar | NO | Название категории. Уникальное |
| `CreatedById` | bigint | NO | FK → `Users.Id`. Кто создал категорию |
| `CreatedAt` | timestamp | NO | Дата создания |

---

## AdminCategoryAccess

Связь Admin ↔ Category. Определяет к каким категориям имеет доступ конкретный администратор.

| Поле | Тип | Nullable | Описание |
|------|-----|----------|----------|
| `AdminId` | bigint | NO | FK → `Users.Id` |
| `CategoryId` | uuid | NO | FK → `Categories.Id` |

Первичный ключ составной: `(AdminId, CategoryId)`.

Администратор может создавать объявления и просматривать данные только по категориям из этой таблицы.

---

## Subscriptions

Подписки пользователей на категории.

| Поле | Тип | Nullable | Описание |
|------|-----|----------|----------|
| `UserId` | bigint | NO | FK → `Users.Id` |
| `CategoryId` | uuid | NO | FK → `Categories.Id` |
| `SubscribedAt` | timestamp | NO | Дата подписки |

Первичный ключ составной: `(UserId, CategoryId)`. Одна запись = одна активная подписка. При отписке запись удаляется.

---

## Templates

Шаблоны объявлений. Принадлежат конкретному администратору — другие администраторы их не видят.

| Поле | Тип | Nullable | Описание |
|------|-----|----------|----------|
| `Id` | uuid | NO | Первичный ключ |
| `Name` | varchar | NO | Название шаблона |
| `Text` | text | NO | Текст шаблона |
| `CreatedById` | bigint | NO | FK → `Users.Id`. Admin-владелец шаблона |
| `CreatedAt` | timestamp | NO | Дата создания |

---

## Announcements

Объявления, созданные администраторами.

| Поле | Тип | Nullable | Описание |
|------|-----|----------|----------|
| `Id` | uuid | NO | Первичный ключ |
| `Text` | text | NO | Текст объявления |
| `CategoryId` | uuid | NO | FK → `Categories.Id`. Категория объявления |
| `TemplateId` | uuid | **YES** | FK → `Templates.Id`. Шаблон, если использовался. Иначе `null` |
| `CreatedById` | bigint | NO | FK → `Users.Id`. Кто создал объявление |
| `CreatedAt` | timestamp | NO | Дата создания |

**Жизненный цикл:** записи автоматически удаляются через 30 дней через `CleanerService`. При удалении каскадно удаляются связанные записи в `DeliveryStatus`.

---

## DeliveryStatus

Статус доставки объявления каждому подписчику. Одна запись = одна попытка доставить объявление конкретному пользователю.

| Поле | Тип | Nullable | Описание |
|------|-----|----------|----------|
| `Id` | uuid | NO | Первичный ключ |
| `AnnouncementId` | uuid | NO | FK → `Announcements.Id` |
| `UserId` | bigint | NO | FK → `Users.Id`. Получатель |
| `Status` | int | NO | `0` = Pending, `1` = Sent, `2` = Failed |
| `RetryCount` | int | NO | Количество попыток отправки. По умолчанию `0` |
| `LastAttemptAt` | timestamp | **YES** | Время последней попытки. `null` если попыток не было |
| `SentAt` | timestamp | **YES** | Время успешной отправки. `null` если не доставлено |

**Retry-логика:** `SenderService` запускается каждые 10 минут, выбирает записи со статусом `Pending` или `Failed` и `RetryCount < 3`, пытается отправить повторно. После 3 неудачных попыток статус остаётся `Failed` — повторов больше не будет.

---

## AdminRequests

Заявки на назначение или переназначение администратора. Обрабатываются Супер Админ'ом.

| Поле | Тип | Nullable | Описание |
|------|-----|----------|----------|
| `Id` | uuid | NO | Первичный ключ |
| `RequesterId` | bigint | NO | FK → `Users.Id`. Кто подаёт заявку |
| `TargetId` | bigint | **YES** | FK → `Users.Id`. На кого направлено. Заполняется только для `Reassignment` |
| `Type` | int | NO | `0` = Appointment (пользователь хочет стать Admin), `1` = Reassignment (Admin хочет передать роль) |
| `Details` | text | YES | Причина заявки |
| `Status` | int | NO | `0` = Pending, `1` = Approved, `2` = Rejected |
| `CreatedAt` | timestamp | NO | Дата подачи заявки |
| `ReviewedAt` | timestamp | **YES** | Дата рассмотрения. `null` пока не обработана |
| `ReviewedById` | bigint | **YES** | FK → `Users.Id`. Кто обработал заявку |

---

## AuditLog

Журнал действий. Заполняется автоматически через перехват `SaveChanges` в EF Core при любом изменении данных.

| Поле | Тип | Nullable | Описание |
|------|-----|----------|----------|
| `Id` | uuid | NO | Первичный ключ |
| `UserId` | bigint | NO | FK → `Users.Id`. Кто выполнил действие |
| `Action` | varchar | NO | Тип действия. Например: `CategoryCreated`, `AdminAppointed`, `AnnouncementSent` |
| `EntityName` | varchar | NO | Название сущности. Например: `Category`, `Template`, `User` |
| `EntityId` | varchar | NO | ID изменённой сущности |
| `Details` | text | YES | Дополнительная информация. Например: старое и новое значение при изменении |
| `CreatedAt` | timestamp | NO | Дата и время события |

---

## Связи

```
Users ──< AdminCategoryAccess >── Categories
Users ──< Subscriptions >── Categories
Users ──< Templates
Users ──< Announcements >── Categories
Users ──< DeliveryStatus >── Announcements
Users ──< AdminRequests (Requester)
Users ──< AdminRequests (Target, nullable)
Users ──< AdminRequests (ReviewedBy, nullable)
Users ──< AuditLog
Announcements ──< DeliveryStatus
Templates ──o Announcements
```