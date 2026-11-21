# Mascota de CafeStock — prompts para un león idéntico en todas las poses

**Objetivo:** que el león sea **exactamente el mismo personaje** en las 10 imágenes de `CafeStock.Blazor/wwwroot/images/mascot/`, y que solo cambien la **postura** y el **objeto** que lleva. La referencia es el de la pantalla de Inicio, `mascot-saludo.png`.

## 1. Qué falla ahora (comparado con `mascot-saludo.png`)

| Rasgo | En el de Inicio (el bueno) | En los demás |
|---|---|---|
| **Expresión** | Alegre y abierta: cejas suaves, ojos grandes y redondos, media sonrisa | Cansado o triste: cejas fruncidas, ojos entrecerrados, mirada caída |
| **Cara** | Redonda, hocico crema ancho | Más alargada y angulosa, la nariz y el hocico cambian |
| **Melena** | Voluminosa, ondulada, ámbar dorado, limpia | Más enmarañada y más oscura o roja; el volumen cambia |
| **Postura de la cabeza** | De frente, mirando a cámara | En tablet, recepción y producto, de perfil o muy girada |
| **Delantal** | Azul marino de cuero, limpio, con "LEONES BARISTAS" nítido | Más oscuro y sucio; el texto cambia de una imagen a otra y en tres sale ilegible |
| **Acabado** | Render 3D limpio, luz suave | Textura "gastada", sombras duras, aspecto envejecido |
| **Cuerpo** | Proporciones amables, de pie y de frente | Proporciones y ángulo distintos en cada una |

El texto del delantal es el síntoma más claro: **las IA no saben escribir texto de forma fiable**. Hay que tratarlo aparte (ver la sección 5).

## 2. La "biblia" del personaje (se copia en TODOS los prompts)

Es la descripción del león de Inicio. No se cambia nunca; en cada pose solo cambia el bloque de **POSE**.

```text
CHARACTER (identical in every image, do not change anything):
A friendly adult male lion mascot, stylized 3D animated-film look (soft Pixar-like render),
standing upright on two legs, full body, facing the camera.
FACE: big round warm amber-brown eyes with a bright highlight, thick dark-brown soft eyebrows
gently arched (kind, cheerful, relaxed expression - never angry, tired or sad), wide cream-colored
muzzle, small pinkish-brown triangular nose, closed mouth with a soft friendly smile, fine white
whiskers, small round ears with pink inner ear peeking out of the mane.
MANE: big, voluminous, wavy golden-amber mane with lighter copper highlights, framing the whole
face and falling over the shoulders; clean, soft, well-groomed (not matted, not scruffy);
cream chest ruff below the chin.
BODY: honey-golden fur, soft and clean, friendly proportions (large head, sturdy torso, short
legs), bare paws with brown-pink pads and short dark claws, a tail with a tuft behind him.
OUTFIT: fitted short-sleeve black crew-neck t-shirt; navy-blue denim-leather barista apron with
a bib, brown leather straps with silver metal buckles and rivets, a large front pocket with
visible stitching on the lower half; a small stainless-steel milk pitcher at his side.
APRON LOGO: gold embroidered text "LEONES BARISTAS" on two lines, centered on the chest, with a
small gold lion-head emblem below it. Exactly this text, nothing else.
STYLE: clean studio lighting from the front-left, soft shadows, warm saturated colors, detailed
but clean fur, plain transparent or pure white background, no cast shadow on the floor.
FRAMING: vertical full-body, aspect ratio 0.606 (480x792), character centered, head in the upper
third, feet fully visible with a small margin, character fills about 90% of the image height.
```

## 3. Método recomendado (el que de verdad da consistencia)

**No generes cada pose desde cero.** Cada imagen nueva debe **editar** la de Inicio:

1. Abre tu generador con soporte de imagen de referencia (ChatGPT/GPT-Image, Gemini, Midjourney con `--cref`, Leonardo, Ideogram…).
2. **Adjunta `mascot-saludo.png`** como referencia.
3. Pega el prompt de edición de abajo con la pose que quieras.
4. Genera 4 variantes, elige la más parecida a la de Inicio y compárala (sección 6).

### Prompt de edición (para generadores con imagen adjunta)

```text
Use the attached image as the EXACT character reference. Keep this same lion: identical face,
eyes, eyebrows, muzzle, mane shape and color, fur color, black t-shirt, navy apron with its
buckles and pocket, body proportions, 3D render style, lighting and framing. Do NOT redesign
the character in any way.
Change ONLY the pose and the prop, as follows:
POSE: <aquí la pose de la sección 4>
Keep the same expression family: cheerful, kind, relaxed (never sad, angry or tired).
Keep the apron logo exactly as in the reference ("LEONES BARISTAS" + small lion emblem), or leave
the chest area untouched; never invent other text.
Output: vertical full body, aspect ratio 0.606, plain white or transparent background, no floor shadow.
```

### Prompt de personaje completo (si tu generador NO admite imagen de referencia)

```text
<pega aquí el bloque CHARACTER de la sección 2>
POSE: <aquí la pose de la sección 4>
```

### Prompt negativo (para generadores que lo admiten)

```text
different face, different lion, sad expression, tired eyes, narrowed eyes, angry eyebrows,
scruffy matted fur, dark reddish mane, dirty worn clothing, faded apron, different outfit,
garbled text, extra fingers, extra limbs, cropped feet, side profile, three-quarter body turn,
background scenery, floor shadow, 2D flat, anime, photo-realistic photograph, low quality
```

