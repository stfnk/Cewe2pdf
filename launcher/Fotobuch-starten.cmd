@echo off
cd /d "%~dp0"
if "%~1"=="" (
  echo Ziehen Sie Ihre .mcf- oder .mcfx-Datei auf diese Startdatei.
  pause
  exit /b 1
)
Cewe2pdf.exe "%~1" %2 %3 %4 %5 %6 %7 %8 %9
echo.
echo Bitte Ausgabe pruefen. Das PDF steht normalerweise neben dem Fotobuch.
pause
