# CafeStock — Contexto del proyecto

Sistema de gestión de stock, proveedores y compras para una cafetería real. .NET 10, Blazor Server, EF Core + SQLite, MudBlazor, Serilog, CSharpFunctionalExtensions (Result/DomainError).

## Arquitectura
Modelo → Entity → Mapper → Repository → Validador → Service → Blazor (Components/Pages).
Sin excepciones para errores de negocio — todo pasa por Result<T, DomainError>.

## Paleta visual (usar siempre estos colores exactos, nunca inventar otros)
- Primary: #6D4C41
- Dark: #3E2723
- Accent: #C17817
- Fondo cálido: #F5EFE6 (nunca blanco puro FFFFFF como fondo de página)

## Reglas de dominio que NO se rompen
- Nunca comparar por texto para lógica de negocio (ej. `Nombre == "Alcampo"`) — usar un campo booleano/enum explícito (ver EsSupermercadoGenerico como ejemplo ya aplicado).
- Los precios reales de cada compra viven en LineaCompra.PrecioUnitario (histórico), NO en Producto.PrecioUnitario (que es un precio de referencia fijo, editable a mano, sin relación con el histórico). No confundir ni mezclar los dos.
- Relaciones FK opcionales necesitan DeleteBehavior.SetNull explícito en Fluent API.
- Enums guardados como int en SQLite: valores nuevos siempre al final, nunca insertados en medio.
- Al añadir un campo nuevo a una entidad: revisar TANTO CreateAsync COMO UpdateAsync — UpdateAsync a veces actualiza campo a campo a mano y es fácil olvidar uno (ya ha pasado más de una vez).

## Bugs técnicos ya resueltos, no los repitas
- MudBlazor con interactividad "por página" causa "Missing MudPopoverProvider" — la app usa @rendermode InteractiveServer GLOBAL en <Routes/>, nunca por página individual.
- Blazor Server prerenderiza antes del circuito interactivo — cualquier lógica que dependa de JS/circuito (diálogos, etc.) debe vivir en OnAfterRenderAsync(firstRender), no en OnInitializedAsync.
- Consultas LINQ con evaluación diferida que capturan una referencia a un campo del componente pueden lanzar NullReferenceException si ese campo cambia a null antes de recorrerse (ej. MudTable desmontándose). Capturar el valor necesario en una variable local y usar .ToList() para materializar antes de que el estado pueda cambiar.

## Disciplina de trabajo (obligatoria, sin excepciones)
- Verificación en tres capas antes de dar CUALQUIER tarea por terminada: 1) dotnet build sin errores, 2) dotnet test en verde, 3) confirmación EXPLÍCITA del usuario de haberlo probado funcionando en el navegador. Nunca asumir "esto ya debería funcionar" sin el paso 3.
- Antes de decir que un commit está hecho: mostrar el hash real con git log -1 --oneline. Un resumen convincente no es lo mismo que el dato confirmado — no dar nada por comiteado sin el hash.
- Commits atómicos: un propósito por commit. Si el trabajo mezcla cosas sin relación, separarlo en varios commits antes de hacer push, nunca forzarlo en uno solo por comodidad.
- appsettings.json y appsettings.Development.json NUNCA se leen, muestran ni imprimen en la terminal — contienen credenciales reales. Si hace falta copiarlos entre carpetas, usar Copy-Item verificando solo por metadatos (tamaño, Test-Path), nunca Get-Content ni cat.
- Nunca hacer push a main sin confirmación explícita del usuario en ese momento concreto.

## Flujo de Git (dev → PR → main)
Esto es deliberadamente una práctica de cara al curso de DAW del usuario (trabajar como en un equipo grande, donde uno no es quien decide qué llega a main), no solo una preferencia técnica — no simplificarlo "porque total es un solo desarrollador".
- Desarrollo (portátil/sobremesa del usuario): todo el trabajo de código va a la rama `dev` (o una rama `feature/*` partiendo de ella), nunca directo a `main`.
- Para llevar algo de `dev` a `main`: abrir un Pull Request en GitHub, nunca un merge local + push directo — aunque hoy el propio usuario sea quien lo apruebe, el hábito es abrir PR y esperar aprobación explícita suya antes de mergear.
- La Pi (donde vive `cafestock.service`) sigue siempre a `main`, nunca a `dev`: solo hace `git pull origin main` + `dotnet publish -c Release -o publish` + `sudo systemctl restart cafestock.service` cuando algo ya se mergeó. La Pi no es entorno de pruebas — el entorno de pruebas es el portátil/sobremesa, con su propia base de datos local, nunca la `.db` real de producción.
- Si una sesión de Claude Code corre en la Pi, su rol es desplegar `main` (pull + publish + restart) y depurar producción, no desarrollar features nuevas ahí — eso se hace en una sesión aparte contra `dev`.
