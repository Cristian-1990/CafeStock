# CafeStock — ¿Qué nos falta? Investigación del sector y revisión de Informes

**Fecha:** 21-11-2026 · **Alcance:** comparar CafeStock con apps del sector (inventario, compras y costes para cafeterías/restauración) y revisar si la pantalla de Informes es correcta, le falta algo o le sobra algo.

> **Cómo leer esto.** Las funciones de las otras apps salen de sus propias páginas y de comparativas escritas por fabricantes: sirven para saber *qué existe*, no para fiarse de precios ni de promesas. Lo que digo de CafeStock lo he comprobado en el código (`InformeService`, modelos, páginas) y en la app con una copia de los datos reales de la Pi.

---

## 1. Resumen (lo importante en un minuto)

1. **CafeStock ya cubre bien lo básico del sector:** stock con máximo, lista de la compra por proveedor (PDF), recepción con precio real, histórico de precios, facturas adjuntas, presupuesto por proveedor, valor de inventario y 3 roles. Para una cafetería es bastante más de lo que ofrece una hoja de cálculo.
2. **Hay un fallo real en Informes:** el gasto **no suma las compras excepcionales** (los artículos sueltos del supermercado). Gasto del mes, gasto por proveedor, presupuesto vs real… todos se quedan cortos por esa cantidad.
3. **Lo que más falta no es un informe, es un dato:** no se guarda el **historial de movimientos de stock**. Cada recuento *pisa* el stock anterior. Sin ese historial no se puede saber cuánto se consume, cuánto se pierde (mermas) ni si el recuento cuadra con lo que debería haber. Es la función que más separa a CafeStock de las apps del sector.
4. **Hay datos que se recogen y no se usan:** los registros de venta de café (`RegistroCafe`, pantalla Venta) no aparecen en ningún informe.
5. **Informes tiene 15 bloques en una sola página.** Algunos se repiten (concentración % = gasto por proveedor en porcentaje) y otros aportan poco para una cafetería (ticket medio por proveedor). Conviene ordenarlo en pestañas y quitar ruido.
6. **Con los datos reales de la Pi, Informes casi no tiene nada que mostrar:** solo hay **3 compras registradas** (todas de Priégola, la última hace 54 días) y ninguna de Puchero. Antes que añadir informes, hay que **recepcionar de verdad cada entrega** en la app; si no, ningún informe sirve.

---

## 2. Qué tiene CafeStock hoy

| Área | Qué hay |
|---|---|
| Stock | Stock actual y máximo por producto, "por reponer" (= máximo − actual), recuento por proveedor (Realizar stock), unidades por pack |
| Compras | Lista de la compra por proveedor en PDF, orden por pasillo para supermercados, recepción con cantidades y **precio real** por línea, compras excepcionales |
| Proveedores | Contacto, día de reparto (texto libre), historial de compras y facturas |
| Facturas | Método de pago, nº de factura del proveedor, foto/PDF adjunto, notas |
| Ventas | Venta de café con seguimiento individual (quién y cuándo) |
| Informes | 15 bloques (ver sección 4) |
| Seguridad | Login con 3 roles (Trabajador, Encargado, Admin), copias de seguridad |
| Datos | Snapshot diario del valor del inventario, presupuesto por proveedor/mes |

---

## 3. Qué hace el sector

Apps revisadas: **MarketMan**, **WISK** (pensadas para cafeterías/restauración), **Sortly** (inventario genérico y barato), **Square** (con el módulo de inventario de MarketMan), **Toast/xtraCHEF**, **Restaurant365**, **MarginEdge**, y opciones en España (**Gstock**, **Qamarero**, **Cuiner**, **Ágora**, **Glop**).

Funciones que se repiten en casi todas:

