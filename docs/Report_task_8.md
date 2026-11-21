# Львівський національний університет імені Івана Франка

**Факультет прикладної математики та інформатики**  
Кафедра програмування

## Звіт
### етапу виконання проєкту №8

**«Неперервна інтеграція»**

**Виконали:** студенти групи ПМІ-33

- Вербова Надія Андріївна
- Махновець Богдан Олександрович
- Митюк Маркіян Андрійович
- Нагірняк Вікторія Іванівна

**Перевірив:** ас. Галамага Любомир Борисович

---

## 1. Мета етапу

Налаштувати в репозиторії процес неперервної інтеграції (CI), який для кожної зміни коду автоматично:

- збирає проєкт;
- запускає тести;
- виконує автоматизовані перевірки якості коду.

---

## 2. Інструменти

| Інструмент | Призначення |
|---|---|
| **GitHub Actions** | Виконання пайплайна CI на серверах GitHub |
| **`windows-latest`** | Раннер Windows: застосунок має target `net10.0-windows` (WinForms) і на Linux не збирається |
| **`global.json`** | Фіксує версію .NET SDK (10.0.x), щоб локально й у CI збирання було однаковим |
| **`unilib.slnx`** | Файл рішення, який об'єднує проєкти `unilib` і `unilib.Tests` |
| **xUnit + coverlet** | Запуск тестів і збирання покриття коду |
| **ReportGenerator** | Таблиця покриття в підсумку запуску та HTML-звіт |
| **`dotnet format` + `.editorconfig`** | Перевірка форматування коду |
| **Dependabot** | Щотижневі PR з оновленнями NuGet-пакетів і GitHub Actions |

---

## 3. Як налаштовано пайплайн

Пайплайн описано у файлі `.github/workflows/ci.yml`.

### 3.1. Коли запускається

- push у `main`;
- кожен pull request у `main`;
- вручну: **Actions → CI → Run workflow**.

Новий запуск для тієї самої гілки скасовує попередній незавершений (`concurrency`). Права пайплайна обмежено читанням коду (`permissions: contents: read`).

### 3.2. Схема

```
push / pull request у main
          │
          ├──► Build and test (windows-latest)
          │       restore → build -warnaserror → test + coverage → звіт → артефакт
          │
          └──► Code quality (windows-latest)
                  restore → перевірка форматування → перевірка вразливих пакетів
```

Обидва jobs виконуються паралельно. Зміна вважається успішною, лише якщо пройшли обидва.

### 3.3. Job «Build and test»: збирання і тести

| Крок | Команда | Коли падає |
|---|---|---|
| Restore | `dotnet restore unilib.slnx` | пакет не вдалося завантажити |
| Build | `dotnet build -c Release -warnaserror` | помилка **або попередження** компілятора |
| Run tests | `dotnet test` з логерами TRX і GitHubActions та збиранням покриття | хоча б один тест не пройшов |
| Coverage summary | ReportGenerator додає таблицю покриття в підсумок запуску | — |
| Upload | артефакт `test-results`: TRX-файл і HTML-звіт покриття (зберігається 14 днів) | — |

Тести працюють на SQLite у пам'яті, тому PostgreSQL у CI не потрібен. Тести, що не пройшли, показуються як анотації прямо в pull request.

### 3.4. Job «Code quality»: автоматизовані перевірки якості

| Перевірка | Команда | Коли падає |
|---|---|---|
| Форматування | `dotnet format whitespace --verify-no-changes` | код не відповідає правилам `.editorconfig` |
| Вразливі пакети | `dotnet package list --vulnerable --include-transitive` | є пакет (прямий або транзитивний) з відомою вразливістю |

Додаткові перевірки якості:

- **Попередження = помилки** (`-warnaserror`). Код із попередженнями компілятора, наприклад можливим `null`, не проходить CI.
- **Відповідність міграцій моделі.** Тест `ModelTests.Migrations_MatchTheModel` падає, якщо модель бази змінено без міграції.
- **Dependabot** щотижня пропонує оновлення пакетів. Кожен такий PR теж проходить CI, тому оновлення не зламає збирання непомітно.

---

## 4. Зміни в репозиторії

Усе налаштування виконано в окремій гілці `ci/continuous-integration` і злито в `main` через pull request #5:

| Коміт | Зміст |
|---|---|
| Add solution file and pin the .NET SDK | `unilib.slnx`, `global.json` |
| Add GitHub Actions workflow that builds the project | `.github/workflows/ci.yml`: restore і build |
| Run the xUnit tests in CI with results and coverage | запуск тестів, TRX, покриття, артефакт |
| Add .editorconfig and fix whitespace formatting | правила форматування; код приведено до них |
| Add code quality checks to CI | job «Code quality» |
| Add Dependabot for NuGet packages and GitHub Actions | `.github/dependabot.yml` |
| Document the CI pipeline and add a README with the status badge | `docs/CI.md`, `README.md` зі значком статусу CI |

Згенеровані міграції EF Core позначено в `.editorconfig` як `generated_code`, тому перевірка форматування їх не чіпає.

---

## 5. Демонстрація успішного автоматизованого запуску

Значок у `README.md` показує статус CI для гілки `main`:

[![CI](https://github.com/SYlence23/unilib/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/SYlence23/unilib/actions/workflows/ci.yml)

Запуски CI (18.11.2026):

| № | Подія | Гілка / зміна | Результат |
|---:|---|---|---|
| 1 | pull request | `ci/continuous-integration`: налаштування CI | ✅ success |
| 2 | push | `main`: злиття PR #5 | ✅ success |
| 3, 9 | pull request | Dependabot: оновлення EF Core SQLite 10.0.12 → 10.0.15 | ✅ success |
| 4, 10 | pull request | Dependabot: оновлення coverlet, Test SDK, xUnit runner | ✅ success |
| 5, 7 | pull request | `feature/logging-errors-security` | ✅ success |
| 6 | pull request | `unilib-tests`: звіти етапів 6 і 7 | ✅ success |
| 8 | push | `main`: злиття PR #9 | ✅ success |

Запуск №8 для `main`: обидва jobs пройшли приблизно за 2 хвилини.

```
Build and test   ✅  checkout → setup .NET → restore → build -warnaserror → run tests
                     → coverage summary → upload test results
Code quality     ✅  checkout → setup .NET → restore → check formatting
                     → check vulnerable packages
```

Результати конкретного запуску: **Actions → CI → запуск → Summary** (тести й покриття) та **Artifacts → `test-results`** (TRX і HTML-звіт покриття).

---

## 6. Як повторити перевірки локально

```powershell
dotnet build unilib.slnx -c Release -warnaserror
dotnet test unilib.slnx -c Release --collect "XPlat Code Coverage"
dotnet format whitespace unilib.slnx --verify-no-changes
dotnet package list --project unilib.slnx --vulnerable --include-transitive
```

Автоматично виправити форматування: `dotnet format whitespace unilib.slnx`.

Детальний опис пайплайна — у [CI.md](./CI.md).

---

## 7. Висновки

1. Налаштовано неперервну інтеграцію на GitHub Actions: кожен push у `main` і кожен pull request автоматично збирає проєкт і запускає всі тести.
2. Додано автоматизовані перевірки якості: попередження компілятора як помилки, перевірка форматування, пошук вразливих пакетів і перевірка відповідності міграцій моделі.
3. Dependabot автоматично пропонує оновлення залежностей, і кожне оновлення перевіряється CI.
4. Усі 10 запусків CI на момент звіту завершилися успішно. Статус видно на значку в `README.md`.
