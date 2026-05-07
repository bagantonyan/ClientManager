# ClientManager

ClientManager — это Web API на ASP.NET Core 9 для работы с клиентами: юридическими лицами, ИП и учредителями.

Основной упор в проекте сделан на нормальную доменную модель, валидацию ИНН, soft-delete, конкурентное обновление через `ETag` / `If-Match` и предсказуемую обработку ошибок.

Проект построен по Clean Architecture: внешние слои зависят от внутренних, а бизнес-логика не завязана напрямую на EF Core или ASP.NET Core.

---

## Что умеет проект

### Клиенты и учредители

В системе есть два типа клиентов:

- `Legal_Entity` — юридическое лицо, ИНН из 10 цифр;
- `Individual_Entrepreneur` — ИП, ИНН из 12 цифр.

Учредители — это физические лица с ИНН из 12 цифр. Один учредитель может быть привязан к нескольким юридическим лицам, поэтому связь сделана как many-to-many через `ClientFounder`.

Для ИНН проверяется не только длина и формат, но и контрольные цифры по алгоритму ФНС.

Часть правил продублирована на уровне БД через CHECK constraints:

- длина ИНН должна соответствовать типу клиента;
- ИНН должен состоять только из цифр;
- учредители разрешены только для `Legal_Entity`;
- у юридического лица должен быть минимум один учредитель.

Последнее правило дополнительно проверяется в сервисах, потому что полностью выразить его обычным CHECK constraint неудобно.

---

## Архитектура

Зависимости идут внутрь:

```text
Presentation  ->  Services  ->  Domain
Persistence   ->  Domain
```

Основная идея такая:

- `Domain` содержит сущности, правила и интерфейсы репозиториев;
- `Services` содержит бизнес-сценарии;
- `Persistence` реализует доступ к данным через EF Core;
- `Presentation` отвечает за HTTP API, DTO, валидацию входных данных и ответы клиенту.

Сервисы работают через интерфейсы из доменного слоя и не зависят напрямую от `DbContext`.

---

## Работа с данными

Для хранения используется EF Core 9 + SQL Server.

Что реализовано в persistence-слое:

- soft-delete для `Client` и `Founder` через поле `DeletedDate`;
- глобальные query filters, чтобы soft-deleted записи не попадали в обычные запросы;
- автоматическое заполнение `CreatedDate`, `ModifiedDate`, `DeletedDate` через audit-хук в `ChangeTracker`;
- hard-delete для `ClientFounder`, потому что сама связь не имеет отдельного жизненного цикла;
- filtered unique indexes по `INN` с условием `WHERE DeletedDate IS NULL`;
- восстановление записи по ИНН, если найден soft-deleted клиент или учредитель;
- optimistic concurrency через SQL Server `rowversion`.

Для времени используется `TimeProvider`, чтобы время в тестах можно было контролировать и чтобы в одном `SaveChanges` использовалось одно значение.

---

## API

API сделан в REST-стиле и поддерживает версионирование через заголовок `api-version`.

Основные возможности:

- постраничный список клиентов;
- поиск и сортировка по разрешённым полям;
- получение коллекции клиентов по списку ID;
- создание одного клиента или пачки клиентов;
- атомарный bulk-create: если один элемент невалидный, вся операция откатывается;
- JSON Patch для частичного обновления;
- `ETag` в ответах на GET / POST / PATCH;
- `If-Match` на PATCH, чтобы не перезаписывать чужие изменения;
- единый формат ошибок через ProblemDetails;
- `correlationId` в ошибках и логах.

### Эндпоинты

| Метод | Путь | Что делает |
|---|---|---|
| GET | `/api/clients` | Список клиентов с pagination / search / sort |
| GET | `/api/clients/{id}` | Один клиент по ID |
| GET | `/api/clients/collection/(id1,id2,...)` | Несколько клиентов по ID |
| POST | `/api/clients` | Создать клиента или восстановить soft-deleted запись по ИНН |
| POST | `/api/clients/collection` | Создать несколько клиентов атомарно |
| PATCH | `/api/clients/{id}` | Частично обновить клиента через JSON Patch |
| DELETE | `/api/clients/{id}` | Soft-delete клиента |
| GET | `/api/clients/{clientId}/founders` | Список учредителей клиента |
| GET | `/api/clients/{clientId}/founders/{id}` | Один учредитель |
| POST | `/api/clients/{clientId}/founders` | Добавить, переиспользовать или восстановить учредителя |
| PATCH | `/api/clients/{clientId}/founders/{id}` | Частично обновить учредителя |
| DELETE | `/api/clients/{clientId}/founders/{id}` | Отвязать учредителя от клиента |
| GET | `/health` | Проверка доступности приложения и SQL Server |
| GET | `/swagger` | Swagger UI в Development |

---

## Доменные правила

Ключевые правила, которые проверяются в проекте:

- юридическое лицо нельзя создать без учредителей;
- у юридического лица нельзя удалить последнего учредителя;
- ИП не может иметь учредителей;
- учредитель может быть связан с несколькими юридическими лицами;
- если учредитель после удаления связи больше нигде не используется, он soft-delete'ится;
- если клиент или учредитель с таким ИНН уже есть среди активных записей, возвращается ошибка;
- если запись с таким ИНН была soft-deleted, она восстанавливается и используется повторно.

