#!/usr/bin/env bash
# Быстрая проверка: запускает приложение и открывает основные адреса, печатает HTTP-коды.
# Использование: tools/smoke.sh  (запускается в GitHub Actions, workflow smoke)
set -u
rm -f EquipmentIssue/equipment.db
cd EquipmentIssue
nohup dotnet run --no-build -c Release --urls http://localhost:5070 > ../app.log 2>&1 &
cd ..
for i in $(seq 1 60); do curl -s http://localhost:5070 > /dev/null && break; sleep 1; done
fail=0
while read -r url expect; do
  [ -z "$url" ] && continue
  code=$(curl -s -o /tmp/body.html -w "%{http_code}" "http://localhost:5070$url")
  mark="OK"; [ "$code" != "$expect" ] && { mark="FAIL"; fail=1; }
  echo "$mark $code (ожидалось $expect) $url"
done < tools/smoke-urls.txt
echo "---- журнал приложения (ошибки) ----"
grep -iE "fail|exception|error" app.log | head -40 || true
pkill -f EquipmentIssue || true
exit $fail