## 4. Las 10 poses (solo cambia este bloque `POSE:`)

La columna "Dónde sale" es la pantalla donde la mascota enseña cada pose (según `MascotMessageProvider`).

| Archivo | Dónde sale | `POSE:` para pegar en el prompt |
|---|---|---|
| `mascot-saludo.png` | **Inicio** | *Ya hecha. Es la referencia: no se regenera.* Saludando con la pata izquierda levantada, jarra de leche en la otra. |
| `mascot-celebracion.png` | Venta de café | Both arms raised in joy, a big open happy smile, eyes bright and cheerful, holding the stainless-steel milk pitcher high in one paw like a toast. |
| `mascot-senalando.png` | (reserva / ayuda) | Right arm extended pointing with the index finger toward the viewer's left, friendly smile, body and head still facing the camera. |
| `mascot-pensativo.png` | (reserva / ayuda) | One paw resting on the chin, head slightly tilted, eyebrows a little raised, curious thoughtful look, still facing the camera. |
| `mascot-albaranes.png` | Facturas | Holding a clipboard with a stack of delivery notes against his chest with one paw, a pen in the other paw, smiling at the camera. |
| `mascot-caja.png` | Stock | Holding a plain brown cardboard shipping box in both paws at chest height, smiling at the camera. The box shows only a small lion emblem and no text. |
| `mascot-telefono.png` | Proveedores | A smartphone held to his ear with one paw, the other paw open and gesturing as if explaining, friendly smile, head facing the camera. |
| `mascot-producto.png` | Catálogo | Holding up a kraft-paper coffee bag in one paw towards the camera, proud smile. The bag has only a small lion emblem and no readable text. |
| `mascot-recepcionar.png` | Recepcionar | One arm carrying an open cardboard box with coffee bags inside, the other paw holding a small tablet with a tick on the screen, smiling at the camera. |
| `mascot-tablet.png` | Informes | Holding a rugged tablet in one paw while tapping the screen with the index finger of the other paw; head still facing the camera, eyes looking at it only slightly. |

> **Regla de oro:** si en una pose el león tiene que mirar el objeto, gira solo los ojos, no la cabeza ni el cuerpo. Ahí es donde se pierde el parecido.

## 5. El texto del delantal: no lo dejes en manos de la IA

Cualquier generador va a deformar "LEONES BARISTAS" en alguna imagen. Soluciones, de mejor a peor:

1. **Recomendado:** genera la pose con el delantal **sin texto nítido** y superpón después el logo real, recortado de `mascot-saludo.png` (un editor como Photopea, Photoshop o GIMP vale).
2. **Inpainting:** selecciona solo el pecho del delantal y pide que se reproduzca el recorte de la referencia, sin cambiar nada más.
3. **Último recurso:** repetir la generación hasta que salga bien, y rechazar cualquier variante con texto distinto al de Inicio.

Las cajas y bolsas llevan **solo el emblema del león**, sin texto: así no hay nada que corregir.

## 6. Lista de comprobación antes de aceptar una imagen

Ponla al lado de `mascot-saludo.png` al 50 % de tamaño. Debe cumplir los 10 puntos:

1. [ ] Misma cara: ojos grandes y redondos, cejas suaves, hocico crema ancho.
2. [ ] Expresión alegre y amable (sin ceño ni cansancio).
3. [ ] Misma melena: volumen, color ámbar dorado y forma ondulada.
4. [ ] Mismo color de pelaje, limpio.
5. [ ] Misma camiseta negra y mismo delantal azul marino (mismo tono, sin envejecer).
6. [ ] Mismas correas marrones con hebillas plateadas y mismo bolsillo.
7. [ ] Texto del delantal idéntico al de Inicio.
8. [ ] Misma altura relativa de la cabeza y mismas proporciones.
9. [ ] Cuerpo y cabeza de frente (solo cambian brazos y objeto).
10. [ ] Mismo encuadre, fondo transparente y pies completos.

## 7. Formato final y sustitución

- Genera en grande (mínimo **1440 × 2376 px**) y reduce a **480 × 792 px** (proporción 0,606, la misma que usa la página).
- Quita el fondo y exporta **PNG con transparencia**.
- Comprime sin perder calidad visible (TinyPNG o `pngquant`): los actuales pesan entre 85 y 120 KB.
- **Conserva los mismos nombres de fichero** y sustitúyelos en `CafeStock.Blazor/wwwroot/images/mascot/`. No hay que tocar código: la app ya los carga por nombre.
- Si guardas las imágenes originales sin comprimir, déjalas fuera de `wwwroot` (la carpeta se publica entera).

## 8. El PNG de referencia tenía un recuadro incrustado (ya corregido)
`mascot-saludo.png` traía un botón de "Google Lens" **incrustado en los píxeles** (esquina inferior derecha), seguramente de una captura de pantalla. Lo he borrado de la imagen, sin tocar al león, y es el único de los diez archivos que lo tenía. Si algún día regeneras esa imagen, comprueba que no vuelva a venir con ningún botón ni marca de agua.

## 9. Por si quieres ir un paso más allá
Si vas a generar muchas poses más adelante, lo más sólido es crear primero una **hoja de personaje** a partir de la imagen de Inicio (de frente, de tres cuartos, de lado y de espaldas, con cuatro expresiones: neutra, contento, sorprendido y pensando) y usarla como referencia de las demás. Con Stable Diffusion, un **LoRA entrenado con esas 15-20 imágenes** consigue la máxima fidelidad.
