#!/usr/bin/env bash
#
# Copia de seguridad de la base de datos de CafeStock (SQLite).
#
# Uso:
#   backup-cafestock.sh [ruta_db] [carpeta_destino] [dias_a_conservar]
#
# Cada argumento puede darse también por variable de entorno (el argumento manda):
#   CAFESTOCK_DB            base de datos a copiar      (por defecto ~/.local/share/cafestock.db)
#   CAFESTOCK_BACKUP_DIR    carpeta de las copias       (por defecto ~/cafestock-backups)
#   CAFESTOCK_BACKUP_DAYS   días que se conservan       (por defecto 30)
#
# Qué hace:
#   1. Copia la base de datos. Con el cliente `sqlite3` usa ".backup", que da una copia
#      CONSISTENTE aunque la app esté escribiendo en ese momento. Sin `sqlite3` recurre a un
#      `cp` simple y lo avisa: a una hora de poco movimiento (22:00) suele bastar, pero no está
#      garantizado. Recomendado: sudo apt install sqlite3
#   2. Comprueba la copia (PRAGMA integrity_check). Si está dañada, la borra y sale con error.
#   3. La comprime (cafestock-AAAAMMDD-HHMMSS.db.gz).
#   4. Borra las copias más antiguas que los días indicados.
#
# Códigos de salida: 0 todo bien, 1 error (la cabecera de cada mensaje dice cuál).
# Más detalle, cron y cómo restaurar: docs/BACKUP.md

set -euo pipefail

DB="${1:-${CAFESTOCK_DB:-$HOME/.local/share/cafestock.db}}"
DEST="${2:-${CAFESTOCK_BACKUP_DIR:-$HOME/cafestock-backups}}"
DIAS="${3:-${CAFESTOCK_BACKUP_DAYS:-30}}"

registrar() { echo "$(date '+%Y-%m-%d %H:%M:%S') [backup] $*"; }
fallar()    { registrar "ERROR: $*" >&2; exit 1; }

[[ -f "$DB" ]] || fallar "no existe la base de datos: $DB"
[[ "$DIAS" =~ ^[0-9]+$ ]] || fallar "los días a conservar deben ser un número entero (recibido: $DIAS)"

mkdir -p "$DEST" || fallar "no se pudo crear la carpeta de destino: $DEST"

marca="$(date +%Y%m%d-%H%M%S)"
copia="$DEST/cafestock-$marca.db"

if command -v sqlite3 >/dev/null 2>&1; then
    sqlite3 "$DB" ".backup '$copia'" || { rm -f "$copia"; fallar "falló sqlite3 .backup"; }

    resultado="$(sqlite3 "$copia" 'PRAGMA integrity_check;' || true)"
    if [[ "$resultado" != "ok" ]]; then
        rm -f "$copia"
        fallar "la copia no pasa la comprobación de integridad: $resultado"
    fi
else
    registrar "AVISO: sqlite3 no está instalado; se hace una copia simple (cp) sin comprobar integridad."
    cp -- "$DB" "$copia" || { rm -f "$copia"; fallar "falló la copia"; }
fi

gzip -9 -- "$copia" || fallar "no se pudo comprimir la copia"
registrar "copia creada: $copia.gz ($(du -h -- "$copia.gz" | cut -f1))"

# Rotación: solo toca los ficheros que crea este script.
borradas="$(find "$DEST" -maxdepth 1 -name 'cafestock-*.db.gz' -type f -mtime "+$DIAS" -print -delete | wc -l)"
registrar "rotación: $borradas copia(s) con más de $DIAS días eliminada(s)"
