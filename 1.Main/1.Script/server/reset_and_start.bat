@echo off
echo 데이터베이스 초기화 시작...
node resetdb.js

echo 서버 재시작...
pm2 restart server || pm2 start server.js

echo 완료!
pause 