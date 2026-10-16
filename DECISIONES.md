# DECISIONES.md — Decisiones de Diseño, Investigación y Arquitectura

Este documento responde a los puntos solicitados en la **Sección 8** de la Práctica Programada 1 y documenta las decisiones técnicas adoptadas en el sistema **Ticket.API**.

---

## 1. Variante Personal y Estudiante
- **Estudiante:** Andres Ignacio Martinez Perez
- **Variante:** `V = 3` (calculado mediante `(último dígito del carnet) % 5 = 3`)
- **Tamaño máximo de página:** `40` (por defecto `10`).
- **Formato del código del ticket:** `HD-NNNN` (prefijo `HD-` seguido de una secuencia correlativa de 4 dígitos, ej. `HD-0001`).
- **Regla de negocio especial (responde HTTP 409 al violarse):** *"Un agente no puede tener más de 5 tickets `EnProceso` al mismo tiempo."*

---

## 2. El `ClienteId` enviado al crear un ticket no existe: ¿responde 400, 404 o 422? Justificación frente a las alternativas

### Decisión elegida en el proyecto: **HTTP 400 Bad Request** (mediante `TicketValidationException` traducida a `ProblemDetails`)

### Comparación frente a las alternativas:
1. **¿Por qué NO `404 Not Found`?**
   Según la semántica de **RFC 9110 (Sección 15.5.5)**, el código `404 Not Found` indica que el servidor no encontró el **recurso objetivo de la URI** solicitada (en este caso, `POST /api/tickets`). Si un endpoint `POST /api/tickets` devuelve `404`, un cliente HTTP o un proxy intermedio interpretará erróneamente que la ruta `/api/tickets` no existe o no está disponible, cuando en realidad lo que falló es una referencia (`ClienteId`) enviada dentro del payload JSON. Usar `404` sería correcto únicamente si la ruta fuera jerárquica sobre el cliente, como `POST /api/clientes/{clienteId}/tickets`.
2. **¿Por qué `400 Bad Request` vs `422 Unprocessable Content`?**
   - **`422 Unprocessable Content` (RFC 9110, Sección 15.5.21):** Indica que el servidor entiende el tipo de contenido (`application/json`) y la sintaxis del JSON es válida, pero no puede procesar las instrucciones semánticas contenidas en él (por ejemplo, una llave foránea `ClienteId` que no existe en `ClienteDA`). Es una alternativa semánticamente muy precisa para errores de validación de dominio.
   - **`400 Bad Request` (RFC 9110, Sección 15.5.1):** Representa cualquier solicitud que el servidor no puede o no procesará debido a algo que se percibe como un error del cliente (datos de entrada inválidos).
   - **Justificación de elegir `400 Bad Request` en este contrato:** El contrato HTTP de la **Sección 5** y la **Sección 6.1** especifica que `POST /api/tickets` responde `201` o `400 si el body es inválido`, y que *"Todos los errores 400, 404 y 409 del API deben responder con `application/problem+json`"*. Por consistencia con el resto de las validaciones de entrada del API y el contrato de pruebas de la práctica, se unificó la respuesta de `ClienteId` inexistente en `400 Bad Request` con un `ProblemDetails` descriptivo indicando qué `ClienteId` falló.

---

## 3. Un id como `abc` en `/api/tickets/abc`: ¿por qué la restricción de ruta responde 404 mientras que un body inválido responde 400? ¿Cuándo usaría cada mecanismo?

### ¿Por qué la restricción de ruta (`{id:int:min(1)}` o `{codigo:codigoTicket}`) responde `404`?
En ASP.NET Core, el middleware de **enrutamiento (`EndpointRoutingMiddleware`)** evalúa las restricciones de ruta (`IRouteConstraint`) **antes** de seleccionar qué controlador o acción ejecutar.
- Las restricciones de ruta forman parte de la **definición del patrón de la URI** (sirven para desambiguar rutas, por ejemplo entre `/api/tickets/{id:int}` y `/api/tickets/resumen`).
- Cuando un cliente solicita `GET /api/tickets/abc` o `GET /api/tickets/codigo/INVALIDO-99`, el motor de enrutamiento determina que **ninguna plantilla de ruta coincide con esa URL**. Como para el servidor no existe ningún endpoint registrado para ese patrón de URI, responde inmediatamente con **`404 Not Found`** sin siquiera instanciar el controlador ni ejecutar el *Model Binding*.

