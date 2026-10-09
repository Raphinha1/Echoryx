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

## Implementación S03-A: núcleo Rewind

- `RewindPose` contiene posición y rotación globales. `RewindHistory` guarda muestras en un anillo de capacidad fija, con ventana máxima configurable hasta cinco segundos y muestreo uniforme (20 Hz iniciales). Interpola posiciones y rotaciones entre muestras, incluso si los frames llegan con intervalos irregulares.
- El historial usa tiempo propio del mecanismo. En modo `Normal`, avanza con el tiempo transcurrido y registra la pose después del movimiento; en `Rewinding`, retrocede por segundos transcurridos y no acepta avance normal. Al finalizar, inserta la pose recuperada si cayó entre muestras, elimina la rama futura y reinicia el muestreo desde ese instante. El mecanismo continúa desde esa pose, sin regresar a la ubicación previa.
- `RewindableMechanism` es la base para mecanismos cinemáticos concretos: una subclase implementa `AdvanceNormal` y puede personalizar captura/aplicación de pose. La base omite `AdvanceNormal` durante Rewind y expone `BeginRewind`, `EndRewind` y `ResetHistory` mediante `IRewindableMechanism` para que S03-B controle un único objeto seleccionado.
- S03-A no agrega componentes a escenas o prefabs, ni depende de Input System, cámara, Player, Echo o física dinámica. Los valores de duración, frecuencia y velocidad de retroceso se configuran en Inspector antes de iniciar Play Mode.

## Implementación S03-B: selección y control Rewind

- `RewindInteractor` vive en `Player.prefab`, separado de `PlayerMotor` y en un ensamblado adaptador de entrada distinto del núcleo Rewind. Lee la acción `Player/Rewind` del asset Input System existente, vinculada a RMB; `Player/Look` sigue leyendo el movimiento del ratón y la cámara continúa orbitando durante la pulsación.
- `RewindTargetResolver` lanza un rayo desde el centro de la cámara, ignora los colliders del jugador y toma el primer obstáculo no propio dentro del alcance configurable. Si ese collider pertenece a un hijo, busca `RewindableMechanism` en sus padres; un obstáculo intermedio bloquea la selección.
- `RewindSelectionSession` intenta iniciar Rewind una vez por pulsación y fija el objetivo hasta soltar RMB. Si se desactiva o destruye, termina o abandona la interacción sin adquirir otro durante la misma pulsación. `RewindableMechanism` conserva además una referencia activa para impedir que dos mecanismos se rebobinen simultáneamente.
- Una marca de seis píxeles en el centro indica objetivo listo (cian), objetivo sin historial suficiente o indisponible (ámbar) y retroceso activo (violeta). Es una ayuda provisional, no una HUD definitiva.
- `S03RewindInstaller` añadió la acción y el componente al prefab mediante APIs de Unity y creó en `Prototype` un rotor cinemático con collider en un hijo, separado del circuito S01 y del rompecabezas Echo. `RewindTestRotor` solo gira para comprobar RMB y el retorno desde la pose recuperada; no es la plataforma de S03-C.

## Implementación S03-C: plataforma y transporte

- `RewindMovingPlatform` deriva de `RewindableMechanism`. `MovingPlatformPath` calcula un viaje único entre dos marcadores con velocidad configurable y posición final fija. El progreso se obtiene del reloj virtual de `RewindHistory`, de modo que al soltar RMB se reanuda desde el tiempo y progreso recuperados; el núcleo sigue siendo el único dueño del historial y su rama futura. En destino, el componente deja de llamar a `TickTimeline` en modo normal: el historial local queda congelado mientras espera, sin registrar poses estáticas que expulsen el origen de la ventana de cinco segundos. Durante Rewind sí sigue actualizando; al recuperar una pose anterior, vuelve a avanzar y, al llegar, se detiene otra vez. No se modificó `RewindHistory` ni el comportamiento del rotor.
- La plataforma actualiza en `Update` antes de `PlayerMotor`; el rotor conserva el `LateUpdate` de la base. Antes de trasladar su collider, la plataforma comprueba si los pies del jugador estaban apoyados sobre la cara superior. Si lo estaban, ofrece el desplazamiento de ese frame a `PlayerPlatformCarrier`; `PlayerMotor` lo suma a su único `CharacterController.Move`. No se parenta ni teletransporta al jugador, y el transporte termina al perder apoyo o saltar. `Physics.SyncTransforms` actualiza el collider móvil para la consulta de cámara y la colisión del mismo frame.
- `PlatformRidePolicy` concentra la comprobación espacial de apoyo; `PlayerPlatformCarrier` requiere además que el `CharacterController` estuviera grounded. Esto evita transportar al jugador por un roce lateral, cuando está en el aire o cuando abandona la plataforma.
- `S03PlatformInstaller` añadió el carrier al prefab y construyó `S03-C Rewind Crossing` en `Prototype` mediante APIs de Unity: acceso desde el suelo inicial, dos muelles, un hueco, marcadores de trayectoria y una plataforma verde azulado con `BoxCollider` sólido. El recorrido mide 7 m a 3,5 m/s (2 s, dentro del historial de 5 s). El acceso tarda más que el primer viaje aun a la velocidad máxima del jugador; la posición final deja más de 6 m entre el borde de salida y el borde más cercano de la plataforma, fuera del alcance de salto incluso con coyote time. Los sistemas S01/S02 y el rotor S03-B permanecen en la escena.

El transporte cubre un jugador sobre una plataforma que solo traslada posición. Rotaciones, varios pasajeros y sincronización de redes quedan fuera del prototipo. La comprobación de contacto, la llegada y el salto requieren Play Mode.
