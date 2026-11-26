# Crea en el escritorio el acceso directo "CafeStock (local)", con el logo como icono, que arranca la app
# de desarrollo y la abre en ventana propia a pantalla completa (ver iniciar-cafestock-local.ps1).
#
# Uso (desde la raíz del proyecto):
#   powershell -ExecutionPolicy Bypass -File scripts\crear-acceso-directo.ps1

$ErrorActionPreference = 'Stop'

$scripts  = $PSScriptRoot
$lanzador = Join-Path $scripts 'iniciar-cafestock-local.ps1'
$icono    = Join-Path $scripts 'cafestock.ico'
$escritorio = [Environment]::GetFolderPath('Desktop')
$ruta = Join-Path $escritorio 'CafeStock (local).lnk'

$shell = New-Object -ComObject WScript.Shell
$acceso = $shell.CreateShortcut($ruta)
$acceso.TargetPath = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
$acceso.Arguments = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$lanzador`""
$acceso.WorkingDirectory = Split-Path -Parent $scripts
$acceso.IconLocation = "$icono,0"
$acceso.Description = 'CafeStock (entorno local de desarrollo)'
$acceso.WindowStyle = 7   # minimizada: no se ve la consola de PowerShell
$acceso.Save()

Write-Output "Acceso directo creado: $ruta"
