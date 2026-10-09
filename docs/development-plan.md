# Plan de desarrollo por slices

Secuencia propuesta para entregar incrementos pequeños. Las dependencias son funcionales; el orden evita validar características sobre una base inexistente. S00 no implementa gameplay.

| Slice | Entrega | Depende de | Criterios de aceptación |
|---|---|---|---|
| **S00 — Fundación** | Estructura, decisiones documentadas, paquetes/configuración inspeccionados, escenas preparadas desde Editor y lista de validaciones. | — | Carpetas organizadas; versiones registradas; `Prototype` y `Laboratory` guardadas en su ubicación y abren sin errores; README, instrucciones, GDD, arquitectura, plan y testing disponibles; `.gitignore` conserva fuentes/metas y excluye generados. No hay mecánicas implementadas. |
| **S01 — Jugador y cámara** | Protagonista humano con traje tecnológico, movimiento base y cámara en tercera persona. | S00 | Control estable con dispositivo acordado; cámara sigue/orbita según diseño aprobado; colisiones y límites de prueba funcionan; sin errores de consola en recorrido de validación. |
| **S02 — Echo** | Captura limitada a cinco segundos y reproducción iniciada manualmente. | S01 | Nunca conserva más de cinco segundos; el jugador inicia/detiene o concluye la reproducción según controles aprobados; reproducción coincide con acciones registradas y se prueba en Prototype. |
| **S03 — Objetos y Rewind** | Interacción con objetos registrados y restauración temporal dirigida. | S02 | Solo objetos habilitados reciben Rewind; un ciclo de cambio/restauración recupera los estados definidos; objetos no registrados permanecen intactos; Echo e interacción no se bloquean mutuamente de forma inesperada. |
| **S04 — Checkpoints** | Guardado de progreso y recuperación del jugador. | S01, S03 | Activar un checkpoint y fallar devuelve al punto correcto; el progreso retenido/p perdido sigue una regla documentada; recarga no duplica estado u objetos. |
| **S05 — Nivel 1 vertical slice** | Un nivel corto integrando las mecánicas y el flujo de principio a fin. | S02, S03, S04 | El nivel se puede completar; presenta Echo y Rewind con propósito legible; checkpoint y salida funcionan; no requiere intervención de Editor durante la partida. |
| **S06 — Niveles 2 y 3** | Dos niveles cortos que amplían el uso de las mecánicas. | S05 | Ambos se completan en orden; cada uno introduce un reto distinguible sin reglas contradictorias; checkpoints y transición preservan el estado esperado. |
| **S07 — Desafío final** | Desafío final integrado tras los tres niveles. | S06 | Se alcanza después del nivel 3, puede completarse y comunica claramente éxito/fallo; usa sistemas ya validados sin crear dependencias de escena rotas. |
| **S08 — Estabilización y entrega** | Revisión integrada, configuración de build y documentación de entrega. | S07 | Validaciones de regresión pasan en Editor y build de destino; escenas incluidas y orden verificadas; sin errores/bloqueos conocidos sin documentar; instrucciones de ejecución reproducibles. |

Los controles, métricas concretas, plataformas destino y criterios artísticos no están definidos en S00. Confirmarlos antes de cerrar los slices que los necesiten. Registrar desviaciones y evidencia de aceptación en cada entrega.

## Estado actual

- S00 cerrado en `aa2f2c5`.
- S01 cerrado según el estado informado por el usuario.
- S02-A implementó captura Echo y Q; el usuario informó validación manual (hasta 101 muestras).
- S02-B implementa reproducción con clon y E. Pasaron 12 pruebas EditMode del proyecto y el usuario confirmó la validación en Play Mode.
- S02-C incorpora una placa y puerta con detección espacial del clon y un rompecabezas en `Prototype`; pasaron 21/21 pruebas EditMode del proyecto y el usuario confirmó S02 en Play Mode.
- S03-A implementa el núcleo Rewind de posición y rotación con historial acotado, retroceso temporal y descarte de futuros. Pasaron 30/30 pruebas EditMode del proyecto; la selección/control del mecanismo y la integración jugable corresponden a S03-B.
- S03-B añade selección por cámara, RMB y un rotor cinemático provisional en `Prototype`. Pasaron 41/41 pruebas EditMode del proyecto y el usuario confirmó 10/10 validaciones manuales.
- S03-C incorporó la plataforma cinemática y el cruce obligatorio con Rewind; pasó 48/48 EditMode y el usuario confirmó S03 completo en `fc73688`.
- S04-A incorpora checkpoints, reaparición, limpieza básica del jugador y contrato de reinicio por sala; pasó 61/61 EditMode del proyecto. El usuario confirmó su validación manual en Play Mode.

