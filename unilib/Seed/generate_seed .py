import argparse
import base64
import hashlib
import random
import struct

try:
    from faker import Faker
except ImportError:
    Faker = None

FACULTIES = [
    "Факультет прикладної математики та інформатики",
    "Механіко-математичний факультет",
    "Фізичний факультет",
    "Хімічний факультет",
    "Біологічний факультет",
    "Географічний факультет",
    "Геологічний факультет",
    "Економічний факультет",
    "Юридичний факультет",
    "Філологічний факультет",
    "Історичний факультет",
    "Філософський факультет",
    "Факультет іноземних мов",
    "Факультет журналістики",
    "Факультет міжнародних відносин",
    "Факультет електроніки та комп'ютерних технологій",
    "Факультет культури і мистецтв",
    "Факультет педагогічної освіти",
    "Факультет управління фінансами та бізнесу",
]

CATEGORIES = {
    "Програмування": (
        ["Факультет прикладної математики та інформатики",
         "Факультет електроніки та комп'ютерних технологій"],
        ["програмування мовою Python", "програмування мовою C#", "алгоритми та структури даних",
         "бази даних", "об'єктно-орієнтоване програмування", "веб-технології",
         "операційні системи", "комп'ютерні мережі", "машинне навчання",
         "тестування програмного забезпечення", "основи кібербезпеки", "функціональне програмування"]),
    "Математика": (
        ["Механіко-математичний факультет"],
        ["математичний аналіз", "лінійна алгебра", "дискретна математика", "теорія ймовірностей",
         "диференціальні рівняння", "математична статистика", "теорія чисел", "топологія",
         "числові методи"]),
    "Фізика": (
        ["Фізичний факультет"],
        ["загальна фізика", "квантова механіка", "термодинаміка", "оптика", "електродинаміка",
         "фізика твердого тіла", "астрофізика", "ядерна фізика"]),
    "Хімія": (
        ["Хімічний факультет"],
        ["органічна хімія", "неорганічна хімія", "аналітична хімія", "фізична хімія",
         "біохімія", "кристалохімія", "хімія полімерів"]),
    "Біологія": (
        ["Біологічний факультет"],
        ["загальна біологія", "ботаніка", "зоологія", "генетика", "екологія", "мікробіологія",
         "фізіологія людини"]),
    "Географія": (
        ["Географічний факультет"],
        ["фізична географія", "економічна географія", "картографія", "геоінформаційні системи",
         "ландшафтознавство", "туризмознавство"]),
    "Геологія": (
        ["Геологічний факультет"],
        ["загальна геологія", "мінералогія", "петрографія", "гідрогеологія", "палеонтологія"]),
    "Економіка": (
        ["Економічний факультет", "Факультет управління фінансами та бізнесу"],
        ["мікроекономіка", "макроекономіка", "бухгалтерський облік", "фінансовий менеджмент",
         "маркетинг", "економічна статистика", "міжнародна економіка", "банківська справа"]),
    "Право": (
        ["Юридичний факультет"],
        ["теорія держави і права", "цивільне право", "кримінальне право", "конституційне право",
         "міжнародне право", "адміністративне право", "трудове право"]),
    "Філологія": (
        ["Філологічний факультет"],
        ["українська мова", "літературознавство", "загальне мовознавство", "стилістика",
         "історія української літератури", "фонетика", "теорія перекладу"]),
    "Історія": (
        ["Історичний факультет"],
        ["історія України", "всесвітня історія", "археологія", "джерелознавство",
         "історіографія", "етнологія"]),
    "Філософія": (
        ["Філософський факультет"],
        ["історія філософії", "логіка", "етика", "естетика", "соціальна філософія", "політологія"]),
    "Іноземні мови": (
        ["Факультет іноземних мов"],
        ["англійська мова", "німецька мова", "польська мова", "французька мова",
         "практична граматика", "ділове спілкування"]),
    "Журналістика": (
        ["Факультет журналістики"],
        ["теорія журналістики", "медіаправо", "редагування", "інтернет-журналістика",
         "реклама і зв'язки з громадськістю", "тележурналістика"]),
    "Міжнародні відносини": (
        ["Факультет міжнародних відносин"],
        ["теорія міжнародних відносин", "дипломатія", "міжнародна безпека",
         "європейська інтеграція", "країнознавство"]),
    "Мистецтвознавство": (
        ["Факультет культури і мистецтв"],
        ["історія мистецтва", "музеєзнавство", "теорія культури", "хореографія",
         "музична педагогіка"]),
    "Педагогіка": (
        ["Факультет педагогічної освіти"],
        ["загальна педагогіка", "психологія розвитку", "методика викладання",
         "інклюзивна освіта", "дидактика"]),
    "Художня література": (
        [],
        []),
}

