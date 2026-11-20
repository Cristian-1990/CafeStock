# Copias de seguridad de CafeStock

Toda la información de la cafetería (productos, proveedores, compras, facturas, presupuestos **y los usuarios**) está en **un único fichero SQLite**. Si se pierde, se pierde todo, así que conviene tener copias automáticas y comprobar de vez en cuando que funcionan.

## Dónde está la base de datos

La app guarda el fichero en la carpeta de datos locales del usuario que la ejecuta (`Environment.SpecialFolder.LocalApplicationData`):

| Sistema | Ruta |
|---|---|
| Linux (Raspberry Pi) | `~/.local/share/cafestock.db` del usuario que ejecuta `cafestock.service` |
| Windows | `C:\Users\<usuario>\AppData\Local\cafestock.db` |

En modo **Development** (pruebas) usa `cafestock-test.db` en la misma carpeta, nunca el fichero real.

> Si el servicio corre como otro usuario (por ejemplo `root`), la ruta cambia (`/root/.local/share/...`).
> Comprueba cuál es con `systemctl cat cafestock.service` (línea `User=`) y con `ls -l`.

## El script

[`scripts/backup-cafestock.sh`](../scripts/backup-cafestock.sh) hace la copia, la verifica, la comprime y borra las antiguas:

```bash
scripts/backup-cafestock.sh [ruta_db] [carpeta_destino] [dias_a_conservar]
```

Por defecto copia `~/.local/share/cafestock.db` a `~/cafestock-backups/` y conserva 30 días. Genera ficheros `cafestock-AAAAMMDD-HHMMSS.db.gz`.

**Necesita el cliente de SQLite** en la Pi: `sudo apt install sqlite3`. Sin él el script se niega a copiar y termina con error, a propósito.

### Por qué no vale un simple `cp`

La base de datos funciona en **modo WAL** (comprobado en la base de desarrollo de un PC con Windows; en la Pi compruébalo con `sqlite3 ~/.local/share/cafestock.db "PRAGMA journal_mode;"`, debe responder `wal`): junto a `cafestock.db` aparecen `cafestock.db-wal` y `cafestock.db-shm`, y los últimos cambios pueden estar solo en `-wal`. Copiar únicamente `cafestock.db` da una copia **anticuada o incoherente, sin ningún aviso**. `sqlite3 .backup` lee el conjunto completo y produce una copia consistente aunque alguien esté guardando justo en ese momento. Por la misma razón, **si copias la base a mano (por ejemplo al portátil), copia también los ficheros `-wal` y `-shm`, o mejor aún, usa una copia de este script**.

## Programarlo (cron)

Como el usuario que ejecuta la app, `crontab -e` y añade (todos los días a las 22:00):

```cron
0 22 * * * /ruta/a/CafeStock/scripts/backup-cafestock.sh >> $HOME/cafestock-backups/backup.log 2>&1
```

Comprueba que el fichero es ejecutable (`chmod +x scripts/backup-cafestock.sh`) y que la carpeta de destino existe (`mkdir -p ~/cafestock-backups`).

### ¿Ya tenías una copia nocturna configurada?

Según lo recordado, había una tarea a las 22:00 en la Pi. Para ver qué hay realmente:

```bash
crontab -l                 # tareas del usuario actual
sudo crontab -l            # tareas de root
systemctl list-timers      # si se hizo con un timer de systemd
ls -lt ~/cafestock-backups | head   # ¿hay copias recientes?
```

Si existe y funciona, no hace falta cambiar nada; este script es la versión versionada en el repositorio, con comprobación de integridad y rotación.

## Guardar una copia fuera de la Pi

Una copia en la misma tarjeta SD no protege de que la tarjeta se estropee. Opciones sencillas:

- Copiar periódicamente la carpeta `~/cafestock-backups` a otro equipo: `scp` o `rsync` desde el portátil.
- Un lápiz USB montado en la Pi como carpeta de destino (tercer argumento del script, o `CAFESTOCK_BACKUP_DIR`).

## Comprobar que las copias sirven

Una copia que nunca se ha restaurado es una suposición. Cada cierto tiempo:

```bash
gunzip -c ~/cafestock-backups/cafestock-AAAAMMDD-HHMMSS.db.gz > /tmp/prueba.db
sqlite3 /tmp/prueba.db "PRAGMA integrity_check;"      # debe responder: ok
sqlite3 /tmp/prueba.db "SELECT COUNT(*) FROM Productos;"
rm /tmp/prueba.db
```

## Restaurar

1. Para la app: `sudo systemctl stop cafestock.service`.
2. Guarda el fichero actual por si acaso: `mv ~/.local/share/cafestock.db ~/.local/share/cafestock.db.antes`.
3. **Borra los restos del modo WAL del fichero antiguo**: `rm -f ~/.local/share/cafestock.db-wal ~/.local/share/cafestock.db-shm`. Si se quedan, SQLite los aplica sobre la copia restaurada y mezcla datos de dos épocas distintas (es un problema real, no teórico).
4. Descomprime la copia elegida en su sitio: `gunzip -c ~/cafestock-backups/cafestock-AAAAMMDD-HHMMSS.db.gz > ~/.local/share/cafestock.db`.
5. Arranca: `sudo systemctl start cafestock.service`. Al arrancar, la app aplica sola las migraciones pendientes si la copia es de una versión anterior.

## Qué NO cubre

- Las **fotos** de productos y proveedores (`wwwroot/images/`) y los **ficheros de factura adjuntos**: la base de datos guarda solo su ruta, el fichero en sí está fuera. Si los necesitas a salvo, copia esas carpetas aparte.
- El fichero `appsettings.json` (con la configuración del correo) tampoco: guárdalo en un sitio seguro, **sin subirlo al repositorio**.
