@echo off
title IOCL Fleet Server (Auto-Restart Watchdog)
:loop
echo [%time%] Starting RunServer.exe...
cd /d "C:\Users\Lenovo\Downloads\IOCL-WebForms"
RunServer.exe
echo [%time%] Server stopped. Restarting in 2 seconds...
timeout /t 2 /nobreak >nul
goto loop
