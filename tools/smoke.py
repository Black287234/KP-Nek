"""Проверка страниц: код ответа и наличие ожидаемого текста в разметке, сформированной на сервере.

Использование: python3 tools/smoke.py http://localhost:5097
"""
import sys
import urllib.error
import urllib.parse
import urllib.request

PAGES = [
    ("/", 200, "Учёт выдачи оборудования"),
    ("/equipment", 200, "ИН-000101"),
    ("/equipment/1", 200, "Инвентарный номер ИН-000101"),
    ("/equipment/999", 200, "Запись не найдена"),
    ("/equipment/new", 200, "Новое оборудование"),
    ("/equipment/2/edit", 200, "Изменение оборудования"),
    ("/employees", 200, "Крючков Виталий Владимирович"),
    ("/employees/1", 200, "Табельный номер"),
    ("/employees/999", 200, "Запись не найдена"),
    ("/employees/new", 200, "Новый сотрудник"),
    ("/no-such-page", 404, "Страница не найдена"),
]


def fetch(url):
    try:
        with urllib.request.urlopen(url, timeout=20) as r:
            return r.status, r.read().decode("utf-8")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", "replace")


def main():
    base = sys.argv[1].rstrip("/")
    bad = 0
    for path, code, text in PAGES:
        status, body = fetch(base + urllib.parse.quote(path, safe="/?=&"))
        ok = status == code and text in body
        bad += not ok
        print(f"{'да ' if ok else 'НЕТ'} {status} (ожидался {code}) «{text}» {'найден' if text in body else 'НЕ найден'}  {path}")
    print(f"Совпало: {len(PAGES) - bad} из {len(PAGES)}")
    sys.exit(1 if bad else 0)


if __name__ == "__main__":
    main()