FICTION_TITLES = [
    "Місто під дощем", "Лист із Карпат", "Остання електричка", "Вечір у Львові",
    "Тиха вода", "Соняшники над Дністром", "Дорога до Черемошу", "Літо без вітру",
    "Хата край села", "Синій зошит", "Зимова казка", "Мости й вулиці",
    "Під зорями Бескидів", "Дім на розі", "Книга спогадів", "Тінь від каштана",
    "Шлях через перевал", "Старий годинник", "Осінній триптих", "Мандрівка на північ",
    "Слово і мовчання", "Весна на Підзамчі", "Гірська стежка", "Кава о п'ятій",
    "Темні вікна", "Гості з минулого", "Острів посеред річки", "Подорож з відпусткою",
    "Дзвони в тумані", "Берег дитинства",
]

TITLE_PATTERNS = [
    "{T}: підручник",
    "{T}: курс лекцій",
    "{T}: практикум",
    "{T}: збірник задач",
    "{T}. Навчальний посібник",
    "{T}: теорія і практика",
    "Вступ до курсу «{t}»",
    "{T} для студентів",
]

DESCRIPTIONS = [
    "Навчальне видання для студентів. Розглядаються основні поняття, методи та приклади з теми «{t}». Містить контрольні питання та завдання для самостійної роботи.",
    "Посібник охоплює ключові розділи курсу «{t}». Матеріал супроводжується прикладами, схемами та переліком рекомендованої літератури.",
    "Видання підготовлене на основі лекційного курсу «{t}». Підходить для підготовки до семінарів, заліків та іспитів.",
    "Практичний довідник із теми «{t}»: стислий виклад теорії, типові задачі та розв'язання, тести для самоперевірки.",
]
FICTION_DESCRIPTIONS = [
    "Збірка оповідань і новел про життя міста, дорогу та пам'ять. Підходить для позакласного читання.",
    "Повість про звичайних людей у незвичайних обставинах, написана простою й образною мовою.",
    "Проза, у якій реальні краєвиди переплітаються зі спогадами героїв. Читається за кілька вечорів.",
]

LANG_BY_TOPIC = {
    "англійська мова": "Англійська", "німецька мова": "Німецька",
    "польська мова": "Польська", "французька мова": "Французька",
}

MALE = ["Андрій", "Олександр", "Богдан", "Василь", "Володимир", "Дмитро", "Ігор", "Іван",
        "Левко", "Максим", "Марко", "Микола", "Михайло", "Назар", "Олег", "Остап", "Павло",
        "Петро", "Роман", "Сергій", "Тарас", "Юрій", "Ярослав", "Святослав", "Орест",
        "Тимофій", "Євген", "Артем", "Денис", "Віталій", "Костянтин", "Любомир", "Мирослав",
        "Ростислав", "Захар"]
FEMALE = ["Анна", "Оксана", "Олена", "Марія", "Наталія", "Ірина", "Софія", "Юлія", "Катерина",
          "Тетяна", "Христина", "Соломія", "Ольга", "Дарина", "Вікторія", "Марта", "Леся",
          "Ярина", "Надія", "Галина", "Людмила", "Уляна", "Віра", "Зоряна", "Мирослава",
          "Яна", "Діана", "Ангеліна", "Богдана", "Інна", "Лілія", "Світлана", "Аліна",
          "Емілія", "Ліна"]
