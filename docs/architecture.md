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

La órbita y el motor usan parámetros serializados ajustables desde Inspector. El rig de referencia y el circuito S01 están configurados en `Prototype`; S05-A2 copia el rig a su escena aislada. El prefab de Player no guarda referencias a objetos de escena.

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

El transporte cubre un jugador sobre una plataforma que solo traslada posición. Rotaciones, varios pasajeros y sincronización de redes quedan fuera del prototipo. El usuario confirmó S03 en Play Mode; conservar comprobaciones de contacto, llegada y salto para regresión tras S04.

## Implementación S04-A: checkpoints y contrato de reinicio

- `CheckpointManager` guarda una pose de reaparición estable y un `RoomResetScope`; antes de activar un checkpoint utiliza `Initial Spawn` y `Initial Room`. `CheckpointTrigger` detecta la entrada de la raíz del jugador en un `BoxCollider` por consulta espacial, sin Rigidbody ni dependencia de callbacks físicos. Echo no activa checkpoints. Solo hay un checkpoint activo; entrar repetidamente no sobrescribe su pose ni reinicia la sala.
- `RestartInput` lee `Player/Restart` (R) del asset Input System existente. `RespawnZone` solicita reaparición al detectar al jugador dentro de su volumen. Las solicitudes se agrupan. Desde S04-B, la liberación y suspensión ocurren al solicitar el reinicio; la restauración y reaparición se ejecutan al final de `LateUpdate` (orden 1000), tras las actualizaciones de gameplay. No se mueve al jugador desde un callback de colisión.
- El orden de S04-A era explícito: liberar la interacción Rewind del jugador; llamar `ReleaseForReset` en todos los participantes de la sala; llamar `ResetForAttempt` en todos ellos; reposicionar y limpiar al jugador; ajustar el objetivo de cámara y notificar el cambio de posición a Cinemachine 3. Todas las liberaciones preceden a todas las restauraciones.
- `IAttemptResetParticipant` define ambas fases. `RoomResetScope` contiene una lista serializada que determina el orden; omite duplicados y referencias destruidas. Solo acepta participantes cuyo ámbito padre más próximo es esa sala. No busca ni reinicia objetos por toda la escena; una excepción de un participante se registra y no impide procesar los siguientes. `AttemptResetTransform` demuestra el contrato con dos cubos de prueba que recuperan su pose local inicial.
- `PlayerMotor.Respawn` limpia velocidades, coyote time y jump buffering, descarta movimiento de plataforma pendiente y desactiva temporalmente el `CharacterController` para colocar al jugador. Ignora el contacto grounded anterior y exige un nuevo `Move` antes de permitir transporte. `InputReleaseGate`, usado por `PlayerInputReader` y los adaptadores Q/E/RMB/R, descarta el frame de reinicio y el siguiente; las acciones activas necesitan volver a estado neutro. No deshabilita ni sustituye el asset compartido.
- `RewindAttemptRelease` solo termina la selección activa y conserva la pose recuperada. En la entrega S04-A no se vaciaba la captura Echo, no se destruye un clon activo, no se limpia el historial Rewind y no se restauran placas/puertas/plataforma/rotor. S04-B completa esta integración como se describe abajo. No usar `AttemptResetTransform` sobre mecanismos temporales: mover su Transform sin reconstruir su estado lógico/historial dejaría un estado incoherente.
- `S04CheckpointInstaller` montó en `Prototype` el punto inicial, el checkpoint A junto al acceso al circuito S01, el B en el inicio del acceso Rewind (tras la corrección final de S04) y una zona de caída bajo el escenario. Cada checkpoint tiene su propia sala lógica y cubo de prueba. La sala activa la define el checkpoint, incluso si el jugador se desplaza físicamente a otro ramal. El prefab de Player conserva sus referencias sin añadir dependencias de escena; `Laboratory` permanece intacta.

El contrato se aplica a intentos dentro de una sesión. Carga de partidas, transiciones de nivel, guardado persistente y el rompecabezas combinado están fuera de S04-A. Las poses de reaparición deben colocarse sobre suelo seguro, fuera de mecanismos y del volumen de peligro.


## Implementación S04-B: reinicio coordinado

