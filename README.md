
---

# 🧭 TravelPlanner

**Планировщик путешествий**: поездки, программа по дням, бюджет и бронирования, сводная аналитика, уведомления в реальном времени.

Учебно-производственный проект полного цикла: **ASP.NET Core 10 (LTS) + EF Core 10 + SQLite + SignalR** на сервере и **React 19 + TypeScript + Vite 7** на клиенте. Приложение запускается без внешней инфраструктуры (одна команда — и БД создаётся и наполняется сама), собирается в один Docker-образ и обслуживает API и SPA с одного адреса.

> **Демо-доступы:** пользователь `demo@travel.local` / `Demo#12345`, администратор `admin@travel.local` / `Admin#12345`.
> Учётные записи создаются сидером только в пустой базе. **Перед промышленным развёртыванием смените пароли и ключ подписи JWT.**

---

## Содержание

1. [О проекте](#1-о-проекте)
2. [Возможности](#2-возможности)
3. [Технологический стек](#3-технологический-стек)
4. [Архитектура](#4-архитектура)
5. [Структура репозитория](#5-структура-репозитория)
6. [Требования](#6-требования)
7. [Быстрый старт](#7-быстрый-старт)
8. [Конфигурация](#8-конфигурация)
9. [База данных и миграции](#9-база-данных-и-миграции)
10. [Доменная модель и бизнес-правила](#10-доменная-модель-и-бизнес-правила)
11. [REST API](#11-rest-api)
12. [Безопасность](#12-безопасность)
13. [Real-time уведомления](#13-real-time-уведомления)
14. [Фоновый воркер](#14-фоновый-воркер)
15. [Фронтенд](#15-фронтенд)
16. [Тестирование](#16-тестирование)
17. [Сборка и развёртывание](#17-сборка-и-развёртывание)
18. [Эксплуатация и обслуживание](#18-эксплуатация-и-обслуживание)
19. [Диагностика проблем](#19-диагностика-проблем)
20. [Исправленные дефекты («грабли»)](#20-исправленные-дефекты-грабли)
21. [Ограничения и план развития](#21-ограничения-и-план-развития)
22. [Соглашения разработчика](#22-соглашения-разработчика)
23. [FAQ](#23-faq)
24. [Лицензия и автор](#24-лицензия-и-автор)

---

## 1. О проекте

### 1.1. Назначение

TravelPlanner помогает планировать поездку от идеи до архива: направления и даты, список попутчиков, программа по дням (транспорт, проживание, музеи, морские прогулки, экстрим, уличная еда), стоимость каждого пункта, статус бронирования и контроль расхода бюджета.

### 1.2. Основные сценарии

| Сценарий                   | Как выполняется                                                                  |
| -------------------------- | -------------------------------------------------------------------------------- |
| Создать поездку            | «Поездки» → «+ Новая поездка» → черновик (`Draft`)                               |
| Составить программу        | Карточка поездки → «+ Добавить пункт» (дата обязана попадать в диапазон поездки) |
| Отметить бронирование      | Статус пункта `Booked` → мгновенное уведомление во всех открытых вкладках        |
| Перевести поездку в работу | Кнопки статусов; недопустимые переходы отклоняются сервером (409)                |
| Контролировать бюджет      | Кольцевая диаграмма расходов по категориям, остаток, пересчёт в другую валюту    |
| Посмотреть сводку          | «Дашборд»: количество поездок, суммы в базовой валюте (RUB), ближайшие выезды    |
| Администрирование          | Роль `Admin`: список пользователей и видимость всех поездок                      |

### 1.3. Чем проект полезен как образец

* Чистое разделение слоёв: домен без зависимостей, инфраструктура, HTTP-слой.
* Бизнес-правила сосредоточены в агрегате `Trip` и покрыты модульными тестами.
* Сквозные механизмы: единый формат ошибок RFC 7807, correlation-id, rate limiting, output caching, health checks.
* Реальные «взрослые» механики: ротация refresh-токенов, оптимистичная конкуренция, фоновая служба, SignalR-группы по пользователю.

---

## 2. Возможности

### 2.1. Сервер

| №   | Возможность                                            | Реализация                                       |
| --- | ------------------------------------------------------ | ------------------------------------------------ |
| 1   | Регистрация, вход, профиль, выход                      | `AuthController`, `AuthService`                  |
| 2   | JWT (HS256) + refresh-токены с ротацией и отзывом      | `TokenService`, таблица `RefreshTokens`          |
| 3   | Хеширование паролей PBKDF2-SHA256, 210 000 итераций    | `PasswordHasher`                                 |
| 4   | Роли `User`/`Admin`, политика `AdminOnly`              | `Program.cs`, `AdminController`                  |
| 5   | CRUD поездок: поиск, фильтры, сортировка, пагинация    | `TripService`, `TripsController`                 |
| 6   | CRUD пунктов программы с автопорядком и валидацией дат | `Trip.AddActivity`, `ActivitiesController`       |
| 7   | Оптимистичная конкуренция по `Version` → HTTP 409      | EF Core concurrency token                        |
| 8   | Дашборд с приведением сумм к базовой валюте            | `DashboardService`, `CurrencyService`            |
| 9   | Конвертер валют и справочники                          | `MetaController`                                 |
| 10  | Уведомления в реальном времени                         | `NotificationsHub`, `SignalRNotificationService` |
| 11  | Фоновая служба: автостатусы и напоминания              | `TripMaintenanceWorker`                          |
| 12  | Единые ошибки ProblemDetails + карта полей `errors`    | `ExceptionHandlingMiddleware`                    |
| 13  | Correlation-id и лог длительности запроса              | `CorrelationIdMiddleware`                        |
| 14  | Ограничение частоты запросов аутентификации            | RateLimiter, политика `auth`                     |
| 15  | Кэширование справочников                               | OutputCache, политика `meta`                     |
| 16  | Проверка работоспособности                             | `/health` (включая проверку БД)                  |
| 17  | OpenAPI 3 + Swagger UI                                 | `AddOpenApi`, `/swagger`                         |
| 18  | Автоматическая миграция и сидирование при старте       | `Program.cs`, `DbSeeder`                         |

### 1.2. Клиент

| №   | Возможность                                             | Реализация                                    |
| --- | ------------------------------------------------------- | --------------------------------------------- |
| 1   | Защищённые маршруты и восстановление сессии             | `AuthContext`, `ProtectedRoute`               |
| 2   | Автоматическое обновление access-токена (single-flight) | `api/client.ts`                               |
| 3   | Единая обработка ошибок сервера                         | класс `ApiError`                              |
| 4   | Список поездок с «живым» поиском (debounce 250 мс)      | `TripsPage`                                   |
| 5   | Программа по дням, модальные формы                      | `TripDetailsPage`, `TripForm`, `ActivityForm` |
| 6   | Кольцевая диаграмма расходов на чистом SVG              | `BudgetDonut`                                 |
| 7   | Всплывающие уведомления SignalR с авто-переподключением | `useNotifications`, `Layout`                  |
| 8   | Русская локализация статусов, категорий, дат и сумм     | `labels.ts`, `Intl`                           |
| 9   | Адаптивная тёмная тема без UI-библиотек                 | `styles.css`                                  |

---

## 3. Технологический стек

| Слой             | Технология                                        | Версия     | Назначение                |
| ---------------- | ------------------------------------------------- | ---------- | ------------------------- |
| Runtime          | .NET                                              | 10.0 (LTS) | серверная платформа       |
| Web              | ASP.NET Core (контроллеры)                        | 10.0       | REST API, статика SPA     |
| ORM              | EF Core + провайдер SQLite                        | 10.0       | доступ к данным, миграции |
| Аутентификация   | `Microsoft.AspNetCore.Authentication.JwtBearer`   | 10.0       | проверка JWT              |
| Токены           | `Microsoft.IdentityModel.JsonWebTokens`           | 8.x        | выпуск JWT                |
| Real-time        | SignalR                                           | 10.0       | push-уведомления          |
| Документация     | `Microsoft.AspNetCore.OpenApi` + Swagger UI (CDN) | 10.0 / 5.x | спецификация и песочница  |
| Тесты            | xUnit + `Microsoft.NET.Test.Sdk`                  | 2.x / 17.x | модульные тесты           |
| Клиент           | React                                             | 19.x       | UI                        |
| Язык клиента     | TypeScript                                        | 5.8        | типизация                 |
| Сборка клиента   | Vite                                              | 7.x        | dev-сервер и бандл        |
| Маршрутизация    | React Router                                      | 7.x        | SPA-навигация             |
| Real-time клиент | `@microsoft/signalr`                              | 8.x        | WebSocket-клиент          |
| Контейнеризация  | Docker / Compose                                  | 24+ / v2   | образ и запуск            |

> **Совместимость с .NET 8/9.** Замените `net10.0` во всех `.csproj` и версии пакетов `10.0.0` на `8.0.x`/`9.0.x`. Для .NET 8 additionally удалите пакет `Microsoft.AspNetCore.OpenApi` и вызовы `AddOpenApi()`/`MapOpenApi()` в `Program.cs` (встроенный генератор OpenAPI появился в .NET 9).

---

## 4. Архитектура

### 4.1. Слои и направление зависимостей

```
┌──────────────────────────────────────────────────────────────┐
│  frontend (React SPA)                                        │
│  страницы → компоненты → api/client.ts → fetch / SignalR      │
└───────────────────────────┬──────────────────────────────────┘
                            │ HTTP JSON, WebSocket
┌───────────────────────────▼──────────────────────────────────┐
│  TravelPlanner.Api  (точка входа, composition root)           │
│  Controllers · Hubs · Middleware · Workers · Program.cs        │
└───────────────────────────┬──────────────────────────────────┘
                            │ ссылки на проекты
┌───────────────────────────▼──────────────────────────────────┐
│  TravelPlanner.Infrastructure                               │
│  AppDbContext · конфигурации EF · сервисы · JWT · хеширование  │
└───────────────────────────┬──────────────────────────────────┘
                            │
┌───────────────────────────▼──────────────────────────────────┐
│  TravelPlanner.Core                                         │
│  сущности · перечисления · исключения · DTO · интерфейсы       │
│  внешних зависимостей НЕТ                                    │
└──────────────────────────────────────────────────────────────┘
```

**Правила, которые нельзя нарушать:**

1. `Core` не ссылается ни на EF Core, ни на ASP.NET Core.
2. `Infrastructure` не знает про HTTP: текущий пользователь приходит через `ICurrentUser`, уведомления — через `INotificationService`.
3. `Api` не содержит бизнес-логики: только транспорт, авторизация, composition root.
4. DTO живут в `Core/Contracts` — иначе возник бы цикл `Api ↔ Infrastructure`.

### 4.2. Конвейер обработки запроса

```
Запрос
  → CorrelationIdMiddleware        (X-Correlation-Id, лог длительности)
  → ExceptionHandlingMiddleware    (перехват исключений → ProblemDetails)
  → CORS (политика "spa")
  → RateLimiter (политика "auth")
  → Authentication (JWT Bearer)
  → Authorization (политики, роли)
  → OutputCache (политика "meta")
  → StaticFiles / DefaultFiles
  → Endpoint: Controller | Hub | Health | Fallback(index.html)
```

### 4.3. Поток изменения данных

```
Controller → ITripService → загрузка агрегата Trip (EF, Include Activities)
           → методы домена (Rename/SetDates/ChangeStatus/AddActivity…)
           → Touch() → новая Version
           → SaveChangesAsync (WHERE Version = original)
              ├─ успех → INotificationService → SignalR → браузеры
              └─ DbUpdateConcurrencyException → ConflictException → HTTP 409
```

### 4.4. Ключевые архитектурные решения

| Решение                                             | Обоснование                                                              |
| --------------------------------------------------- | ------------------------------------------------------------------------ |
| Ручной маппинг (`Mappers`) вместо AutoMapper        | прозрачность, отсутствие «магии», нулевые зависимости                    |
| `DateOnly`/`TimeOnly` → ISO-строки через конвертеры | предсказуемая сортировка и сравнение дат в SQL на любом провайдере       |
| Перечисления как строки в БД и JSON                 | читаемость данных, устойчивость к переупорядочиванию, удобный TypeScript |
| `Travelers` — коллекция примитивов (JSON-колонка)   | не требует отдельной таблицы для простого списка имён                    |
| `Version` (GUID) как токен конкуренции              | работает в SQLite, где нет `rowversion`                                  |
| Загрузка страницы поездок с `AsSplitQuery`          | нет «размножения» строк поездки при `Include` активностей                |
| Агрегаты дашборда в памяти                          | простота и корректность для SQLite; для больших объёмов — SQL-агрегация  |

---

## 5. Структура репозитория

```
TravelPlanner/
├─ README.md                     ← этот документ
├─ TravelPlanner.sln
├─ Dockerfile                    ← multi-stage: Node → SDK → runtime
├─ docker-compose.yml
├─ .dockerignore
├─ .gitignore
├─ src/
│  ├─ TravelPlanner.Core/
│  │  ├─ Domain/                 ← Enums, EntityBase, Users, Trip, ActivityItem, Exceptions
│  │  ├─ Abstractions/           ← ISecurity.cs, IServices.cs
│  │  └─ Contracts/              ← Common, Auth, Trip, Activity контракты (DTO)
│  ├─ TravelPlanner.Infrastructure/
│  │  ├─ Persistence/            ← AppDbContext, EntityConfigurations, DesignTimeDbContextFactory, DbSeeder
│  │  ├─ Security/               ← PasswordHasher, JwtOptions, TokenService
│  │  ├─ Services/               ← AuthService, TripService, DashboardService, CurrencyService
│  │  ├─ Mapping/Mappers.cs
│  │  └─ DependencyInjection.cs  ← AddInfrastructure()
│  └─ TravelPlanner.Api/
│     ├─ Controllers/            ← Auth, Trips, Activities, Dashboard, Meta, Admin
│     ├─ Hubs/NotificationsHub.cs
│     ├─ Realtime/SignalRNotificationService.cs
│     ├─ Security/HttpContextCurrentUser.cs
│     ├─ Middleware/             ← ExceptionHandling, CorrelationId
│     ├─ Workers/TripMaintenanceWorker.cs
│     ├─ OpenApi/SwaggerUiPage.cs
│     ├─ Properties/launchSettings.json
│     ├─ App_Data/               ← файл SQLite (создаётся автоматически)
│     ├─ wwwroot/                ← сюда собирается SPA
│     ├─ appsettings.json / appsettings.Development.json
│     └─ Program.cs
├─ tests/TravelPlanner.Tests/    ← DomainTests.cs, InfrastructureTests.cs
└─ frontend/
   ├─ package.json, tsconfig.json, vite.config.ts, index.html
   └─ src/
      ├─ main.tsx, App.tsx, styles.css, labels.ts
      ├─ api/{types,client}.ts
      ├─ auth/AuthContext.tsx
      ├─ hooks/useNotifications.ts
      ├─ components/            ← Layout, ProtectedRoute, Modal, Loader, StatusBadge, BudgetDonut, TripForm, ActivityForm
      └─ pages/                 ← Login, Register, Dashboard, Trips, TripDetails, Admin
```

---

## 6. Требования

| Компонент            | Минимум                               | Проверка                                     |
| -------------------- | ------------------------------------- | -------------------------------------------- |
| .NET SDK             | 10.0.x (LTS)                          | `dotnet --version`                           |
| Node.js              | 22.x (Vite 7 требует 20.19+ / 22.12+) | `node -v`                                    |
| npm                  | 10+                                   | `npm -v`                                     |
| `dotnet-ef`          | 10.x                                  | `dotnet ef --version`                        |
| Docker (опционально) | 24+ и Compose v2                      | `docker --version`, `docker compose version` |
| ОС                   | Windows 10/11, Linux, macOS           | —                                            |

Установка инструмента миграций:

```bash
dotnet tool install --global dotnet-ef --version 10.*
# если уже установлен:
dotnet tool update --global dotnet-ef --version 10.*
```

> Каталог `~/.dotnet/tools` (Linux/macOS) или `%USERPROFILE%\.dotnet\tools` (Windows) должен быть в `PATH`.

---

## 7. Быстрый старт

### 7.1. Первый запуск (два терминала)

```bash
# Терминал 1 — сервер
dotnet restore
dotnet build
dotnet ef migrations add InitialCreate \
  --project src/TravelPlanner.Infrastructure \
  --startup-project src/TravelPlanner.Api
dotnet run --project src/TravelPlanner.Api
```

```bash
# Терминал 2 — клиент
cd frontend
npm install
npm run dev
```

Откройте **http://localhost:5095** и войдите как `demo@travel.local` / `Demo#12345`.

> Миграции создавать необязательно: если в сборке их нет, `Program.cs` вызовет `EnsureCreatedAsync()`. Однако для управляемой эволюции схемы миграции предпочтительны.

### 7.2. Адреса по умолчанию

| Что                             | Адрес                                 |
| ------------------------------- | ------------------------------------- |
| SPA (dev, Vite)                 | http://localhost:5095                 |
| API (dev, профиль `http`)       | http://localhost:5080                 |
| API (dev, профиль `https`)      | https://localhost:7080                |
| Swagger UI (только Development) | http://localhost:5080/swagger         |
| Спецификация OpenAPI            | http://localhost:5080/openapi/v1.json |
| Health check                    | http://localhost:5080/health          |
| Всё вместе в Docker             | http://localhost:8080                 |

### 7.3. Что проверить сразу после запуска

* [ ] `/health` отвечает `Healthy`.
* [ ] Вход под демо-пользователем проходит, дашборд заполнен.
* [ ] Поиск и пагинация в «Поездках» работают (запросы уходят на сервер).
* [ ] Добавление пункта со статусом `Booked` вызывает всплывающее уведомление.
* [ ] В течение минуты приходит напоминание «Скоро в путь» (поездка «Санкт-Петербург…» стартует через 2 дня).
* [ ] Сохранение одной поездки в двух вкладках даёт `409 Conflict` во второй.
* [ ] Под администратором виден раздел «Администрирование» и все поездки.

### 7.4. Пересоздание решения с нуля

Если требуется воспроизвести каркас командой (а не клонировать репозиторий):

```bash
mkdir TravelPlanner && cd TravelPlanner
dotnet new sln -n TravelPlanner
dotnet new gitignore
dotnet new classlib -n TravelPlanner.Core           -o src/TravelPlanner.Core
dotnet new classlib -n TravelPlanner.Infrastructure -o src/TravelPlanner.Infrastructure
dotnet new web      -n TravelPlanner.Api            -o src/TravelPlanner.Api
dotnet new xunit    -n TravelPlanner.Tests          -o tests/TravelPlanner.Tests
dotnet sln add src/TravelPlanner.Core src/TravelPlanner.Infrastructure src/TravelPlanner.Api tests/TravelPlanner.Tests
dotnet add src/TravelPlanner.Infrastructure reference src/TravelPlanner.Core
dotnet add src/TravelPlanner.Api reference src/TravelPlanner.Core src/TravelPlanner.Infrastructure
dotnet add tests/TravelPlanner.Tests reference src/TravelPlanner.Infrastructure

dotnet add src/TravelPlanner.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite --version 10.0.0
dotnet add src/TravelPlanner.Infrastructure package Microsoft.Extensions.Options.ConfigurationExtensions --version 10.0.0
dotnet add src/TravelPlanner.Infrastructure package Microsoft.IdentityModel.JsonWebTokens
dotnet add src/TravelPlanner.Api package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.0
dotnet add src/TravelPlanner.Api package Microsoft.AspNetCore.OpenApi --version 10.0.0
dotnet add src/TravelPlanner.Api package Microsoft.EntityFrameworkCore.Design --version 10.0.0
dotnet add src/TravelPlanner.Api package Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore --version 10.0.0

npm create vite@latest frontend -- --template react-ts
cd frontend && npm install && npm install react-router-dom @microsoft/signalr && cd ..
```

Далее разместите исходные файлы по структуре из раздела 5.

---

## 8. Конфигурация

### 8.1. Параметры

| Ключ                        | Значение по умолчанию                   | Переменная окружения         | Описание                      |
| --------------------------- | --------------------------------------- | ---------------------------- | ----------------------------- |
| `ConnectionStrings:Default` | `Data Source=App_Data/travelplanner.db` | `ConnectionStrings__Default` | строка подключения SQLite     |
| `Jwt:Issuer`                | `TravelPlanner`                         | `Jwt__Issuer`                | издатель токена               |
| `Jwt:Audience`              | `TravelPlanner.Spa`                     | `Jwt__Audience`              | аудитория токена              |
| `Jwt:SigningKey`            | пусто (в Development задан учебный)     | `Jwt__SigningKey`            | **обязателен**, ≥ 32 символов |
| `Jwt:AccessTokenMinutes`    | `15`                                    | `Jwt__AccessTokenMinutes`    | время жизни access-токена     |
| `Jwt:RefreshTokenDays`      | `14`                                    | `Jwt__RefreshTokenDays`      | время жизни refresh-токена    |
| `ASPNETCORE_ENVIRONMENT`    | `Development` (launchSettings)          | `ASPNETCORE_ENVIRONMENT`     | окружение                     |
| `ASPNETCORE_URLS`           | `http://localhost:5080`                 | `ASPNETCORE_URLS`            | адреса прослушивания          |
| `Logging:LogLevel:*`        | Information / Warning                   | `Logging__LogLevel__Default` | уровни логов                  |

> Двойное подчёркивание `__` в имени переменной окружения соответствует вложенности секций JSON.

### 8.2. Генерация ключа подписи

```bash
# Linux / macOS
openssl rand -base64 48
```

```powershell
# Windows PowerShell
[Convert]::ToBase64String((1..48 | % { Get-Random -Maximum 256 }))
```

Локально ключ удобно хранить в user-secrets (не попадает в репозиторий):

```bash
dotnet user-secrets init --project src/TravelPlanner.Api
dotnet user-secrets set "Jwt:SigningKey" "<сгенерированный ключ>" --project src/TravelPlanner.Api
```

При отсутствии или коротком ключе приложение **не запустится** и выдаст понятное сообщение — это сделано намеренно, чтобы исключить небезопасный продакшен.

### 8.3. Отличия окружений

| Аспект                          | Development                               | Production                          |
| ------------------------------- | ----------------------------------------- | ----------------------------------- |
| Swagger UI и `/openapi/v1.json` | доступны                                  | отключены                           |
| Ключ JWT                        | учебный из `appsettings.Development.json` | только из ENV/секрета               |
| Уровень логов EF                | `Information` (виден SQL)                 | `Warning`                           |
| CORS                            | разрешён `http://localhost:5173`          | не требуется (SPA с того же адреса) |

### 8.4. Порты и прокси в разработке

`frontend/vite.config.ts` проксирует на `http://localhost:5080`:

| Путь      | Прокси | Особенность                        |
| --------- | ------ | ---------------------------------- |
| `/api`    | да     | обычный HTTP                       |
| `/health` | да     | проверка сервера из браузера       |
| `/hubs`   | да     | `ws: true` — WebSocket для SignalR |

Благодаря прокси браузер считает запросы однопоточными по источнику: проблемы CORS и cookie в dev отсутствуют. Если меняете порт API — правьте `launchSettings.json` **и** `vite.config.ts`.

---

## 9. База данных и миграции

### 9.1. Схема

```
Users                        RefreshTokens
─────                        ─────────────
Id (PK, GUID)                Id (PK, GUID)
Email (unique, 256)          UserId (FK → Users, cascade)
DisplayName (64)             Token (unique, 256)
PasswordHash (512)           ExpiresAtUtc
Role (string, 16)            RevokedAtUtc (nullable)
CreatedAtUtc                 ReplacedByToken (256, nullable)
                             CreatedAtUtc
      │ 1
      │
      │ *                    Activities
Trips                        ──────────
─────                        Id (PK, GUID)
Id (PK, GUID)                TripId (FK → Trips, cascade)
OwnerId (FK → Users)         Title (160)
Title (120)                  Category (string, 24)
City (80) / Country (80)     Status (string, 16)
Description (2000)           Date (TEXT 'yyyy-MM-dd')
StartDate / EndDate (TEXT)   StartTime (TEXT 'HH:mm', nullable)
Status (string, 16)          DurationMinutes (int)
Budget (decimal 18,2)        Cost (decimal 18,2)
Currency (3)                 Location (200) / Notes (1000)
Travelers (JSON-массив)      Order (int)
Version (concurrency token)  CreatedAtUtc
ReminderSentUtc (nullable)
CreatedAtUtc / UpdatedAtUtc
```

**Индексы:** `Users.Email` (unique), `RefreshTokens.Token` (unique), `RefreshTokens.UserId`, `Trips(OwnerId, StartDate)`, `Trips.Status`, `Activities(TripId, Date, Order)`.

### 9.2. Соглашения хранения

| Тип .NET       | Хранение                      | Причина                                                |
| -------------- | ----------------------------- | ------------------------------------------------------ |
| `DateOnly`     | `TEXT` `yyyy-MM-dd`           | лексикографический порядок совпадает с хронологическим |
| `TimeOnly?`    | `TEXT` `HH:mm`                | формат HTML `input[type=time]`                         |
| `enum`         | `TEXT`                        | читаемость и устойчивость к изменениям порядка         |
| `List<string>` | `TEXT` (JSON)                 | примитивная коллекция EF Core 8+                       |
| `decimal`      | `TEXT`/`REAL` (SQLite)        | см. ограничение в разделе 21                           |
| `Guid Version` | `TEXT` + `IsConcurrencyToken` | оптимистичная конкуренция                              |

Вычисляемые свойства (`DurationDays`, `PlannedCost`, `BookedCost`, `Remaining`, `BudgetUsagePercent`, `RefreshToken.IsActive`) в БД **не отображаются** — они явно исключены в конфигурации.

### 9.3. Команды миграций

```bash
# создать миграцию
dotnet ef migrations add <Name> --project src/TravelPlanner.Infrastructure --startup-project src/TravelPlanner.Api

# применить к БД
dotnet ef database update --project src/TravelPlanner.Infrastructure --startup-project src/TravelPlanner.Api

# откатиться к конкретной миграции
dotnet ef database update InitialCreate --project src/TravelPlanner.Infrastructure --startup-project src/TravelPlanner.Api

# удалить последнюю (не применённую) миграцию
dotnet ef migrations remove --project src/TravelPlanner.Infrastructure --startup-project src/TravelPlanner.Api

# SQL-скрипт для DBA / продакшена
dotnet ef migrations script --idempotent -o ./artifacts/schema.sql \
  --project src/TravelPlanner.Infrastructure --startup-project src/TravelPlanner.Api

# список миграций и их состояние
dotnet ef migrations list --project src/TravelPlanner.Infrastructure --startup-project src/TravelPlanner.Api
```

Design-time фабрика `DesignTimeDbContextFactory` позволяет выполнять эти команды без запуска веб-хоста; строка подключения берётся из `ConnectionStrings__Default` либо из значения по умолчанию.

### 9.4. Сидирование

`DbSeeder.SeedAsync` выполняется **только если таблица `Users` пуста**. Создаётся:

| Объект        | Contents                                                                                        |
| ------------- | ----------------------------------------------------------------------------------------------- |
| Администратор | `admin@travel.local` / `Admin#12345`, роль `Admin`                                              |
| Пользователь  | `demo@travel.local` / `Demo#12345`, роль `User`                                                 |
| Поездка 1     | «Санкт-Петербург: флот и крепости», сегодня +2…+6 дней, `Booked`, 60 000 ₽, 7 пунктов программы |
| Поездка 2     | «Шанхай: уличная еда и набережные», сегодня +30…+37, `Planned`, 180 000 ₽                       |
| Поездка 3     | «Сочи: экстрим-выходные», сегодня −40…−35, `Completed`, 45 000 ₽ (для статистики)               |

Даты относительны текущему дню, поэтому напоминание фонового воркера воспроизводится при любом дне запуска.

### 9.5. Полный сброс данных

```bash
# остановить приложение, затем:
rm -f src/TravelPlanner.Api/App_Data/travelplanner.db*
dotnet run --project src/TravelPlanner.Api      # схема и демо-данные создадутся заново
```

```powershell
Remove-Item src\TravelPlanner.Api\App_Data\travelplanner.db* -Force
```

В Docker данные лежат в томе `travel-data`: `docker compose down -v` удалит том **вместе с данными**.

### 9.6. Переход на PostgreSQL

1. Заменить пакет `Microsoft.EntityFrameworkCore.Sqlite` на `Npgsql.EntityFrameworkCore.PostgreSQL`.
2. В `DependencyInjection.AddInfrastructure` — `options.UseNpgsql(connectionString)`.
3. В `DesignTimeDbContextFactory` — то же изменение.
4. Удалить каталог `Migrations` и создать миграции заново (провайдер-специфичные типы).
5. Убрать конвертеры `DateOnly`/`TimeOnly` (Npgsql поддерживает их нативно) — по желанию.
6. Проверить `HasPrecision(18,2)`: в PostgreSQL `numeric` даёт точные денежные агрегаты и сортировку.
7. Health check `AddDbContextCheck<AppDbContext>` менять не нужно.

---

## 10. Доменная модель и бизнес-правила

### 10.1. Агрегат `Trip`

| Правило                                                           | Где проверяется               | Ошибка                      |
| ----------------------------------------------------------------- | ----------------------------- | --------------------------- |
| Название 3…120 символов                                           | `Rename`                      | `ValidationException` → 400 |
| Дата окончания ≥ даты начала                                      | `SetDates`                    | 400                         |
| Длительность ≤ 366 дней                                           | `SetDates`                    | 400                         |
| Нельзя сузить даты, если пункты программы окажутся вне диапазона  | `SetDates`                    | 400                         |
| Бюджет ≥ 0, округление до 2 знаков                                | `SetBudget`                   | 400                         |
| Валюта — 3 буквы ISO 4217 и входит в поддерживаемые               | `SetCurrency` + `TripService` | 400                         |
| Не более 12 попутчиков, имя ≤ 64 символов, дубликаты удаляются    | `SetTravelers`                | 400                         |
| Дата пункта программы внутри дат поездки                          | `EnsureActivityDate`          | 400                         |
| Порядок пункта назначается автоматически (max + 1), если не задан | `AddActivity`                 | —                           |
| Завершённая/отменённая поездка — только чтение                    | `EnsureEditable`              | `ConflictException` → 409   |
| Переходы статусов ограничены матрицей                             | `ChangeStatus`                | 409                         |
| Параллельное изменение                                            | `Version` + EF                | 409                         |

### 10.2. Матрица переходов статуса поездки

| Из \ В         | Draft | Planned | Booked | InProgress | Completed | Cancelled |
| -------------- | :---: | :-----: | :----: | :--------: | :-------: | :-------: |
| **Draft**      |   —   |    ✅    |   —    |     —      |     —     |     ✅     |
| **Planned**    |   ✅   |    —    |   ✅    |     ✅      |     —     |     ✅     |
| **Booked**     |   —   |    ✅    |   —    |     ✅      |     —     |     ✅     |
| **InProgress** |   —   |    —    |   —    |     —      |     ✅     |     ✅     |
| **Completed**  |   —   |    —    |   —    |     —      |     —     |     —     |
| **Cancelled**  |   ✅   |    —    |   —    |     —      |     —     |     —     |

Переход в тот же статус — идемпотентная операция (без ошибки). Автоматические переходы `Planned/Booked → InProgress → Completed` выполняет фоновый воркер по календарю.

### 10.3. Вычисляемые показатели

| Показатель           | Формула                                                                    |
| -------------------- | -------------------------------------------------------------------------- |
| `DurationDays`       | `EndDate.DayNumber − StartDate.DayNumber + 1`                              |
| `PlannedCost`        | сумма стоимостей пунктов со статусом ≠ `Cancelled`                         |
| `BookedCost`         | сумма стоимостей пунктов `Booked` + `Done`                                 |
| `Remaining`          | `Budget − PlannedCost` (может быть отрицательным — это сигнал перерасхода) |
| `BudgetUsagePercent` | `PlannedCost / Budget × 100`, округление до 0,1; при нулевом бюджете — 0   |

### 10.4. Статусы пунктов программы

`Planned` → `Booked` → `Done`; `Cancelled` исключается из расчёта расходов. Ограничений на переходы пунктов нет (в отличие от поездки) — это осознанное упрощение.

### 10.5. Валюты

Поддерживаются: **USD, EUR, RUB, CNY, TRY, THB, AED, GEL, KZT**. Курсы заданы относительно USD (учебная «заглушка», работает офлайн).

Формула конвертации — через опорную валюту, **без промежуточного округления**:

```
result = round(amount / UsdRates[from] * UsdRates[to], 2)
```

Справочник `GET /api/meta/rates/{base}` возвращает `rates[X] = UsdRates[X] / UsdRates[base]`, то есть «сколько единиц X в одной единице базы» (округление до 6 знаков — только для отображения).

Контрольные значения:

| Конвертация          | Результат |
| -------------------- | --------- |
| 9250 RUB → USD       | 100,00    |
| 100 USD → RUB        | 9250,00   |
| 25000 RUB → CNY      | 1959,46   |
| 9250 RUB → USD → RUB | 9250,00   |

Дашборд приводит все суммы к базовой валюте **RUB** (`DashboardService.BaseCurrency`).

---

## 11. REST API

Базовый адрес: `http://localhost:5080` (dev) или адрес развёртывания. Все ответы — `application/json`; перечисления передаются строками; даты — `yyyy-MM-dd`; время — `HH:mm`.

### 11.1. Аутентификация

```
Authorization: Bearer <accessToken>
```

Access-токен живёт 15 минут. При `401` клиент однократно вызывает `POST /api/auth/refresh` и повторяет исходный запрос. Refresh-токен одноразовый (ротация).

### 11.2. Сводка эндпоинтов

| Метод  | Путь                                          | Доступ               | Назначение               | Успех |
| ------ | --------------------------------------------- | -------------------- | ------------------------ | ----- |
| POST   | `/api/auth/register`                          | анонимно, rate limit | регистрация              | 200   |
| POST   | `/api/auth/login`                             | анонимно, rate limit | вход                     | 200   |
| POST   | `/api/auth/refresh`                           | анонимно, rate limit | обновление токенов       | 200   |
| POST   | `/api/auth/logout`                            | JWT, rate limit      | отзыв refresh-токена     | 204   |
| GET    | `/api/auth/me`                                | JWT                  | профиль                  | 200   |
| GET    | `/api/trips`                                  | JWT                  | список с фильтрами       | 200   |
| GET    | `/api/trips/{id}`                             | JWT, владелец/Admin  | поездка + программа      | 200   |
| POST   | `/api/trips`                                  | JWT                  | создать (статус `Draft`) | 201   |
| PUT    | `/api/trips/{id}`                             | JWT, владелец/Admin  | обновить (+`version`)    | 200   |
| DELETE | `/api/trips/{id}`                             | JWT, владелец/Admin  | удалить                  | 204   |
| POST   | `/api/trips/{tripId}/activities`              | JWT                  | добавить пункт           | 200   |
| PUT    | `/api/trips/{tripId}/activities/{activityId}` | JWT                  | изменить пункт           | 200   |
| DELETE | `/api/trips/{tripId}/activities/{activityId}` | JWT                  | удалить пункт            | 200   |
| GET    | `/api/dashboard`                              | JWT                  | сводные показатели       | 200   |
| GET    | `/api/meta`                                   | анонимно, кэш 10 мин | справочники              | 200   |
| GET    | `/api/meta/rates/{baseCurrency}`              | анонимно             | таблица курсов           | 200   |
| GET    | `/api/meta/convert`                           | анонимно             | конвертация суммы        | 200   |
| GET    | `/api/admin/users`                            | роль `Admin`         | пользователи             | 200   |
| GET    | `/health`                                     | анонимно             | работоспособность        | 200   |
| WS     | `/hubs/notifications`                         | JWT (`access_token`) | push-уведомления         | —     |
| GET    | `/swagger`, `/openapi/v1.json`                | Development          | документация             | 200   |

> Мутации пунктов программы возвращают **полный обновлённый `TripDetails`** — клиенту не нужно перезапрашивать поездку.

### 11.3. Параметры списка поездок

| Параметр   | Тип       | По умолчанию | Описание                                                             |
| ---------- | --------- | ------------ | -------------------------------------------------------------------- |
| `search`   | string    | —            | подстрока в названии, городе, стране, описании (без учёта регистра)  |
| `status`   | enum      | —            | `Draft`, `Planned`, `Booked`, `InProgress`, `Completed`, `Cancelled` |
| `city`     | string    | —            | точное совпадение города (без учёта регистра)                        |
| `from`     | date      | —            | поездки, заканчивающиеся не раньше даты                              |
| `to`       | date      | —            | поездки, начинающиеся не позже даты                                  |
| `sortBy`   | string    | `startDate`  | `startDate`, `endDate`, `title`, `status`, `createdAt`               |
| `desc`     | bool      | `false`      | направление сортировки                                               |
| `page`     | int ≥ 1   | `1`          | номер страницы                                                       |
| `pageSize` | int 1…100 | `10`         | размер страницы                                                      |

Формат ответа: `{ items, page, pageSize, totalCount, totalPages }`.

### 11.4. Формат ошибок (RFC 7807)

| Исключение домена         | HTTP | Заголовок                 |
| ------------------------- | ---- | ------------------------- |
| `ValidationException`     | 400  | Ошибка валидации          |
| `AuthenticationException` | 401  | Ошибка аутентификации     |
| `ForbiddenException`      | 403  | Доступ запрещён           |
| `NotFoundException`       | 404  | Ресурс не найден          |
| `ConflictException`       | 409  | Конфликт данных           |
| прочие                    | 500  | Внутренняя ошибка сервера |

Пример ошибки валидации:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Ошибка валидации",
  "status": 400,
  "detail": "Ошибка валидации данных.",
  "instance": "/api/trips",
  "errors": { "EndDate": ["Дата окончания не может быть раньше даты начала."] },
  "traceId": "00-8f1c...-01"
}
```

Пример конфликта параллельного изменения:

```json
{
  "title": "Конфликт данных",
  "status": 409,
  "detail": "Данные изменены другим пользователем. Обновите страницу и повторите действие.",
  "instance": "/api/trips/6f1e...",
  "traceId": "00-2b7a...-01"
}
```

Ошибки DataAnnotations (атрибуты DTO) формируются ASP.NET Core автоматически и имеют ту же структуру с полем `errors`.

### 11.5. Примеры запросов

```bash
BASE=http://localhost:5080

# Регистрация
curl -s -X POST $BASE/api/auth/register -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Str0ng#Pass","displayName":"Александр"}'

# Вход и извлечение токена
TOKEN=$(curl -s -X POST $BASE/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"demo@travel.local","password":"Demo#12345"}' \
  | python3 -c "import sys,json;print(json.load(sys.stdin)['accessToken'])")

# Список поездок
curl -s "$BASE/api/trips?search=петер&status=Booked&sortBy=startDate&page=1&pageSize=5" \
  -H "Authorization: Bearer $TOKEN"

# Создание поездки
curl -s -X POST $BASE/api/trips -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{
  "title":"Владимир — Суздаль: выходные",
  "city":"Владимир","country":"Россия",
  "startDate":"2026-12-05","endDate":"2026-12-07",
  "budget":25000,"currency":"RUB",
  "description":"Зодчество, прогулки, местная кухня",
  "travelers":["Александр","Анна"]}'

# Добавление пункта программы (TRIP_ID — идентификатор из ответа выше)
curl -s -X POST $BASE/api/trips/$TRIP_ID/activities \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{
  "title":"Золотые ворота","category":"Museum","status":"Planned",
  "date":"2026-12-05","startTime":"11:00","durationMinutes":90,
  "cost":600,"location":"Владимир","order":0}'

# Смена статуса поездки (обязательно передать актуальную version)
curl -s -X PUT $BASE/api/trips/$TRIP_ID -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{
  "title":"Владимир — Суздаль: выходные","city":"Владимир","country":"Россия",
  "startDate":"2026-12-05","endDate":"2026-12-07","budget":25000,"currency":"RUB",
  "status":"Planned","travelers":["Александр","Анна"],"version":"'$VERSION'"}'

# Дашборд, справочники, конвертация
curl -s $BASE/api/dashboard -H "Authorization: Bearer $TOKEN"
curl -s $BASE/api/meta
curl -s "$BASE/api/meta/convert?amount=25000&from=RUB&to=CNY"

# Администрирование
curl -s "$BASE/api/admin/users?page=1&pageSize=20" -H "Authorization: Bearer $ADMIN_TOKEN"
```

### 11.6. Ограничения и кэширование

| Механизм       | Параметры                                                                                     |
| -------------- | --------------------------------------------------------------------------------------------- |
| Rate limiting  | политика `auth`: 10 запросов в минуту на IP для `/api/auth/*`; при превышении — `429`         |
| Output caching | политика `meta`: `GET /api/meta` кэшируется на 10 минут                                       |
| Пагинация      | `pageSize` принудительно ограничивается диапазоном 1…100                                      |
| CORS           | в Development разрешены `http://localhost:5173` и `http://127.0.0.1:5173`, с учётными данными |

---

## 12. Безопасность

### 12.1. Что реализовано

| Механизм                         | Детали                                                                                                                                          |
| -------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| Хеширование паролей              | PBKDF2-SHA256, 210 000 итераций, соль 16 байт, ключ 32 байта; формат `iterations.salt.hash`; сравнение в постоянном времени (`FixedTimeEquals`) |
| Access-токен                     | JWT HS256, 15 минут, claims: `sub`, `email`, `role`, `name`, `jti`                                                                              |
| Проверка токена                  | издатель, аудитория, подпись, срок; допуск рассинхронизации часов 30 с                                                                          |
| Refresh-токен                    | 512 бит случайных данных, 14 дней, **ротация** с записью `ReplacedByToken` и отзывом                                                            |
| Очистка                          | при входе удаляются отозванные и истёкшие токены пользователя                                                                                   |
| Единое сообщение об ошибке входа | «Неверный e-mail или пароль» — не раскрывает наличие аккаунта                                                                                   |
| Разграничение данных             | обычный пользователь видит только свои поездки; `Admin` — все                                                                                   |
| Политики                         | `AdminOnly` требует роль `Admin`                                                                                                                |
| Rate limiting                    | защита эндпоинтов аутентификации от перебора                                                                                                    |
| Секреты                          | ключ подписи не хранится в репозитории для продакшена; поддерживаются user-secrets и ENV                                                        |
| Валидация                        | DataAnnotations на DTO + инварианты домена                                                                                                      |
| Диагностика без утечек           | клиенту не отдаются стек-трейсы; подробности 500 — только в логах                                                                               |

### 12.2. Особенности конфигурации JWT

* `MapInboundClaims = false` — короткие имена claims сохраняются как есть.
* `NameClaimType = "sub"`, `RoleClaimType = "role"` — поэтому `[Authorize(Roles = "Admin")]` работает без дополнительных映射.
* Для SignalR токен передаётся в query-параметре `access_token` (браузерный WebSocket не умеет ставить заголовок `Authorization`); обработчик `OnMessageReceived` принимает его **только** для путей `/hubs/*`.

### 12.3. Обязательные меры перед промышленной эксплуатацией

1. Сгенерировать и передать `Jwt__SigningKey` из хранилища секретов (не в коде и не в образе).
2. Сменить пароли демо-учётных записей или отключить сидирование.
3. Включить HTTPS и перенаправление, HSTS, безопасные заголовки.
4. Рассмотреть перенос токенов из `localStorage` в `httpOnly` + `Secure` cookie по схеме BFF (защита от XSS-кражи).
5. Добавить аудит входов, блокировку после N неудачных попыток, при необходимости — 2FA.
6. Ограничить CORS конкретными доменами, включить строгую политику CSP для SPA.
7. Настроить резервное копирование БД и ротацию логов.

---

## 13. Real-time уведомления

### 13.1. Подключение

Хаб: `/hubs/notifications`, авторизация обязательна. Каждый клиент добавляется в группу `user:{userId}` — уведомления получают только вкладки владельца поездки (все его устройства).

```ts
const connection = new HubConnectionBuilder()
  .withUrl('/hubs/notifications', { accessTokenFactory: () => accessToken })
  .withAutomaticReconnect()
  .build();

connection.on('notification', n => console.log(n.title, n.message));
await connection.start();
```

### 13.2. События

Событие одно — `notification`, полезная нагрузка: `{ type, title, message, tripId, createdAtUtc }`.

| `type`             | Источник                | Повод                                |
| ------------------ | ----------------------- | ------------------------------------ |
| `trip.created`     | `TripService`           | поездка создана                      |
| `trip.updated`     | `TripService`           | параметры поездки изменены           |
| `trip.deleted`     | `TripService`           | поездка удалена                      |
| `activity.booked`  | `TripService`           | пункт программы переведён в `Booked` |
| `activity.deleted` | `TripService`           | пункт программы удалён               |
| `trip.started`     | `TripMaintenanceWorker` | дата начала наступила → `InProgress` |
| `trip.completed`   | `TripMaintenanceWorker` | дата окончания прошла → `Completed`  |
| `trip.reminder`    | `TripMaintenanceWorker` | до начала ≤ 3 дней (однократно)      |

### 13.3. Поведение клиента

`useNotifications` хранит последние 5 сообщений, показывает индикатор соединения в шапке и автоматически переподключается. Сообщения закрываются вручную; история в БД не сохраняется (это осознанное упрощение — при необходимости добавьте таблицу `Notifications`).

### 13.4. Масштабирование

При нескольких экземплярах сервера подключите backplane:

```csharp
builder.Services.AddSignalR().AddStackExchangeRedis("<redis-connection>");
```

либо используйте Azure SignalR Service. Без backplane уведомление дойдёт только до клиентов того экземпляра, который обработал запрос.

---

## 14. Фоновый воркер

`TripMaintenanceWorker` — `BackgroundService` с `PeriodicTimer`, интервал **1 минута**, первый прогон сразу после старта.

| Задача             | Условие                                                                                 | Действие                                                  |
| ------------------ | --------------------------------------------------------------------------------------- | --------------------------------------------------------- |
| Начало поездки     | статус `Planned`/`Booked`, `StartDate ≤ сегодня ≤ EndDate`                              | `ChangeStatus(InProgress)` + уведомление `trip.started`   |
| Завершение поездки | статус `InProgress`, `EndDate < сегодня`                                                | `ChangeStatus(Completed)` + уведомление `trip.completed`  |
| Напоминание        | статус `Planned`/`Booked`, `сегодня < StartDate ≤ сегодня+3`, `ReminderSentUtc IS NULL` | установка `ReminderSentUtc` + уведомление `trip.reminder` |

Особенности реализации:

* Каждый прогон создаёт собственный DI-scope (`IServiceScopeFactory`) — `DbContext` не живёт дольше итерации.
* Кандидаты отбираются по статусу в SQL, даты сравниваются в памяти (набор небольшой) — исключает любые нюансы трансляции конвертеров.
* Идемпотентность напоминаний обеспечивается полем `ReminderSentUtc`.
* Исключение одной итерации логируется и **не останавливает** службу; `OperationCanceledException` при остановке приложения обрабатывается штатно.

---

## 15. Фронтенд

### 15.1. Маршруты

| Путь         | Страница          | Доступ                                        |
| ------------ | ----------------- | --------------------------------------------- |
| `/login`     | `LoginPage`       | анонимно                                      |
| `/register`  | `RegisterPage`    | анонимно                                      |
| `/`          | `DashboardPage`   | JWT                                           |
| `/trips`     | `TripsPage`       | JWT                                           |
| `/trips/:id` | `TripDetailsPage` | JWT, владелец/Admin                           |
| `/admin`     | `AdminPage`       | роль `Admin` (пункт меню скрыт для остальных) |
| прочее       | редирект на `/`   | —                                             |

### 15.2. Клиент API

`src/api/client.ts` — единственный слой обращения к серверу:

* токены хранятся в `localStorage` (`tp.accessToken`, `tp.refreshToken`);
* при `401` выполняется **однократное** обновление токена (single-flight: параллельные запросы ждут один и тот же промис), затем запрос повторяется;
* при неудачном обновлении хранилище очищается и рассылается событие `tp:unauthorized` → `AuthContext` сбрасывает пользователя;
* `204 No Content` возвращается как `undefined`;
* любой неуспешный ответ превращается в `ApiError` с полями `status`, `message`, `errors`.

### 15.3. Типы и локализация

* `api/types.ts` зеркально повторяет DTO сервера (camelCase). Перечисления описаны строковыми union-типами — несоответствиеcaught на этапе компиляции.
* `labels.ts` содержит русские названия статусов и категорий, цвета категорий, форматирование денег (`Intl.NumberFormat`) и дат. Дата парсится как `${iso}T00:00:00`, чтобы избежать сдвига часового пояса.

### 15.4. Особенности интерфейса

* Поиск с задержкой 250 мс — меньше запросов к серверу.
* Формы поездок и пунктов программы в модальных окнах, клиентская предпроверка дат (сервер проверяет повторно).
* Для завершённых и отменённых поездок кнопки редактирования программы отключены — согласовано с правилом домена.
* Диаграмма расходов — чистый SVG (`stroke-dasharray`), без библиотек графиков.
* Индикатор состояния SignalR и всплывающие уведомления в `Layout`.

### 15.5. Сборка

```bash
cd frontend
npm run dev        # dev-сервер с HMR и прокси
npm run typecheck  # проверка типов без сборки
npm run build      # tsc --noEmit + vite build → ../src/TravelPlanner.Api/wwwroot
npm run preview    # предпросмотр собранного бандла
```

`emptyOutDir: true` очищает `wwwroot`, поэтому исходная заглушка `index.html` заменяется реальным SPA — это ожидаемое поведение.

---

## 16. Тестирование

### 16.1. Запуск

```bash
dotnet test                                              # все тесты
dotnet test --filter "FullyQualifiedName~DomainTests"     # только домен
dotnet test --filter "FullyQualifiedName~CurrencyServiceTests"
dotnet test --collect:"XPlat Code Coverage"               # покрытие (coverlet)
```

### 16.2. Что покрыто

| Набор                  | Проверки                                                                                                                                                                                                                                                                                                     |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `DomainTests`          | длительность поездки; нормализация валюты; запрет обратных дат; исключение отменённых пунктов из расходов; автопорядок и запрет даты вне диапазона; недопустимый переход статуса; «только чтение» для завершённой поездки; запрет сужения дат при наличии пунктов вне диапазона; смена `Version` в `Touch()` |
| `PasswordHasherTests`  | корректный пароль принимается; неверный отклоняется; соль случайна; повреждённый хеш не приводит к исключению                                                                                                                                                                                                |
| `CurrencyServiceTests` | тождество валют; RUB→USD; обратное USD→RUB без дрейфа; кросс-курс RUB→CNY; замкнутый пересчёт; регистронезависимость; ноль; неизвестная валюта; база справочника; согласованность справочника и конвертера                                                                                                   |

### 16.3. Чего пока нет

* Интеграционные тесты HTTP на `WebApplicationFactory<Program>` (авторизация, коды ответов, конкурентные PUT).
* Тесты репозиторного слоя на SQLite in-memory.
* E2E-тесты интерфейса (Playwright).
* Тесты фонового воркера с подменой времени (`TimeProvider`) — требует введения абстракции времени.

---

## 17. Сборка и развёртывание

### 17.1. Единый процесс (без Docker)

```bash
cd frontend && npm ci && npm run build && cd ..
dotnet publish src/TravelPlanner.Api -c Release -o ./publish

export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=http://+:8080
export Jwt__SigningKey="<криптостойкий ключ ≥ 32 символов>"
export ConnectionStrings__Default="Data Source=App_Data/travelplanner.db"
dotnet ./publish/TravelPlanner.Api.dll
```

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS = "http://+:8080"
$env:Jwt__SigningKey = "<криптостойкий ключ ≥ 32 символов>"
dotnet .\publish\TravelPlanner.Api.dll
```

API и SPA доступны с одного адреса — CORS не нужен, клиентский роутинг обеспечивает `MapFallbackToFile("index.html")`.

### 17.2. Docker

```bash
docker compose up --build -d
docker compose logs -f web
docker compose down            # том с данными сохраняется
docker compose down -v         # ВНИМАНИЕ: удаляет данные
```

Образ собирается в три этапа: Node (сборка SPA) → .NET SDK (publish) → ASP.NET runtime (только артефакты). Итоговый образ не содержит Node.js, исходников и SDK.

| Параметр контейнера     | Значение                                                     |
| ----------------------- | ------------------------------------------------------------ |
| Порт                    | 8080                                                         |
| Том                     | `travel-data` → `/app/App_Data`                              |
| Обязательная переменная | `Jwt__SigningKey` (замените значение в `docker-compose.yml`) |
| Строка подключения      | `Data Source=/app/App_Data/travelplanner.db`                 |

Без Docker:

```bash
docker build -t travelplanner:1.0 .
docker run -d --name travelplanner -p 8080:8080 \
  -e Jwt__SigningKey="<ключ>" \
  -v travel-data:/app/App_Data travelplanner:1.0
```

### 17.3. Обратный прокси (nginx)

Обязательна поддержка WebSocket для `/hubs`:

```nginx
server {
    listen 80;
    server_name travel.example.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name travel.example.com;

    ssl_certificate     /etc/letsencrypt/live/travel.example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/travel.example.com/privkey.pem;

    location /hubs/ {
        proxy_pass         http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection "upgrade";
        proxy_set_header   Host $host;
        proxy_read_timeout 3600s;
    }

    location / {
        proxy_pass         http://127.0.0.1:8080;
        proxy_set_header   Host $host;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
```

При работе за прокси добавьте в `Program.cs` обработку `ForwardedHeaders`, иначе rate limiter будет видеть IP прокси:

```csharp
builder.Services.Configure<ForwardedHeadersOptions>(o =>
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
// ...
app.UseForwardedHeaders();   // до UseRateLimiter
```

### 17.4. systemd (Linux, без контейнера)

```ini
[Unit]
Description=TravelPlanner
After=network.target

[Service]
WorkingDirectory=/opt/travelplanner
ExecStart=/usr/bin/dotnet /opt/travelplanner/TravelPlanner.Api.dll
Restart=always
RestartSec=10
User=travelplanner
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://+:8080
Environment=ConnectionStrings__Default=Data Source=/var/lib/travelplanner/travelplanner.db
EnvironmentFile=/etc/travelplanner/secrets.env

[Install]
WantedBy=multi-user.target
```

### 17.5. Пример CI (GitHub Actions)

```yaml
name: ci
on: [push, pull_request]
jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0.x' }
      - uses: actions/setup-node@v4
        with: { node-version: '22' }
      - run: dotnet restore
      - run: dotnet build --no-restore -c Release
      - run: dotnet test --no-build -c Release
      - run: npm ci
        working-directory: frontend
      - run: npm run build
        working-directory: frontend
```

> В промышленном конвейере миграции применяются отдельным шагом (`dotnet ef database update` или idempotent-скрипт), а не при старте приложения — это исключает гонки при нескольких репликах.

---

## 18. Эксплуатация и обслуживание

### 18.1. Логи и трассировка

Каждый запрос получает `X-Correlation-Id` (входящий заголовок уважается), попадает в область логирования и в ответ. Итоговая строка лога: метод, путь, код ответа, длительность в миллисекундах. Ошибки 500 логируются с исключением, бизнес-ошибки — как `Warning`.

```bash
# найти все записи по конкретному запросу
docker compose logs web | grep 3f9c1a2b
```

### 18.2. Проверка работоспособности

```bash
curl -i http://localhost:8080/health     # 200 Healthy; проверяется доступность БД
```

Пригодно для `HEALTHCHECK` в Docker, проб Kubernetes и мониторинга.

### 18.3. Резервное копирование SQLite

```bash
# корректная «горячая» копия
sqlite3 src/TravelPlanner.Api/App_Data/travelplanner.db ".backup '/backups/tp-$(date +%F).db'"

# либо: остановить приложение и скопировать файл вместе с -wal/-shm
```

Восстановление — подмена файла при остановленном приложении. Не копируйте только `.db` при активном WAL: часть данных останется в `-wal`.

### 18.4. Обновление версии

1. Остановить сервис (или обновлять поочерёдно при backplane SignalR).
2. Сделать резервную копию БД.
3. Применить миграции.
4. Развернуть новый артефакт/образ, проверить `/health` и вход пользователя.

---

## 19. Диагностика проблем

| Симптом                                            | Причина                                                                 | Решение                                                                           |
| -------------------------------------------------- | ----------------------------------------------------------------------- | --------------------------------------------------------------------------------- |
| `Не задан Jwt:SigningKey` при старте               | не подхватился `appsettings.Development.json` либо не задана переменная | проверьте `ASPNETCORE_ENVIRONMENT`, задайте `Jwt__SigningKey` (≥ 32 символов)     |
| `dotnet ef` не найден                              | инструмент не установлен или не в `PATH`                                | `dotnet tool install --global dotnet-ef --version 10.*`, перезапустить терминал   |
| `unable to open database file`                     | нет каталога `App_Data` или прав записи                                 | каталог создаётся в `Program.cs`; проверьте права, в Docker — том                 |
| SPA отдаёт заглушку «Интерфейс ещё не собран»      | не выполнен `npm run build`                                             | соберите клиент                                                                   |
| `401` в SignalR                                    | истёк access-токен                                                      | клиент обновит автоматически; при постоянных 401 сверьте `Issuer`/`Audience`/ключ |
| `429` при входе                                    | сработал rate limiter (10/мин на IP)                                    | подождите минуту; за прокси настройте `ForwardedHeaders`                          |
| `409` при сохранении                               | запись изменена в другой вкладке                                        | обновите страницу: изменения применяются к актуальной `version`                   |
| `409` при добавлении пункта                        | поездка `Completed`/`Cancelled`                                         | верните поездку в `Draft` (из `Cancelled`) либо создайте новую                    |
| `400` «Дата активности должна попадать в диапазон» | дата вне дат поездки                                                    | измените дату пункта или расширьте даты поездки                                   |
| `400` при сужении дат                              | есть пункты вне нового диапазона                                        | сначала перенесите пункты                                                         |
| CORS-ошибка в dev                                  | клиент открыт не с `localhost:5173`                                     | используйте адрес из политики или дополните `WithOrigins`                         |
| Диаграмма пуста                                    | все пункты отменены или стоимость 0                                     | это ожидаемое поведение                                                           |
| Курс валют «странный»                              | перепутано направление котировки                                        | см. раздел 20.2 и семантику `rates`                                               |

---

## 20. Исправленные дефекты («грабли»)

Раздел сохранён намеренно: это реальные ошибки, найденные при сборке и тестировании, и полезные уроки.

### 20.1. CS0119 + CS8422 в `MetaController`

**Симптом.** Сборка падала с ошибками «`Convert` является метод, который недопустим в данном контексте» и «Статическая локальная функция не может содержать ссылку на `this`».

**Причина.** Действие контроллера было названо `Convert`, что затенило тип `System.Convert` внутри класса. Вызов `Convert.ToInt32(...)` связывался с методом экземпляра, недоступным в статической локальной функции.

**Исправление.** Действие переименовано в `ConvertCurrency` (маршрут `api/meta/convert` сохранён — клиент не менялся), помощник вынесен в `private static` метод, тип указан полностью: `System.Convert.ToInt32(...)`.

**Урок.** Не называйте члены именами типов BCL (`Convert`, `Task`, `Path`, `Environment`, `Random`, `File`). При неизбежном совпадении используйте полную квалификацию, включая `global::`.

### 20.2. Неверное направление конвертации валют

**Симптом.** Тест `Convert_RubToUsd_UsesRate` ожидал `100`, получал `855 625,00` — ровно `9250 × 92,5`.

**Причина.** В `ConvertAsync` курсы запрашивались с базой «целевая валюта», где `rates[from]` означает «сколько `from` в одном `to`», но сумма **умножалась** вместо деления.

**Исправление.** Конвертация считается напрямую через опорную валюту без промежуточного округления:

```csharp
var amountInUsd = amount / UsdRates[from];
var result = decimal.Round(amountInUsd * UsdRates[to], 2);
```

Промежуточный вариант «просто заменить `*` на `/`» проходил исходный тест, но давал дрейф в обратную сторону (`100 USD → 9249,84 RUB` вместо `9250,00`) из-за округления курсов до 6 знаков — поэтому он отвергнут.

**Дополнительно.** В `MetaController` база курсов изменена на исходную валюту, чтобы поле `rate` читалось естественно: «1 `from` = rate `to`».

**Урок.** Округляйте только конечный денежный результат; документируйте семантику курсов; для конвертеров обязательны тесты инвариантов — тождество, обратимость, согласованность со справочником.

---

## 21. Ограничения и план развития

### 21.1. Ограничения текущей версии

| Ограничение                                   | Влияние                                                                                     | Как снять                                                  |
| --------------------------------------------- | ------------------------------------------------------------------------------------------- | ---------------------------------------------------------- |
| SQLite: `decimal` без точной арифметики в SQL | сортировка/агрегация по деньгам приблизительны (в коде такие операции выполняются в памяти) | PostgreSQL или хранение сумм в копейках (`long`)           |
| Курсы валют — константы                       | не отражают реальный рынок                                                                  | `IHttpClientFactory` + API ЦБ РФ/ECB, Polly, кэш уже готов |
| Токены в `localStorage`                       | уязвимость при XSS                                                                          | `httpOnly` cookie + BFF                                    |
| Сидер с фиксированными паролями               | небезопасно в продакшене                                                                    | отдельная команда инициализации, пароль из секрета         |
| Агрегаты дашборда в памяти                    | линейный рост стоимости от числа поездок                                                    | SQL-агрегация, материализованные представления             |
| Автомиграция при старте                       | гонки при нескольких репликах                                                               | шаг конвейера + блокировка                                 |
| SignalR без backplane                         | уведомления только в рамках одного экземпляра                                               | Redis / Azure SignalR                                      |
| Уведомления не сохраняются                    | история теряется при перезагрузке                                                           | таблица `Notifications` + эндпоинт истории                 |
| Нет абстракции времени                        | воркер и напоминания трудно тестировать                                                     | `TimeProvider` во внедрении зависимостей                   |
| Логи только в консоль                         | нет централизованного поиска                                                                | Serilog/OpenTelemetry, OTLP-экспорт                        |

### 21.2. План развития

1. **Вложения**: чеки и билеты (`IFileStorage`, S3/MinIO), лимиты и проверка типов.
2. **Совместное планирование**: `TripMember` с правами, SignalR-группы на поездку.
3. **Экспорт**: маршрутный лист PDF (QuestPDF), календарь ICS (`Ical.Net`).
4. **Карта**: координаты пунктов, MapLibre/Leaflet, маршрут дня.
5. **Наблюдаемость**: метрики, трейсинг, дашборды Grafana.
6. **Интеграционные тесты**: `WebApplicationFactory` + SQLite in-memory, сценарии авторизации и конкуренции.
7. **Мобильный клиент**: тот же API пригоден для .NET MAUI или React Native.

---

## 22. Соглашения разработчика

### 22.1. Код

* Язык кода — C# / TypeScript; язык комментариев и сообщений пользователю — русский.
* Nullable включён везде; предупреждения анализаторов не подавляются без обоснования.
* Бизнес-правила — только в домене. Сервис оркеструет, контроллер транспортирует.
* Новый эндпоинт = DTO в `Core/Contracts` + метод сервиса + тест правила.
* Имена действий — предметные (`ConvertCurrency`, `GetRates`), не совпадающие с типами BCL.
* Деньги: `decimal`, округление до 2 знаков **только** в конце вычисления.
* Даты: `DateOnly`/`TimeOnly` в домене, ISO-строки в JSON.

### 22.2. Проверки перед коммитом

```bash
dotnet build          # 0 ошибок и 0 новых предупреждений
dotnet test           # все тесты зелёные
cd frontend && npm run typecheck && npm run build
```

### 22.3. Чек-лист ревью

* [ ] Правило проверено в домене, а не только на клиенте.
* [ ] Добавлен/обновлён тест (для конвертеров и правил — инварианты).
* [ ] Ошибки возвращаются в формате ProblemDetails с корректным кодом.
* [ ] Проверен доступ: владелец против чужой поездки, `User` против `Admin`.
* [ ] Обновлены `README.md` и справочник эндпоинтов при изменении контракта.
* [ ] Нет секретов в коде, логах и клиентском бандле.
* [ ] Изменения схемы оформлены миграцией.

---

## 23. FAQ

**Вопрос: обязательно ли создавать миграции перед первым запуском?**
Нет. При отсутствии миграций вызывается `EnsureCreatedAsync()`. Но для управляемого изменения схемы миграции нужны.

**Вопрос: почему после `npm run build` исчезла заглушка в `wwwroot`?**
Так задумано: `emptyOutDir: true` очищает каталог, заглушка заменяется реальным SPA.

**Вопрос: можно ли запускать только сервер?**
Да. API полностью работоспособен, а `/swagger` позволяет exercises все эндпоинты без клиента.

**Вопрос: почему суммы дашборда в рублях, если поездки в юанях?**
`DashboardService` приводит все значения к базовой валюте RUB через `ICurrencyService`. Складывать разные валюты напрямую нельзя.

**Вопрос: почему поездку нельзя отредактировать после завершения?**
Правило домена `EnsureEditable`: завершённые и отменённые поездки — архив. Из `Cancelled` можно вернуться в `Draft`.

**Вопрос: как добавить новую валюту?**
Дополните словарь `UsdRates` в `CurrencyService`. Справочник `/api/meta`, формы и конвертер подхватят её автоматически.

**Вопрос: как добавить категорию активности?**
Новое значение в `ActivityCategory` (Core), затем подпись и цвет в `frontend/src/labels.ts`. Миграция не требуется: перечисления хранятся строками.

**Вопрос: почему при сохранении приходит 409, хотя я работаю один?**
Открыта вторая вкладка со устаревшей `version`. Обновите страницу — клиент всегда отправляет версию, полученную при чтении.

**Вопрос: где взять реальные курсы валют?**
Замените словарь в `CurrencyService` на вызов API ЦБ РФ (`https://www.cbr-xml-daily.ru/daily_json.js`) или ECB через `IHttpClientFactory`, сохранив кэширование и семантику «единиц X в одной единице базы».

---

## 24. Лицензия и автор

**Лицензия:** MIT (при использовании проекта как шаблона сохраните уведомление об авторских правах).

**Автор:** Александр Васильевич Лоханов.

**Назначение документа:** учебное пособие и эксплуатационная инструкция одновременно. При изменении контрактов API, схемы БД или правил домена обновляйте соответствующие разделы — README считается частью кода и проходит ревью наравне с ним.