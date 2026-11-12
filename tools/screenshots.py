# -*- coding: utf-8 -*-
"""
Автоматический прогон ручных проверок через браузер и снятие скриншотов.
Запускается на сервере GitHub (workflow screenshots.yml) при работающем приложении.

  python tools/screenshots.py before   — основной сценарий до перезапуска
  python tools/screenshots.py after    — проверка после перезапуска приложения
  python tools/screenshots.py tests test-output.txt — картинка с выводом dotnet test

Фактические результаты (что реально появилось на странице или пришло от API)
пишутся в docs/screenshots/results.json — их читают со страницы, а не придумывают.
"""
import html
import json
import sys
from datetime import date, timedelta
from pathlib import Path

from playwright.sync_api import sync_playwright

BASE = "http://localhost:5070"
OUT = Path("docs/screenshots")
OUT.mkdir(parents=True, exist_ok=True)
RESULTS = OUT / "results.json"
TODAY = date.today()


def d(days_ago=0):
    return (TODAY - timedelta(days=days_ago)).isoformat()


def load_results():
    return json.loads(RESULTS.read_text(encoding="utf-8")) if RESULTS.exists() else {}


def save_results(results):
    RESULTS.write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8")


def alerts(page):
    """Тексты всех сообщений на странице (успех, ошибка)."""
    texts = page.locator(".alert").all_inner_texts()
    return " | ".join(t.strip().replace("\n", " ") for t in texts if t.strip())


def shot(page, name):
    page.screenshot(path=str(OUT / f"{name}.png"), full_page=True)


def ids(page):
    """Id оборудования и сотрудников по инвентарному и табельному номеру (из API)."""
    eq = {e["inventoryNumber"]: e["id"] for e in page.request.get(BASE + "/api/equipment?ShowWrittenOff=true").json()}
    emp = {e["personnelNumber"]: e["id"] for e in page.request.get(BASE + "/api/employees").json()}
    return eq, emp


def api(page, method, url, data=None):
    resp = page.request.post(BASE + url, data=data) if method == "POST" else page.request.get(BASE + url)
    text = resp.text()
    try:
        body = resp.json()
        text = body.get("error") or body.get("message") or json.dumps(body, ensure_ascii=False)
    except Exception:
        pass
    return f"HTTP {resp.status}: {text[:300]}"


def where(page):
    """Строка «Где находится» из карточки оборудования."""
    return page.locator("dl").first.inner_text().split("Где находится")[-1].strip().replace("\n", " ")


