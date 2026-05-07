# ClientManager

Web API на ASP.NET Core 9 для управления клиентами (юридическими лицами и индивидуальными предпринимателями) и их учредителями. Построен на слоистой Clean Architecture с EF Core и SQL Server, с soft-delete, оптимистичной блокировкой, distributed tracing и валидацией ИНН по алгоритму ФНС.

---

## Возможности

### Доменная модель
- Два типа клиента: `Legal_Entity` (ИНН 10 цифр) и `Individual_Entrepreneur` (ИНН 12 цифр).
- Учредители — физические лица (ИНН 12 цифр), могут быть связаны с несколькими юр. лицами.
- Связь many-to-many `Client ↔ Founder` через join-сущность `ClientFounder`.
- Валидация контрольных цифр ИНН по алгоритму ФНС.
- Доменные инварианты обеспечены и в коде, и на уровне БД:
  - Длина ИНН соответствует типу клиента (CHECK constraint).
  - ИНН состоит только из цифр (CHECK constraint).
  - Учредители разрешены только клиентам типа `Legal_Entity` (CHECK на shadow-FK столбце).
  - У юр. лица всегда есть ≥1 учредитель (валидируется при создании и при удалении последнего).

### Слой персистентности
- EF Core 9 + SQL Server.
- **Soft-delete** для `Client` и `Founder` (поле `DeletedDate` + глобальный query filter), проставляется автоматически через audit-хук в `ChangeTracker`.
- **Hard-delete** для связей `ClientFounder` — сама связь не имеет жизненного цикла с аудитом.
- **Filtered unique indexes** на `INN` (`WHERE DeletedDate IS NULL`) — мягко удалённая запись сохраняет свой ИНН, но не блокирует создание новой с тем же значением.
- **Restore-by-INN** при создании: если присылают клиента/учредителя с ИНН, который уже есть в БД soft-deleted'ом, запись восстанавливается вместо ошибки уникальности.
- **Оптимистичная блокировка** через `RowVersion` (тип SQL Server `rowversion`) + HTTP-заголовки `If-Match` / `ETag`.
- Audit-метки (`CreatedDate` / `ModifiedDate` / `DeletedDate`) проставляются через `TimeProvider` (тестируемо, единое значение времени на весь `SaveChanges`).

### API
- REST-эндпоинты с версионированием (заголовок `api-version`).
- Pagination, поиск и сортировка по whitelist'у полей на `GET /api/clients`.
- JSON Patch (`application/json-patch+json`) на `PATCH`-эндпоинтах с подробным репортом ошибок через `ModelState`.
- Bulk-создание (`POST /api/clients/collection`) — атомарное, всё-или-ничего.
- Кастомный `ArrayModelBinder` для GET'а коллекции по ID'ам.
- Ответы об ошибках в формате ProblemDetails (RFC 7807) с extension'ом `correlationId`.
- ETag на GET/POST/PATCH — клиенты могут использовать `If-Match` чтобы избежать lost-update гонок.