| Función | Qué es | ¿Lo tiene CafeStock? |
|---|---|---|
| **Niveles *par* y alertas de stock bajo** | Un mínimo por producto; avisa al llegar y propone el pedido | Parcial: solo tiene máximo; no hay mínimo ni aviso |
| **Pedido sugerido** | Calcula cuánto pedir según par y consumo | Sí, simple (máximo − actual) |
| **Recepción y control de facturas** | Registrar lo que llega y a qué precio | **Sí** |
| **Seguimiento de precios de proveedor** | Histórico y alerta de subidas | Parcial: histórico y PPV; sin alertas |
| **Registro de mermas** | Anotar lo que se tira o rompe, con motivo | **No** |
| **Real vs teórico (varianza)** | Compara lo que *debería* haberse gastado con lo que se gastó; si hay diferencia, hay pérdida | **No** (necesita historial de movimientos) |
| **Escandallos / costes de receta** | Coste de cada café o bocadillo según ingredientes | No |
| **Integración con el TPV** | Descuenta el stock al vender | No (hay un campo preparado: `AplicaConciliacionTpv`) |
| **Lectura de facturas con foto** | Extrae precios de la foto de la factura | No (se adjunta, pero no se lee) |
| **Caducidades / lotes** | Aviso de productos próximos a caducar | No |
| **Exportar a Excel/CSV** | Para la gestoría o análisis | No |
| **Varios locales** | Traspasos y centralización | No hace falta hoy |

