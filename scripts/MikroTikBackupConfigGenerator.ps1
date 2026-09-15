<#
.SYNOPSIS
    Generates the routers section of a MikroTik Backup Manager configuration.

.DESCRIPTION
    Reads router names and addresses from a simple text file and generates
    a complete config.yaml based on an existing configuration template.

    The existing configuration is preserved except for the top-level
    routers section.

.EXAMPLE
    .\MikroTikBackupConfigGenerator.ps1 `
        -InputFile .\routers.txt `
        -OutputFile .\config.generated.yaml

.EXAMPLE
    .\MikroTikBackupConfigGenerator.ps1 `
        -InputFile .\routers.txt `
        -ValidateOnly

.NOTES
    Does not store or process router passwords.
    Credential values are references to entries stored by MikroTik Backup Manager.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$InputFile,

    [Parameter(Mandatory = $false)]
    [string]$OutputFile,

    [Parameter(Mandatory = $false)]
    [string]$TemplateFile = "config.yaml",

    [Parameter(Mandatory = $false)]
    [string]$Credential = "backup",

    [ValidateSet("api", "api-ssl")]
    [string]$Protocol = "api-ssl",

    [ValidateRange(1, 65535)]
    [int]$Port = 8729,

    [ValidateRange(1, 65535)]
    [int]$TransferPort = 22,

    [switch]$ValidateOnly,

    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Fail {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    throw $Message
}

function Quote-Yaml {
    param(
        [AllowEmptyString()]
        [string]$Value
    )

    if ($null -eq $Value) {
        return "''"
    }

    return "'" + ($Value -replace "'", "''") + "'"
}

function Get-FullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return [System.IO.Path]::GetFullPath(
        [System.IO.Path]::Combine(
            (Get-Location).Path,
            $Path
        )
    )
}

function Test-Address {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Address
    )

    $parsed = $null

    if ([System.Net.IPAddress]::TryParse($Address, [ref]$parsed)) {
        return $true
    }

    return $Address -match '^(?=.{1,253}$)([A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?)(\.([A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?))*$'
}

function Read-Routers {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Fail "Input file not found: $Path"
    }

    $fullPath = (Resolve-Path -LiteralPath $Path).Path
    $routers = [System.Collections.Generic.List[object]]::new()
    $lineNumber = 0

    foreach ($rawLine in [System.IO.File]::ReadAllLines($fullPath)) {
        $lineNumber++

        $line = $rawLine.Trim()

        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        if ($line.StartsWith("#")) {
            continue
        }

        $parts = $line -split ';', 2

        if ($parts.Count -ne 2) {
            Fail "Invalid input at line $lineNumber. Expected: name;address"
        }

        $name = $parts[0].Trim()
        $address = $parts[1].Trim()

        if ([string]::IsNullOrWhiteSpace($name)) {
            Fail "Invalid input at line $lineNumber. Router name is empty."
        }

        if ([string]::IsNullOrWhiteSpace($address)) {
            Fail "Invalid input at line $lineNumber. Router address is empty."
        }

        if (-not (Test-Address -Address $address)) {
            Fail "Invalid input at line $lineNumber. Invalid address: $address"
        }

        $routers.Add(
            [PSCustomObject]@{
                Name = $name
                Address = $address
                Line = $lineNumber
            }
        )
    }

    if ($routers.Count -eq 0) {
        Fail "Input file contains no routers."
    }

    $duplicateNames = @(
        $routers |
            Group-Object -Property Name |
            Where-Object { $_.Count -gt 1 }
    )

    if ($duplicateNames.Count -gt 0) {
        $names = ($duplicateNames | ForEach-Object { $_.Name }) -join ", "
        Fail "Duplicate router names: $names"
    }

    $duplicateAddresses = @(
        $routers |
            Group-Object -Property Address |
            Where-Object { $_.Count -gt 1 }
    )

    if ($duplicateAddresses.Count -gt 0) {
        $addresses = ($duplicateAddresses | ForEach-Object { $_.Name }) -join ", "
        Fail "Duplicate router addresses: $addresses"
    }

    return $routers
}

