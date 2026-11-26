# Game Design Document

## Visión

Echoryx es un videojuego 3D en el que una persona equipada con un traje tecnológico supera desafíos cortos usando la observación, la repetición de acciones y el control temporal de objetos. El alcance se organiza en tres niveles breves y un desafío final.

## Pilares confirmados

1. **Protagonista humano:** el personaje lleva un traje tecnológico.
2. **Lectura espacial:** cámara en tercera persona.
3. **Echo:** registra hasta cinco segundos y el jugador decide cuándo iniciar la reproducción.
4. **Rewind dirigido:** se aplica a objetos controlados; no se considera rebobinado global del mundo. Al terminar, el mecanismo continúa desde el estado recuperado.
5. **Progresión breve:** checkpoints, tres niveles cortos y un desafío final.

## Ciclo de juego previsto

Observar un desafío, actuar, decidir si una repetición Echo ayuda a coordinar la solución y usar Rewind sobre los objetos habilitados cuando sea necesario. El nivel confirma el avance en un checkpoint y conduce al siguiente reto. El prototipo actual graba hasta cinco segundos y consume cada grabación tras una reproducción; coste, otros tipos de objetos y reglas de fallo/recuperación siguen abiertos.

## Prototipo S02-C

Un ramal de `Prototype` contiene una placa alejada de una puerta. El jugador puede activar la placa, pero la puerta se cierra antes de que llegue si abandona la placa. La solución prevista es grabar con Q unos segundos de permanencia sobre la placa, acercarse a la puerta y reproducir con E: el Echo reaparece en la placa y sostiene la apertura mientras el jugador cruza. La grabación se consume tras una reproducción. Esta prueba define el comportamiento del prototipo, no la disposición de niveles definitivos.

## Prototipo S03-C

Un segundo ramal de `Prototype` conecta el suelo inicial con el muelle de salida mediante un acceso largo. La plataforma cinemática parte de ese muelle, llega al de destino en unos dos segundos y se detiene antes de que el jugador pueda alcanzarla en el primer viaje. El jugador debe apuntarle y mantener RMB para devolverla al muelle de salida. Al soltarla, vuelve a avanzar desde el punto recuperado y puede transportarlo hasta el otro lado. La plataforma detenida queda fuera del alcance del salto normal, por lo que esperar no resuelve el cruce. Mientras espera en destino, conserva su historial de viaje para que el jugador pueda recuperarla incluso después de una espera prolongada.

## Prototipo S04-A

Entrar en un checkpoint activa ese punto y sustituye al anterior. R o una caída devuelven al jugador al checkpoint activo; antes del primero se usa el punto inicial. El checkpoint se mantiene entre intentos de la sesión y no se guarda en disco. El reinicio solo afecta a los mecanismos inscritos en la sala asociada a ese checkpoint. S04-A limpia el movimiento del jugador y libera su interacción Rewind. S04-B completa la regla: cada reinicio cancela la captura Echo, descarta la grabación preparada y elimina el clon; restaura poses, progreso, historiales, placas y puertas de la sala activa. La grabación no se recupera tras reaparecer y su reproducción habitual sigue siendo de un solo uso. En Prototype, el punto inicial y A comparten la sala central (Echo y rotor); B reinicia la plataforma del cruce.

## Prototipo S04-C: sala combinada

El ramal `S04-C Combined Puzzle` se alcanza por el puente a la derecha del suelo inicial (entrada en x = 22, z = -1). Tiene un único checkpoint de entrada y dos barreras consecutivas. No hay un checkpoint intermedio que permita saltarse la primera fase después de R.

1. **Echo:** permanecer en la placa naranja y grabar con Q unos 4–5 segundos. Acercarse a la puerta azul y pulsar E: el clon permanece en la placa mientras el jugador cruza. La placa está a nueve metros de la puerta; salir corriendo o saltando sin clon deja que cierre antes de alcanzar el umbral. Paredes altas y techo impiden rodearla o saltarla. La puerta conserva la protección del umbral.
2. **Zona segura:** una superficie sólida detrás de la puerta permite detenerse y dejar que Echo desaparezca. La segunda fase no necesita resolverse dentro de la reproducción. El jugador puede continuar o usar R para volver al inicio; nunca depende de un clon para permanecer sobre suelo seguro.
3. **Rewind:** desde la marca verde azulado, apuntar a la plataforma detenida al otro lado del hueco, mantener RMB hasta devolverla al origen y subir caminando mientras sigue mantenido. Soltar RMB ya apoyado; viaja al destino y permite llegar al marcador verde de salida. El viaje de siete metros dura dos segundos y conserva su historial durante la espera. La plataforma detenida y el muelle final quedan fuera del alcance de salto.

R o una caída limpian las habilidades y restauran los tres mecanismos inscritos (placa, puerta y plataforma), reapareciendo antes de ambas fases. Incluso ignorando la puerta, recorrer los 19 m entre reaparición y borde de salida tarda al menos 3,17 s a 6 m/s: no se puede alcanzar el viaje inicial de 2 s. Repetir R vuelve a iniciar ese mismo intento, sin adelantar al jugador. El objetivo del prototipo se considera alcanzado al llegar al marcador; no añade pantalla de victoria ni transición de nivel.

**Corrección del acceso B tras reinicio:** el checkpoint y su reaparición están ahora al principio de `Approach Floor` (x = 5,5; z = -4), no en el muelle de salida. R y caída mantienen el viaje inicial de dos segundos, pero alcanzar la plataforma exige más de 2,37 s incluso a velocidad máxima y descontando radio y margen del CharacterController. La plataforma ya está detenida fuera del alcance de salto al llegar; RMB sigue siendo necesario. No hay temporizadores nuevos ni cambios de velocidad, historial o transporte. Mantener RMB hasta origen, subir mientras sigue pulsado y soltar ya apoyado permite cruzar sin precisión temporal excesiva. Una prueba conserva el antiguo spawn únicamente en memoria para reproducir y contrastar el atajo.

## Alcance de S00

Preparación del proyecto, estructura, escenas vacías creadas desde Editor, documentación y validación base. No incluye controles, Echo funcional, Rewind, HUD, interacción ni lógica de niveles.

## Dirección visual confirmada para S05

Ciencia ficción temporal estilizada, con acabado 3D de detalle medio. Estructura de grafito/titanio y señales cian para Echo y violeta para Rewind. S05-A1 prepara un subconjunto gratuito CC0 de Quaternius y materiales URP en una galería independiente; el arte final del nivel y del humanoide todavía no se reemplaza. Ver [biblioteca visual](archivo/visual-assets.md). *(Actualización: el subconjunto Quaternius y su galería se retiraron; `Level_01_Tutorial` usa los kits CC0 de Kenney.)*

## Fuera de decisiones confirmadas

No se fijan argumento, arte final, plataforma destino, duración total, asignación de habilidades a niveles ni reglas de puntuación. No tratarlos como requisitos hasta confirmación.
