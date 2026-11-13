<#
.SYNOPSIS
    Собирает решение в песочнице агента (DSH) без запроса повышенных прав.

.DESCRIPTION
    Песочница запрещает named pipes, которыми обмениваются узлы MSBuild, поэтому обычный
    `dotnet build` с несколькими узлами падает молча: код возврата 1 и «Ошибок: 0».
    Скрипт всегда передаёт -m:1 и работает из любого каталога.

    Телеметрия Avalonia отключена в Directory.Build.targets, поэтому отдельная переменная
    окружения не нужна.

    Восстановление пакетов выполняется по умолчанию; если в кэше нет нужных версий, ему нужен
    доступ в сеть, и такой запуск требует подтверждения (danger-full-access). С ключом
    -NoRestore сборка идёт строго офлайн.

.PARAMETER Configuration
    Конфигурация сборки, по умолчанию Debug.

.PARAMETER NoRestore
    Не восстанавливать пакеты (--no-restore).

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File util-scripts/build.ps1

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File util-scripts/build.ps1 -Configuration Release -NoRestore
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Debug',
    [switch]$NoRestore
)

$ErrorActionPreference = 'Stop'

$repo = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repo 'Cyriller.sln'

if (-not (Test-Path -LiteralPath $solution))
{
    Write-Host "build.ps1: решение не найдено: $solution" -ForegroundColor Red
    exit 1
}

$arguments = @('build', $solution, '-m:1', '-c', $Configuration)

if ($NoRestore)
{
    $arguments += '--no-restore'
}

& dotnet @arguments
exit $LASTEXITCODE