SURNAMES = ["Шевченко", "Коваленко", "Бондаренко", "Мельник", "Ткачук", "Кравчук", "Олійник",
            "Шевчук", "Мороз", "Коваль", "Лисенко", "Петренко", "Гаврилюк", "Гребенюк",
            "Мазур", "Савчук", "Данилюк", "Романюк", "Кушнір", "Білик", "Гончар", "Дзюба",
            "Пархоменко", "Семенюк", "Яремчук", "Вознюк", "Гнатюк", "Стельмах", "Климчук",
            "Левицький", "Грушевський", "Дорошенко", "Сидоренко", "Мартинюк", "Поліщук",
            "Кузьменко", "Паламарчук", "Волошин", "Федорчук", "Лозинський", "Колодій",
            "Юрчишин", "Хомин", "Бойко", "Мисик", "Вовк", "Паньків", "Герасимчук", "Стецюк",
            "Зінкевич", "Рибак", "Чорнобай", "Іваничук", "Ковальчук", "Мирний", "Гайдук",
            "Бабій", "Лесюк", "Максимюк", "Процик", "Рудий", "Пасічник", "Мацюк", "Демчук",
            "Заярний", "Винничук", "Андрусів", "Кобилянський", "Ольховський", "Горбань"]

TRANSLIT = {
    "а": "a", "б": "b", "в": "v", "г": "h", "ґ": "g", "д": "d", "е": "e", "є": "ye",
    "ж": "zh", "з": "z", "и": "y", "і": "i", "ї": "yi", "й": "y", "к": "k", "л": "l",
    "м": "m", "н": "n", "о": "o", "п": "p", "р": "r", "с": "s", "т": "t", "у": "u",
    "ф": "f", "х": "kh", "ц": "ts", "ч": "ch", "ш": "sh", "щ": "shch", "ь": "", "ю": "yu",
    "я": "ya", "'": "", "’": "", "-": "",
}


def translit(s):
    return "".join(TRANSLIT.get(ch, ch) for ch in s.lower())


def feminize(surname):
    for end, rep in (("ський", "ська"), ("цький", "цька"), ("ий", "а"), ("ій", "я")):
        if surname.endswith(end):
            return surname[: -len(end)] + rep
    return surname


class Names:
    def __init__(self, seed):
        self.rng = random.Random(seed)
        self.fake = None
        if Faker is not None:
            self.fake = Faker("uk_UA")
            self.fake.seed_instance(seed)

    def person(self):
        gender = self.rng.choice("mf")
        if self.fake is not None:
            first = self.fake.first_name_male() if gender == "m" else self.fake.first_name_female()
            last = self.fake.last_name()
        else:
            first = self.rng.choice(MALE if gender == "m" else FEMALE)
            last = self.rng.choice(SURNAMES)
        if gender == "f":
            last = feminize(last)
        return first[:50], last[:50], gender


def q(value):
    if value is None:
        return "NULL"
    return "'" + str(value).replace("'", "''") + "'"


def num(value):
    return "NULL" if value is None else str(value)


def isbn13(rng, used):
    while True:
        body = "978966" + "".join(str(rng.randint(0, 9)) for _ in range(6))
        total = sum(int(d) * (1 if i % 2 == 0 else 3) for i, d in enumerate(body))
        check = (10 - total % 10) % 10
        full = body + str(check)
        isbn = f"{full[:3]}-{full[3:6]}-{full[6:11]}-{full[11:12]}-{full[12]}"
        if isbn not in used:
            used.add(isbn)
            return isbn


def identity_v3_hash(password, rng, iterations=100000):
    salt = bytes(rng.getrandbits(8) for _ in range(16))
    subkey = hashlib.pbkdf2_hmac("sha256", password.encode("utf-8"), salt, iterations, 32)
    blob = b"\x01" + struct.pack(">III", 1, iterations, len(salt)) + salt + subkey
    return base64.b64encode(blob).decode("ascii")


def values_block(rows):
    return ",\n".join("  (" + ", ".join(r) + ")" for r in rows)

