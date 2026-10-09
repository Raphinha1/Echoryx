# Arquitectura

## Plataforma y estado base

- Unity 6 **6000.3.22f1**; renderizado URP **17.3.0**.
- Paquetes directos existentes: Input System **1.20.1**, Cinemachine **3.1.7**. Las versiones se conservan desde `Packages/manifest.json` y `packages-lock.json`.
- `Assets/Settings/` y `Assets/Scenes/SampleScene.unity` son contenido existente. No se mueven ni sustituyen durante S00.
- Unity crea y mantiene los `.meta`; conservarlos en control de versiones.

## Estructura de trabajo

`Assets/_Echoryx/` separa `Art`, `Audio`, `Materials`, `Prefabs`, `Scenes`, `Scripts`, `Settings` y `Tests`. `Scripts` se divide en `Player`, `Echo`, `Rewind`, `Interactions`, `Game` y `UI`. Es una organización objetivo; los recursos de plantilla existentes permanecen donde están hasta que un cambio posterior justifique migrarlos.

## Límites previstos

- **Player:** representación/control del protagonista y puente con cámara.
- **Echo:** captura acotada y reproducción manual de datos de acción.
- **Rewind:** historial y restauración de estado para objetos explícitamente habilitados.
- **Interactions:** contratos entre jugador y objetos/desafíos.
- **Game:** estado de partida, checkpoints y flujo de escenas/niveles.
- **UI:** presentación de estado y ayudas al jugador.

Estas son responsabilidades de diseño, no componentes ya implementados. Mantener la captura temporal separada de la reproducción y limitar Rewind a objetos registrados evita acoplar las mecánicas al estado global. Definir contratos y formato de datos durante sus respectivos slices.

## Escenas

- `Prototype`: espacio pequeño para validar el ciclo de juego cuando las mecánicas estén disponibles.
- `Laboratory`: escena aislada para probar mecánicas y objetos.

Ambas escenas existen. `Prototype` contiene ahora el circuito S01 y `Laboratory` permanece sin cambios. Los recursos de escena y prefab de S01 se generaron mediante Unity Editor, sin escribir YAML manualmente.

## Implementación S01

- `PlayerInputReader` usa el mapa `Player` del asset existente `Assets/InputSystem_Actions.inputactions` y expone Move, Look y el flanco de Jump. No usa el sistema de entrada antiguo.
- `PlayerMotor` mueve un `CharacterController` respecto al plano horizontal de la cámara, con aceleración, frenado, control aéreo, giro, gravedad y salto. `JumpWindow` concentra coyote time y jump buffering para poder probar esa lógica sin escena.
- `PlayerCameraOrbit` mueve un objetivo independiente del giro del personaje. `CinemachineCamera` con `CinemachineThirdPersonFollow` sigue ese objetivo; su opción `AvoidObstacles` cubre colisiones con geometría de la capa Default.
- `Player.prefab` contiene raíz con `CharacterController`, `VisualRoot/Capsule Placeholder` y `CameraAnchor`. El modelo futuro puede reemplazar el hijo visual sin modificar el collider o el motor.
- `S01PrototypeBuilder` (menú `Echoryx/S01/Build Player and Prototype`) generó el prefab y el circuito usando APIs de Unity. Rechaza reconstruir una escena que ya contenga elementos S01.

La órbita y el motor usan parámetros serializados ajustables desde Inspector. La cámara y el circuito solo están configurados en `Prototype`; el prefab de Player no guarda referencias a objetos de escena.

## Implementación S02-A: captura Echo

