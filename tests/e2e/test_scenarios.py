"""Сценарии работы пользователя. Выполняются по порядку: каждый следующий учитывает изменения предыдущих.

Каждый сценарий записывает фактический результат в docs/evidence/scenarios.json и проверяет его утверждением assert.
Запуск: pytest tests/e2e -m main, затем после перезапуска приложения: pytest tests/e2e -m restart
"""
import pytest

from conftest import day

pytestmark = pytest.mark.main


def check(journal, code, title, expected, actual, passed, shot=None):
    journal.write(code, title, expected, actual, passed, shot)
    assert passed, f"{code}: {actual}"


def test_b01_home(ui, journal):
    ui.go("/")
    figures = " / ".join(" ".join(t.split()) for t in ui.page.locator(".figures > div").all_inner_texts())
    check(journal, "Б01", "Главная страница", "Четыре показателя и последние операции", figures,
          ui.page.locator(".figures > div").count() == 4, ui.shot("01_home"))


def test_b02_search(ui, journal):
    ui.go("/equipment")
    ui.page.fill(".filters input", "ноутбук")
    ui.page.keyboard.press("Tab")
    ui.page.wait_for_timeout(400)
    rows = ui.page.locator("table.register tbody tr td:nth-child(2)").all_inner_texts()
    check(journal, "Б02", "Поиск оборудования по слову «ноутбук»", "Четыре ноутбука", "Найдено: " + "; ".join(rows),
          len(rows) == 4, ui.shot("02_search"))


def test_b03_add_equipment(ui, journal):
    ui.go("/equipment/new")
    ui.page.fill("#inv", "ин-000105")
    ui.page.fill("#name", "Ноутбук Lenovo IdeaPad 3")
    ui.page.fill("#serial", "pf4abc77")
    ui.submit()
    title = ui.page.locator(".lead").first.inner_text()
    check(journal, "Б03", "Добавление оборудования с номером «ин-000105»", "Добавлено как ИН-000105, открыта карточка",
          f"{ui.notes()}; {title}", "ИН-000105" in title, ui.shot("03_equipment_added"))


def test_b04_duplicate(ui, journal):
    ui.go("/equipment/new")
    ui.page.fill("#inv", "ИН-000105")
    ui.page.fill("#name", "Ноутбук")
    ui.submit()
    msg = ui.notes()
    check(journal, "Б04", "Повтор инвентарного номера ИН-000105", "Отказ: номер уже есть в перечне", msg, "уже есть" in msg)


def test_b05_invalid_form(ui, journal):
    ui.go("/equipment/new")
    ui.page.fill("#inv", "12345")
    ui.submit()
    msg = ui.notes()
    check(journal, "Б05", "Форма оборудования: номер «12345», без наименования", "Сообщения о формате номера и об отсутствии наименования",
          msg, "ИН-000123" in msg and "наименование" in msg, ui.shot("05_validation"))


def test_b06_add_employee(ui, journal):
    ui.go("/employees/new")
    ui.page.fill("#number", "000300")
    ui.page.fill("#last", "пономарев")
    ui.page.fill("#first", "никита")
    ui.page.fill("#middle", "сергеевич")
    ui.page.fill("#position", "Лаборант")
    ui.page.fill("#department", "Отделение информационных технологий")
    ui.submit()
    h1 = ui.page.locator("h1").inner_text()
    check(journal, "Б06", "Добавление сотрудника, ФИО строчными буквами", "Сохранён как «Пономарев Никита Сергеевич»",
          f"{ui.notes()}; {h1}", h1 == "Пономарев Никита Сергеевич", ui.shot("06_employee_added"))


def test_b07_duplicate_employee(ui, journal):
    ui.go("/employees/new")
    for sel, v in [("#number", "000300"), ("#last", "Иванов"), ("#first", "Иван"), ("#position", "Лаборант"), ("#department", "Учебная часть")]:
        ui.page.fill(sel, v)
    ui.submit()
    msg = ui.notes()
    check(journal, "Б07", "Повтор табельного номера 000300", "Отказ: табельный номер уже есть", msg, "уже есть" in msg)


def test_b08_issue(ui, journal):
    ui.go("/issue")
    ui.select("#equipment", "ИН-000105")
    ui.select("#employee", "Пономарев Никита")
    ui.page.fill("#due", day(7).isoformat())
    ui.submit()
    rows = ui.page.locator("table.register").first.inner_text()
    check(journal, "Б08", "Выдача ИН-000105 сотруднику Пономареву Н. С. со сроком возврата 7 дней",
          "Выдача оформлена, оборудование в карточке сотрудника", f"{ui.notes()}; в карточке: {'ИН-000105' in rows}",
          "Выдача оформлена" in ui.notes() and "ИН-000105" in rows, ui.shot("08_issued"))


