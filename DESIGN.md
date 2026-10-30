# DESIGN.md — Lorekeeper UI System: Codex Lithographica (Future Medieval)

> **Source of Truth** para el sistema de diseño de Lorekeeper.
> Un códice leído por una máquina: la solemnidad de los frontispicios litográficos (*The Green Knight*) y la precisión de un terminal (*Newsreader* + *IBM Plex Mono*). Dentro del Future Medieval se sitúa en la rama sobria y tecnológica (referencias: teenage engineering EP–1320 medieval, Dither Boy / Script Slayer), no en la maximalista de blackletter y ornamento denso.

---

## 1. Principios de Diseño

1. **Light-First / Codex Pergamino:** un único fondo de pergamino (`#fcf9f3`) sobre el que se compone todo; la estructura la dan filetes de tinta, no tarjetas. Huye deliberadamente del "gris dashboard corporativo" y de las cajas blancas con sombra.
2. **Trilogía Tipográfica Canónica** (§3):
   - **Display / Monumental:** *Cinzel Decorative* solo para títulos de página (`h1`).
   - **Editorial / Prosa:** *Newsreader* para lectura, subtítulos, etiquetas de formulario y enlaces.
   - **Datos / Máquina:** *IBM Plex Mono* para datos reales (fechas, recuentos, identificadores, localizadores `L. 84`) y para la acción principal. Es la capa "cifras digitales" del EP–1320.