### Cross-cutting
- **FluentValidation** на всех входных DTO, включая рекурсивный `RuleForEach` по коллекции `Founders`.
- **AutoMapper** (16.x) — единый профиль.
- **Serilog** (Console / File / [Seq](https://datalust.co/seq)) с обогащением через `LogContext`.
- **Correlation ID middleware** — читает или генерирует заголовок `X-Correlation-Id` и пушит его в Serilog-контекст.
- **OpenTelemetry** — distributed tracing для ASP.NET Core, HttpClient и SQL Client; экспортёры: Console (dev) и OTLP (prod).
- Trace ID (`Activity.TraceId`) попадают в каждую log-строку через `Serilog.Enrichers.Span`.
- **Rate limiting** (глобальный window + per-endpoint policy через `[EnableRateLimiting]`).
- **Health checks** (`/health` + Health Checks UI).
- **Глобальный exception handler** маппит доменные исключения на HTTP-статусы:
  - `NotFoundException` → 404
  - `BadRequestException` → 400
  - `ConflictException` / `DbUpdateConcurrencyException` → 409
  - всё остальное → 500
- В логах: `Warning` для 4xx, `Error` для 5xx — никаких ложных алертов на штатные 404.

### Качество
- Юнит-тесты на xUnit + FluentAssertions + NSubstitute, покрывающие алгоритм валидации ИНН, валидаторы DTO (матрица типа клиента × наличие учредителей × длина ИНН) и бизнес-правила сервисного слоя (restore, конфликт, каскадное удаление, защита последнего учредителя).

---

## Стек технологий

| Назначение | Библиотека |
|---|---|
| Платформа | .NET 9 / ASP.NET Core 9 |
| ORM | EF Core 9 (SQL Server provider) |
| Маппинг | AutoMapper 16 |
| Валидация | FluentValidation 11 |
| Patch | `Microsoft.AspNetCore.JsonPatch` + форматтер `NewtonsoftJson` |
| Логирование | Serilog (sinks `Console`, `File`, `Seq`) + `Serilog.Enrichers.Span` |
| Трейсинг | OpenTelemetry (инструментации `AspNetCore`, `HttpClient`, `SqlClient`; экспортёры OTLP + Console) |
| Версионирование API | `Asp.Versioning.Mvc` |
| Rate limiting | `Microsoft.AspNetCore.RateLimiting` |
| Health checks | `AspNetCore.HealthChecks.SqlServer` + `HealthChecks.UI` |
| Документация API | `Swashbuckle.AspNetCore` (Swagger / OpenAPI) |
| Тесты | xUnit, FluentAssertions, NSubstitute |

---

Граф зависимостей идёт от внешних слоёв (Presentation, Persistence) внутрь к Domain. Сервисы зависят от интерфейсов в `Core.Domain.Repositories`, никогда — от `Persistence`. Presentation зависит от `Core.Services.Abstractions`.

---

## Запуск проекта

### Требования

- [.NET SDK 9.0](https://dotnet.microsoft.com/download)
- SQL Server (для разработки достаточно LocalDB; дефолтная строка подключения — `(localdb)\MSSQLLocalDB`)
- Опционально: [Seq](https://datalust.co/seq) — для агрегации логов, [Jaeger](https://www.jaegertracing.io/) — для просмотра трейсов

### Локально

```bash
git clone https://github.com/<your-account>/ClientManager.git
cd ClientManager

dotnet build
```

В `Development` миграции применяются автоматически при старте. Чтобы запустить вручную:

```bash
dotnet ef database update -p ClientManager.Infrastructure.Persistence -s ClientManager
```

Запуск API:

```bash
dotnet run --project ClientManager
```

Swagger UI: `https://localhost:<port>/swagger`.

### Запуск тестов

```bash
dotnet test
```

---

## Конфигурация

Ключи `appsettings.json`:

| Ключ | Назначение |
|---|---|
| `ConnectionStrings:sqlConnection` | Строка подключения к SQL Server |
| `Cors:AllowedOrigins` | Production-whitelist для CORS (массив URL'ов). В `Development` разрешены любые origin'ы |
| `Serilog` | Конфиг Serilog — sinks, минимальный уровень, шаблон вывода |
| `OpenTelemetry:OtlpEndpoint` | Опциональный OTLP/gRPC-endpoint (например `http://localhost:4317`). Если задан — трейсы отправляются туда дополнительно к консоли |
| `HealthChecksUI` | Конфиг UI-дашборда для health-checks |

Локальный Jaeger:

```bash
docker run -d --name jaeger -p 4317:4317 -p 16686:16686 jaegertracing/all-in-one:latest
```
---

## Обзор API

| Метод | Путь | Описание |
|---|---|---|
| GET | `/api/clients` | Постраничный список (поиск / сортировка / фильтр через query-параметры) |
| GET | `/api/clients/{id}` | Один клиент (возвращает `ETag`) |
| GET | `/api/clients/collection/(id1,id2,…)` | Получить пачку клиентов по ID'ам |
| POST | `/api/clients` | Создать клиента. Если ИНН совпадает с soft-deleted — запись восстанавливается |
| POST | `/api/clients/collection` | Создать пачкой (атомарно) |
| PATCH | `/api/clients/{id}` | JSON Patch. Принимает `If-Match`, возвращает новый `ETag` |
| DELETE | `/api/clients/{id}` | Soft-delete. Каскадно мягко удаляет осиротевших учредителей |
| GET | `/api/clients/{clientId}/founders` | Список учредителей клиента |
| GET | `/api/clients/{clientId}/founders/{id}` | Один учредитель (возвращает `ETag`) |
| POST | `/api/clients/{clientId}/founders` | Добавить (или восстановить / переиспользовать) учредителя |
| PATCH | `/api/clients/{clientId}/founders/{id}` | JSON Patch. Принимает `If-Match`, возвращает новый `ETag` |
| DELETE | `/api/clients/{clientId}/founders/{id}` | Отвязать. Если у учредителя не остаётся активных связей — он soft-delete'ится |
| GET | `/health` | Health-check (доступность SQL) |
| GET | `/swagger` | Документация API (только в Development) |

### Correlation

В каждом ответе есть заголовок `X-Correlation-Id`. Клиент может прислать свой — иначе сервер сгенерирует новый. Этот же ID пушится в `LogContext` Serilog, поэтому каждая лог-строка в рамках запроса несёт его. В ProblemDetails-ответах ID также доступен под extension'ом `correlationId`.

---

## Доменные правила

- **Юр. лицо** всегда имеет ≥ 1 учредителя. Создать ЮЛ без учредителей нельзя. Удалить последнего учредителя нельзя (блокируется 400-кой).
- **ИП** не может иметь учредителей. Попытка добавить → 400.
- Учредитель, привязанный к нескольким клиентам, не удаляется при soft-delete одного из них — soft-delete'ятся только осиротевшие (без активных связей).
- Создание клиента/учредителя с уже существующим ИНН:
  - Активная запись с тем же ИНН → 400 (`ClientWithSameInnExistsException` / `FounderAlreadyLinkedToClientException`).
  - Мягко удалённая запись с тем же ИНН → восстанавливается, потом привязывается.

### Валидация ИНН

Реализована в `InnValidator` по алгоритму ФНС:

- **10 цифр (юр. лицо):** `(2,4,10,3,5,9,4,6,8) · digits[0..8] mod 11 mod 10 == digits[9]`
- **12 цифр (физ. лицо / ИП):**
  - 11-я цифра: `(7,2,4,10,3,5,9,4,6,8) · digits[0..9] mod 11 mod 10`
  - 12-я цифра: `(3,7,2,4,10,3,5,9,4,6,8) · digits[0..10] mod 11 mod 10`

Проверки формата (длина, только цифры) дополнительно зафиксированы как CHECK-констрейнты в БД; алгоритмическая проверка контрольных цифр выполняется на уровне приложения.

---

## Тестирование

Tier-1 unit-тесты покрывают самое ценное и часто изменяемое поведение:

- `InnValidatorTests` — алгоритм валидации для обеих длин ИНН.
- `ClientForCreationDtoValidatorTests` — матрица `ClientType × Founders × длина ИНН`.
- `ClientServiceTests` — restore-by-INN, каскадный soft-delete, конфликт по дублю.
- `FounderServiceTests` — restore + reuse, конфликт связи, защита последнего учредителя.