def test_b09_issue_again(ui, journal):
    eq_id = ui.page.locator("a", has_text="ИН-000105").first.get_attribute("href").split("/")[-1]
    ui.go(f"/issue?equipment={eq_id}")
    options = ui.page.locator("#equipment option").all_inner_texts()
    ui.select("#employee", "Лаптева")
    ui.submit()
    msg = ui.notes()
    check(journal, "Б09", "Повторная выдача ИН-000105 (переход по ссылке с кодом оборудования)",
          "Оборудования нет в списке, сервер отказывает: уже выдано", f"в списке: {any('ИН-000105' in o for o in options)}; {msg}",
          "уже выдано" in msg and not any("ИН-000105" in o for o in options))


def test_b10_issue_broken(ui, journal):
    ui.go("/equipment?x=1")
    ui.page.fill(".filters input", "ИН-000502")
    ui.page.keyboard.press("Tab")
    ui.page.wait_for_timeout(400)
    eq_id = ui.page.locator("table.register a").first.get_attribute("href").split("/")[-1]
    ui.go(f"/issue?equipment={eq_id}")
    ui.select("#employee", "Бойко")
    ui.submit()
    msg = ui.notes()
    check(journal, "Б10", "Выдача неисправной колонки ИН-000502", "Отказ: неисправное оборудование не выдаётся", msg,
          "неисправное оборудование не выдаётся" in msg, ui.shot("10_issue_broken"))


def test_b11_issue_to_dismissed(ui, journal):
    ui.go("/employees")
    ui.page.check(".filters input[type=checkbox]")
    ui.page.wait_for_timeout(400)
    emp = ui.page.locator("table.register a", has_text="Орлов").get_attribute("href").split("/")[-1]
    ui.go(f"/issue?employee={emp}")
    ui.select("#equipment", "ИН-000202")
    ui.submit()
    msg = ui.notes()
    check(journal, "Б11", "Выдача уволенному сотруднику Орлову Г. В.", "Отказ: сотрудник уволен", msg, "уволен" in msg)


def test_b12_issue_future(ui, journal):
    ui.go("/issue")
    ui.select("#equipment", "ИН-000202")
    ui.select("#employee", "Лаптева")
    ui.page.fill("#issued", day(1).isoformat())
    ui.submit()
    msg = ui.notes()
    check(journal, "Б12", "Выдача с завтрашней датой", "Отказ: дата позже сегодняшней", msg, "позже сегодняшней" in msg)


def test_b13_overlap_history(ui, journal):
    # ИН-000301 было у Тихоновой М. П. с −15 по −12 день; выдача датой −14 дней пересекается с этим периодом
    ui.go("/equipment")
    ui.page.fill(".filters input", "ИН-000301")
    ui.page.keyboard.press("Tab")
    ui.page.wait_for_timeout(400)
    eq_id = ui.page.locator("table.register a").first.get_attribute("href").split("/")[-1]
    ui.go("/issue")
    ui.select("#equipment", "ИН-000301")
    ui.select("#employee", "Гарипова")
    ui.page.fill("#issued", day(-14).isoformat())
    ui.submit()
    msg = ui.notes()
    ui.go(f"/equipment/{eq_id}")
    check(journal, "Б13", "Выдача ИН-000301 датой 14 дней назад — в период, когда фотоаппарат был у другого сотрудника",
          "Отказ: в этот период оборудование числилось за другим сотрудником", msg,
          "числилось" in msg, ui.shot("13_overlap"))


def test_b14_holdings(ui, journal):
    ui.go("/holdings")
    groups = ui.page.locator(".holder h2").all_inner_texts()
    has = "Пономарев Никита Сергеевич" in " ".join(groups)
    ui.page.check(".filters input[type=checkbox]")
    ui.page.wait_for_timeout(400)
    overdue = ui.page.locator(".holder td:first-child").all_inner_texts()
    check(journal, "Б14", "Оборудование у сотрудников и отбор просроченных",
          "Пономарев Н. С. в списке; среди просроченных — ИН-000102", f"групп: {len(groups)}, Пономарев: {'да' if has else 'нет'}; просрочено: {', '.join(overdue)}",
          has and "ИН-000102" in overdue, ui.shot("14_holdings_overdue"))


def test_b15_return_before_issue(ui, journal):
    ui.go("/return")
    ui.select("#equipment", "ИН-000105")
    ui.page.fill("#returned", day(-1).isoformat())
    ui.submit()
    msg = ui.notes()
    check(journal, "Б15", "Возврат ИН-000105 вчерашней датой (выдано сегодня)", "Отказ: дата возврата раньше даты выдачи", msg,
          "раньше даты выдачи" in msg)


def test_b16_return_broken_without_note(ui, journal):
    ui.go("/return")
    ui.select("#equipment", "ИН-000105")
    ui.page.select_option("#condition", label="неисправно")
    ui.submit()
    msg = ui.notes()
    check(journal, "Б16", "Возврат в состоянии «неисправно» без описания", "Отказ: опишите неисправность", msg, "неисправность" in msg)


