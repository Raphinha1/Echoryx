# Flujo del juego

Menú principal, niveles encadenados, pausa, reinicio y progreso. No cambia la lógica de `PlayerMotor`, Echo, Rewind ni checkpoints.

## Situación previa (auditoría)

1. `Level_01_Tutorial` era la primera escena del build; el juego no llamaba a `LoadScene`.
2. UI: `HUD Canvas` con hints y panel de fin (intentos, rewinds, tiempo), generado por `Level01TutorialBuilder`.
3. Sin botones, `EventSystem`, `GraphicRaycaster` ni contadores en vivo.
4. Input: `InputSystem_Actions`, mapa `Player`; el input de juego pasa por `PlayerInputReader.CanReadAction`.
5. `Esc` solo liberaba el cursor y un clic lo volvía a bloquear.
6. `R` = `Player/Restart`: volver al checkpoint (cuenta un intento). No había reinicio de nivel ni persistencia.

## Controles añadidos

| Tecla | Acción |
|---|---|
| `Esc` (mando: Start) | Pausar / reanudar |
| `Retroceso` | Reiniciar el nivel desde cero (también tras completarlo) |
| `R` | **Sin cambios**: volver al último checkpoint |

El reinicio de nivel no usa `R` porque ya estaba asignada. Las dos acciones nuevas están en el mapa `System` del asset de input. Los menús se manejan con ratón, o con flechas/WASD y Enter (acciones UI por defecto del Input System).

## Piezas (`Assets/_Echoryx/Scripts/Flow/`, asmdef `Echoryx.Flow`)

| Pieza | Función |
|---|---|
| `LevelCatalog` (asset `Resources/LevelCatalog.asset`) | Lista ordenada de niveles (`id`, nombre, ruta de escena), escena del menú y asset de input |
| `GameProgress` | Niveles completados y mejores marcas |
| `MainMenuController` (escena `MainMenu`) | Jugar, Seleccionar nivel, Salir |
| `LevelFlow` + `LevelFlowBootstrap` | Pausa, reinicio y botones de fin de nivel |
| `UiKit`, `SceneNavigator` | Estilo común y carga de escenas (siempre con `timeScale = 1`) |
| `Editor/GameFlowBuilder` | Menús `Echoryx/Flow/…` |

La UI de flujo **se crea por código en tiempo de ejecución**. `LevelFlow` se añade solo a las escenas del catálogo (también al darle Play a un nivel desde el Editor); por eso no hay cambios en la escena del nivel y `Build Tutorial` no puede borrar los menús. En el panel de fin existente agrega los botones, lo agranda a 700 de alto y añade un `GraphicRaycaster` al HUD. `Prototype` y `RobotIntegrationLab` no están en el catálogo y no cambian.

**Jugar** abre el primer nivel desbloqueado sin completar; si están todos completos, el primero.

## Añadir un nivel (sin tocar código)

1. Crea la escena. Para pausa y reinicio basta con estar en el catálogo. Para completarse y guardar necesita el `Player` con su cámara orbital y el conjunto `LevelStats` + `GoalZone` + `LevelCompletePanel`, cableado como en `Level_01_Tutorial` (hijo `Title` incluido).
2. En `Assets/_Echoryx/Resources/LevelCatalog.asset`, añade una entrada **al final** o en la posición deseada. El `id` es la clave del progreso: no lo cambies después de publicarlo.
3. Ejecuta `Echoryx/Flow/2 Sync Build Settings From Catalog` (menú primero, niveles en orden, el resto después).

`ShippedFlowAssetsTests` falla si el catálogo tiene errores o un nivel no está en el build. El selector muestra hasta **12 niveles** (3 × 4); con más haría falta paginar o desplazar.

`Echoryx/Flow/1 Build Main Menu` regenera la escena del menú. No sobrescribe un catálogo existente.

## Pausa: qué se congela y cómo

`timeScale = 0` no basta: `Update` y `LateUpdate` siguen ejecutándose. Al pausar:

- El input de juego queda suspendido (`PlayerInputReader.Suspended`): Q, E, R, clic derecho, salto, movimiento y mirada no hacen nada.
- Cámara orbital desactivada y cursor libre.
- **El Rewind se suelta**, igual que al soltar el botón (`ReleaseForRespawn`); el mecanismo conserva la pose recuperada y sigue desde ahí. `RewindInteractor` se desactiva para que su retículo no se pinte sobre el menú.
- Echo no se toca: usa tiempo escalado, así que una grabación o reproducción se detiene y continúa donde estaba. El tiempo en pausa no cuenta en las estadísticas.

Al reanudar, **las teclas que sigan pulsadas hay que volver a pulsarlas** (misma regla que tras un respawn). Así no hay saltos heredados ni un Rewind que se reactive solo y sume un uso.

**Detalle de implementación (no mover sin repetir las pruebas):** el frame en que se pausa aún tiene `deltaTime > 0` y se simula entero. `PlayerMotor` se desactiva solo en frames con `deltaTime = 0` y se reactiva en el `LateUpdate` del frame de reanudación. Si se congela un frame antes, o se reactiva durante el `Update` del primer frame que avanza, el jugador pierde un frame de transporte de la plataforma en cada pausa (0,04–0,08 m).

## Progreso

JSON en `PlayerPrefs`, clave `Echoryx.Progress`:

```json
{"version":1,"levels":[{"id":"level-01-tutorial","bestAttempts":0,"bestRewinds":1,"bestSeconds":42.5}]}
```

Una entrada significa «completado». Cada mejor marca se conserva por separado (pueden venir de partidas distintas). Se desbloquea el primer nivel, el siguiente a uno completado y cualquier nivel ya completado. Un JSON ilegible se trata como progreso vacío (con aviso) y se sobrescribe al completar un nivel. No hay botón para borrar el progreso; para hacerlo a mano, borra esa clave (por ejemplo `Edit > Clear All PlayerPrefs` en el Editor, que borra todas).

Las pruebas sustituyen el almacenamiento con `GameProgress.Storage = new MemoryProgressStorage()`. **Cualquier prueba nueva que complete un nivel del catálogo debe hacer lo mismo** para no escribir en el progreso real.

## Pruebas

- EditMode: `LevelProgressTests` (orden, validación, desbloqueo, mejores marcas, JSON corrupto, ida y vuelta por `PlayerPrefs` con clave temporal) y `ShippedFlowAssetsTests`.
- PlayMode (`Tests/PlayMode/Flow/`): menú y selector; pausa durante movimiento, grabación, reproducción, Rewind y sobre la plataforma; reintento, Retroceso y vuelta al menú; fin de nivel con y sin siguiente; clics de ratón. El caso «hay siguiente» usa un catálogo de prueba con `Prototype` como segunda escena.
- `Level01TutorialPlayChecks`: la ruta completa comprueba además el guardado y los botones al llegar a la meta real.

## Sin verificar visualmente (Unity sin gráficos)

- Aspecto de `MainMenu`: cielo, piezas de fondo y su encuadre, colores, legibilidad.
- Aspecto de la pausa y del panel de fin ampliado, y su convivencia con los hints. Solo se comprobó por geometría que los elementos del panel de fin no se solapan ni se salen.
- Otras resoluciones y relaciones de aspecto.
- Bloqueo del cursor en la vista Game y en un build (en el Editor, `Esc` además libera el cursor por sí mismo).
- Mando físico (Start y navegación).
- Build real: arranque en el menú, botón Salir y persistencia entre ejecuciones.
