"""Общие средства сценариев: браузер, переходы с ожиданием интерактивного режима Blazor, журнал результатов."""
import json
import os
from datetime import date, timedelta
from pathlib import Path

import pytest
from playwright.sync_api import sync_playwright

BASE = os.environ.get("BASE", "http://localhost:5097")
OUT = Path("docs/evidence")
SHOTS = OUT / "screens"
SHOTS.mkdir(parents=True, exist_ok=True)
LOG = OUT / "scenarios.json"


def day(shift=0):
    return date.today() + timedelta(days=shift)


class Journal:
    def __init__(self):
        self.data = json.loads(LOG.read_text(encoding="utf-8")) if LOG.exists() else {"items": []}
        self.data["date"] = day().strftime("%d.%m.%Y")

    def write(self, code, title, expected, actual, passed, shot=None):
        self.data["items"] = [i for i in self.data["items"] if i["code"] != code]
        self.data["items"].append(dict(code=code, title=title, expected=expected, actual=actual, passed=bool(passed), shot=shot))
        self.data["items"].sort(key=lambda i: i["code"])
        LOG.write_text(json.dumps(self.data, ensure_ascii=False, indent=2), encoding="utf-8")


class Ui:
    """Обёртка над страницей браузера: переходы и чтение сообщений."""

    def __init__(self, page):
        self.page = page

    def go(self, path):
        self.page.goto(BASE + path)
        self.page.wait_for_selector("#live", state="attached", timeout=20000)   # соединение Blazor установлено

    def notes(self):
        texts = self.page.locator(".notice.bad, .notice.good, .validation-message").all_inner_texts()
        return " | ".join(" ".join(t.split()) for t in texts if t.strip())

    def submit(self, button="button[type=submit]"):
        url = self.page.url
        self.page.click(button)
        # результат: переход на другую страницу или сообщение на текущей
        self.page.wait_for_function(
            "u => location.href !== u || document.querySelector('.notice.bad, .notice.good, .validation-message')", arg=url, timeout=15000)
        if self.page.url != url:
            self.page.wait_for_selector("#live", state="attached")
        self.page.wait_for_timeout(150)

    def shot(self, name):
        self.page.screenshot(path=str(SHOTS / f"{name}.png"), full_page=True)
        return f"{name}.png"

    def select(self, selector, text):
        """Выбор пункта списка по части текста."""
        options = self.page.locator(f"{selector} option").all_inner_texts()
        label = next(o for o in options if text in o)
        self.page.select_option(selector, label=label)


@pytest.fixture(scope="session")
def journal():
    return Journal()


@pytest.fixture(scope="session")
def ui():
    with sync_playwright() as p:
        browser = p.chromium.launch()
        page = browser.new_page(viewport={"width": 1300, "height": 860}, locale="ru-RU")
        yield Ui(page)
        browser.close()