`Game` integra las habilidades mediante adaptadores `IAttemptResetParticipant`: `EchoAttemptReset` es global y referencia al único `EchoPlayback` del jugador; no se duplica dentro de las listas de sala. `RewindAttemptRelease` sigue liberando la selección del jugador. `RewindMechanismAttemptReset`, `PressurePlateAttemptReset` y `DoorAttemptReset` viven junto al mecanismo e integran las dos fases del contrato. Los núcleos no dependen de `Game`; solo exponen operaciones pequeñas de suspensión/restauración. `EchoCapture`, `RewindHistory`, los controles y la semántica de consumo único no cambiaron.

Orden efectivo, común a R y caída:

1. `RequestRespawn` fija la solicitud y bloquea cambios de checkpoint mientras está pendiente. Conserva `InputReleaseGate` para descartar acciones mantenidas.
2. Inmediatamente, cancela Echo en cualquier estado y desactiva el clon antes de destruirlo (destrucción diferida en Play Mode). Limpia las referencias del reproductor y vuelve a `Available`; el registro interactivo elimina al actor en `OnDisable`. Libera la sesión RMB, conservando su pose hasta la restauración.
3. Ejecuta todas las liberaciones de la lista de la sala activa, una sola vez por solicitud: mecanismos Rewind suspendidos, placas vacías y suspendidas, puertas suspendidas. No muestrean ni detectan ocupación entre esta fase y la restauración. Los mecanismos de otras salas siguen su actualización normal.
4. Al final de `LateUpdate`, restaura todos los participantes en el orden serializado: placas vacías, puertas cerradas sin retención de umbral y mecanismos con la pose inicial capturada en `Awake`. Cada historial reutiliza su anillo, conserva solo una muestra inicial y reinicia su reloj a cero. La plataforma reconstruye progreso cero a partir de ese reloj. No hay muestras del intento anterior ni transporte de una pose restaurada: se omite `ApplyPose` al restaurar para no ofrecer un delta de teletransporte al pasajero.
5. Reposiciona y limpia al jugador mediante el método existente de S04-A; este sincroniza colliders, descarta el transporte pendiente y actualiza la cámara. Los mecanismos restaurados omiten cualquier tick restante de ese frame; en el siguiente vuelven al funcionamiento normal. `RespawnNow` directo aplica las mismas fases sincronizadas.

`RoomResetScope` conserva su validación del padre más próximo y su lista explícita, sin búsqueda global. `S04AbilityResetInstaller` (menú `Echoryx/S04-B/Install Coordinated Reset`) enlaza `Prototype` con APIs de Unity, conserva participantes existentes y evita duplicados. Reparenta las raíces existentes preservando sus poses globales: `Room A` contiene Echo (placa/puerta) y rotor; `Room B` contiene el cruce de plataforma. El punto inicial usa el ámbito A; su pose no cambió. La sala activa sigue definida por el checkpoint, incluso si el jugador visita otro ramal. Los prefabs no adquieren referencias a salas y `Laboratory` no cambia.

La eliminación del Echo global y el teletransporte del jugador pueden liberar naturalmente una placa en otra sala cuando vuelva a evaluarse; esto no ejecuta su contrato de reinicio ni borra su historial. Una sesión RMB sobre una sala ajena se libera, pero no restaura ese mecanismo. El checkpoint activo persiste. El contrato actual restaura mecanismos cinemáticos con pose y progreso derivados del reloj; futuros mecanismos con estado adicional necesitarán su propio adaptador.

Las poses de reaparición deben permanecer fuera del umbral de una puerta restaurada. R en B restaura la plataforma en salida y comienza un nuevo viaje. La antigua reaparición en el muelle permitía embarcar sin RMB: eliminaba el acceso largo previsto en S03-C. La corrección final mueve únicamente `Checkpoint Room B` a (5,5; 0; -4) y `Room B/Respawn Point` a (5,5; 0,05; -4). `S04CheckpointInstaller.FixRoomBResetAccess` aplica el ajuste mediante APIs de Unity, sin recrear objetos; las instalaciones nuevas usan las mismas posiciones. Una comprobación EditMode acota el contacto más temprano con la superficie aun cortando esquinas o saltando, con más de 0,25 s de margen sobre el viaje inicial. No cambia el contrato ni el orden de reinicio, la plataforma o su historial congelado en destino. La sala combinada mantiene su propio acceso.


## Implementación S04-C: montaje combinado

