@echo off
REM Git initialization and push script for doanNET
REM Run this AFTER installing Git from https://git-scm.com

setlocal enabledelayedexpansion

echo.
echo ========================================
echo Git Setup - doanNET Repository
echo ========================================
echo.

REM Check if git is available
git --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Git is NOT installed!
    echo.
    echo Please install Git from: https://git-scm.com/download/win
    echo.
    echo After installation, run this script again.
    pause
    exit /b 1
)

echo [OK] Git is installed: 
git --version
echo.

REM Change to project directory
cd /d "d:\DATA\dotNet-duAn\doAn_dotNET\DoAnNet"
if %errorlevel% neq 0 (
    echo [ERROR] Cannot access project directory!
    pause
    exit /b 1
)

echo [OK] Current directory: %cd%
echo.

REM Step 1: Add header to README
echo [1/7] Creating README.md header...
echo # doanNET >> README.md
echo [OK] README.md updated
echo.

REM Step 2: Initialize git
echo [2/7] Initializing git repository...
git init
if %errorlevel% neq 0 (
    echo [ERROR] git init failed!
    pause
    exit /b 1
)
echo [OK] Repository initialized
echo.

REM Step 3: Configure git user (first time)
echo [3/7] Configuring git user...
git config user.name "Tran Huu Vong"
git config user.email "tranhuuvong23092006@gmail.com"
echo [OK] Git user configured
echo.

REM Step 4: Add all files
echo [4/7] Adding files to staging...
git add .
if %errorlevel% neq 0 (
    echo [ERROR] git add failed!
    pause
    exit /b 1
)
echo [OK] Files added
echo.

REM Step 5: First commit
echo [5/7] Creating first commit...
git commit -m "first commit"
if %errorlevel% neq 0 (
    echo [WARNING] git commit returned error (may be normal if no changes)
)
echo [OK] Commit created
echo.

REM Step 6: Rename branch to main
echo [6/7] Setting main branch...
git branch -M main
if %errorlevel% neq 0 (
    echo [OK] Branch already configured
) else (
    echo [OK] Branch renamed to main
)
echo.

REM Step 7: Add remote
echo [7/7] Adding remote repository...
git remote add origin https://github.com/tranvonghoclaptrinh/doanNET.git
if %errorlevel% neq 0 (
    echo [WARNING] Remote may already exist, continuing...
)
echo [OK] Remote added
echo.

REM Push to GitHub
echo ========================================
echo [PUSH] Pushing to GitHub...
echo ========================================
echo.
echo NOTE: You will be asked for credentials
echo Username: tranvonghoclaptrinh
echo Password: Your GitHub Personal Access Token (NOT your password)
echo.
echo Generate token at: https://github.com/settings/tokens
echo.

git push -u origin main

if %errorlevel% eq 0 (
    echo.
    echo ========================================
    echo [SUCCESS] Push completed!
    echo ========================================
    echo.
    echo Repository: https://github.com/tranvonghoclaptrinh/doanNET
    echo.
    echo Your code is now on GitHub!
) else (
    echo.
    echo [ERROR] Push failed
    echo.
    echo Possible reasons:
    echo 1. Authentication failed (wrong token/password)
    echo 2. Branch conflict
    echo 3. Network issue
    echo.
    echo For help, see: HUONG_DAN_PUSH_CHI_TIET.md
)

echo.
pause
