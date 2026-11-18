# Неперервна інтеграція (CI)

CI працює на GitHub Actions. Опис пайплайна — у
[`.github/workflows/ci.yml`](../.github/workflows/ci.yml).

## Коли запускається

- push у `main`;
- кожен pull request у `main`;
- вручну: вкладка **Actions** → **CI** → **Run workflow**.

Новий запуск для тієї самої гілки скасовує попередній, який ще не завершився.

## Що перевіряється

Два jobs виконуються паралельно на `windows-latest`, бо застосунок
має target `net10.0-windows` (WinForms).

### Build and test

| Крок | Що робить | Коли падає |
|---|---|---|
| Restore | `dotnet restore unilib.slnx` | пакет не вдалося завантажити |
| Build | `dotnet build -c Release -warnaserror` | помилка або попередження компілятора |
| Run tests | `dotnet test` для всіх xUnit-тестів, збирає покриття коду | хоча б один тест не пройшов |
| Coverage summary | ReportGenerator додає таблицю покриття в підсумок запуску | — |
| Upload | артефакт `test-results`: TRX-файл і HTML-звіт покриття | — |

Тести використовують SQLite в памʼяті, тому PostgreSQL у CI не потрібен.
Тести, що не пройшли, показуються як анотації у PR і в підсумку запуску.

### Code quality

| Крок | Що робить | Коли падає |
|---|---|---|
| Formatting | `dotnet format whitespace --verify-no-changes` | код не відповідає `.editorconfig` |
| Vulnerable packages | `dotnet package list --vulnerable --include-transitive` | є пакет з відомою вразливістю |

Окрім цього, Dependabot ([`.github/dependabot.yml`](../.github/dependabot.yml))
щотижня відкриває PR з оновленнями NuGet-пакетів і GitHub Actions; кожен
такий PR теж проходить CI.

## Як повторити перевірки локально

```powershell
dotnet build unilib.slnx -c Release -warnaserror
dotnet test unilib.slnx -c Release --collect "XPlat Code Coverage"
dotnet format whitespace unilib.slnx --verify-no-changes
dotnet package list --project unilib.slnx --vulnerable --include-transitive
```

Щоб автоматично виправити форматування: `dotnet format whitespace unilib.slnx`.

## Де дивитися результат

- статус кожного коміту і PR — галочка або хрестик біля нього на GitHub;
- деталі — вкладка **Actions** → потрібний запуск → **Summary**
  (результати тестів і покриття) та **Artifacts** (`test-results`).
