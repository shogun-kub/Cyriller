<#
.SYNOPSIS
    Собирает решение и запускает тестовые проекты по одному в песочнице агента (DSH).

.DESCRIPTION
    `dotnet test` на решении в песочнице зависает: testhost и узлы MSBuild используют named
    pipes, которые песочница запрещает. Скрипт собирает решение с -m:1 и запускает каждый
    тестовый проект отдельно с --no-build --no-restore.

    Проект считается тестовым, если в его csproj есть <IsTestProject>true</IsTestProject>.

.PARAMETER Configuration
    Конфигурация сборки и тестов, по умолчанию Debug.

.PARAMETER Filter
    Необязательный фильтр VSTest, например "FullyQualifiedName~NameDeclensionTests".

.PARAMETER NoBuild
    Не собирать решение, тестировать уже собранные сборки.

.PARAMETER NoRestore
    Не восстанавливать пакеты при сборке.

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File util-scripts/test.ps1

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File util-scripts/test.ps1 -Filter "FullyQualifiedName~CorpusFile"

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File util-scripts/test.ps1 -NoBuild
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Debug',
    [string]$Filter,
    [switch]$NoBuild,
    [switch]$NoRestore
)

$ErrorActionPreference = 'Stop'

$repo = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repo 'Cyriller.sln'

if (-not (Test-Path -LiteralPath $solution))
{
    Write-Host "test.ps1: решение не найдено: $solution" -ForegroundColor Red
    exit 1
}

if (-not $NoBuild)
{
    $build = Join-Path $PSScriptRoot 'build.ps1'
    $buildArguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $build, '-Configuration', $Configuration)

    if ($NoRestore)
    {
        $buildArguments += '-NoRestore'
    }

    & pwsh @buildArguments

    if ($LASTEXITCODE -ne 0)
    {
        Write-Host 'test.ps1: сборка не удалась, тесты не запускались.' -ForegroundColor Red
        exit 1
    }
}

$testProjects = Get-ChildItem -Path $repo -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\packages\\' -and $_.FullName -notmatch '\\Test_data\\' } |
    Where-Object { Select-String -LiteralPath $_.FullName -Pattern '<IsTestProject>\s*true\s*</IsTestProject>' -Quiet }

if (-not $testProjects)
{
    Write-Host 'test.ps1: тестовые проекты не найдены.' -ForegroundColor Red
    exit 1
}

$failed = New-Object System.Collections.Generic.List[string]

foreach ($project in $testProjects)
{
    Write-Host "test.ps1: тесты $($project.Name)" -ForegroundColor Cyan

    $arguments = @('test', $project.FullName, '-c', $Configuration, '--no-build', '--no-restore')

    if ($Filter)
    {
        $arguments += @('--filter', $Filter)
    }

    & dotnet @arguments

    if ($LASTEXITCODE -ne 0)
    {
        $failed.Add($project.Name)
    }
}

if ($failed.Count -gt 0)
{
    Write-Host "test.ps1: не прошли: $($failed -join ', ')" -ForegroundColor Red
    exit 1
}

Write-Host 'test.ps1: все тестовые проекты прошли.' -ForegroundColor Green
exit 0
