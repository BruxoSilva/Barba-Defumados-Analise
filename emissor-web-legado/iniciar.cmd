@echo off
cd /d "%~dp0"
if not exist ".next\BUILD_ID" (
  echo Primeiro execute: npm install ^&^& npm run build
  pause
  exit /b 1
)
echo Abra http://127.0.0.1:3000 no navegador.
npm run start