- S04-B incorpora adaptadores de Echo y mecanismos por sala, liberación inmediata y restauración antes de reaparecer; pasó 74/74 EditMode. El usuario confirmó 11/11 comprobaciones manuales.

- S04-C añade el rompecabezas combinado en Prototype, con ámbito y checkpoint propios, sin implementar niveles definitivos. Pasaron 82/82 EditMode y 3/3 PlayMode automatizados; el usuario confirmó 11/11 comprobaciones manuales. La corrección final del acceso B tras R/caída está documentada en testing; requiere su propia regresión visual antes de cerrar S04.

## Desglose de S04

| Etapa | Depende de | Entrega y aceptación |
|---|---|---|
| S04-A | S01–S03 | Un checkpoint activo y punto inicial; R y caída reaparecen sin velocidad, salto o transporte residual; liberación, restauración y reaparición en orden determinista; solo recibe reinicio la sala activa. Pruebas EditMode y recorrido manual documentados. |
| S04-B | S04-A | Adaptadores completos para cancelar y limpiar Echo, historiales Rewind, plataforma, rotor, placas y puertas en el ámbito correcto; reinicios repetidos sin estados residuales ni regresiones. |
| S04-C | S04-B | Rompecabezas combinado que use las mecánicas existentes y checkpoints; resolverlo, fallar y repetir sin intervención del Editor. |

## Preparación visual de S05

| Etapa | Entrega | Aceptación |
|---|---|---|
| S05-A1 | Subconjunto Standard gratuito, procedencia/licencia, materiales URP y galería independiente. | Fuentes identificadas y acotadas; materiales compatibles, escala registrada e importación comprobada; Prototype y S01–S04 intactos. No crea el nivel final. |
| S05-A2 (terminado y validado según el usuario) | Sala de referencia modular, wrappers/prefabs visuales y colliders simples. | Encajes y proporciones coherentes, iluminación legible y cámaras/colisiones comprobadas en escena separada antes de integrar un nivel. |
| S05-B1 (evaluado y retirado) | Revisión histórica de Cybernet Warrior. | No apto: sin rig/skinning y 157.410 triángulos; recursos exclusivos retirados. Registro y hashes conservados. |
| S05-B2 (aprobado) | SCI-FI SuperSoldier, rig Humanoid, materiales y laboratorio aislado. | Avatar y escala verificados; aprobado técnica y visualmente. |
| S05-B3 (aprobado) | Locomoción Humanoid aislada, Animator y correcciones de clips/transiciones. | Idle/Walk/Run/Jump/Fall/Land aprobados técnica y visualmente; motor y física fuera de alcance. |
| S05-B4 (siguiente, pendiente) | Integrar presentación visual con PlayerMotor y validar locomoción en juego. | PlayerMotor conserva autoridad sobre movimiento/raíz; probar sincronía de salto/caída, escala y cámara. Echo/clon queda sujeto a alcance acordado para B4. |

Las decisiones y evidencia S05-A1 se conservan en [visual-assets](visual-assets.md); esta preparación no cierra por sí sola el vertical slice S05.

S05-A2 creó VisualReferenceRoom y cinco prefabs de estructura; la verificación automatizada y las capturas están documentadas en [sala de referencia](visual-reference-room.md). El usuario confirmó su cierre/validación al iniciar S05-B1. S05-B1 evaluó y retiró Cybernet Warrior; su registro y hashes se conservan. S05-B2/B3 aprobaron técnica y visualmente el SuperSoldier y su locomoción en laboratorios aislados. S05-B4 integrará el aspecto visual con PlayerMotor; todavía no comienza.

Evidencia B3 y pasos para reproducir la validación manual: [laboratorio de locomoción](supersoldier-locomotion.md).