---

## Валидация ИНН

Валидация вынесена в `InnValidator`.

Для юридического лица проверяется 10-значный ИНН:

```text
(2, 4, 10, 3, 5, 9, 4, 6, 8) · digits[0..8] mod 11 mod 10 == digits[9]
```

Для физического лица / ИП проверяется 12-значный ИНН:

```text
11-я цифра:
(7, 2, 4, 10, 3, 5, 9, 4, 6, 8) · digits[0..9] mod 11 mod 10

12-я цифра:
(3, 7, 2, 4, 10, 3, 5, 9, 4, 6, 8) · digits[0..10] mod 11 mod 10
```

Форматные проверки также зафиксированы на уровне базы, чтобы невалидные данные нельзя было обойти через persistence-слой.

---

## Ошибки и concurrency

Ошибки возвращаются в формате ProblemDetails.

Маппинг основных исключений:

| Исключение | HTTP status |
|---|---|
| `NotFoundException` | 404 |
| `BadRequestException` | 400 |
| `ConflictException` | 409 |
| `DbUpdateConcurrencyException` | 409 |
| остальные ошибки | 500 |

Для конкурентного обновления используется `RowVersion`.

Сценарий такой:

1. Клиент делает `GET` и получает `ETag`.
2. При `PATCH` отправляет этот `ETag` в `If-Match`.
3. Если запись уже изменилась, API возвращает конфликт вместо тихой перезаписи данных.

---

## Логирование, tracing и health checks

В проекте подключены:

- Serilog с выводом в Console, File и Seq;
- correlation id через заголовок `X-Correlation-Id`;
- OpenTelemetry для ASP.NET Core, HttpClient и SQL Client;
- OTLP exporter для production-сценариев;
- Console exporter для разработки;
- health checks для приложения и SQL Server;
- Health Checks UI.

Если клиент передаёт `X-Correlation-Id`, API использует его. Если нет — генерирует новый. Этот ID попадает в логи и в ProblemDetails.

---

## Стек

| Задача | Используется |
|---|---|
| Платформа | .NET 9 / ASP.NET Core 9 |
| ORM | EF Core 9 + SQL Server |
| Маппинг | AutoMapper 16 |
| Валидация | FluentValidation 11 |
| JSON Patch | `Microsoft.AspNetCore.JsonPatch` + `NewtonsoftJson` formatter |
| Логи | Serilog, Console/File/Seq sinks, `Serilog.Enrichers.Span` |
| Tracing | OpenTelemetry: AspNetCore, HttpClient, SqlClient |
| API versioning | `Asp.Versioning.Mvc` |
| Rate limiting | `Microsoft.AspNetCore.RateLimiting` |
| Health checks | `AspNetCore.HealthChecks.SqlServer`, `HealthChecks.UI` |
| Swagger | `Swashbuckle.AspNetCore` |
| Тесты | xUnit, FluentAssertions, NSubstitute |

---

## Запуск

### Требования

- .NET SDK 9.0
- SQL Server
- LocalDB подойдёт для локальной разработки
- Seq и Jaeger опциональны

По умолчанию используется LocalDB:

```text
(localdb)\MSSQLLocalDB
```

### Сборка

```bash
git clone https://github.com/<your-account>/ClientManager.git
cd ClientManager

dotnet build
```

В `Development` миграции применяются автоматически при старте приложения.

Если нужно применить их вручную:

```bash
dotnet ef database update -p ClientManager.Infrastructure.Persistence -s ClientManager
```

### Запуск API

```bash
dotnet run --project ClientManager
```

Swagger будет доступен по адресу:

```text
https://localhost:<port>/swagger
```

### Тесты

```bash
dotnet test
```

---

## Конфигурация

Основные ключи в `appsettings.json`:

| Ключ | Для чего нужен |
|---|---|
| `ConnectionStrings:sqlConnection` | Подключение к SQL Server |
| `Cors:AllowedOrigins` | Список разрешённых origin'ов для production |
| `Serilog` | Настройки логирования |
| `OpenTelemetry:OtlpEndpoint` | OTLP/gRPC endpoint, например `http://localhost:4317` |
| `HealthChecksUI` | Настройки UI для health checks |

В `Development` CORS настроен свободнее, чтобы не мешать локальной разработке.

### Локальный Jaeger

```bash
docker run -d --name jaeger -p 4317:4317 -p 16686:16686 jaegertracing/all-in-one:latest
```

После запуска UI будет доступен на порту `16686`.

---

## Тестирование

Основной упор в тестах сделан на правила, которые проще всего сломать при изменениях:

- `InnValidatorTests` — проверка алгоритма ИНН для 10 и 12 цифр;
- `ClientForCreationDtoValidatorTests` — комбинации типа клиента, ИНН и учредителей;
- `ClientServiceTests` — восстановление по ИНН, soft-delete, конфликты;
- `FounderServiceTests` — восстановление, повторное использование, конфликт связи, защита последнего учредителя.
