# Level_01_Tutorial

Escena: `Assets/_Echoryx/Scenes/Level_01_Tutorial.unity` (añadida a Build Settings tras `Prototype`). Nivel de tres salas en línea (+Z) que enseña Echo y Rewind. Se regenera con `Echoryx > Level 01 > Build Tutorial` (requiere TMP Essentials, ver abajo).

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
- Batchmode con `-nographics`: no valida aspecto visual, cámara ni encuadre.

## Qué falta / a revisar a mano

- Revisar visualmente cámara Cinemachine (colisión con muros de 6 m de ancho), iluminación y legibilidad de los hints en el Editor.
- Jugador con cápsula placeholder (el prefab `Player` no incluye todavía el personaje animado) y bloques con color plano; reemplazar arte después.
- TMP Essential Resources se importó en `Assets/TextMesh Pro/` (necesario para los textos).
- Sin audio, sin menú ni botón de reintento tras la meta, sin persistencia.
- La plataforma parte al cargar la escena; tras un respawn en la sala 3 vuelve a partir de inmediato (el Rewind siempre es un recurso válido).
