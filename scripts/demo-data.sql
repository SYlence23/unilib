-- Demo data for UniLib: libraries, a faculty, categories, authors and books.
--
-- Run it after the first start of UniLib (the app creates the tables), for example:
--   psql -h localhost -U postgres -d unilib -f scripts/demo-data.sql
-- or paste it into the pgAdmin Query Tool.
--
-- Running it again adds nothing twice. It creates no user accounts: register them in the app,
-- because passwords are hashed by the application.
-- Library coordinates are approximate and only place the pins on the campus map.

BEGIN;

INSERT INTO faculty (name)
VALUES ('Faculty of Applied Mathematics and Informatics')
ON CONFLICT (name) DO NOTHING;

INSERT INTO library (name, latitude, longitude, phone)
SELECT v.name, v.latitude, v.longitude, v.phone
FROM (VALUES
    ('Main Building Library',     49.840583, 24.022531, '+380 32 239 4111'),
    ('Scientific Library',        49.835722, 24.026389, '+380 32 239 4222'),
    ('Applied Mathematics Library', 49.840100, 24.023200, '+380 32 239 4333')
) AS v(name, latitude, longitude, phone)
WHERE NOT EXISTS (SELECT 1 FROM library l WHERE l.name = v.name);

INSERT INTO category (name)
VALUES ('Programming'), ('Algorithms'), ('Databases'), ('Networks'),
       ('Mathematics'), ('Artificial Intelligence'), ('Operating Systems')
ON CONFLICT (name) DO NOTHING;

INSERT INTO author (first_name, last_name)
SELECT v.first_name, v.last_name
FROM (VALUES
    ('Robert C.', 'Martin'), ('Joshua', 'Bloch'),
    ('Thomas H.', 'Cormen'), ('Charles E.', 'Leiserson'), ('Ronald L.', 'Rivest'), ('Clifford', 'Stein'),
    ('Erich', 'Gamma'), ('Richard', 'Helm'), ('Ralph', 'Johnson'), ('John', 'Vlissides'),
    ('David', 'Thomas'), ('Andrew', 'Hunt'),
    ('Abraham', 'Silberschatz'), ('Henry F.', 'Korth'), ('S.', 'Sudarshan'),
    ('Andrew S.', 'Tanenbaum'), ('David J.', 'Wetherall'),
    ('David C.', 'Lay'),
    ('Ronald L.', 'Graham'), ('Donald E.', 'Knuth'), ('Oren', 'Patashnik'),
    ('Stuart', 'Russell'), ('Peter', 'Norvig'),
    ('Remzi H.', 'Arpaci-Dusseau'), ('Andrea C.', 'Arpaci-Dusseau')
) AS v(first_name, last_name)
WHERE NOT EXISTS (
    SELECT 1 FROM author a WHERE a.first_name = v.first_name AND a.last_name = v.last_name);

INSERT INTO book (isbn, title, category_id, library_id, faculty_id, publication_year, pages, language, amount, description)
SELECT v.isbn, v.title, c.id, l.id, f.id, v.year, v.pages, 'English', v.amount, v.description
FROM (VALUES
    ('9780132350884', 'Clean Code', 'Programming', 'Applied Mathematics Library', 2008, 464, 4,
     'A handbook of agile software craftsmanship.'),
    ('9780134685991', 'Effective Java', 'Programming', 'Applied Mathematics Library', 2018, 412, 3,
     'Best practices for the Java platform, third edition.'),
    ('9780201633610', 'Design Patterns', 'Programming', 'Main Building Library', 1994, 395, 2,
     'Elements of reusable object-oriented software.'),
    ('9780135957059', 'The Pragmatic Programmer', 'Programming', 'Main Building Library', 2019, 352, 2,
     '20th anniversary edition.'),
    ('9780262033848', 'Introduction to Algorithms', 'Algorithms', 'Scientific Library', 2009, 1292, 5,
     'Third edition.'),
    ('9780078022159', 'Database System Concepts', 'Databases', 'Scientific Library', 2019, 1376, 3,
     'Seventh edition.'),
    ('9780132126953', 'Computer Networks', 'Networks', 'Scientific Library', 2010, 960, 2,
     'Fifth edition.'),
    ('9780321982384', 'Linear Algebra and Its Applications', 'Mathematics', 'Main Building Library', 2015, 576, 6,
     'Fifth edition.'),
    ('9780201558029', 'Concrete Mathematics', 'Mathematics', 'Applied Mathematics Library', 1994, 657, 1,
     'A foundation for computer science, second edition.'),
    ('9780134610993', 'Artificial Intelligence: A Modern Approach', 'Artificial Intelligence', 'Scientific Library', 2020, 1136, 2,
     'Fourth edition.'),
    ('9781985086593', 'Operating Systems: Three Easy Pieces', 'Operating Systems', 'Applied Mathematics Library', 2018, 714, 3,
     'Virtualization, concurrency and persistence.')
) AS v(isbn, title, category, library, year, pages, amount, description)
JOIN category c ON c.name = v.category
JOIN library l ON l.name = v.library
CROSS JOIN (SELECT id FROM faculty WHERE name = 'Faculty of Applied Mathematics and Informatics') f
ON CONFLICT (isbn) DO NOTHING;

INSERT INTO book_author (book_id, author_id)
SELECT v.isbn, a.id
FROM (VALUES
    ('9780132350884', 'Robert C.', 'Martin'),
    ('9780134685991', 'Joshua', 'Bloch'),
    ('9780201633610', 'Erich', 'Gamma'), ('9780201633610', 'Richard', 'Helm'),
    ('9780201633610', 'Ralph', 'Johnson'), ('9780201633610', 'John', 'Vlissides'),
    ('9780135957059', 'David', 'Thomas'), ('9780135957059', 'Andrew', 'Hunt'),
    ('9780262033848', 'Thomas H.', 'Cormen'), ('9780262033848', 'Charles E.', 'Leiserson'),
    ('9780262033848', 'Ronald L.', 'Rivest'), ('9780262033848', 'Clifford', 'Stein'),
    ('9780078022159', 'Abraham', 'Silberschatz'), ('9780078022159', 'Henry F.', 'Korth'),
    ('9780078022159', 'S.', 'Sudarshan'),
    ('9780132126953', 'Andrew S.', 'Tanenbaum'), ('9780132126953', 'David J.', 'Wetherall'),
    ('9780321982384', 'David C.', 'Lay'),
    ('9780201558029', 'Ronald L.', 'Graham'), ('9780201558029', 'Donald E.', 'Knuth'),
    ('9780201558029', 'Oren', 'Patashnik'),
    ('9780134610993', 'Stuart', 'Russell'), ('9780134610993', 'Peter', 'Norvig'),
    ('9781985086593', 'Remzi H.', 'Arpaci-Dusseau'), ('9781985086593', 'Andrea C.', 'Arpaci-Dusseau')
) AS v(isbn, first_name, last_name)
JOIN author a ON a.first_name = v.first_name AND a.last_name = v.last_name
JOIN book b ON b.isbn = v.isbn
ON CONFLICT DO NOTHING;

COMMIT;
