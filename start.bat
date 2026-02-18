@echo off
echo Building and Running RPG Game...
echo.
echo.
if %ERRORLEVEL% == 0 (
    echo ✓ Build successful! Starting game...
    echo.
    dotnet run
) else (
    echo ✗ Build failed! Cannot run game.
)
echo.
pause