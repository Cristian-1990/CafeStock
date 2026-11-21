<#
.SYNOPSIS
  Prepara una imagen de la mascota (leon) generada por IA para usarla en la app.

.DESCRIPTION
  1. Quita el fondo verde liso (#00FF00) y deja transparencia (con un pequeno ajuste de bordes
     para que no quede un halo verde).
  2. Recorta el leon y lo coloca en un lienzo de 480x792 con el mismo tamano y la misma base
     que el leon de Inicio (mascot-saludo.png), para que todos los leones midan igual.
  3. Guarda un PNG con transparencia.

  Uso:
    .\scripts\mascota-procesar.ps1 -Entrada "C:\ruta\leon-gpt.png" `
        -Salida "CafeStock.Blazor\wwwroot\images\mascot\mascot-producto.png"

  Los originales grandes NO deben quedarse dentro de wwwroot: esa carpeta se publica entera.
  (Texto en ASCII a proposito: PowerShell 5.1 lee mal los acentos de un .ps1 sin BOM.)
#>
param(
    [Parameter(Mandatory = $true)][string]$Entrada,
    [Parameter(Mandatory = $true)][string]$Salida,
    [int]$Ancho = 480,           # lienzo final (igual que el resto de imagenes de la mascota)
    [int]$Alto = 792,
    [int]$AltoFigura = 712,      # alto del leon, como mascot-saludo.png (710)
    [int]$PieY = 752,            # donde terminan los pies, como mascot-saludo.png
    [int]$UmbralFondo = 100,     # "verdor" a partir del cual un pixel es fondo
    [int]$UmbralBorde = 25,      # por debajo de esto el pixel es opaco; entre ambos, semitransparente
    # Rectangulos "x,y,ancho,alto" en coordenadas de la imagen ORIGINAL (opcionales):
    [string[]]$Proteger = @(),   # no se toca: sirve para detalles verdes legitimos (p. ej. un tick verde en la tableta)
    [string[]]$LimpiarOliva = @() # borra el relleno oliva/dorado que la IA deja en huecos estrechos (entre cola y pierna)
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

Add-Type -TypeDefinition @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class MascotaFondo
{
    // Devuelve la imagen con el verde convertido en transparencia.
    static bool Dentro(int[][] zonas, int x, int y)
    {
        foreach (int[] z in zonas)
            if (x >= z[0] && y >= z[1] && x < z[0] + z[2] && y < z[1] + z[3]) return true;
        return false;
    }

    public static Bitmap QuitarVerde(Bitmap origen, int umbralBorde, int umbralFondo, int[][] proteger, int[][] oliva)
    {
        int w = origen.Width, h = origen.Height;
        Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp)) { g.DrawImage(origen, 0, 0, w, h); }

        BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int stride = data.Stride;
        byte[] px = new byte[Math.Abs(stride) * h];
        Marshal.Copy(data.Scan0, px, 0, px.Length);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = y * stride + x * 4;
                int b = px[i], g = px[i + 1], r = px[i + 2];
                int verdor = g - Math.Max(r, b);          // cuanto mas verde que rojo/azul
                if (Dentro(proteger, x, y)) continue;      // detalle verde legitimo: intacto
                if (Dentro(oliva, x, y) && g >= 0.8 * r)   // relleno oliva: el pelaje real tiene g bastante menor que r
                {
                    px[i] = 0; px[i + 1] = 0; px[i + 2] = 0; px[i + 3] = 0;
                }
                else if (verdor >= umbralFondo)
                {
                    px[i] = 0; px[i + 1] = 0; px[i + 2] = 0; px[i + 3] = 0;
                }
                else if (verdor > umbralBorde)
                {
                    double a = (umbralFondo - verdor) / (double)(umbralFondo - umbralBorde);
                    px[i + 3] = (byte)(a * 255);
                    px[i + 1] = (byte)Math.Min(g, (int)(Math.Max(r, b) * 0.85));   // quita el verde que se mezcla en el borde
                }
                else if (verdor > 0)
                {
                    px[i + 1] = (byte)Math.Min(g, (int)(Math.Max(r, b) * 0.85));
                }
            }
        }

        // Borde: se come el primer anillo (verde residual), el segundo queda a medio alfa y en los 5 px
        // junto al fondo se limita el verde (g <= 0.85 del mayor entre rojo y azul): el pelo dorado
        // tiene g menor que r, asi que no se altera nada real.
        byte[] alfa = new byte[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) alfa[y * w + x] = px[y * stride + x * 4 + 3];
        int[] dist = new int[w * h];
        for (int k = 0; k < dist.Length; k++) dist[k] = alfa[k] == 0 ? 0 : 99;
        for (int pasada = 1; pasada <= 5; pasada++)
        {
            int[] sig = (int[])dist.Clone();
            for (int y = 1; y < h - 1; y++)
                for (int x = 1; x < w - 1; x++)
                {
                    int k = y * w + x;
                    if (dist[k] == 99 && (dist[k - 1] == pasada - 1 || dist[k + 1] == pasada - 1 || dist[k - w] == pasada - 1 || dist[k + w] == pasada - 1)) sig[k] = pasada;
                }
            dist = sig;
        }
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int k = y * w + x, i = y * stride + x * 4;
                if (dist[k] == 1) px[i + 3] = 0;
                else if (dist[k] == 2) px[i + 3] = (byte)(alfa[k] / 2);
                if (dist[k] >= 1 && dist[k] <= 5 && px[i + 3] > 0)
                {
                    int tope = (int)(Math.Max(px[i + 2], px[i]) * 0.85);
                    if (px[i + 1] > tope) px[i + 1] = (byte)tope;
                }
            }
        Marshal.Copy(px, 0, data.Scan0, px.Length);
        bmp.UnlockBits(data);
        return bmp;
    }

    // Caja que contiene los pixeles con algo de opacidad.
    public static Rectangle CajaFigura(Bitmap bmp, int minAlfa)
    {
        int w = bmp.Width, h = bmp.Height, minx = w, miny = h, maxx = 0, maxy = 0;
        BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        int stride = data.Stride;
        byte[] px = new byte[Math.Abs(stride) * h];
        Marshal.Copy(data.Scan0, px, 0, px.Length);
        bmp.UnlockBits(data);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (px[y * stride + x * 4 + 3] >= minAlfa)
                {
                    if (x < minx) minx = x; if (x > maxx) maxx = x;
                    if (y < miny) miny = y; if (y > maxy) maxy = y;
                }
        return new Rectangle(minx, miny, maxx - minx + 1, maxy - miny + 1);
    }
}
"@ -ReferencedAssemblies System.Drawing

if (-not (Test-Path -LiteralPath $Entrada)) { throw "No existe la imagen de entrada: $Entrada" }

$origen = New-Object System.Drawing.Bitmap -ArgumentList $Entrada
function ConvertTo-Zonas([string[]]$lista) {
    $zonas = New-Object 'System.Collections.Generic.List[int[]]'
    foreach ($t in $lista) { $zonas.Add([int[]]($t -split ',' | ForEach-Object { [int]$_ })) }
    return , $zonas.ToArray()
}
$sinFondo = [MascotaFondo]::QuitarVerde($origen, $UmbralBorde, $UmbralFondo, (ConvertTo-Zonas $Proteger), (ConvertTo-Zonas $LimpiarOliva))
$caja = [MascotaFondo]::CajaFigura($sinFondo, 40)
if ($caja.Width -lt 50 -or $caja.Height -lt 50) { throw "No se encontro la figura: revisa que el fondo sea verde liso (#00FF00)." }

# Escala para que el leon mida AltoFigura de alto; si no cabe a lo ancho, se reduce para dejar 12 px de margen.
$escala = $AltoFigura / $caja.Height
if ($caja.Width * $escala -gt ($Ancho - 24)) { $escala = ($Ancho - 24) / $caja.Width }
$anchoFig = [int][Math]::Round($caja.Width * $escala)
$altoFig = [int][Math]::Round($caja.Height * $escala)
$x0 = [int][Math]::Round(($Ancho - $anchoFig) / 2)
$y0 = $PieY - $altoFig

# Lienzo premultiplicado: al reducir no aparecen halos oscuros en los bordes.
$lienzo = New-Object System.Drawing.Bitmap -ArgumentList $Ancho, $Alto, ([System.Drawing.Imaging.PixelFormat]::Format32bppPArgb)
$g = [System.Drawing.Graphics]::FromImage($lienzo)
$g.Clear([System.Drawing.Color]::Transparent)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$destino = New-Object System.Drawing.Rectangle -ArgumentList $x0, $y0, $anchoFig, $altoFig
$g.DrawImage($sinFondo, $destino, $caja.X, $caja.Y, $caja.Width, $caja.Height, [System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose()

$carpeta = Split-Path -Parent $Salida
if ($carpeta -and -not (Test-Path -LiteralPath $carpeta)) { New-Item -ItemType Directory -Path $carpeta | Out-Null }
$lienzo.Save($Salida, [System.Drawing.Imaging.ImageFormat]::Png)

$origen.Dispose(); $sinFondo.Dispose(); $lienzo.Dispose()
$kb = [Math]::Round((Get-Item -LiteralPath $Salida).Length / 1KB)
Write-Output ("OK: {0} ({1}x{2}, figura {3}x{4} en x={5}, y={6}, {7} KB)" -f $Salida, $Ancho, $Alto, $anchoFig, $altoFig, $x0, $y0, $kb)