No incorpora lógica de gameplay nueva. `S04CombinedPuzzleInstaller` construye el ramal mediante APIs de Unity y enlaza los componentes existentes en `Prototype`: `PressurePlate` → `DoorController`, `RewindMovingPlatform` y un `CheckpointTrigger` asociado al `RoomResetScope` de `S04-C Combined Puzzle`. Reutiliza los materiales de placa, puerta, plataforma y checkpoint; solo crea `CombinedExit.mat` (URP Unlit verde) para distinguir la salida. Las señales carecen de collider y no sirven de apoyos adicionales.

La sala está separada espacialmente de S01/S02/S03. El puente une `Start Floor` con una entrada lateral; paredes de 7,5 m y techo cierran el resto del corredor. El volumen del checkpoint cubre el ancho y la altura de salto de la entrada. La placa está en z local 2, la puerta en 11, la superficie segura termina en 18 y el muelle de llegada comienza en 26,5. La plataforma viaja entre 18,5 y 25,5 a 3,5 m/s, con la misma política de espera y transporte de S03. El hueco permanece sin suelo. Su propia zona de caída solicita al mismo manager la reaparición; la zona general puede coincidir, y las solicitudes se agrupan como antes.

La lista del ámbito nuevo contiene, en orden, `PressurePlateAttemptReset`, `DoorAttemptReset` y `RewindMechanismAttemptReset`. Usa el Echo y el liberador Rewind globales ya existentes; no añade otro jugador, grabador ni manager. La activación del checkpoint no reinicia una sala ni captura progreso parcial. R/caída aplican las fases de S04-B; solo restauran los mecanismos del ámbito activo. A y B conservan sus raíces, posiciones, referencias y listas. `Laboratory`, prefabs, controles, parámetros del motor y núcleos temporales no cambian.

`CombinedPuzzleTests` verifica referencias, propiedad de ámbito, restauración, alcance de salto, acceso inicial, cierre de puerta, superficies y línea de selección. `CombinedPuzzlePlayChecks` vive en un ensamblado PlayMode de pruebas; carga la escena con la API de Editor y ejercita el controlador, Input System, colisiones y transporte reales. Usa entrada virtual y cámaras de prueba para que el recorrido sea determinista a 60 Hz. Sus ajustes temporales de Input System permiten entrada sin Game View enfocada en batchmode y se restauran al terminar. No modifica configuración del proyecto ni aporta componentes a la escena guardada. El renderizado, la órbita manual y otros FPS necesitan comprobación en el Editor visible.

## Preparación visual S05-A1

Las fuentes de terceros se aíslan en `Art/Environment/Quaternius/ModularSciFiMegaKitStandard`, con licencia original y manifiesto de procedencia. `Materials/Environment` contiene cuatro materiales básicos y cinco adaptaciones URP Lit para las trim sheets/decals seleccionadas. ModelImporter conserva unidades/pivotes y remapea slots a esos materiales; no añade colliders, animación ni componentes runtime.

`S05VisualAssetsSetup` reutiliza el ensamblado de herramientas de Editor existente para preparar y validar únicamente este subconjunto y crear `VisualAssetLab`, una escena de presentación independiente. La galería desplaza/gira instancias para mostrarlas; los FBX siguen intactos. No modifica Build Settings ni depende de Player/Echo/Rewind para renderizar. Las mallas visuales y colisiones de futura sala se separarán en S05-A2; detalles de escala y uso en [visual-assets](visual-assets.md).

## Sala visual S05-A2

`Prefabs/Environment` contiene Floor4, Wall4, InnerCorner4, Column5 y DoorFramePanel, con raíces lógicas y BoxCollider independientes de instancias FBX. Los offsets/escalas locales pertenecen a wrappers, no a importers/fuentes. El marco y panel están separados: desactivar Panel deja libre el umbral y sus colliders de postes/dintel. La escena nueva usa una instancia original de Player y copias del rig Cinemachine de Prototype con referencias remapeadas; no añade un motor, habilidades o mecanismos propios.

`S05ReferenceRoomBuilder` es una herramienta de Editor para guardar componentes estándar y validar la escena aislada. El nuevo archivo PlayMode `VisualReferenceRoomPlayChecks` prueba movimiento/colisiones/cámara existentes en ese montaje; no cambia sus implementaciones ni tests previos. Datos, disposición y límites en [visual-reference-room](visual-reference-room.md).