def generate(args):
    rng = random.Random(args.seed)
    names = Names(args.seed)
    out = []
    emit = out.append

    emit("-- UniLib: тестові дані (згенеровано generate_seed.py, seed=%d)" % args.seed)
    emit("-- Бібліотеки НЕ створюються: використовуються ті, що вже є в таблиці library.")
    emit("-- Запускати один раз на базі, де ще немає цих книг. Усе виконується однією транзакцією.")
    emit("-- Пароль усіх згенерованих користувачів: %s" % args.password)
    emit("")
    emit("SET client_encoding = 'UTF8';")
    emit("BEGIN;")
    emit("")
    emit("DO $$ BEGIN")
    emit("  IF NOT EXISTS (SELECT 1 FROM library) THEN")
    emit("    RAISE EXCEPTION 'Таблиця library порожня: спочатку додайте бібліотеки';")
    emit("  END IF;")
    emit("END $$;")
    emit("")

    emit("-- Факультети й категорії (наявні з такою самою назвою пропускаються)")
    emit("INSERT INTO faculty (name) VALUES\n" + values_block([[q(f)] for f in FACULTIES])
         + "\nON CONFLICT (name) DO NOTHING;")
    emit("")
    emit("INSERT INTO category (name) VALUES\n" + values_block([[q(c)] for c in CATEGORIES])
         + "\nON CONFLICT (name) DO NOTHING;")
    emit("")

    cat_names = list(CATEGORIES)
    authors = []
    seen_names = set()
    while len(authors) < args.authors:
        first, last, gender = names.person()
        if (first, last) in seen_names:
            continue
        seen_names.add((first, last))
        cat = cat_names[len(authors) % len(cat_names)]
        authors.append({"first": first, "last": last, "gender": gender, "cat": cat})

    bio_roles = {
        "Художня література": ("письменник", "письменниця"),
    }
    author_rows = []
    for a in authors:
        male_role, female_role = bio_roles.get(a["cat"], ("викладач", "викладачка"))
        role = male_role if a["gender"] == "m" else female_role
        pubs = rng.randint(5, 80)
        topic = a["cat"].lower()
        bio = (f"{role.capitalize()}, автор понад {pubs} публікацій у сфері «{topic}». "
               "Тестовий запис для демонстрації каталогу.")
        author_rows.append([q(a["first"]), q(a["last"]), q(bio)])
    emit("-- Автори")
    emit("INSERT INTO author (first_name, last_name, biography) VALUES\n" + values_block(author_rows) + ";")
    emit("")

    authors_by_cat = {}
    for a in authors:
        authors_by_cat.setdefault(a["cat"], []).append(a)

    used_isbn = set()
    used_titles = set()
    books = []
    fiction_idx = 0
    for i in range(args.books):
        cat = cat_names[i % len(cat_names)] if i < len(cat_names) * 2 else rng.choice(cat_names)
        faculties, topics = CATEGORIES[cat]
        if cat == "Художня література":
            base = FICTION_TITLES[fiction_idx % len(FICTION_TITLES)]
            fiction_idx += 1
            topic = "художня проза"
            descr = rng.choice(FICTION_DESCRIPTIONS)
            faculty = None
        else:
            topic = rng.choice(topics)
            pattern = rng.choice(TITLE_PATTERNS)
            base = pattern.format(T=topic[0].upper() + topic[1:], t=topic)
            descr = rng.choice(DESCRIPTIONS).format(t=topic)
            faculty = rng.choice(faculties)
        title, edition = base, 1
        while title in used_titles:
            edition += 1
            title = f"{base} (вид. {edition})"
        used_titles.add(title)

        lang = LANG_BY_TOPIC.get(topic, "Українська")
        pool = authors_by_cat.get(cat) or authors
        k = rng.choices([1, 2, 3], weights=[70, 22, 8])[0]
        chosen = rng.sample(pool, min(k, len(pool)))
        books.append({
            "isbn": isbn13(rng, used_isbn),
            "faculty": faculty,
            "lib_n": rng.randint(0, 10_000),
            "cat": cat,
            "title": title[:200],
            "year": rng.randint(1995, 2025),
            "pages": rng.randint(80, 900),
            "lang": lang,
            "descr": descr,
            "amount": rng.randint(2, 10),
            "authors": chosen,
        })

    book_rows = [[q(b["isbn"]), q(b["faculty"]), str(b["lib_n"]), q(b["cat"]), q(b["title"]),
                  str(b["year"]), str(b["pages"]), q(b["lang"]), q(b["descr"]), str(b["amount"])]
                 for b in books]
    emit("-- Книги. library_id розподіляється рівномірно між бібліотеками, що вже є в базі.")
    emit("INSERT INTO book (isbn, faculty_id, library_id, category_id, title, publication_year, pages, language, description, amount)")
    emit("SELECT v.isbn, f.id, l.id, c.id, v.title, v.year, v.pages, v.lang, v.descr, v.amount")
    emit("FROM (VALUES\n" + values_block(book_rows))
    emit(") AS v(isbn, faculty, lib_n, category, title, year, pages, lang, descr, amount)")
    emit("JOIN category c ON c.name = v.category")
    emit("LEFT JOIN faculty f ON f.name = v.faculty")
    emit("JOIN (SELECT id, row_number() OVER (ORDER BY id) - 1 AS rn, count(*) OVER () AS cnt FROM library) l")
    emit("  ON l.rn = v.lib_n % l.cnt;")
    emit("")

    ba_rows = [[q(b["isbn"]), q(a["first"]), q(a["last"])] for b in books for a in b["authors"]]
    emit("-- Зв'язки книга <-> автор")
    emit("INSERT INTO book_author (book_id, author_id)")
    emit("SELECT v.isbn, a.id FROM (VALUES\n" + values_block(ba_rows))
    emit(") AS v(isbn, fn, ln)")
    emit("JOIN author a ON a.first_name = v.fn AND a.last_name = v.ln;")
    emit("")

    pw_hash = identity_v3_hash(args.password, rng)
    students = []
    used_emails = set()
    for i in range(1, args.students + 1):
        first, last, _ = names.person()
        email = f"{translit(first)}.{translit(last)}{i}@unilib.test"
        used_emails.add(email)
        students.append({
            "first": first, "last": last, "email": email,
            "sid": f"LNU{rng.choice([21, 22, 23, 24, 25])}{i:04d}",
            "phone": "+380" + rng.choice(["67", "68", "50", "63", "66", "73", "93", "96", "97", "98"])
                     + "".join(str(rng.randint(0, 9)) for _ in range(7)),
            "faculty": rng.choice(FACULTIES),
            "days": rng.randint(5, 700),
        })
    librarians = []
    for i in range(1, args.librarians + 1):
        first, last, _ = names.person()
        librarians.append({
            "first": first, "last": last,
            "email": f"librarian{i}@unilib.test",
            "phone": "+380" + rng.choice(["67", "50", "63", "93"])
                     + "".join(str(rng.randint(0, 9)) for _ in range(7)),
            "lib_n": i * 2,
            "days": rng.randint(100, 900),
        })

    emit("-- Користувачі: студенти (role = student) та бібліотекарі (role = librarian)")
    user_rows = []
    for s in students:
        user_rows.append([
            "(SELECT id FROM role WHERE name = 'student')",
            f"(SELECT id FROM faculty WHERE name = {q(s['faculty'])})",
            "NULL", q(s["first"]), q(s["last"]), q(s["sid"]), q(s["email"]), q(s["phone"]),
            q(pw_hash), f"now() - interval '{s['days']} days'"])
    for l in librarians:
        user_rows.append([
            "(SELECT id FROM role WHERE name = 'librarian')", "NULL",
            f"(SELECT id FROM library ORDER BY id OFFSET ({l['lib_n']} % (SELECT count(*) FROM library)) LIMIT 1)",
            q(l["first"]), q(l["last"]), "NULL", q(l["email"]), q(l["phone"]),
            q(pw_hash), f"now() - interval '{l['days']} days'"])
    emit("INSERT INTO \"user\" (role_id, faculty_id, library_id, first_name, last_name, student_id, email, phone, password_hash, created_at) VALUES\n"
         + values_block(user_rows) + ";")
    emit("")

    unreturned = {b["isbn"]: 0 for b in books}
    weights = [rng.random() ** 2 + 0.05 for _ in books]
    loan_rows, seen_loans = [], set()
    counts = {"returned": 0, "active": 0, "overdue": 0}
    attempts = 0
    while len(loan_rows) < args.loans and attempts < args.loans * 20:
        attempts += 1
        b = rng.choices(books, weights=weights)[0]
        s = rng.choice(students)
        status = rng.choices(["returned", "active", "overdue"], weights=[60, 28, 12])[0]
        if status != "returned":
            if unreturned[b["isbn"]] >= b["amount"]:
                continue
            if any(k[0] == s["email"] and k[1] == b["isbn"] and k[3] != "returned" for k in seen_loans):
                continue
        if status == "returned":
            borrow = rng.randint(15, 240)
            ret = borrow - rng.randint(1, min(21, borrow))
        elif status == "active":
            borrow, ret = rng.randint(0, 13), None
        else:
            borrow, ret = rng.randint(15, 50), None
        key = (s["email"], b["isbn"], borrow, status)
        if key in seen_loans:
            continue
        seen_loans.add(key)
        if status != "returned":
            unreturned[b["isbn"]] += 1
        counts[status] += 1
        loan_rows.append([q(b["isbn"]), q(s["email"]), str(borrow), num(ret), q(status)])
    emit("-- Видачі: borrow/return задані днями тому від поточної дати, due_date = borrow + 14 днів.")
    emit("-- Активні: термін ще не минув; прострочені: термін минув, не повернуті; повернені: є return_date.")
    emit("INSERT INTO user_book (book_isbn, user_id, borrow_date, due_date, return_date, status)")
    emit("SELECT v.isbn, u.id, current_date - v.b, current_date - v.b + 14,")
    emit("       CASE WHEN v.r IS NULL THEN NULL ELSE current_date - v.r END, v.status")
    emit("FROM (VALUES\n" + values_block(loan_rows))
    emit(") AS v(isbn, email, b, r, status)")
    emit("JOIN \"user\" u ON u.email = v.email;")
    emit("")

    res_rows = []
    for _ in range(args.reservations):
        b = rng.choice(books)
        s = rng.choice(students)
        status = rng.choices(["pending", "active", "cancelled", "completed"], weights=[30, 25, 20, 25])[0]
        age = {"pending": (0, 5), "active": (0, 10), "cancelled": (1, 40), "completed": (5, 60)}[status]
        res_rows.append([q(b["isbn"]), q(s["email"]), str(rng.randint(*age)), q(status)])
    emit("-- Бронювання (pending, active, cancelled, completed)")
    emit("INSERT INTO reservation (book_id, user_id, reservation_date, status)")
    emit("SELECT v.isbn, u.id, current_date - v.d, v.status FROM (VALUES\n" + values_block(res_rows))
    emit(") AS v(isbn, email, d, status)")
    emit("JOIN \"user\" u ON u.email = v.email;")
    emit("")

    fav_rows, seen_fav = [], set()
    attempts = 0
    while len(fav_rows) < args.favorites and attempts < args.favorites * 20:
        attempts += 1
        b = rng.choice(books)
        s = rng.choice(students)
        if (s["email"], b["isbn"]) in seen_fav:
            continue
        seen_fav.add((s["email"], b["isbn"]))
        fav_rows.append([q(b["isbn"]), q(s["email"]), str(rng.randint(0, 180))])
    emit("-- Обране")
    emit("INSERT INTO favorite (user_id, book_id, added_at)")
    emit("SELECT u.id, v.isbn, current_date - v.d FROM (VALUES\n" + values_block(fav_rows))
    emit(") AS v(isbn, email, d)")
    emit("JOIN \"user\" u ON u.email = v.email;")
    emit("")
    emit("COMMIT;")

    summary = {
        "faculties": len(FACULTIES), "categories": len(CATEGORIES), "authors": len(authors),
        "books": len(books), "book_author": len(ba_rows), "students": len(students),
        "librarians": len(librarians), "loans": len(loan_rows), "loan_status": counts,
        "reservations": len(res_rows), "favorites": len(fav_rows),
        "names_from": "Faker uk_UA" if names.fake is not None else "вбудовані списки",
    }
    return "\n".join(out) + "\n", summary


def main():
    p = argparse.ArgumentParser(description="Генератор seed.sql для UniLib")
    p.add_argument("--out", default="seed.sql")
    p.add_argument("--seed", type=int, default=42)
    p.add_argument("--authors", type=int, default=90)
    p.add_argument("--books", type=int, default=300)
    p.add_argument("--students", type=int, default=60)
    p.add_argument("--librarians", type=int, default=6)
    p.add_argument("--loans", type=int, default=220)
    p.add_argument("--reservations", type=int, default=50)
    p.add_argument("--favorites", type=int, default=110)
    p.add_argument("--password", default="test1234", help="пароль усіх згенерованих користувачів")
    args = p.parse_args()

    sql, summary = generate(args)
    with open(args.out, "w", encoding="utf-8") as f:
        f.write(sql)
    print(f"Записано {args.out}")
    for k, v in summary.items():
        print(f"  {k}: {v}")


if __name__ == "__main__":
    main()