3. **Color Semántico Litográfico:** pimentón para acción, acento y estado afirmativo, nunca verde de SaaS. Hierro azul y oro para distinciones secundarias. La paleta, sola, se lee como el look "crema + serif + terracota" que se asocia a lo generado; la identidad la dan la tipografía, la estructura de códice y la imagen del craft pass (#17).
4. **Marginalia y Foliación con dato:** la foliación y la información contextual aportan un hecho (cuántas campañas, qué campaña, qué localizador). Una línea de folio sin dato es decoración y no se usa.
5. **Sobriedad:** ornamento, textura e ilustración son herramientas opcionales del craft pass (#17), no un requisito de cada pantalla.

---

## 2. Design Tokens

Implementados en `apps/web/src/styles.css` como variables de tema de Tailwind v4 (`@theme` para primitivos, `@theme inline` para semánticos que referencian otros tokens). Cada token `--color-*` genera utilidades (`bg-*`, `text-*`, `border-*`).

| Grupo | Token | Valor | Uso |
| --- | --- | --- | --- |
| Pergamino | `parchment-base` / `-subtle` / `-dim` / `-dark` | `#fcf9f3` / `#f6f3ed` / `#ede9e1` / `#dcdad4` | Fondo, zonas hundidas, estados deshabilitados |
| Tinta | `ink-primary` / `-secondary` / `-tertiary` / `-border` | `#191c1b` / `#424745` / `#717876` / `#d4d0c7` | Texto, texto secundario, bordes de campo |
| Pimentón | `pimenton` / `-deep` / `-light` / `-border` | `#dd4124` / `#b33018` / `#fbeee9` / `rgba(221,65,36,.35)` | Acento grande y foco / acción y texto de acento / tinte / borde |
| Secundarios | `gold-halo`, `iron-blue`, `amber-hidden` | `#c8963e`, `#23395d`, `#b45309` | Ornato, distinciones, asimetría de conocimiento |
| Superficie | `surface`, `surface-dim`, `surface-sunken`, `surface-accent-tint` | pergamino base / subtle / dim / pimentón light | Fondo único; una zona hundida por región que lo necesite |
| Superficie | `surface-raised` | `#ffffff` | Reservado (p. ej. un futuro panel de cita). No es el contenedor por defecto |
| Sobre acento | `on-accent` | pergamino base | Texto sobre pimentón profundo (no blanco puro) |
| Filetes | `border-muted`, `border-strong`, `border-accent` | tinta 14 %, tinta 35 %, pimentón borde | Separadores punteados, filetes de sección |
| Estado | `state-canonical`, `state-hidden`, `state-boundary` | pimentón, ámbar, tinta terciaria | Bloques de evidencia (§4.B) |
| Tracking | `tracking-action`, `-ceremonial`, `-monumental`, `-tightest` | `0.1em`, `0.22em`, `0.35em`, `-0.03em` | Botón principal, folio, rótulos, ajuste óptico |
| Movimiento | `--duration-fast` / `-normal` / `-slow` | 120 / 240 / 400 ms | Solo color y opacidad; anulado con `prefers-reduced-motion` |
| Craft (#17) | `--paper-grain-opacity`, `--dither-illustration-color` | `0.035`, pimentón | Declarados, aún sin aplicar |

No hay sombras de contenedor: la profundidad se expresa con filetes y la zona hundida.

### Combinaciones de contraste verificadas (WCAG 2.2)

| Texto / elemento | Fondo | Ratio | Uso permitido |
| --- | --- | ---: | --- |
| `ink-primary` | pergamino base | 16.34 | Todo |
| `ink-secondary` | pergamino base / dim | 9.01 / 7.82 | Texto secundario a cualquier tamaño |
| `ink-tertiary` | pergamino base | 4.30 | Solo texto grande (≥ 24 px, o 18.66 px en negrita) y bordes de campo (≥ 3:1) |
| `pimenton-deep` | pergamino base / dim | 5.96 / 5.17 | Texto de acento, errores, enlaces, datos mono |
| `pimenton` | pergamino base | 4.12 | Solo texto grande, foco (3 px) y elementos no textuales |
| `on-accent` | `pimenton-deep` | 5.96 | Botón principal |
| `on-accent` | `ink-primary` / `ink-secondary` | 16.34 / 9.01 | Botón principal en hover / active |
| `ink-secondary` | `parchment-dim` | 7.82 | Botón deshabilitado |

---

## 3. Tipografía e integración con Tailwind v4

Fuentes servidas con la aplicación (sin CDN) desde paquetes `@fontsource` versionados en `apps/web/package.json` (licencia SIL OFL 1.1 incluida en cada paquete). Solo el subconjunto `latin`, que cubre el español, y `font-display: swap`.

| Rol | Familia | Pesos / estilos cargados | Token / utilidad | Fallback |
| --- | --- | --- | --- | --- |
| Display (`h1`) | Cinzel Decorative | 700 | `--font-display` / `font-display` | Georgia, Times New Roman, serif |
| Lectura y UI | Newsreader | 400, 400 italic, 600 | `--font-serif` / `font-serif` (fuente por defecto) | Georgia, Times New Roman, serif |
| Datos y acción | IBM Plex Mono | 400, 600 | `--font-mono` / `font-mono` | ui-monospace, Menlo, Consolas, monospace |

Reglas:

- Una familia por rol. Bodoni Moda queda como alternativa de diseño no cargada; no se añade blackletter.
- Cinzel Decorative es casi toda versal: no se aplica `uppercase` ni tracking ceremonial a los títulos, y se adaptan con `text-balance` y `wrap-break-word`.
- Mono solo para datos reales y la acción principal; nunca para etiquetas de formulario ni para enlaces.
- Para añadir un peso, se importa su `latin-<peso>.css` en `styles.css` y se documenta aquí.

---

## 4. Componentes y patrones

### Implementados

| Pieza | Dónde | Contrato |
| --- | --- | --- |
| `action-primary` | `@utility` en `styles.css` | Acción principal de una región (`button`, o `a` solo si la acción navega a un flujo). Mono 600, versales, `tracking-action`, pimentón profundo con `on-accent`; hover tinta, active tinta secundaria; deshabilitado por tres canales (fondo, tinta, cursor) más el atributo `disabled`. Una sola por región. |
| `action-link` | `@utility` | Navegación y acciones secundarias. Newsreader 600, pimentón profundo subrayado; hover tinta; `aria-current="page"` en tinta con subrayado grueso. Área táctil ampliada con `py-2` en las navegaciones. |
| `field-input` | `@utility` | Borde `ink-tertiary` (≥ 3:1), fondo pergamino; `aria-invalid="true"` pasa el borde a pimentón profundo; el error es texto asociado con `aria-describedby`, nunca solo color. Etiqueta visible en Newsreader. |
| `folio` | `@utility` | Línea de foliación: mono, versales, tracking ceremonial, pimentón profundo. Solo con un dato real ("3 campañas"); se omite si no lo hay. Sin separadores `·` ni `◆` de relleno. |
| `codex-row` + `codex-margin` | `@utility` | Retícula de página de códice: columna de marginalia (≈1/4, alineada a la derecha en escritorio) para datos y rúbricas (`h2` de sección, fechas, recuento, navegación de sección) junto a la columna de lectura (≈3/4). En móvil la marginalia pasa al flujo, encima de su contenido. |
| Lenguaje de filetes | `rule-double`, `codex-rule`, punteado | Un solo lenguaje con tres pesos: doble (4 px) bajo el título de página, filete sencillo `border-strong` entre regiones, punteado entre filas. Nunca dos separadores seguidos; nunca tarjeta dentro de tarjeta. |
| `drop-cap` | `@utility` | Capitular en Cinzel pimentón profundo para la entradilla, solo en escritorio (≥ 48 rem); en móvil ocupa demasiado. Una por página como máximo y solo si hay entradilla. |
| Barra superior | `app.component.html` | Wordmark "Lorekeeper" en Cinzel, enlace a Campañas, filete inferior. Única pieza de chrome global. |
| Zona hundida | `bg-surface-sunken` | Una por región cuando agrupa controles (el formulario de creación). |
| `app-page-header` | `shared/ui/page-header` | Cabecera de página en `codex-row`: en el margen, folio opcional con dato o contenido `[pageHeaderMargin]` (p. ej. fecha de creación); en la columna de lectura, único `h1` en Cinzel con `rule-double` y contenido proyectado (entradilla). |
| Workspace | `campaign-workspace-shell` | El `h1` es la campaña con "Creada" + fecha mono en el margen; la navegación Chat / Fuentes (`aria-current`) y "Todas las campañas" forman la rúbrica de margen de la sección. Los estados (cargando, enlace no válido, no encontrada, error recuperable) sustituyen el `h1` por el estado y conservan el enlace a Campañas. |
| Fechas | `formatCampaignDate` | Fecha corta en español y UTC (`29 oct 2026`) dentro de `<time datetime>`, en mono pimentón profundo. |

### Reglas de la marginalia

1. El margen solo lleva datos, localizadores, rúbricas y navegación de sección; nunca decoración sola.
2. Cada nota del margen se alinea con lo que anota (cabecera, fila, párrafo, fragmento) y en el DOM va junto a ese contenido, de modo que el orden de lectura siga siendo correcto.
3. En móvil la marginalia pasa al flujo: la rúbrica encima de su contenido y la cita debajo. Nada queda accesible solo al pasar el ratón.
4. Texto del margen en Newsreader (rúbricas, etiquetas) o Plex Mono (datos), en pimentón profundo o tinta secundaria, siempre por encima de AA.
5. Solo se pinta un hueco cuando su bloque existe. Usos previstos: estado y fecha de cada fuente (04A), localizador de cada fragmento (05A), rúbrica de evidencia y cita de cada párrafo (06).

### Extensiones para el craft pass (#17)

- **Dónde:** viñeta del frontispicio del registro (columna de margen o banda bajo el filete doble) y estados vacíos. En móvil se reduce o se omite. Sin arte por campaña hasta que esa capacidad tenga alcance propio.
- **Formato:** imágenes exportadas (Dither Boy / Script Slayer) a 1 bit con transparencia, PNG o SVG; el ASCII se exporta como imagen, nunca como texto vivo.
- **Color:** `mask-image` con `background: var(--dither-illustration-color)`, para recolorear con tokens sin reexportar.
- **Render:** `image-rendering: pixelated` a escala entera, medidas fijas (sin saltos de layout), `aria-hidden` y `pointer-events: none`.
- **Textura:** una sola familia por página (grano de papel con `--paper-grain-opacity`); el tramado de una imagen pertenece a la imagen.

### Patrones futuros (documentados, no implementados)

Se implementan en el bloque que posee su capacidad, nunca como controles deshabilitados de relleno: `OracleVerdictCard` (06), citas (06), `IngestionDropzone` y `SourceStatusRow` (04A), búsqueda (05A), `MarginaliaColumn`, ilustración de frontispicio y textura (#17). Ver `docs/11-backlog.md`.

#### A. Frontispicio

Cabecera alineada a la columna de lectura: dato en el margen, título Cinzel con filete doble y entradilla con capitular. El craft pass (#17) puede añadir ilustración dither o grabado como acento en el margen o bajo la cabecera, oculta del árbol accesible.

#### B. Veredicto del Oráculo (bloques condicionales según evidencia)
No es obligatorio mostrar los tres bloques en cada respuesta. Se muestran solo los que correspondan a la evidencia recuperada.

1. **Hecho Canónico** (`border-l-2 border-pimenton`): solo hechos que las fuentes acrediten como ocurridos; una nota de preparación no basta. Resaltados de entidad en negrita `text-pimenton-deep`.
2. **Asimetría en Mesa** (`bg-amber-hidden/10 border-l-2 border-amber-hidden`): solo cuando las fuentes acrediten la diferencia entre conocimiento del DJ, jugadores o PJ. La ausencia de una mención no demuestra desconocimiento.
3. **Límite de Información** (`bg-ink-primary/5 border-l-2 border-ink-tertiary`): transparencia epistémica donde las notas no detallan el hecho, sin inventar datos.

#### C. Consulta (entrada del oráculo)
Campo `field-input` con la consulta en Newsreader y una `action-primary`.

#### D. Citas y atribución a archivos
- Nombre de archivo: `font-mono text-sm text-pimenton-deep font-semibold`.
- Fragmento citado: `font-serif italic text-ink-secondary`.
- Localizador: `font-mono text-sm text-ink-secondary` (`L. 84`, `Fol. 14v`), solo si existe (§5).

---

## 5. Evidencia y localizadores reales

- La presentación no determina canon ni conocimiento de los jugadores. Si el estado es desconocido, debe conservarse como tal. Las contradicciones no se convierten en un veredicto definitivo.
- Los ejemplos de campaña, folios y líneas de este documento son ilustrativos, no datos ni localizadores que deban generarse para decorar.
- Mostrar líneas, folios o secciones únicamente si existen en la fuente y se conserva una correspondencia verificable con el fragmento.
- Para Notion: página y encabezado, con enlace cuando esté disponible. Para Markdown: archivo, sección y líneas solo si se conservan. Para texto pegado: título y fragmento; no inventar paginación.
- Las citas abren el fragmento de la fuente de esa campaña. Si una fuente histórica no está disponible, indicarlo; nunca redirigir silenciosamente a otra versión o contenido sin explicarlo.

## 6. Arquitectura de información y estados

Se mantiene el [primer enfoque de workspace e ingesta](docs/12-workspace-and-ingestion-ux.md): selector/creación de campaña y secciones Chat / Fuentes. La estética de oráculo no incorpora funcionalidades nuevas ni cambia el alcance.

Cada flujo define inicial/vacío, carga, éxito, validación, error recuperable y deshabilitado/solo lectura cuando corresponda. Fuentes incluye progreso por documento, fallos parciales, reintento y actualización fallida con versión anterior disponible. El chat sin fuentes enlaza a ingesta; si hay fuentes disponibles puede consultarlas mientras otras se procesan. Los resultados tardíos de otra campaña no aparecen en la activa.

## 7. Responsive

- En escritorio, navegación/selector, contenido y referencias marginales mantienen una jerarquía clara sin competir por anchura.
- En móvil, la navegación puede plegarse pero el selector y Chat / Fuentes siguen accesibles. Referencias y marginalia pasan al flujo de lectura o a un panel accesible; ninguna evidencia queda disponible solo al pasar el ratón.
- Reorganizar filas de fuentes como tarjetas o filas adaptadas, conservando estado y acciones. Evitar desbordamiento horizontal de la página; el contenido que lo requiera dispone de desplazamiento localizado.
- Los títulos monumentales y su tracking se adaptan al espacio; no fuerzan cortes ni reducen el contenido funcional a texto diminuto. El compositor no tapa mensajes, citas o acciones al abrir el teclado.
- Concretar medidas, breakpoints y composición de cada pantalla antes de su slice; este documento fija prioridades y comportamiento.

## 8. Accesibilidad

- HTML semántico, encabezados ordenados, etiquetas y nombres accesibles para controles; navegación completa por teclado y foco visible.
- Gestionar foco en diálogos/paneles, devolverlo al control de origen al cerrar y evitar trampas de teclado. Asociar errores a sus campos.
- Anunciar cambios de estado de ingesta y respuesta de forma moderada, sin leer cada token o actualización irrelevante.
- Estado, canon, asimetría y errores utilizan texto y/o iconos además de color. La textura y marginalia decorativas no interfieren con lectura ni puntero y se excluyen del árbol accesible.
- Verificar contraste de texto, controles y foco en sus combinaciones reales. Los tokens son una base visual, no una garantía de contraste. Si una combinación falla, utilizar una variante de tinta con contraste suficiente y registrar el ajuste semántico sin reinterpretar la paleta.
- Los tamaños de 9–10 px de los ejemplos son referencias estilísticas, no mínimos obligatorios. Metadatos, citas y acciones deben ser legibles, permitir zoom/reflujo y disponer de objetivos táctiles cómodos.
- Respetar reducción de movimiento; no hacer depender ninguna información de animaciones. Mantener alternativas de fuente y lectura usable durante su carga.

## 9. Regla de implementación y decisiones pendientes

Antes de implementar una pantalla, revisar este documento, el flujo UX y la pantalla existente más cercana. Preparar y verificar campos, copy, estados, composición, tokens, componentes, responsive, teclado y movimiento, y pasar `docs/conventions/frontend-design-review.md`.

Copy visible en español, en registro práctico de mesa (ADR-005). La estética monumental es visual; los verbos son llanos ("Crear campaña", no "Consagrar campaña").

Resuelto por #8: carga tipográfica, mapeo a Tailwind v4, contraste verificado, superficies, foliación y primitivas actuales. Pendiente por slice: composición, medidas y breakpoints de cada pantalla futura; textura, ilustración y ajuste óptico pertenecen al craft pass (#17). Light-first no exige modo oscuro en la PoC. La semántica de evidencia y los contratos se cierran según los bloqueos del roadmap.
