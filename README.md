# CafeStock

Sistema de gestión de stock para cafetería. Sustituye el control manual en papel del inventario: registra los productos, calcula automáticamente lo que hay que reponer y genera la lista de la compra en PDF.

Nace de una necesidad real de trabajo — la gestión del stock de una cafetería con lápiz y papel — y se desarrolla siguiendo principios de arquitectura limpia y separación de responsabilidades.

---

## Qué hace

- Gestión completa de productos (crear, editar, eliminar, consultar).
- Control de stock actual y stock máximo (el nivel objetivo de cada producto).
- Cálculo automático de la cantidad a comprar de cada producto (`StockMáximo − StockActual`).
- Generación de la lista de la compra: solo los productos por debajo de su nivel objetivo.
- Exportación de la lista a PDF descargable.

---

## Arquitectura

El proyecto sigue una separación estricta en capas, con el backend independiente de la interfaz:

```
Program (UI Blazor)  →  Service (lógica de negocio)  →  Validator (reglas)  →  Repository (persistencia)
```

La solución se divide en tres proyectos:

| Proyecto | Responsabilidad |
|----------|-----------------|
| `CafeStock.Back` | Dominio, validación, persistencia y lógica de negocio. Sin dependencias de UI. |
| `CafeStock.Blazor` | Interfaz web (Blazor Server + MudBlazor) y generación de PDF. |
| `CafeStock.Tests` | Pruebas unitarias del backend. |


---

## Pruebas

El backend está cubierto por 17 pruebas unitarias organizadas por capa:

| Capa | Enfoque |
|------|---------|
| Validador | Verifica cada regla de negocio (nombre obligatorio, stock no negativo, coherencia entre stock actual y máximo). |
| Servicio | Aísla la lógica con mocks (Moq): comprueba que valida antes de persistir y que propaga los errores correctos. |
| Repositorio | CRUD completo contra una base de datos SQLite temporal, aislada por test. |


## Roadmap

La aplicación nace para resolver la compra semanal del supermercado, pero su objetivo a largo plazo es agilizar **todo** el proceso de reabastecimiento de la cafetería y convertir la información de consumo en un activo útil para la toma de decisiones.

**v2 — Listas por proveedor**

El mayor cuello de botella actual no es contar el stock, sino coordinar *cuándo* se cuenta y *cuándo* se pide: los pedidos se resuelven por teléfono en momentos que rara vez convienen a las dos partes a la vez. El objetivo es desacoplar ambas tareas.

- Agrupar los productos por proveedor (el de la leche: entera, sin lactosa, nata…; el del pan: mollete, focaccia…), con una lista de reposición independiente para cada uno.
- Cada persona actúa cuando le viene bien: un empleado apunta las existencias al entrar por la mañana y la app calcula automáticamente lo que falta para completar el cupo; el responsable consulta a la hora que prefiera qué queda y qué hay que pedir.
- Se elimina la llamada telefónica como punto de coordinación.

**v3 — Histórico y análisis de gasto**

- Registro de cada compra con fecha y precio real (entidades `Compra` y `LineaCompra`).
- Confirmación post-compra: al volver del proveedor, la app pregunta producto a producto la cantidad realmente comprada y el precio, ajustándolo solo si ha cambiado.
- Métricas y gráficas a partir de esos datos: precio medio, precio máximo y evolución por producto, gasto mensual, gasto por proveedor y productos de mayor rotación — información relevante para controlar y reducir el gasto.

**v4 — Distribución y roles**

- Envío de la lista de la compra por email.
- Compartir la lista por WhatsApp (texto vía enlace `wa.me`).
- Sistema de roles (gestor / usuario) con permisos diferenciados.

**Despliegue**

- Publicación en VPS para acceso permanente desde cualquier dispositivo, sin depender de una máquina concreta encendida.

---

## Autor

Proyecto desarrollado por Cristian como parte de una solución a un problema real en el día a día de una cafetería 