def before(page, r):
    eq, emp = ids(page)

    # 1. Главная — сводка
    page.goto(BASE)
    stats = page.locator(".card .stat").all_inner_texts()
    r["home"] = f"В учёте: {stats[0]}, на складе: {stats[1]}, выдано: {stats[2]}, требует ремонта: {stats[3]}"
    shot(page, "01_home")

    # 2. Справочник оборудования
    page.goto(BASE + "/Equipment")
    r["equipment_list"] = f"Строк в справочнике: {page.locator('tbody tr').count()}"
    shot(page, "02_equipment")

    # 3. Поиск
    page.goto(BASE + "/Equipment?Search=ноутбук")
    r["search"] = "Найдено: " + "; ".join(page.locator("tbody tr td:nth-child(2)").all_inner_texts())
    shot(page, "03_search")

    # 4. Фильтр «у сотрудников» и «требует ремонта»
    page.goto(BASE + "/Equipment?Location=issued")
    r["filter_issued"] = f"У сотрудников: {page.locator('tbody tr').count()} ед."
    page.goto(BASE + "/Equipment?Condition=NeedsRepair")
    r["filter_repair"] = "Требует ремонта: " + "; ".join(page.locator("tbody tr td:nth-child(2)").all_inner_texts())

    # 5. Карточка OB-0001
    page.goto(f"{BASE}/Equipment/Details/{eq['OB-0001']}")
    r["card"] = f"OB-0001: {where(page)}; строк истории: {page.locator('tbody tr').count()}"
    shot(page, "04_equipment_card")

    # 6. Выдача — успех: OB-0003 → Попова М. В.
    page.goto(f"{BASE}/Operations/Issue?equipmentId={eq['OB-0003']}")
    page.select_option("#EmployeeId", str(emp["T-1004"]))
    page.fill("#Comment", "Для проведения вебинаров")
    shot(page, "05_issue_form")
    page.click("button[type=submit]")
    r["issue_ok"] = f"{alerts(page)}; где находится: {where(page)}"
    shot(page, "06_issue_done")

    # 7. Главное правило: выдача уже выданного OB-0001 (подмена формы — сервер проверяет сам)
    page.goto(BASE + "/Operations/Issue")
    options = page.locator("#EquipmentId option").all_inner_texts()
    r["issue_list"] = "OB-0001 в списке выдачи: " + ("есть" if any("OB-0001" in o for o in options) else "нет") + \
                      "; OB-0301 (требует ремонта): " + ("есть" if any("OB-0301" in o for o in options) else "нет")
    page.evaluate(f"""() => {{
        const s = document.querySelector('#EquipmentId');
        const o = document.createElement('option'); o.value = '{eq['OB-0001']}'; o.text = 'OB-0001 (подмена)';
        s.appendChild(o); s.value = '{eq['OB-0001']}'; }}""")
    page.select_option("#EmployeeId", str(emp["T-1001"]))
    page.click("button[type=submit]")
    r["issue_busy"] = alerts(page)
    shot(page, "07_issue_rejected")

    # 8. Выдача через API: уже выдано, требует ремонта, уволенному, без сотрудника, дата в будущем
    r["api_issue_busy"] = api(page, "POST", "/api/operations/issue",
                              {"equipmentId": eq["OB-0001"], "employeeId": emp["T-1001"], "date": d()})
    r["api_issue_repair"] = api(page, "POST", "/api/operations/issue",
                                {"equipmentId": eq["OB-0301"], "employeeId": emp["T-1001"], "date": d()})
    r["api_issue_fired"] = api(page, "POST", "/api/operations/issue",
                               {"equipmentId": eq["OB-0102"], "employeeId": emp["T-1008"], "date": d()})
    r["api_issue_written_off"] = api(page, "POST", "/api/operations/issue",
                                     {"equipmentId": eq["OB-0501"], "employeeId": emp["T-1001"], "date": d()})

    # 9. Выдача без сотрудника (форма)
    page.goto(f"{BASE}/Operations/Issue?equipmentId={eq['OB-0102']}")
    page.click("button[type=submit]")
    r["issue_no_employee"] = alerts(page)

    # 10. Дата в будущем (форма)
    page.goto(f"{BASE}/Operations/Issue?equipmentId={eq['OB-0102']}")
    page.select_option("#EmployeeId", str(emp["T-1001"]))
    page.fill("#Date", d(-1))
    page.click("button[type=submit]")
    r["issue_future"] = alerts(page)

    # 11. Возврат «Требует ремонта» без комментария — отказ; с комментарием — успех
    page.goto(f"{BASE}/Operations/Return?equipmentId={eq['OB-0002']}")
    page.select_option("#Condition", "2")
    page.click("button[type=submit]")
    r["return_no_comment"] = alerts(page)
    shot(page, "08_return_rejected")
    page.fill("#Comment", "Не работают клавиши F1–F4")
    page.click("button[type=submit]")
    r["return_repair"] = f"{alerts(page)}; где находится: {where(page)}"
    shot(page, "09_return_done")

    # 12. Повторная выдача неисправного OB-0002 — отказ
    r["api_issue_after_repair_return"] = api(page, "POST", "/api/operations/issue",
                                             {"equipmentId": eq["OB-0002"], "employeeId": emp["T-1007"], "date": d()})

    # 13. Возврат того, что на складе; возврат раньше даты выдачи
    r["api_return_not_issued"] = api(page, "POST", "/api/operations/return",
                                     {"equipmentId": eq["OB-0102"], "date": d(), "condition": "Good"})
    r["api_return_before_issue"] = api(page, "POST", "/api/operations/return",
                                       {"equipmentId": eq["OB-0401"], "date": d(10), "condition": "Good"})

    # 14. Ремонт: смена состояния OB-0002 на «Исправно», затем выдача Морозову
    page.goto(f"{BASE}/Equipment/Details/{eq['OB-0002']}")
    page.select_option("form[action*='ChangeCondition'] select", "Good")
    page.fill("form[action*='ChangeCondition'] input[name=Comment]", "Заменена клавиатура")
    page.click("form[action*='ChangeCondition'] button")
    r["condition_change"] = alerts(page)
    r["api_issue_after_fix"] = api(page, "POST", "/api/operations/issue",
                                   {"equipmentId": eq["OB-0002"], "employeeId": emp["T-1007"], "date": d()})

    # 15. Списание: выданного — отказ, со склада — успех
    page.goto(f"{BASE}/Equipment/Details/{eq['OB-0001']}")
    r["writeoff_issued_form"] = "Форма списания у выданного: " + ("есть" if page.locator("form[action*='WriteOff']").count() else "нет")
    page.goto(f"{BASE}/Equipment/Details/{eq['OB-0402']}")
    page.fill("form[action*='WriteOff'] input[name=Reason]", "Сломан переключатель режимов")
    page.click("form[action*='WriteOff'] button")
    r["writeoff_ok"] = f"{alerts(page)}; где находится: {where(page)}"
    shot(page, "10_writeoff")

    # 16. Увольнение сотрудника с оборудованием на руках — отказ
    page.goto(f"{BASE}/Employees/Details/{emp['T-1003']}")
    page.click("text=Отметить увольнение")
    r["fire_with_equipment"] = alerts(page)
    shot(page, "11_employee_card")

    # 17. Оборудование у сотрудников
    page.goto(BASE + "/Operations/OnHand")
    r["onhand"] = f"Строк: {page.locator('tbody tr').count()}"
    shot(page, "12_onhand")
    page.goto(BASE + "/Operations/OnHand?Department=IT-отдел")
    r["onhand_it"] = "IT-отдел: " + "; ".join(page.locator("tbody tr td:nth-child(3)").all_inner_texts())

    # 18. История с фильтрами
    page.goto(f"{BASE}/Operations?EquipmentId={eq['OB-0002']}")
    r["history_equipment"] = page.locator("p.text-muted").last.inner_text().strip()
    shot(page, "13_history")
    page.goto(BASE + "/Operations?Type=Return")
    r["history_returns"] = page.locator("p.text-muted").last.inner_text().strip()
    page.goto(f"{BASE}/Operations?From={d(14)}&To={d()}")
    r["history_period"] = page.locator("p.text-muted").last.inner_text().strip()
    page.goto(f"{BASE}/Operations?From={d()}&To={d(30)}")
    r["history_bad_period"] = alerts(page)

    # 19. Добавление оборудования, повтор номера, пустое наименование, изменение
    page.goto(BASE + "/Equipment/Create")
    page.fill("#InventoryNumber", "OB-0601")
    page.fill("#Name", "Планшет Huawei MatePad 11")
    page.fill("#Category", "Планшеты")
    page.click("button[type=submit]")
    r["create_ok"] = alerts(page)
    page.click("text=Изменить")
    page.fill("#SerialNumber", "HW-77")
    page.click("button[type=submit]")
    r["edit_ok"] = alerts(page)
    page.goto(BASE + "/Equipment/Create")
    page.fill("#InventoryNumber", "ob-0001")
    page.fill("#Name", "Дубль")
    page.fill("#Category", "Ноутбуки")
    page.click("button[type=submit]")
    r["create_duplicate"] = alerts(page)
    shot(page, "14_create_duplicate")
    page.goto(BASE + "/Equipment/Create")
    page.fill("#InventoryNumber", "OB-0602")
    page.fill("#Category", "Планшеты")
    page.click("button[type=submit]")
    r["create_empty_name"] = alerts(page)

    # 20. Сотрудник: добавление
    page.goto(BASE + "/Employees/Create")
    page.fill("#FullName", "Лебедева Ксения Андреевна")
    page.fill("#PersonnelNumber", "T-1009")
    page.fill("#Department", "Учебная часть")
    page.click("button[type=submit]")
    r["employee_create"] = alerts(page)
    page.goto(BASE + "/Employees")
    shot(page, "15_employees")

    # 21. Неизвестная запись
    resp = page.goto(BASE + "/Equipment/Details/999")
    r["not_found_page"] = f"HTTP {resp.status}: " + page.locator("h4").inner_text().strip()
    shot(page, "16_not_found")
    r["not_found_api"] = api(page, "GET", "/api/equipment/999")

    # 22. API: список у сотрудников (JSON) и неверные данные в запросе
    page.goto(BASE + "/api/onhand")
    r["api_onhand"] = page.locator("body").inner_text().strip()[:220]
    shot(page, "17_api_onhand")
    resp = page.request.post(BASE + "/api/operations/issue",
                             data='{"equipmentId": 1, "employeeId": 1, "date": "вчера"}',
                             headers={"Content-Type": "application/json"})
    r["api_bad_json"] = f"HTTP {resp.status}: {resp.text()[:300]}"
    resp = page.request.post(BASE + "/api/operations/issue", data="", headers={"Content-Type": "application/json"})
    r["api_empty_body"] = f"HTTP {resp.status}: {resp.text()[:300]}"

    # 23. Подмена значения в форме: нечисловой Id оборудования
    page.goto(BASE + "/Operations/Issue")
    page.evaluate("""() => {
        const s = document.querySelector('#EquipmentId');
        const o = document.createElement('option'); o.value = 'abc'; o.text = 'abc'; s.appendChild(o); s.value = 'abc'; }""")
    page.select_option("#EmployeeId", str(emp["T-1001"]))
    page.click("button[type=submit]")
    r["form_bad_value"] = alerts(page)
    shot(page, "20_form_bad_value")


