# UniLib

[![CI](https://github.com/SYlence23/unilib/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/SYlence23/unilib/actions/workflows/ci.yml)
[![CodeQL](https://github.com/SYlence23/unilib/actions/workflows/codeql.yml/badge.svg?branch=main)](https://github.com/SYlence23/unilib/actions/workflows/codeql.yml)

Настільний застосунок для мережі факультетських бібліотек університету: єдиний каталог,
бронювання книг, мапа бібліотек і кабінет читача. Бібліотекар бачить чергу заявок
і додає книги до каталогу.

Проєкт команди групи ПМІ-33, ЛНУ імені Івана Франка.

## Можливості

**Студент**
- пошук за назвою, автором чи ISBN, фільтр за категорією, вигляд сіткою або списком;
- кількість вільних примірників і розташування книги: бібліотека та полиця;
- бронювання (до 3 відкритих заявок) і скасування заявки;
- збережені книги;
- мапа бібліотек із планом полиць, кнопка «Locate» показує, де стоїть книга;
- профіль: студентський квиток, заявки, позики, зміна пароля.

**Бібліотекар і адміністратор** (усе, що має студент, і ще)
- показники: активні позики, заявки, що чекають рішення, прострочені книги, кількість примірників;
- затвердження, відхилення і скасування рішення для кількох заявок одразу;
- додавання книги з новими авторами й категорією.

**Безпека й надійність:** паролі хешуються (PBKDF2 із сіллю), стандартний пароль
адміністратора треба змінити під час першого входу, вхідні дані перевіряються в сервісах,
помилки пишуться в журнал без персональних даних. Деталі: [docs/Security.md](docs/Security.md).

## Технології

.NET 10, Windows Forms і MaterialSkin, Entity Framework Core 10, PostgreSQL, Serilog,
xUnit і SQLite у пам'яті для тестів, GitHub Actions і CodeQL.

## Запуск

### Що потрібно

- Windows 10 або 11;
- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0);
- [PostgreSQL](https://www.postgresql.org/download/windows/) 14 або новіший, який працює локально
  чи доступний у мережі.

### 1. Отримати код

```powershell
git clone https://github.com/SYlence23/unilib.git
cd unilib
```

### 2. Вказати пароль до бази

Скопіюйте `unilib/appsettings.Local.example.json` у `unilib/appsettings.Local.json`
і впишіть свій пароль від PostgreSQL:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=unilib;Username=postgres;Password=ваш-пароль"
  }
}
```

Файл `appsettings.Local.json` є в `.gitignore`, тому пароль не потрапить у репозиторій.
Замість файлу можна задати змінну середовища `UNILIB_ConnectionStrings__Default`.

### 3. Запустити

```powershell
dotnet run --project unilib
```

Під час першого запуску застосунок сам створює базу `unilib` (якщо користувач PostgreSQL
має на це право; інакше створіть її командою `createdb unilib`), таблиці та обліковий запис
адміністратора.

### 4. Увійти

| Email | Пароль |
|---|---|
| `admin@unilib.com` | `admin123` |

Під час першого входу застосунок вимагає змінити пароль, і `admin123` вже не можна встановити знову.
Студентський акаунт створюється кнопкою «Create an account» на екрані входу.

### 5. Додати демо-дані (за бажанням)

Нова база порожня. Скрипт додає 3 бібліотеки з координатами, 7 категорій, 25 авторів і 11 книг:

```powershell
psql -h localhost -U postgres -d unilib -f scripts/demo-data.sql
```

Можна також відкрити `scripts/demo-data.sql` у pgAdmin (Query Tool) і виконати його.
Повторний запуск нічого не дублює.

### Готовий exe

```powershell
dotnet publish unilib -c Release -r win-x64 --self-contained false -o publish
```

Якщо `unilib/appsettings.Local.json` існує, він копіюється в `publish` автоматично.
На іншому комп'ютері покладіть цей файл поруч з `unilib.exe`. Release-збірка не показує
на екрані входу підказку з паролем адміністратора.

## Журнали

`%LOCALAPPDATA%\UniLib\logs\unilib-<дата>.log`. Один файл на день, зберігаються останні 14.
Рівень і папку задає секція `Logging` в `unilib/appsettings.json`.

## Тести

```powershell
dotnet test unilib.slnx
```

201 автоматичний тест. Вони працюють із SQLite у пам'яті, тому PostgreSQL для них не потрібен.
Покриття бізнес-логіки становить 90 %. Результати фінального тестування й чекліст ручної
перевірки: [docs/Testing.md](docs/Testing.md).

## Структура

```text
unilib/                 застосунок
  Models/               сутності: книга, автор, бібліотека, користувач, заявка, позика…
  Data/                 LibraryDbContext, міграції EF Core
  Services/             бізнес-логіка, правила, перевірка вводу
  UI/Forms, UI/Controls екрани: вхід, каталог, профіль, мапа, адміністрування
  AppConfig.cs, AppLog.cs  налаштування і журнал
unilib.Tests/           xUnit-тести
scripts/demo-data.sql   демо-дані
docs/                   звіти етапів і документація
.github/workflows/      CI і CodeQL
```

## Документація

| Документ | Зміст |
|---|---|
| [Report_task_1.md](docs/Report_task_1.md) | картка проєкту, команда, ролі |
| [Report_task_2.md](docs/Report_task_2.md) | вимоги, історії користувачів, MVP |
| [MarketAnalysis.md](docs/MarketAnalysis.md) | конкурентний аналіз |
| [Report_task_3.md](docs/Report_task_3.md) | UML-моделювання |
| [Report_task_4.md](docs/Report_task_4.md) | архітектура і модель даних |
| [Report_task_5.md](docs/Report_task_5.md) | початкова структура проєкту |
| [Report_task_6.md](docs/Report_task_6.md) | ітеративна реалізація функціональності |
| [Report_task_7.md](docs/Report_task_7.md) | тестування та забезпечення якості |
| [Tests.md](docs/Tests.md) | що перевіряє кожен файл тестів |
| [Report_task_8.md](docs/Report_task_8.md) · [CI.md](docs/CI.md) | неперервна інтеграція |
| [Security.md](docs/Security.md) | журналювання, обробка помилок, безпека (етап 9) |
| [Testing.md](docs/Testing.md) | фінальне тестування (етап 10) |
| [Demo.md](docs/Demo.md) | сценарій демонстрації |
| [presentation/](docs/presentation/) | фінальна презентація (.pptx і .pdf) |

## Відомі обмеження

- Видача й повернення книг реалізовані в сервісах і покриті тестами, але екрана для них ще немає.
- Застосунок підключається до PostgreSQL напряму, тому права ролей перевіряє інтерфейс.
  Для реального розгортання потрібен серверний API або окремий користувач БД з мінімальними правами.