**Qué cuenta de verdad para el sector:** la comparación *real vs teórico* y el registro de mermas. Los propios fabricantes insisten en que sin una merma registrada no se distingue la pérdida evitable (rotura, caducidad) de la inexplicable ([Toast](https://pos.toasttab.com/blog/on-the-line/food-cost-variance), [Restaurant365](https://www.restaurant365.com/blog/restaurant-waste-and-variance-reporting/)).

**Cuidado con copiar de más.** MarketMan y WISK están pensadas para cadenas (cientos de integraciones, varios locales, escandallos completos) y rondan los 140–200 € al mes por local ([GetApp/Capterra](https://www.capterra.com/compare/136439-169199/Marketman-Restaurant-Inventory-vs-Sortly-Pro)). CafeStock es para **una** cafetería: no hace falta replicar eso, sino coger lo que más rinde (ver sección 5).

---

## 4. Revisión de Informes

### 4.1 Lo que está bien
- **Los cálculos están probados** (`InformeServiceTest`) y usan el precio **real** de cada línea (`LineaCompra.PrecioUnitario`), como manda el proyecto.
- Los filtros usan fechas de calendario (semana, mes y año **en curso**), no ventanas móviles. Está documentado en el código; solo hay que saber que "Último mes" significa "este mes hasta hoy".
- **Imprescindibles del sector que ya están:** gasto por proveedor, evolución de precio por producto, productos con más peso en el gasto (el 80/20), presupuesto vs real, valor de inventario en el tiempo, frecuencia de compra vs reparto esperado, proveedores sin compra reciente.

### 4.2 Fallos y cosas a corregir

| # | Hallazgo | Gravedad | Qué hacer |
|---|---|---|---|
| 1 | **No suma las compras excepcionales** (`ImporteComprasExcepcionales`). Todos los importes salen solo de las líneas. El gasto real es mayor que el que muestra. | **Alta** | Incluirlo en gasto mensual, por proveedor, presupuesto vs real y método de pago (o mostrarlo como barra aparte) |
| 2 | **Ticket medio** cuenta como 0 € las compras que solo tienen compra excepcional (no tienen líneas) y baja la media. | Media | Excluir esas compras del cálculo, o sumar su importe |
| 3 | La tabla **PPV** lista todos los productos (49 filas) aunque la desviación sea 0,00 €. Es ruido. | Media | Mostrar solo los de desviación distinta de 0 y los 10 mayores |
| 4 | **PPV compara con el precio de referencia**, que se edita a mano y puede estar desactualizado: entonces "se pagó de más" puede ser solo una referencia vieja. | Media | Añadir comparación con la **compra anterior** del mismo producto (más fiable) o avisar de la antigüedad de la referencia |
| 5 | El **valor de inventario** usa stock × precio de referencia, no el último precio pagado. Si las referencias no se actualizan, el valor sale mal. | Media | Usar el precio de la última compra cuando exista |
| 6 | "Gasto: supermercado genérico vs habituales" deja fuera las compras **sin proveedor**. | Baja | Mostrarlas como "Sin proveedor" para que la suma cuadre |
| 7 | Con pocos datos (hoy 3 compras) casi todos los bloques dicen "no hay compras". | Info | No es un fallo del informe; es falta de datos (ver resumen, punto 6) |

### 4.3 Lo que sobra o se repite
- **Concentración de gasto por proveedor (%)**: es el mismo dato que "Gasto por proveedor", en porcentaje. Mejor un interruptor € / % en un solo gráfico.
- **Ticket medio de compra por proveedor**: en una cafetería que pide a pocos proveedores aporta poco. Candidato a quitar o a esconder en el detalle del proveedor.
- **Gasto por método de pago**: solo tiene sentido si la gestoría lo pide; si no, es secundario. Dejarlo en una sección "Contabilidad".
- **Quince bloques seguidos en una sola página** es demasiado para quien solo quiere saber "¿cómo vamos este mes?". Mejor **pestañas**: *Resumen* (gasto del mes, presupuesto vs real, alertas), *Compras y precios*, *Proveedores*, *Inventario*.

### 4.4 Lo que falta (de más a menos útil)
1. **Consumo de café y ventas** — ya se registran en Venta y no se usan: ventas por día y por franja horaria, por tipo de café, por trabajador.
2. **Mermas y diferencias de recuento** — hoy imposible, falta el dato (ver sección 5).
3. **Productos agotados / bajo mínimo** — cuántas veces y cuánto tiempo se ha quedado cada producto sin stock.
4. **Rotación y días de cobertura** — cuántos días dura cada producto (leche, vasos…), para ajustar el máximo.
5. **Subidas de precio** — avisos de "este producto ha subido un X % respecto a la última compra".
6. **Exportar a CSV/Excel** — para la gestoría.

---

## 5. Qué añadir primero (propuesta ordenada)

Pensado para una cafetería, sin pasarse. Orden de mayor a menor rendimiento:

| Prioridad | Qué | Por qué | Esfuerzo |
|---|---|---|---|
| **1** | Corregir el gasto para incluir **compras excepcionales** (hallazgo 1 y 2) | Es un error de importes | Pequeño |
| **2** | **Historial de movimientos de stock** (recuento, recepción, venta, ajuste) | Es la base de todo lo demás: consumo, mermas, diferencias, rotación | Medio |
| **3** | **Registro de mermas** con motivo (caducado, roto, regalado, error) | Lo que más pide el sector; con el punto 2 permite detectar pérdidas | Pequeño–medio |
| **4** | **Stock mínimo y alerta** de bajo stock en Inicio | Hoy solo hay "máximo"; con mínimo se pide antes de quedarse sin nada | Pequeño |
| **5** | Informe de **ventas/consumo de café** | Aprovecha datos que ya existen | Pequeño |
| **6** | Reordenar Informes en pestañas y limpiar ruido (sección 4.3) | Más claro, sin perder nada | Pequeño |
| **7** | **Caducidades** para frescos (leche, nata, yogur) | Menos mermas | Medio |
| **8** | **Exportar a CSV/Excel** | La gestoría lo agradece | Pequeño |
| Más adelante | Comparador de precios entre proveedores; escandallos; integración con TPV; lectura de facturas por foto | Valiosos pero grandes; mejor cuando haya meses de datos | Grande |

**Antes de todo, una condición:** que cada entrega se **recepcione en la app**. Con 3 compras en meses, ningún informe (ni los actuales ni los nuevos) tiene datos para ser útil. Merece la pena hablar con el equipo de cómo hacerlo fácil, por ejemplo con el botón de Recepcionar siempre a mano.

---

## 6. Fuentes
- [MarketMan — Cafe Inventory Management](https://marketman.com/cafe-inventory-management) y [comparativa Capterra MarketMan vs Sortly](https://www.capterra.com/compare/136439-169199/Marketman-Restaurant-Inventory-vs-Sortly-Pro)
- [WISK — sistema de gestión para cafeterías](https://www.wisk.ai/for/coffee-shop-management-system-wisk)
- [Toast — food cost variance](https://pos.toasttab.com/blog/on-the-line/food-cost-variance) y [xtraCHEF Inventory Analytics](https://support.toasttab.com/article/xtraCHEF-Inventory-Analytics)
- [Restaurant365 — waste and variance reporting](https://www.restaurant365.com/blog/restaurant-waste-and-variance-reporting/)
- [MarginEdge — actual vs theoretical](https://www.marginedge.com/blog/a-restaurant-operators-guide-to-actual-vs-theoretical-food-costs-and-usage)
- [Square — Inventory management](https://squareup.com/us/en/point-of-sale/features/inventory-management) y [Square + MarketMan](https://squareup.com/es/ca/press/square-restaurant-inventory-marketman)
- [Gstock (Capterra)](https://www.capterra.es/software/1019904/gstock), [Qamarero — software de gestión](https://qamarero.com/blog/mejores-software-gestion-restaurantes/) y [control de stock en restaurante](https://qamarero.com/blog/control-de-stock-en-restaurante-gestion-y-cocina/)
