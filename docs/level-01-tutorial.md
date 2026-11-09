# Level_01_Tutorial

Escena: `Assets/_Echoryx/Scenes/Level_01_Tutorial.unity` (con pase de arte, ver [Arte](#arte)) (añadida a Build Settings tras `Prototype`). Nivel de tres salas en línea (+Z) que enseña Echo y Rewind. Se regenera con `Echoryx > Level 01 > Build Tutorial` (requiere TMP Essentials, ver abajo).

## Controles reales

WASD mover · ratón mirar · Espacio saltar · **Q** grabar/parar Echo (máx. 5 s) · **E** reproducir el Echo · **clic derecho sostenido** apuntando a un mecanismo = Rewind · **R** reiniciar al último checkpoint.

> Rewind **no rebobina al jugador**: retrocede el movimiento de un mecanismo (historial de 5 s, alcance 12 m desde la cámara). En la sala 3 se usa sobre la plataforma móvil.

## Piezas (prefabs en `Assets/_Echoryx/Prefabs/Level/`, materiales URP Lit de color plano en `Materials/Level/`)

| Prefab | Contenido | Notas |
|---|---|---|
| `PressurePlate` | `PressurePlate` existente + `PressurePlateAttemptReset` + `PressurePlateEvents` | Se activa con jugador o Echo. `PressurePlateEvents` (nuevo) expone `Activated`/`Deactivated` sin tocar `PressurePlate`. |
| `Door` | `DoorController` existente + `DoorAttemptReset` | Se abre mientras su placa está activa; asignar `plate` en la instancia. |
| `Hazard` | `BoxCollider` trigger + `RespawnZone` existente | Reinicia al último checkpoint (mismo flujo que R). Asignar `manager` en la instancia y escalar al foso. |
| `Goal` | Volumen + `GoalZone` (nuevo) | Al entrar el jugador completa el nivel y muestra el panel. |
| `TutorialHint` | Volumen + `TutorialHint` (nuevo) | Muestra su texto (TextMeshPro) mientras el jugador está dentro. |

Scripts nuevos en `Assets/_Echoryx/Scripts/Level/` (asmdef `Echoryx.Level`): `LevelStats`, `LevelStatsTracker`, `LevelStatsAttemptCounter`, `LevelCompletePanel`, `GoalZone`, `TutorialHint`, `TutorialHintDisplay`, `PressurePlateEvents`. No se modificó `PlayerMotor`, Rewind, Echo ni checkpoints.

## Estadísticas del panel final (sin persistencia)

- **Intentos**: muertes y reinicios. Cada respawn resetea una vez la sala activa, así que un `LevelStatsAttemptCounter` dentro de cada `RoomResetScope` cuenta un intento.
- **Usos de Rewind**: transiciones a `Rewinding` de la plataforma (una por cada vez que se mantiene el clic derecho con éxito).
- **Tiempo total**: desde que carga la escena hasta entrar en la meta.

## Salas y ruta de solución

1. **Movimiento** (z −2…18). Hint con controles. Escalón bajo, hueco de 2,5 m sobre un foso (`Hazard`). *Ruta:* caminar, saltar el hueco, llegar al checkpoint del fondo (z 15,5).
2. **Echo** (z 18…36). Placa (z 21) abre una puerta (z 30). Hints en forma de pregunta (“¿Cómo puedes mantener la placa presionada y cruzar al mismo tiempo?”). *Ruta:* pararse en la placa → **Q** (esperar los 5 s del auto-stop o pulsar Q de nuevo) → **E** (el Echo reaparece en la placa y la mantiene presionada) → correr hasta la puerta y cruzar antes de que el Echo termine → checkpoint (z 34,5).
3. **Rewind** (z 36…56). Foso de 9 m, imposible de saltar. La plataforma parte sola al cargar el nivel y queda estacionada en el extremo lejano. *Ruta:* acercarse al borde → mantener **clic derecho** apuntando a la plataforma (retrocede a su inicio) → subirse **sin soltar** → soltar → viajar → bajar en la meta (z 51). *Si se falla* (se suelta antes de subir, se cae): caer al foso reinicia en el checkpoint y la plataforma vuelve a quedar aparcada lejos; basta repetir el Rewind.

## Validación ejecutada

- EditMode: 99/99 (incluye `LevelStatsTrackerTests` y `Level01WiringTests`).
- PlayMode: 27/27. `Level01TutorialPlayChecks` juega la ruta completa con input real (caída y R cuentan como intentos, salto, Q/E con puerta, Rewind + plataforma, meta + panel) y comprueba que caer en el foso 2 reinicia la plataforma.
- Batchmode con `-nographics` no valida el aspecto; para el arte se renderizaron capturas reales (OpenGL Core) y se revisaron a mano (ver Arte).

## Qué falta / a revisar a mano

- Revisar visualmente cámara Cinemachine (colisión con muros de 6 m de ancho), iluminación y legibilidad de los hints en el Editor.
- Jugador con cápsula placeholder (el prefab `Player` no incluye todavía el personaje animado) y bloques con color plano; reemplazar arte después.
- TMP Essential Resources se importó en `Assets/TextMesh Pro/` (necesario para los textos).
- Sin audio, sin menú ni botón de reintento tras la meta, sin persistencia.
- La plataforma parte al cargar la escena y queda aparcada lejos; tras un respawn en la sala 3 se restaura **aparcada** en el mismo estado (ver *Plataforma y respawn*), así que siempre hace falta Rewind.

## Arte

Pase visual con los kits CC0 de Kenney (ver [credits](credits.md)); no toca lógica ni colliders (los bloques de juego conservan su `BoxCollider` y solo se desactiva su `MeshRenderer`). Se aplica sobre la escena ya generada con `Echoryx > Level 01 > Apply Art Pass` (`Level01ArtPass`); reejecutarlo reconstruye el arte sin duplicar.

- **Paleta:** paredes lila/lavanda con acento naranja, pisos gris azulado oscuro, cian solo para el Echo (`EchoHologram.mat`, cian semitransparente, sin cambios).
- **Materiales** (`Materials/Art/`): URP Lit mate (Smoothness 0,08, Metallic 0, sin brillos especulares ni reflejos). Los materiales importados de los FBX ya eran `Universal Render Pipeline/Lit`, por lo que no hizo falta el Render Pipeline Converter; se crearon materiales propios con el `colormap` de cada kit y un tinte de paleta.
- **Prefabs con visuales nuevos** (`Prefabs/Level/`): `PressurePlate` (pieza del kit; mantiene el cambio de color naranja→verde del script), `Door` (marco + hoja del kit; la hoja sigue al panel lógico con suavizado `SmoothVisualFollow`, el panel y su collider no cambian), `Hazard` (campo emisivo rojo), `Goal` (portal con marco y campo verde), `RewindPlatformVisual` (nuevo; pieza del kit + `RewindMechanismFeedback`: la plataforma brilla en violeta mientras se rebobina, leyendo `RewindableMechanism.Mode`).
- **Salas:** pisos, paredes, cierres y bloques de casco bajo el suelo con piezas del kit; 5 pantallas de pared por encima de la cabeza (sin obstruir el carril) y fondo con secciones de estación, asteroides y contenedores flotando.
- **Atmósfera:** skybox con degradado y estrellas (`EchoryxGradientSkybox`), luz direccional cálida con sombras suaves, niebla exponencial suave, `Global Volume` (`Settings/Level01VolumeProfile.asset`) con Bloom suave, Color Adjustments (saturación +14) y Vignette ligero. Un solo Volume y una luz con sombras: pensado para rendimiento.
- **Hints de la sala 3** reescritos como preguntas guiadas: Rewind retrocede el movimiento de un mecanismo, no al jugador.

### Revisar a mano en el Editor

- Recorrer el nivel en Play y ajustar a gusto intensidad de Bloom, niebla (`Lighting > Environment`) y colores del skybox (`Materials/Art/ArtSky.mat`).
- Las pantallas de pared y los bloques del fondo son decoración estática sin collider: moverlos/quitarlos no afecta al juego.
- El jugador sigue siendo la cápsula placeholder.

### Correcciones visuales (post-revisión en el Editor)

- **Hoja de puerta y plataforma sin movimiento / brillo ausente:** el pase de arte marcaba todas las piezas como *Batching Static*; el batching estático hornea la malla en su posición inicial, así que el visual no se movía aunque la lógica sí. Ahora `Level01ArtPass.Spawn` limpia los flags estáticos y solo el decorado puro bajo `Level Art` se marca estático (`MarkScenery`). Esto también arregla el color de la placa y del marcador de checkpoint (MaterialPropertyBlock).
- **Parpadeo del suelo:** el `CinemachineCamera` tenía near 0,1 / far 5000 (relación 50 000:1, poca precisión de profundidad). Ahora near 0,25 / far 300. Además, las piezas planas apoyadas en el suelo se separan 1–1,5 cm y el casco bajo el suelo queda 3 cm por debajo de las baldosas.
- **Cámara atravesando paredes:** se añadió `CinemachineDeoccluder` (capa Default: paredes, suelos, panel de puerta y plataforma, que ya tienen collider; el arte no lleva colliders) y `CameraRadius` 0,4 en `ThirdPersonFollow` y Deoccluder (mayor que el desplazamiento de las esquinas del near plane). Con la hoja de la puerta inmóvil el jugador y la cámara parecían atravesar una puerta cerrada: era el mismo bug del batching.
- Tests: `MovingVisualsAreNeverStatic`, `CameraHasSaneDepthRange…` (EditMode) y `DoorLeafAndPlatformVisualsFollowTheirLogicalObjects`, `CameraDoesNotPassThroughWallsOrTheClosedDoor` (PlayMode).

## Personaje: robot animado

Sustituye la cápsula del Player y el visual del Echo con el paquete *Animated Low Poly Robot Free* (ver [credits](credits.md)); no se tocó `PlayerMotor`, Rewind, checkpoints ni colliders.

- **Paquete:** rig *Generic* (sin Humanoid), 10 clips sin root motion. Usados: `StaticIdle`, `Walk`, `Run`, `Jump_Start`, `Jump_Air`, `Jump_Landing`. No usados: `CombatIdle`, `BasicAttack`, `GetHit`, `Death`. Los materiales ya son URP Lit / ShaderGraph; las versiones con Metallic 1 se veían negras, por lo que hay copias mate en `Materials/Robot/`. La variante de color se elige con el desplazamiento del atlas (`RobotPlayerBase`/`RobotPlayerOrange`, fila 16: acentos naranja-marrón); el casco/torso negro es el diseño del robot.
- **Controller** `Art/Animations/Robot/PlayerLocomotion.controller`: Blend Tree 1D por `Speed` (Idle 0, Walk ≈2 m/s, Run 6 m/s con `timeScale` calibrado ≈1,5 según la zancada del clip) y estados `Jump_Start → Jump_Air → Jump_Landing` por `Grounded`. Apply Root Motion desactivado. La calibración (zancada estimada del pie en el clip) es una aproximación: ajusta el *threshold*/`timeScale` si ves patinaje.
- **`RobotAnimationDriver`** (puente de solo lectura, en el prefab `RobotVisual`): usa `PlayerMotor.PlanarSpeed`, que ya es el desplazamiento resuelto del `CharacterController` descontando el transporte de plataformas y el movimiento bloqueado por paredes (reutilizado en lugar de recalcularlo, y sin tocar el motor); suaviza con SmoothDamp (`speedDampTime`, `stopThreshold`, `groundedGrace` expuestos). El driver del SuperSoldier (`PlayerAnimationDriver`) sigue intacto.
- **Prefabs** (`Prefabs/Characters/`): `RobotVisual` (escala 1,1 ≈ 1,85 m, pies en el suelo, mira a +Z) y `RobotEchoVisual` (mismo modelo con `EchoHologram`, cian semitransparente, sin sombras). `Player.prefab` y `EchoActor.prefab` los instancian; la cápsula y el `Hologram Visual` antiguos quedan **desactivados** (no borrados) para no romper overrides en escenas.
- **Echo:** se anima con su propio desplazamiento (sin saltos: siempre "en suelo").
- **Validación previa:** `RobotIntegrationLab.unity` (Player sin modificar + robot en escena). Se probó caminar/correr/parar, salto, pared y plataforma antes de integrar. `PlayerAnimationIntegrationLab` (SuperSoldier) oculta el robot en su instancia de Player, y una aserción antigua ("el Player compartido no tiene Animator") se actualizó: ahora los únicos Animators del Player son los del robot.
- **Reconstruir:** `Echoryx > Robot > 1 Build Controller, Prefabs and Lab` y `2 Integrate Into Player and Echo`.

### Revisar a mano

- Patinaje de pies al caminar/correr, brazos que sobresalen (±0,5 m) al rozar paredes y el aspecto del Echo (cian) en Play.
- El robot es negro con acentos de color; si prefieres otro color cambia `PlayerColorRow` en `RobotVisualBuilder` y reconstruye.

## Echo reutilizable y plataforma tras el respawn

### Echo: la grabación se conserva

- **Antes:** al terminar una reproducción (y al reaparecer) la grabación se descartaba; `E` solo valía una vez.
- **Ahora:** una grabación se puede reproducir con `E` tantas veces como se quiera; `Q` graba una nueva que **reemplaza** a la anterior solo cuando termina (si la captura nueva se aborta, p. ej. al caer, vuelve la anterior). `EchoCapture.CompletePlayback` pasa a `Ready` y `ReleaseActivity()` (nuevo) hace lo propio al respawn: quita el clon, aborta una captura inacabada y conserva la grabación. `Cancel()` sigue vaciando todo.
- **`E` con un Echo activo:** se **ignora** (el mismo clon sigue; nada se reinicia). `Q` durante la reproducción también se ignora (comportamiento previo).
- **Respawn (R o caída):** la grabación **se conserva**; no hay razón técnica para borrarla (el clon se limpia aparte).
- Tests: `ARecordingPlaysRepeatedlyWithTheSameResult`, `ANewRecordingReplacesThePreviousOneOnlyWhenItCompletes` (EditMode) y `RecordingReplaysRepeatedlyWithTheSameResultAndIsReplacedByANewOne` (PlayMode: tres reproducciones idénticas, `E` ignorado en curso, R conserva, la nueva reemplaza). Se actualizaron los tests antiguos que asumían "consumir" la grabación.

### Plataforma y respawn

- **Causa raíz:** `RewindMechanismAttemptReset.ResetForAttempt` devuelve el mecanismo a su pose **inicial** (el inicio del recorrido, pegado al borde del checkpoint) con una línea de tiempo nueva en t=0, y `RewindMovingPlatform` parte de inmediato. Tras caer, el jugador reaparecía a ~1,5 m de una plataforma que estaba en el inicio o a mitad de viaje (2,4 s), alcanzable andando sin Rewind. Al cargar el nivel pasa lo mismo, pero el jugador está a 30 m y cuando llega ya está aparcada.
- **Invariante:** tras cualquier respawn la plataforma queda **aparcada en su destino** (lejos, quieta, con historial de Rewind válido), igual que cuando se llega a la sala. El salto máximo desde el borde (≈5,2 m) no alcanza el borde cercano de la plataforma aparcada (5,9 m).
- **Cambio:** `RewindableMechanism.StepNormalTimeline(dt)` (método protegido nuevo, aditivo; no cambia el comportamiento existente) avanza la línea de tiempo con un paso explícito; `RewindMovingPlatform.ParkAtDestination()` lo usa para llevar el viaje a su fin sin transportar pasajeros; `ParkedPlatformAttemptReset` (en el ámbito de la sala 3) restaura y aparca. Migración de escenas existentes: `Echoryx > Level 01 > Apply Parked Platform Reset`; el builder ya lo incluye.
- **Placa y puerta:** viven en el ámbito de la sala 2; tras un respawn en el checkpoint de la sala 3 no hay nadie en la placa y el clon se limpia, así que la placa queda libre y la puerta cerrada (comprobado en el test), sin tocar la ruta de solución.
- Tests PlayMode: `AfterFallingTheRewindPlatformIsParkedOutOfReachAtEveryMoment` (dos caídas seguidas; la plataforma permanece en su destino cada uno de 300 fotogramas y su borde queda fuera del alcance de salto calculado a partir del motor) y la ruta completa, que ahora incluye una caída en la sala 3 antes de resolverla con Rewind.

### Otros

- `TutorialHintDisplay` ya no deja la etiqueta vacía al salir de un hint que se solapa con otro todavía activo (muestra el anterior).
