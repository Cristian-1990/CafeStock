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
# Requisito: el cliente de SQLite  ->  sudo apt install sqlite3
#
# Por qué es obligatorio y no vale un simple `cp`: la base de datos funciona en modo WAL, así
# que los últimos cambios pueden estar todavía en el fichero cafestock.db-wal y NO en
# cafestock.db. Copiar solo cafestock.db daría una copia anticuada (o incoherente) sin
# avisar. `sqlite3 .backup` lee el conjunto completo y produce una copia consistente aunque
# la app esté escribiendo en ese momento.
#
# Qué hace:
#   1. Copia la base con `sqlite3 .backup`.
#   2. Comprueba la copia (PRAGMA integrity_check). Si está dañada, la borra y sale con error.
#   3. La comprime (cafestock-AAAAMMDD-HHMMSS.db.gz).
#   4. Borra las copias más antiguas que los días indicados.
#
# Códigos de salida: 0 todo bien, 1 error (el mensaje dice cuál).
# Más detalle, cron y cómo restaurar: docs/BACKUP.md

set -euo pipefail

DB="${1:-${CAFESTOCK_DB:-$HOME/.local/share/cafestock.db}}"
DEST="${2:-${CAFESTOCK_BACKUP_DIR:-$HOME/cafestock-backups}}"
DIAS="${3:-${CAFESTOCK_BACKUP_DAYS:-30}}"

registrar() { echo "$(date '+%Y-%m-%d %H:%M:%S') [backup] $*"; }
fallar()    { registrar "ERROR: $*" >&2; exit 1; }

command -v sqlite3 >/dev/null 2>&1 \
    || fallar "falta sqlite3 (sudo apt install sqlite3). No se hace una copia simple con cp: en modo WAL quedaría incompleta."
[[ -f "$DB" ]] || fallar "no existe la base de datos: $DB"
[[ "$DIAS" =~ ^[0-9]+$ ]] || fallar "los días a conservar deben ser un número entero (recibido: $DIAS)"

mkdir -p "$DEST" || fallar "no se pudo crear la carpeta de destino: $DEST"

marca="$(date +%Y%m%d-%H%M%S)"
copia="$DEST/cafestock-$marca.db"

sqlite3 "$DB" ".backup '$copia'" || { rm -f "$copia"; fallar "falló sqlite3 .backup"; }

resultado="$(sqlite3 "$copia" 'PRAGMA integrity_check;' || true)"
if [[ "$resultado" != "ok" ]]; then
    rm -f "$copia"
    fallar "la copia no pasa la comprobación de integridad: $resultado"
fi

gzip -9 -- "$copia" || fallar "no se pudo comprimir la copia"
registrar "copia creada: $copia.gz ($(du -h -- "$copia.gz" | cut -f1))"

# Rotación: solo toca los ficheros que crea este script.
borradas="$(find "$DEST" -maxdepth 1 -name 'cafestock-*.db.gz' -type f -mtime "+$DIAS" -print -delete | wc -l)"
registrar "rotación: $borradas copia(s) con más de $DIAS días eliminada(s)"
