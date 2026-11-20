# CafeStock

Sistema de gestión de stock, proveedores y compras para una cafetería. Sustituye el control manual en papel del inventario: registra los productos, calcula automáticamente lo que hay que reponer, genera la lista de la compra y guarda el histórico de compras y de gasto.

Nace de una necesidad real de trabajo — la gestión del stock de una cafetería con lápiz y papel — y se desarrolla siguiendo principios de arquitectura limpia y separación de responsabilidades.

**Stack:** .NET 10 · Blazor Server + MudBlazor · EF Core + SQLite · ASP.NET Core Identity · Serilog · QuestPDF · MailKit · ApexCharts · CSharpFunctionalExtensions (`Result<T, DomainError>`).

---

## Qué hace

- **Productos y proveedores**: alta, edición, baja y consulta, con fotos, precio de referencia y día de reparto de cada proveedor.
- **Stock**: stock actual y máximo de cada producto, con un recuento guiado por proveedor ("Realizar stock") y el cálculo automático de lo que falta (`StockMáximo − StockActual`).
- **Lista de la compra**: solo lo que está por debajo de su nivel objetivo, agrupada por proveedor (y por pasillo en los supermercados genéricos), exportable a PDF y enviable por correo.
- **Recepción de pedidos**: al volver de comprar se confirma producto a producto lo recibido y su precio real. Cada recepción queda registrada como una compra con sus líneas.
- **Facturas**: datos de facturación de cada compra (método de pago, número de factura del proveedor, notas, adjunto) y compras excepcionales de artículos sueltos.
- **Venta de café**: descuento de stock unidad a unidad de los productos con seguimiento individual, con registro de quién lo vendió.
- **Informes**: gasto por proveedor y por mes, evolución de precios, desviación sobre el precio de referencia (PPV), ticket medio, concentración de gasto, frecuencia de reparto frente al día esperado, recepciones que superan el stock máximo, presupuesto frente a gasto real y evolución del valor del inventario.
- **Usuarios y roles**: inicio de sesión con tres roles (Trabajador, Encargado y Admin). Ver [Acceso y roles](#acceso-y-roles).
- Instalable como PWA y con un personaje-guía opcional.

---

## Arquitectura

Separación estricta en capas, con el backend independiente de la interfaz. Los errores de negocio no son excepciones: viajan como `Result<T, DomainError>`.

```
Blazor (Components/Pages)  →  Service (lógica de negocio)  →  Validador (reglas)  →  Repository (persistencia)
                                                                  Modelo ↔ Mapper ↔ Entity (EF Core)
```

| Proyecto | Responsabilidad |
|----------|-----------------|
| `CafeStock.Back` | Dominio, validación, persistencia, migraciones y lógica de negocio (incluidos los cálculos de los informes y la gestión de usuarios). Sin dependencias de UI. |
| `CafeStock.Blazor` | Interfaz web (Blazor Server + MudBlazor), generación de PDF, envío de correo y endpoints de inicio/cierre de sesión. |
| `CafeStock.Tests` | Pruebas del backend. |

Piezas del backend que conviene conocer:

- `Infraestructure/BackServiceCollectionExtensions.cs`: único sitio donde se registran repositorios, validadores y servicios en el contenedor de dependencias.
- `Services/Informes/InformeService.cs`: los cálculos de los informes. Es puro (recibe los datos, devuelve resultados) y usa un `TimeProvider`, por lo que se prueba con fecha fija. La página `Informes.razor` solo carga datos y pinta.
- `Services/Usuarios/UsuarioService.cs`: altas, cambios de rol, contraseñas y bajas sobre Identity.
- `Infraestructure/DatabaseInitializer.cs` y `Migrations/`: ver [Base de datos y migraciones](#base-de-datos-y-migraciones).
- `Infraestructure/ReintentoConcurrencia.cs`: ver [Concurrencia](#concurrencia-del-stock).

---

## Cómo ejecutarlo

Requisitos: SDK de .NET 10.

```bash
dotnet run --project CafeStock.Blazor --launch-profile http
```

Se abre en <http://localhost:5294>. En el modo **Development** (el de `dotnet run`) la app usa una base de datos de pruebas aparte (`cafestock-test.db`) y muestra una cabecera roja "MODO PRUEBAS"; la base real (`cafestock.db`) solo la usa Production.

**Primer arranque:** mientras no exista ningún usuario, la app lleva a `/primer-acceso`, donde se crea la cuenta de administrador. No hay contraseñas escritas en el código ni en ficheros.

**Correo (opcional):** copia `CafeStock.Blazor/appsettings.json.example` como `appsettings.json` y rellena la sección `Email`. Ese fichero contiene credenciales y no se sube al repositorio; sin él todo funciona salvo el envío de correo.

---

## Acceso y roles

Cada página declara qué política necesita (`[Authorize(Policy = ...)]`); ocultar un enlace del menú es solo comodidad. Cada rol incluye los permisos del anterior.

| Rol | Puede entrar en |
|-----|-----------------|
| **Trabajador** | Inicio, Stock, Realizar stock, Venta y Recepcionar |
| **Encargado** | Lo anterior + Proveedores, Productos, Facturas e Informes |
| **Admin** | Lo anterior + Usuarios (altas, cambios de rol, contraseñas y bajas) |

Detalles de seguridad:

- Contraseñas con hash de Identity; mínimo 8 caracteres con letras y un número. Tras 5 intentos fallidos, el usuario queda bloqueado 5 minutos.
- El inicio y el cierre de sesión son formularios HTML con token antifalsificación hacia endpoints normales (`/auth/login`, `/auth/logout`): la cookie solo se puede escribir en una respuesta HTTP, no dentro del circuito de Blazor Server. La sesión dura 12 horas y se renueva con el uso.
- Si un Admin cambia el rol de alguien o lo elimina, el cambio llega a sus pestañas abiertas en un máximo de unos 5 minutos.
- No se puede eliminar ni quitar el rol al único Admin, y nadie puede eliminarse a sí mismo.

---

## Base de datos y migraciones

SQLite, un único fichero (en modo WAL: junto al `.db` aparecen `-wal` y `-shm`, ver [docs/BACKUP.md](docs/BACKUP.md)). El esquema lo gestiona **EF Core Migrations**; `DatabaseInitializer` lo aplica al arrancar la app, antes de aceptar peticiones. Distingue tres casos:

1. **Base nueva:** aplica todas las migraciones.
2. **Base antigua** (creada antes de existir las migraciones, sin tabla `__EFMigrationsHistory`): la actualiza por última vez con el código heredado (`AppDbContext.EsquemaHeredado.cs`) y marca `InitialCreate` como ya aplicada.
3. **Base ya gestionada por migraciones:** aplica solo las pendientes.

Para cambiar el modelo (nueva columna o tabla):

```bash
dotnet ef migrations add NombreDelCambio --project CafeStock.Back --output-dir Migrations
```

Revisa la migración generada y commitéala junto al cambio de modelo. Un test compara el esquema que producen las migraciones con el del modelo y falla si se olvida este paso.

`AppDbContext.EsquemaHeredado.cs` se podrá borrar cuando todas las bases reales (producción, pruebas, portátil, sobremesa) hayan arrancado una vez con esta versión.

Copias de seguridad: [docs/BACKUP.md](docs/BACKUP.md).

### Concurrencia del stock

Varias tablets pueden tocar el stock a la vez. `StockActual` es un token de concurrencia de EF y las operaciones de leer → modificar → guardar (venta, recepción, ajuste) se repiten con datos frescos si otra se adelantó (`ReintentoConcurrencia`). También se reintentan los bloqueos `database is locked` de SQLite, que solo admite un escritor a la vez. Sin esto, 20 ventas simultáneas dejaban el stock en 16 en vez de 0.

---

## Pruebas

```bash
dotnet test
```

190 pruebas (a fecha de esta versión) organizadas por capa:

| Capa | Enfoque |
|------|---------|
| Validador | Cada regla de negocio (nombre obligatorio, stock no negativo, coherencia entre stock actual y máximo…). |
| Servicio | Lógica aislada con mocks (Moq): valida antes de persistir y propaga los errores correctos. `InformeService` se prueba con fecha fija y `UsuarioService` con Identity real. |
| Repositorio | CRUD completo contra una base SQLite temporal, aislada por test, incluidas las operaciones simultáneas sobre el stock. |
| Infraestructura | `DatabaseInitializer` (base nueva, base antigua con datos, esquema de migraciones frente al modelo) y reintentos de concurrencia. |

No hay pruebas automáticas de la interfaz: se comprueba a mano en el navegador.

---

## Flujo de trabajo (Git)

- Todo el trabajo va a la rama `dev` o a ramas `feature/*`, `fix/*`… que parten de ella, con commits atómicos (un propósito por commit).
- Para llevar algo a `main` se abre un **Pull Request** y se espera aprobación; nunca un push directo.
- La Raspberry Pi (servicio `cafestock.service`) sigue siempre a `main`: `git pull` + `dotnet publish -c Release` + reinicio del servicio.

---

## Roadmap

La aplicación nace para resolver la compra semanal del supermercado, pero su objetivo a largo plazo es agilizar **todo** el proceso de reabastecimiento de la cafetería y convertir la información de consumo en un activo útil para la toma de decisiones.

**v2 — Listas por proveedor** · ✅ hecho

El mayor cuello de botella no era contar el stock, sino coordinar *cuándo* se cuenta y *cuándo* se pide: los pedidos se resolvían por teléfono en momentos que rara vez convenían a las dos partes. Los productos se agrupan por proveedor, con una lista de reposición independiente para cada uno: cada persona actúa cuando le viene bien y se elimina la llamada como punto de coordinación.

**v3 — Histórico y análisis de gasto** · ✅ hecho

Registro de cada compra con fecha y precio real, confirmación post-compra producto a producto, y los informes de gasto, precios, presupuesto y valor del inventario.

**v4 — Distribución y roles** · parcialmente hecho

- ✅ Envío de la lista de la compra por correo.
- ✅ Roles con permisos diferenciados (Trabajador, Encargado, Admin).
- ⬜ Compartir la lista por WhatsApp (texto vía enlace `wa.me`).

**Despliegue** · ✅ en una Raspberry Pi (servicio systemd), con acceso permanente desde cualquier dispositivo de la red.

### Pendiente

- **Ocultar precios al personal.** Hoy los trabajadores ven la columna Precio en la página Stock, y en Recepcionar introducen el precio real de cada compra. Con los roles ya implementados se puede esconder la columna de Stock a quien no sea Encargado o Admin; en Recepcionar hay que decidir antes quién anota el precio.
- Compartir la lista por WhatsApp.
- Revisión de la interfaz (estilos repetidos dentro de las páginas, centralizarlos en el tema).

---

## Autor

Proyecto desarrollado por Cristian como parte de una solución a un problema real en el día a día de una cafetería.