function New-RoutersYaml {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Routers,

        [Parameter(Mandatory = $true)]
        [string]$CredentialName,

        [Parameter(Mandatory = $true)]
        [string]$ProtocolName,

        [Parameter(Mandatory = $true)]
        [int]$ApiPort,

        [Parameter(Mandatory = $true)]
        [int]$SftpPort
    )

    $lines = [System.Collections.Generic.List[string]]::new()

    $lines.Add("routers:")

    foreach ($router in $Routers) {
        $lines.Add("  - name: $(Quote-Yaml $router.Name)")
        $lines.Add("    address: $(Quote-Yaml $router.Address)")
        $lines.Add("    protocol: $(Quote-Yaml $ProtocolName)")
        $lines.Add("    port: $ApiPort")
        $lines.Add("    transfer_port: $SftpPort")
        $lines.Add("    credential: $(Quote-Yaml $CredentialName)")
    }

    return ($lines -join [Environment]::NewLine)
}

function Replace-RoutersSection {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Yaml,

        [Parameter(Mandatory = $true)]
        [string]$RoutersYaml
    )

    $pattern = '(?ms)^routers:\s*\r?\n.*?(?=^[A-Za-z_][A-Za-z0-9_]*:\s*$|\z)'

    if ($Yaml -notmatch $pattern) {
        Fail "Could not find the top-level 'routers:' section in template."
    }

    $replacement = $RoutersYaml + [Environment]::NewLine + [Environment]::NewLine

    return [regex]::Replace(
        $Yaml,
        $pattern,
        $replacement,
        1
    )
}

try {
    Write-Host ""
    Write-Host "MikroTik Backup Config Generator"
    Write-Host ""

    $inputPath = Get-FullPath -Path $InputFile

    $routers = @(Read-Routers -Path $inputPath)

    if ([string]::IsNullOrWhiteSpace($Credential)) {
        Fail "Credential name cannot be empty."
    }

    Write-Host "Input:       $InputFile"
    Write-Host "Routers:     $($routers.Count)"
    Write-Host "Credential:  $Credential"
    Write-Host "Protocol:    $Protocol"
    Write-Host "API port:    $Port"
    Write-Host "SFTP port:   $TransferPort"
    Write-Host ""

    if ($ValidateOnly) {
        Write-Host "Validation successful."
        Write-Host "No output file was written."
        Write-Host ""

        exit 0
    }

    if ([string]::IsNullOrWhiteSpace($OutputFile)) {
        Fail "OutputFile is required unless -ValidateOnly is specified."
    }

    $templatePath = Get-FullPath -Path $TemplateFile
    $outputPath = Get-FullPath -Path $OutputFile

    if (-not (Test-Path -LiteralPath $templatePath -PathType Leaf)) {
        Fail "Template file not found: $TemplateFile"
    }

    if ((Test-Path -LiteralPath $outputPath -PathType Leaf) -and -not $Force) {
        Fail "Output file already exists: $OutputFile`nUse -Force to overwrite it."
    }

    $template = [System.IO.File]::ReadAllText($templatePath)

    $routersYaml = New-RoutersYaml `
        -Routers $routers `
        -CredentialName $Credential `
        -ProtocolName $Protocol `
        -ApiPort $Port `
        -SftpPort $TransferPort

    $result = Replace-RoutersSection `
        -Yaml $template `
        -RoutersYaml $routersYaml

    $outputDirectory = Split-Path -Parent $outputPath

    if (-not (Test-Path -LiteralPath $outputDirectory -PathType Container)) {
        New-Item `
            -ItemType Directory `
            -Path $outputDirectory `
            -Force |
            Out-Null
    }

    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)

    [System.IO.File]::WriteAllText(
        $outputPath,
        $result,
        $utf8NoBom
    )

    Write-Host "Configuration generated successfully."
    Write-Host "Output:      $OutputFile"
    Write-Host ""
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}