### ¿Por qué un body inválido responde `400`?
Cuando el cliente envía un `POST /api/tickets` o `PATCH /api/tickets/1/estado` con un JSON malformado o con campos inválidos:
- La URI **sí coincidió** exitosamente con una ruta existente (el recurso existe).
- El fallo ocurre en la etapa posterior de **Model Binding y Validación de Modelo** (facilitada automáticamente por el atributo `[ApiController]`). Como el cliente se dirigió a un endpoint válido pero envió una representación o parámetros defectuosos, corresponde **`400 Bad Request`**.

### ¿Cuándo usar cada mecanismo?
- **Restricciones de ruta (`IRouteConstraint`):**
  - Cuando se necesita **desambiguar rutas** que comparten estructura de segmentos.
  - Cuando el identificador en la URL tiene un formato estructural estricto que define la identidad misma del recurso en la URI (enteros positivos `:int:min(1)`, GUIDs `:guid`, o el formato `^HD-\d{4}$` en `CodigoTicketRouteConstraint`).
  - *No* debe usarse para validar reglas de negocio cambiantes, sino para el patrón sintáctico del segmento de URL.
- **Validación de modelo / Body (`400 Bad Request`):**
  - Para validar payloads JSON (`[FromBody]`), parámetros de filtrado y paginación en el query string (`[FromQuery]`), rangos de fechas (`Desde <= Hasta`), campos obligatorios y reglas de entrada del usuario donde el cliente necesita recibir retroalimentación detallada en `application/problem+json` sobre qué campo corregir.

---

## 4. ¿Cómo comunica `Flujo` un error de regla de negocio al `API` sin depender de HTTP? Comparación de alternativas

La regla de arquitectura establece que el proyecto `Flujo` no puede referenciar `Microsoft.AspNetCore.*` ni conocer códigos de estado HTTP. Se evaluaron dos alternativas principales:

### Alternativa A: Excepciones personalizadas de dominio *(Elegida)*
Definir jerarquías de excepciones semánticas en la capa `Abstracciones` ([`TicketExcepciones.cs`](file:///home/mamalona/RiderProjects/Ticket.API/Abstracciones/Excepciones/TicketExcepciones.cs)):
- `TicketNotFoundException`: el recurso buscado no existe (traducido por `API` a `404 Not Found`).
- `TicketConflictException`: violación de transición de estado o límite de 5 tickets en proceso por agente (traducido por `API` a `409 Conflict`).
- `TicketValidationException`: parámetros o datos de entrada inválidos (traducido por `API` a `400 Bad Request`).

### Alternativa B: Objeto de Resultado (*Result Pattern* / `Result<T>`)
Definir una clase genérica `Resultado<T>` en `Abstracciones` con propiedades como `bool Exitoso`, `T? Datos`, `TipoError Error` (enum `NoEncontrado`, `ConflictoNegocio`, `Validacion`) y `string Mensaje`. Los métodos de `ITicketFlujo` devolverían `Resultado<Ticket>` en lugar de `Ticket`.

### Comparación y Costo de la Decisión
| Criterio | Excepciones Personalizadas (Elegida) | Objeto de Resultado (`Result<T>`) |
| :--- | :--- | :--- |
| **Claridad en firmas (`ITicketFlujo`)** | Firmas limpias (`Ticket ObtenerTicket(int id)`). El camino feliz (*happy path*) es directo. | Todas las firmas deben envolverse en `Resultado<T>`, agregando verbosidad al contrato. |
| **Seguridad en tiempo de compilación** | El compilador de C# no obliga a capturar excepciones (*unchecked exceptions*); requiere disciplina en el Controller/Minimal API. | Obliga al llamador a inspeccionar `resultado.Exitoso` antes de acceder al valor. |
| **Costo en rendimiento (Performance)** | **Mayor costo en caso de error:** lanzar una excepción en .NET captura el *stack trace* y desenrolla la pila (*stack unwinding*). | **Menor costo:** es una simple asignación de objeto en memoria sin captura de *stack trace*. |

- **Justificación de la elección:** Se eligió **Excepciones Personalizadas de Dominio** porque mantiene limpias las interfaces de `ITicketFlujo`, separa nítidamente el flujo normal del flujo de error y permite que tanto `TicketController` como los endpoints `MapGet`/`MapPost` en `Program.cs` traduzcan cada tipo de excepción de dominio a `ProblemDetails` (RFC 9457). El costo de rendimiento del *stack unwinding* es aceptable en este contexto porque las violaciones de reglas de negocio no ocurren en bucles intensivos de alta frecuencia.

---

## 5. Paginación: límite máximo, página fuera de rango y paginación por cursor

### ¿Por qué limitar el tamaño máximo de página (`MaxTamanoPagina = 40` en Variante 3)?
1. **Protección contra denegación de servicio (DoS) y agotamiento de recursos:** Si no se limita `tamano`, un cliente malintencionado o con un bug podría solicitar `?tamano=10000000`, forzando al servidor a cargar, serializar en JSON y transmitir megabytes de memoria y CPU en una sola petición.
2. **Previsibilidad de latencia (SLA):** Acotar el tamaño máximo garantiza tiempos de respuesta acotados tanto en la capa de datos como en la red.

### ¿Qué devuelve una página fuera de rango (ej. `pagina=99` cuando solo hay 25 tickets) y por qué?
- **Respuesta:** Devuelve **`200 OK`** con un arreglo JSON vacío `[]` y los headers `X-Total-Count: 25`, `X-Page: 99`, `X-Page-Size: 10`.
- **¿Por qué `200 OK` con `[]` y no `404` ni `400`?**
  - Los parámetros `pagina=99` y `tamano=10` son sintáctica y semánticamente válidos (enteros positivos dentro del rango permitido), por lo que **no es un `400 Bad Request`**.
  - La colección `/api/tickets` sí existe como recurso, por lo que **no es un `404 Not Found`**. Una consulta paginada o filtrada que no tiene elementos en ese desplazamiento es equivalente a un `SELECT ... OFFSET 980 LIMIT 10`: el resultado matemáticamente correcto de un conjunto sin coincidencias en ese segmento es el **conjunto vacío `[]`**. Además, como los datos son dinámicos (pueden cerrarse o filtrarse tickets entre peticiones), devolver `200 []` con `X-Total-Count` permite que la interfaz gráfica sepa exactamente cuántos registros totales existen sin romper el flujo.

### ¿Qué es la paginación por cursor (*Cursor-based / Keyset Pagination*) y por qué no se usó aquí?
- **¿Qué es?** En lugar de usar `pagina` y `tamano` (`Skip`/`Take` u `OFFSET`/`LIMIT`), el cliente envía una referencia opaca al último elemento visto (el *cursor*, por ejemplo `afterId=15` o `afterFecha=2026-10-01_id=15`) y el servidor ejecuta un filtro directo (`WHERE FechaCreacion < @cursorFecha LIMIT @tamano`).
- **Ventajas del cursor:** Evita el problema de *drift* (elementos duplicados u omitidos cuando se insertan nuevos registros mientras el usuario navega páginas) y tiene rendimiento $O(1)$ con índices en bases de datos grandes, mientras que `OFFSET` grande degrada a $O(N)$ porque debe escanear y descartar filas.
- **¿Por qué no se usó aquí?**
  1. El contrato HTTP de la práctica exige explícitamente paginación por desplazamiento (`pagina`, `tamano`, headers `X-Total-Count`, `X-Page`, `X-Page-Size`).
  2. Para una mesa de ayuda con navegación en tabla administrativa donde el usuario necesita ver el total de páginas y saltar entre páginas con filtros variados en una colección en memoria, la paginación `Skip`/`Take` (*offset-based*) es más simple y adecuada.

---

## 6. ETag: ¿qué problema resuelve? ¿Fuerte o débil, y cómo lo calculó? ¿Qué situación no cubre?

### ¿Qué problema resuelve?
1. **Ahorro de ancho de banda y caché condicional (GET):** Permite que el cliente almacene en caché la representación de un ticket (`GET /api/tickets/{id}`) y en futuras consultas envíe el header `If-None-Match: "<etag>"`. Si el ticket no ha cambiado, el servidor responde **`304 Not Modified` sin cuerpo HTTP**, ahorrando serialización y transferencia de red.
2. *(Complementariamente en HTTP)* También sirve para control de concurrencia optimista mediante `If-Match` (evitar el problema de *lost updates*).

### ¿Fuerte o débil, y cómo se calculó?
- **Tipo elegido:** **ETag Fuerte** (entre comillas dobles `"<hash>"`, sin el prefijo `W/`).
  - Un *ETag fuerte* garantiza que si dos representaciones tienen el mismo ETag, su contenido es equivalente byte a byte en todos sus campos relevantes; un *ETag débil* (`W/"..."`) solo indica equivalencia semántica aproximada.
- **Cómo se calculó ([`TicketController.CalcularETag`](file:///home/mamalona/RiderProjects/Ticket.API/Ticket.API/TicketController.cs#L167-L174)):**
  Se construye una cadena determinista concatenando los campos que definen el estado y versión del recurso:
  `"{Id}|{Codigo}|{Estado}|{Prioridad}|{AgenteId}|{Reaperturas}|{FechaActualizacion:O}"`
  Sobre los bytes UTF-8 de esta cadena se calcula un hash **MD5** en hexadecimal minúscula envuelto entre comillas dobles RFC 9110 (ej. `"d41d8cd98f00b204e9800998ecf8427e"`). Cualquier cambio de estado, prioridad, agente, reapertura o fecha de actualización produce inmediatamente un ETag distinto.

### ¿Qué situación no cubre nuestra implementación actual?
1. **Concurrencia optimista en escrituras (*Lost Update Problem*):** Actualmente el ETag solo se evalúa con `If-None-Match` en la lectura (`GET /api/tickets/{id}`). No estamos exigiendo el header `If-Match` en `PATCH /api/tickets/{id}/estado` (que respondería `412 Precondition Failed` si dos supervisores modifican el mismo ticket al mismo tiempo).
2. **Ahorro de consulta a capa de datos:** Aunque el servidor evita enviar el cuerpo JSON por la red al devolver `304 Not Modified`, el servidor igualmente debe consultar `TicketDA` en memoria para obtener el ticket y recalcular su ETag antes de compararlo.
3. **Variación por compresión Content-Encoding:** Los ETags fuertes pueden verse afectados si un proxy o servidor web aplica compresión (`gzip`/`br`) sin ajustar el ETag por codificación.

---

## 7. Una transición de estado inválida: ¿por qué 409 y no 400 ni 422?

Cuando un cliente solicita pasar un ticket de `Abierto` directamente a `Cerrado`, o intenta reabrir un ticket que aún está `Abierto`, o asigna un sexto ticket `EnProceso` a un mismo agente (Variante 3):
- **¿Por qué NO `400 Bad Request`?**
  El JSON enviado (`{ "estado": "Cerrado" }`) es sintácticamente perfecto, los tipos de datos son válidos y el valor `"Cerrado"` pertenece legítimamente al enum `EstadoTicket`. No hay ningún error en el formato de la petición.
- **¿Por qué NO `422 Unprocessable Content`?**
  `422` aplica cuando los datos enviados en la petición son intrínsecamente inválidos por sí mismos (por ejemplo, una validación semántica de campos del propio payload sin depender del ciclo de vida cambiante del recurso). Sin embargo, `{ "estado": "Cerrado" }` **sí es una petición completamente válida** si el ticket estuviera actualmente en estado `Resuelto`.
- **¿Por qué SÍ `409 Conflict` (RFC 9110, Sección 15.5.10)?**
  `409 Conflict` indica que **la petición no pudo completarse debido a un conflicto con el estado actual del recurso objetivo**. El problema no está en la petición en sí misma, sino en que el estado presente del ticket en el servidor (`Abierto` o `Cerrado`, o la carga actual del agente en `EnProceso`) impide ejecutar esa transición en este momento. Si el estado del recurso cambia (por ejemplo, cuando el ticket pase a `Resuelto` o el agente libere un ticket), la misma petición pasará a ser válida.

---

## 8. Decisiones Adicionales de Implementación

- **Filtrado y Ordenamiento (`ReqFiltro`):** Se manejó el parámetro `Orden` como `string?` (`fecha`, `-fecha`, `prioridad`, `-prioridad`, con `-fecha` por defecto) porque los identificadores de un `enum` en C# no admiten el prefijo `-` en el model binder de query string. Si se envía un valor fuera de los permitidos o rangos inválidos (`pagina < 1`, `tamano < 1`, `tamano > 40`, `desde > hasta`), `TicketFlujo` lanza `TicketValidationException` para responder `400 Bad Request` con `ProblemDetails`.
- **Generación automática de `Codigo` y Fechas:** Al crear un ticket (`POST /api/tickets`), el servidor ignora cualquier código o fecha enviada por el cliente, asignando siempre el siguiente correlativo `HD-NNNN`, estado `Abierto`, `Reaperturas = 0` y `FechaCreacion = FechaActualizacion = DateTime.Now`.

---

## 9. Fuentes Consultadas
1. **RFC 9457 — Problem Details for HTTP APIs:** https://www.rfc-editor.org/rfc/rfc9457
2. **RFC 9110 — HTTP Semantics (Status Codes 304, 400, 404, 409, 412, 422 & ETag):** https://www.rfc-editor.org/rfc/rfc9110
3. **ASP.NET Core — Handle errors in ASP.NET Core web APIs (`ProblemDetails` / `AddProblemDetails`):** https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors
4. **ASP.NET Core — Routing & Custom Route Constraints (`IRouteConstraint`):** https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing#custom-route-constraints
5. **System.Text.Json — How to customize property names and values (`JsonStringEnumConverter`):** https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/customize-properties#enums-as-strings
6. **ASP.NET Core — OpenAPI Metadata in Minimal APIs and Controllers:** https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/include-metadata

---

## 10. Declaración de Uso de IA
De acuerdo con las reglas de la sección 2 de la práctica, se declara el uso de asistente de IA durante el desarrollo:

1. **Qué se le pidió:**
   - Analizar el documento de especificación de la Práctica Programada 1 y auditar el cumplimiento del contrato HTTP sobre la solución en 4 capas (`Abstracciones`, `DA`, `Flujo`, `Ticket.API`).
   - Apoyo en la generación de los 25 tickets semilla en `TicketDA`, la implementación de `CodigoTicketRouteConstraint` para la Variante 3 (`HD-NNNN`), y la maquetación de la página estática `wwwroot/tickets.html` consumiendo el API con paginación y filtros.
2. **Qué devolvió:**
   - Estructura de controladores y Minimal APIs con `AddProblemDetails()`, cálculo de `ETag` con hash MD5 sobre los campos de estado/actualización del ticket, y la interfaz gráfica en HTML/CSS/JS en `tickets.html`.
3. **Qué se corrigió o rechazó:**
   - **Corrección de Enum de Ordenamiento:** Inicialmente se planteó usar un `EnumOrden` para el parámetro `orden`, pero se rechazó y reemplazó por validación sobre `string?` en `ReqFiltro` porque el query binder de ASP.NET Core no enlaza nombres con prefijo `-` (`-fecha`, `-prioridad`) en enums sin convertidores personalizados de modelo.
   - **Corrección de ruta en `TicketController`:** Se corrigió `[Route("api/[controller]")]` (que generaba `/api/ticket` en singular) por `[Route("api/tickets")]` para respetar exactamente el contrato HTTP de la Sección 5.
   - **Validación estricta de parámetros de consulta y transiciones:** Se verificó que los errores de validación y reglas de negocio en `TicketFlujo` no muten el estado en memoria si ocurre un conflicto `409` y que todas las respuestas de error utilicen `application/problem+json`.