def test_b17_return_needs_repair(ui, journal):
    ui.go("/return")
    ui.select("#equipment", "ИН-000105")
    ui.page.select_option("#condition", label="требует ремонта")
    ui.page.fill("#note", "Не работает клавиша Enter, западает пробел")
    ui.submit()
    facts = " ".join(ui.page.locator(".pairs").inner_text().split())
    check(journal, "Б17", "Возврат ИН-000105 в состоянии «требует ремонта» с описанием",
          "Возврат принят; состояние «требует ремонта», местонахождение «на складе»", f"{ui.notes()}; {facts}",
          "требует ремонта" in facts and "на складе" in facts, ui.shot("17_returned"))


def test_b18_return_keeps_condition(ui, journal):
    # ИН-000103 имеет износ: выдача и возврат без изменения поля «Состояние»
    ui.go("/issue")
    ui.select("#equipment", "ИН-000103")
    ui.select("#employee", "Лаптева")
    ui.submit()
    ui.go("/return")
    ui.select("#equipment", "ИН-000103")
    chosen = ui.page.locator("#condition option:checked").inner_text()
    ui.submit()
    facts = " ".join(ui.page.locator(".pairs").inner_text().split())
    check(journal, "Б18", "Возврат ноутбука ИН-000103 с износом, поле «Состояние» не менялось",
          "По умолчанию предложено состояние при выдаче («исправно, есть износ»), оно и сохранено",
          f"предложено: «{chosen}»; карточка: {facts}", "есть износ" in chosen and "есть износ" in facts, ui.shot("18_return_condition"))


def test_b19_write_off_issued(ui, journal):
    ui.go("/equipment")
    ui.page.locator("table.register a", has_text="ИН-000101").click()
    ui.page.wait_for_selector("#reason")
    ui.page.fill("#reason", "Проверка списания выданного оборудования")
    ui.page.click("button.danger")
    ui.page.wait_for_selector(".notice")
    msg = ui.notes()
    check(journal, "Б19", "Списание ноутбука ИН-000101, который находится у Крючкова В. В.",
          "Отказ: сначала оформите возврат", msg, "сначала оформите возврат" in msg, ui.shot("19_write_off_issued"))


def test_b20_dismiss_holder(ui, journal):
    ui.go("/employees")
    ui.page.locator("table.register a", has_text="Крючков").click()
    ui.page.wait_for_selector("button.danger")
    ui.page.click("button.danger")
    ui.page.wait_for_selector(".notice.bad")
    msg = ui.notes()
    check(journal, "Б20", "Отметка увольнения Крючкова В. В., за которым числится оборудование",
          "Отказ с числом единиц оборудования", msg, "числится оборудование" in msg, ui.shot("20_dismiss_holder"))


def test_b21_history(ui, journal):
    ui.go("/history")
    ui.page.select_option("#employee", label="Пономарев Н. С.")
    ui.page.wait_for_timeout(400)
    ops = ui.page.locator("table.register tbody .op").all_inner_texts()
    check(journal, "Б21", "История операций сотрудника Пономарева Н. С.", "Две операции: возврат (сверху) и выдача",
          "операции: " + ", ".join(ops), ops == ["Возврат", "Выдача"], ui.shot("21_history"))


def test_b22_history_period(ui, journal):
    ui.go("/history")
    ui.page.fill("#from", day(0).isoformat())
    ui.page.fill("#to", day(-5).isoformat())
    ui.page.keyboard.press("Tab")
    ui.page.wait_for_timeout(500)
    msg = ui.notes()
    check(journal, "Б22", "История с началом периода позже окончания", "Сообщение «Начало периода позже его окончания»",
          msg, "позже его окончания" in msg)


def test_b23_not_found(ui, journal):
    ui.go("/equipment/9999")
    h1 = ui.page.locator("h1").inner_text()
    ui.page.goto(ui.page.url.split("/equipment")[0] + "/no-such-page")
    h1b = ui.page.locator("h1").inner_text()
    check(journal, "Б23", "Несуществующие запись и страница", "«Запись не найдена» и «Страница не найдена»", f"{h1}; {h1b}",
          h1 == "Запись не найдена" and h1b == "Страница не найдена", ui.shot("23_not_found"))


@pytest.mark.restart
def test_b24_after_restart(ui, journal):
    ui.go("/employees")
    found = ui.page.locator("table.register a", has_text="Пономарев").count()
    ui.page.locator("table.register a", has_text="Пономарев").first.click()
    ui.page.wait_for_selector(".op")
    ops = ui.page.locator(".op").count()
    check(journal, "Б24", "Сохранность данных после перезапуска", "Сотрудник Пономарев Н. С. и две его операции сохранены",
          f"сотрудник найден: {'да' if found else 'нет'}; операций: {ops}", found == 1 and ops == 2, ui.shot("24_after_restart"))