- `EchoRecordInput` lee la acción `Player/EchoRecord` (Q) del asset Input System existente y solicita iniciar/detener a `EchoRecorder`. No cambia `PlayerMotor`.
- `EchoRecorder` se ejecuta después del movimiento de S01 en `Update` y entrega la pose final de cada frame a `EchoCapture` con `Time.timeAsDouble`. La primera muestra tiene tiempo relativo cero.
- `EchoCapture` mantiene un búfer de tamaño fijo; interpola muestras sobre una cuadrícula temporal configurable (20 Hz iniciales) y añade la pose terminal. Limita cualquier captura a cinco segundos. Solo al completarla copia las muestras a un `EchoRecording` estable para una futura reproducción.
- Estados de captura en S02-A: `Available` → `Recording` → `Ready`; desde `Ready` se puede iniciar una captura nueva, y cancelar desde `Recording` o `Ready` vuelve a `Available`.
- El prefab `Player` hereda el grabador y la acción Q en `Prototype`. `S02EchoInstaller` añadió la acción mediante APIs del Input System y actualizó el prefab con `PrefabUtility`.

La captura guarda posición y rotación en coordenadas globales y tiempo relativo; no registra acciones/interacciones. Cambiar frecuencia o duración en Inspector durante Play Mode requiere reiniciar la sesión para reconstruir el búfer.

## Implementación S02-B: reproducción Echo

- `EchoPlaybackInput` lee `Player/EchoPlay` (E) del mismo asset Input System. `EchoPlayback` crea como máximo un `EchoActor` desde la **primera pose grabada**, no desde la posición actual del jugador.
- `EchoRecording.TryEvaluate` busca las muestras contiguas por tiempo relativo e interpola posición y rotación. `EchoPlayback` consulta esa función con `Time.timeAsDouble - inicio`; el resultado no depende del número de frames renderizados.
- Transiciones completas: `Ready` → `Playing` al crear el clon; al llegar a la duración grabada se destruye y vuelve a `Available`. Q no inicia otra captura durante `Playing`; E no crea otro clon. Cancelar o desactivar el reproductor también libera el actor y la grabación.
- `EchoActor` identifica al clon para su reproducción. El prefab usa una cápsula cian semitransparente con material URP Unlit. **No tiene Collider, CharacterController ni Rigidbody**, por lo que no bloquea al jugador ni participa en física.
- `S02PlaybackInstaller` generó acción, material y prefab con APIs de Unity y enlazó el reproductor en `Player.prefab`. `Prototype` hereda esos componentes de la instancia existente del prefab.

El mapa de entrada de plantilla todavía contiene `Player/Interact` con una asignación E de tipo Hold; ningún componente del proyecto la consume en S02-B. Revisar esta coincidencia cuando se implemente la interacción del jugador.

## Implementación S02-C: placa y puerta Echo

- `MechanismActor` marca explícitamente al jugador o al Echo. Su registro de instancias activas permite detectar al clon aunque no participe en física; los prefabs conservan sus componentes de S01/S02.
- `PressurePlate` comprueba posiciones dentro de un volumen local cada `LateUpdate`. `PlateOccupancy` separa el estado lógico y permite presencia simultánea; al desactivar o destruir el clon, su marcador sale del registro y la siguiente evaluación limpia la placa. El clon se desactiva antes de su destrucción al terminar la reproducción.
- `DoorController` consulta la placa y mueve un panel con `BoxCollider` entre posición cerrada y desplazamiento abierto. `DoorGateState` retiene la apertura si el jugador está en el umbral tras soltarse la placa; si el panel comenzó a cerrar y el jugador vuelve al umbral, reabre. Una puerta totalmente cerrada no se abre por la sola proximidad del jugador.
- `S02InteractionInstaller` añadió marcadores a ambos prefabs y construyó `S02-C Echo Puzzle` en `Prototype` con APIs de Unity. El ramal tiene placa, corredor, puerta, paredes y salida; `S01 Test Circuit` y `Laboratory` permanecen intactos.

El volumen de detección de la placa, el tamaño del umbral, el desplazamiento y la velocidad de la puerta se ajustan en Inspector. La detección usa la posición raíz de cada entidad; no modela múltiples jugadores ni física de plataformas móviles. La reproducción sigue siendo de un solo uso.
