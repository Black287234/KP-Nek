# -*- coding: utf-8 -*-
"""
Сборка пояснительной записки «Курсовая работа»: python3 docs/report/build_note.py
Результат: docs/Курсовая_работа.docx и PDF.
Фактические результаты ручных проверок подставляются из docs/screenshots/results.json.
"""
import json
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from gost_docx import build  # noqa: E402

ROOT = HERE.parents[1]

INFO = {
    "kind": "КУРСОВАЯ РАБОТА",
    "topic": "Проектирование и разработка модуля учета выдачи оборудования сотрудникам",
    "pm": "ПО ПМ.01 РАЗРАБОТКА МОДУЛЕЙ ПРОГРАММНОГО ОБЕСПЕЧЕНИЯ ДЛЯ КОМПЬЮТЕРНЫХ СИСТЕМ",
    "mdk": "МДК.01.01 РАЗРАБОТКА ПРОГРАММНЫХ МОДУЛЕЙ",
    "specialty": "Специальность 09.02.07 «Информационные системы и программирование»",
    "group": "ИСиП-23/1",
    "student": "Пономарев Никита Сергеевич",
    "teacher": "Крючков Виталий Владимирович",
    "year": "2026",
}


def fill(text, results):
    for key, value in results.items():
        text = text.replace("{" + key + "}", str(value).replace("|", "/").replace("\n", " "))
    return text


results = json.loads((ROOT / "docs/screenshots/results.json").read_text(encoding="utf-8"))
text = fill((HERE / "note.txt").read_text(encoding="utf-8"), results)
missing = sorted(set(__import__("re").findall(r"\{([a-z_]+)\}", text)) - {"holder"})
if missing:
    sys.exit("Нет фактических результатов для: " + ", ".join(missing))
tmp = HERE / "_note_filled.txt"
tmp.write_text(text, encoding="utf-8")

out = ROOT / "docs" / "Курсовая_работа.docx"
builder, pages = build(HERE / "title_template.docx", INFO, tmp, out)
tmp.unlink()
print("Таблиц:", builder.table_no, "Рисунков:", builder.figure_no)
for level, title in builder.toc_entries:
    print(("   " if level == 2 else "") + title, pages.get(title))