def after(page, r):
    eq, emp = ids(page)
    page.goto(BASE + "/Equipment?Search=OB-0601")
    found = page.locator("tbody tr td:nth-child(2)").all_inner_texts()
    page.goto(f"{BASE}/Equipment/Details/{eq['OB-0003']}")
    r["after_restart"] = (f"OB-0601 найден: {'да' if found else 'нет'} ({'; '.join(found)}); "
                          f"OB-0003 после перезапуска: {where(page)}")
    shot(page, "18_after_restart")


def tests_image(page, text_file):
    text = Path(text_file).read_text(encoding="utf-8", errors="replace")
    lines = [l for l in text.splitlines() if l.strip()][-70:]
    body = html.escape("\n".join(lines))
    page.set_content(
        "<html><body style='margin:0;background:#0c0c0c'>"
        "<pre style='font:13px Consolas,monospace;color:#d0d0d0;padding:16px;white-space:pre-wrap'>"
        f"runner@github-actions:~/KP-Nek$ dotnet test\n{body}</pre></body></html>")
    shot(page, "19_dotnet_test")


def main():
    mode = sys.argv[1]
    r = load_results()
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", args=["--lang=ru-RU"])
        page = browser.new_page(viewport={"width": 1280, "height": 800}, locale="ru-RU")
        if mode == "before":
            before(page, r)
        elif mode == "after":
            after(page, r)
        elif mode == "tests":
            tests_image(page, sys.argv[2])
        browser.close()
    save_results(r)


if __name__ == "__main__":
    main()
