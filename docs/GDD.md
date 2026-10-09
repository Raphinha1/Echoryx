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

## Alcance de S00

Preparación del proyecto, estructura, escenas vacías creadas desde Editor, documentación y validación base. No incluye controles, Echo funcional, Rewind, HUD, interacción ni lógica de niveles.

## Fuera de decisiones confirmadas

No se fija aquí género de ciencia ficción específico, argumento, arte final, plataforma destino, duración total, asignación de habilidades a niveles ni reglas de puntuación. No tratarlos como requisitos hasta confirmación.
