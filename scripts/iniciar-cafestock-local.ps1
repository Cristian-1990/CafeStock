# Arranca CafeStock en LOCAL (entorno de desarrollo, base de pruebas) y lo abre como una aplicación:
# ventana propia, sin pestañas ni barra de direcciones, a pantalla completa.
#
#   - Si el servidor local ya está en marcha, solo abre la ventana.
#   - Si no, lo arranca en una consola minimizada ("CafeStock - servidor local"): ahí se ven los logs
#     y se detiene cerrando esa consola (o con Ctrl+C).
#   - Salir de pantalla completa: F11. Cerrar la ventana: Alt+F4.
#
# Lo lanza el acceso directo del escritorio que crea crear-acceso-directo.ps1.

param(
    [string]$Url = 'http://localhost:5294'
)

$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot

function Test-Servidor {
    try {
        $r = Invoke-WebRequest -Uri "$Url/login" -UseBasicParsing -TimeoutSec 2
        return $r.StatusCode -eq 200
    } catch { return $false }
}

function Get-Navegador {
    $candidatos = @(
        "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
        "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
        "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe",
        "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
        "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe"
    )
    return $candidatos | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not (Test-Servidor)) {
    # Consola minimizada con el servidor: dotnet run usa el perfil de desarrollo (base cafestock-test.db).
    $comando = "title CafeStock - servidor local && cd /d `"$raiz`" && set ASPNETCORE_ENVIRONMENT=Development && dotnet run --project CafeStock.Blazor --urls $Url"
    Start-Process -FilePath 'cmd.exe' -ArgumentList '/k', $comando -WindowStyle Minimized

    # Espera a que responda (la primera vez compila): hasta 3 minutos.
    $limite = (Get-Date).AddMinutes(3)
    while (-not (Test-Servidor)) {
        if ((Get-Date) -gt $limite) {
            Add-Type -AssemblyName System.Windows.Forms
            [System.Windows.Forms.MessageBox]::Show("CafeStock no ha arrancado en 3 minutos. Mira la consola 'CafeStock - servidor local' en la barra de tareas.", 'CafeStock') | Out-Null
            exit 1
        }
        Start-Sleep -Seconds 2
    }
}

$navegador = Get-Navegador
if (-not $navegador) {
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show('No encuentro Chrome ni Edge para abrir la aplicación.', 'CafeStock') | Out-Null
    exit 1
}

# --app: ventana de aplicación (sin barra del navegador). --start-fullscreen: arranca a pantalla completa.
Start-Process -FilePath $navegador -ArgumentList "--app=$Url", '--start-fullscreen'
