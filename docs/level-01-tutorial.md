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
3. **Rewind** (z 36…56). Foso de 9 m, imposible de saltar. La plataforma parte sola al cargar el nivel y queda estacionada en el extremo lejano. *Ruta:* acercarse al borde → mantener **clic derecho** apuntando a la plataforma (retrocede a su inicio) → subirse **sin soltar** → soltar → viajar → bajar en la meta (z 51). *Si se falla* (se suelta antes de subir, se cae): caer al foso reinicia en el checkpoint y la plataforma vuelve a partir; basta repetir el Rewind.

## Validación ejecutada

- EditMode: 99/99 (incluye `LevelStatsTrackerTests` y `Level01WiringTests`).
- PlayMode: 27/27. `Level01TutorialPlayChecks` juega la ruta completa con input real (caída y R cuentan como intentos, salto, Q/E con puerta, Rewind + plataforma, meta + panel) y comprueba que caer en el foso 2 reinicia la plataforma.
- Batchmode con `-nographics` no valida el aspecto; para el arte se renderizaron capturas reales (OpenGL Core) y se revisaron a mano (ver Arte).

## Qué falta / a revisar a mano

- Revisar visualmente cámara Cinemachine (colisión con muros de 6 m de ancho), iluminación y legibilidad de los hints en el Editor.
- Jugador con cápsula placeholder (el prefab `Player` no incluye todavía el personaje animado) y bloques con color plano; reemplazar arte después.
- TMP Essential Resources se importó en `Assets/TextMesh Pro/` (necesario para los textos).
- Sin audio, sin menú ni botón de reintento tras la meta, sin persistencia.
- La plataforma parte al cargar la escena; tras un respawn en la sala 3 vuelve a partir de inmediato (el Rewind siempre es un recurso válido).

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
