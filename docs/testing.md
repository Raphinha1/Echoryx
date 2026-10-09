# Validación y pruebas

## Preparación en Unity Editor

1. Abrir el proyecto con Unity **6000.3.22f1** y esperar a que termine Package Manager/importación.
2. Revisar Package Manager: URP 17.3.0, Input System 1.20.1 y Cinemachine 3.1.7 aparecen resueltos, sin errores de descarga o incompatibilidad.
3. En Project Settings > Graphics y Quality, verificar que el pipeline activo corresponde a los assets URP existentes. Comprobar que el modo de entrada usa Input System Package (New) o Both, consistente con `activeInputHandler: 1`.
4. Abrir `Assets/Scenes/SampleScene.unity` y confirmar que la plantilla sigue operativa.
5. Crear desde Unity Editor dos escenas vacías con plantilla 3D/URP apropiada, guardarlas en `Assets/_Echoryx/Scenes/Prototype.unity` y `Laboratory.unity`. Confirmar que aparecen como assets y que Unity crea sus `.meta`.
6. Reabrir cada escena desde Project y verificar ausencia de errores de importación/serialización. No se requiere contenido de juego en S00.
7. Revisar Package Manager y consola; anotar cualquier advertencia que ya exista antes de seguir.
8. Confirmar en Git que `Assets`, `Packages` y `ProjectSettings` continúan incluidos, los `.meta` están visibles para seguimiento y `Library`, `Temp`, `Logs`, `Obj`, `Builds` y `UserSettings` quedan ignorados.

La lista anterior se escribió en S00. El estado actual de las escenas y la verificación de S01 se registra abajo.

## S01: jugador y cámara

**Ejecutado:** Unity 6000.3.22f1 en batchmode compiló scripts y generó `Player.prefab` y el circuito de `Prototype` mediante `S01PrototypeBuilder`. Las pruebas EditMode `Echoryx.Player.Tests` pasaron 3/3 (coyote time vigente/expirado, buffering vigente/expirado y consumo único). Esto no verifica control, cámara o geometría en Play Mode.

**Validación manual:** S01 fue confirmado por el usuario. Los pasos siguientes se conservan para regresión integrada y para comprobar configuraciones tras cambios de prefab.

1. Abrir `Prototype`, comprobar que `Main Camera` tiene `CinemachineBrain`, `Player Cinemachine Camera` sigue a `Camera Orbit Target`, y el prefab `Player` tiene el asset de acciones asignado.
2. Entrar en Play Mode. Con WASD avanzar, retroceder y desplazarse lateralmente; girar la cámara con ratón y repetir. Verificar que la dirección de movimiento sigue la cámara y que la cápsula gira con suavidad hacia el movimiento.
3. Pulsar Espacio en suelo; comprobar ascenso, caída y aterrizaje. Ajustar `moveSpeed`, `acceleration`, `deceleration`, `airControl`, `turnSpeed`, `jumpHeight`, `gravity`, `coyoteTime` y `jumpBuffer` en el Inspector para confirmar efecto. Restablecer los valores tras la prueba si no se desea conservar ajustes.
4. Saltar justo después de dejar un borde (coyote time) y pulsar Espacio justo antes de aterrizar (jump buffering). Repetir fuera de cada ventana para comprobar que no ocurre el salto.
5. Orbitar frente a un bloque y acercarse a una pared; comprobar que la cámara evita atravesarla. Ajustar sensibilidad de ratón en `Camera Orbit Target` y radio/filtro de colisión en `Player Cinemachine Camera`.
6. Recorrer `Start Floor`, `Platform 1`, `Platform 2`, `Platform 3` y `Finish Floor`, pasando los obstáculos. Inspeccionar consola durante la prueba y anotar cualquier error.
7. Confirmar que `Laboratory` abre sin cambios funcionales.

La física de plataformas móviles queda para una integración posterior; este circuito solo usa geometría estática.

## S02-A: grabación Echo

**Verificación automatizada ejecutada:** Unity 6000.3.22f1 en batchmode compiló S02-A y pasó 8/8 pruebas EditMode del proyecto (5 de Echo y 3 de S01). Las pruebas Echo cubren límite de cinco segundos, intervalos de muestreo con frames irregulares, datos temporales ordenados, rotación final, transiciones, sustitución y cancelación. Una prueba de integración de assets comprueba la acción Q y los componentes del prefab. Estas pruebas no verifican pulsaciones ni movimiento real en Play Mode.

**Validación manual:** el usuario confirmó S02-A en Play Mode. Los pasos siguientes se conservan para regresión.

1. Abrir `Prototype` y entrar en Play Mode. En el Inspector del Player, verificar `EchoRecorder` (5 s, 20 Hz) y `EchoRecordInput` con el asset de acciones asignado.
2. Pulsar Q una vez, mover y girar la cápsula, pulsar Q de nuevo antes de cinco segundos. La consola debe mostrar inicio y fin con duración menor de 5 s y número de muestras mayor que uno.
3. Pulsar Q, esperar más de cinco segundos sin volver a pulsar: debe terminar automáticamente con duración de 5 s. Pulsar Q otra vez debe iniciar una grabación nueva.
4. Iniciar una nueva grabación, usar `Cancel Echo Recording` desde el menú contextual del componente y verificar el mensaje de cancelación. Repetir tras detener una grabación.
5. Durante las pruebas anteriores comprobar que movimiento, salto y cámara de S01 siguen operativos y que la consola no muestra errores.

La reproducción, el clon, la placa y la puerta no formaban parte de S02-A.

## S02-B: reproducción Echo

**Verificación automatizada ejecutada:** Unity 6000.3.22f1 compiló S02-B y pasó 12/12 pruebas EditMode del proyecto. Cubren interpolación temporal y rotacional, límites y una sola muestra, exclusividad del estado `Playing`, cancelación y limpieza de datos, referencias de E/prefabs y ausencia de componentes físicos en el actor. No demuestran el aspecto visual, la destrucción en Play Mode ni el control del jugador durante la reproducción.

**Validación manual:** el usuario confirmó S02-B en Play Mode. Los pasos siguientes se conservan para regresión integrada.

1. Abrir `Prototype` en Play Mode; comprobar que `Player` hereda `EchoPlayback` y `EchoPlaybackInput`, y que el prefab `EchoActor` tiene material cian transparente.
2. Pulsar E sin grabación: no debe aparecer un clon. Grabar con Q una trayectoria que incluya avance, salto y giro; detenerla con Q o esperar el límite.
3. Alejar al jugador del punto de grabación y pulsar E. Verificar que aparece **un** clon en el origen grabado, con apariencia claramente distinta, y que recorre y gira suavemente por la trayectoria.
4. Pulsar E varias veces durante la reproducción: no debe aparecer un segundo clon. Pulsar Q mientras se reproduce: no debe iniciarse otra grabación. Mover y saltar con el jugador; conservar el control y comprobar que puede atravesar al clon sin bloqueo.
5. Comparar la duración visual con la duración registrada en consola, tanto con grabaciones cortas como de cinco segundos. Repetir con FPS distintos si el Editor lo permite. Al terminar, comprobar que el clon desaparece del Hierarchy y que Q puede iniciar otra grabación.
6. Probar una captura detenida inmediatamente (una muestra) y cancelar una reproducción desde `Cancel Echo Recording` o desactivando el componente `EchoPlayback`; comprobar limpieza sin excepciones.
7. Revisar Console: sin errores de scripts, referencias faltantes o conflictos visibles de entrada. `Player/Interact` también tiene E/Hold en la plantilla y se revisará al implementar interacciones.

## S02-C: placa y puerta Echo

**Verificación automatizada ejecutada:** Unity 6000.3.22f1 compiló los scripts, `S02InteractionInstaller` guardó los cambios de prefabs y `Prototype` mediante APIs del editor y pasaron **21/21** pruebas EditMode del proyecto (9 de S02-C y 12 existentes). Las pruebas nuevas cubren activación, desactivación, ocupación simultánea, desaparición del clon, limpieza, volumen espacial, protección del umbral y referencias de prefabs/escena. El menú `Echoryx/S02-C/Install Echo Puzzle` evita duplicar el ramal si ya existe. El usuario confirmó posteriormente S02 en Play Mode.

**Regresión en Unity Editor:** el usuario confirmó S02 en Play Mode. Conservar estos pasos para comprobar que S03-B no altera la placa ni la puerta.

1. Abrir `Prototype` y localizar `S02-C Echo Puzzle` al lado izquierdo de `Start Floor`. Comprobar `Echo Pressure Plate`, `Echo Door/Door Panel` con `BoxCollider`, corredor y `Exit Marker`. Confirmar que `S01 Test Circuit` conserva sus plataformas y que `Laboratory` no cambió.
2. Entrar en Play Mode. Avanzar hasta la placa: debe cambiar de naranja a verde y abrir la puerta. Salir de la placa sin Echo; la placa debe apagarse y la puerta cerrar. Acercarse a la puerta cerrada: debe impedir el paso y no abrirse por proximidad.
3. Volver a la placa, pulsar Q, permanecer sobre ella entre 3 y 5 segundos y pulsar Q para terminar. Salir, acercarse al lado de entrada de la puerta y pulsar E. El Echo debe aparecer sobre la placa, activarla y permitir cruzar hasta `Exit Marker`. La reproducción se consume una vez; E de nuevo no debe crear otro clon.
4. Repetir con una grabación en la que el jugador abandone la placa antes de terminar: la placa debe desactivarse cuando el Echo salga de su volumen. Repetir con el jugador y Echo simultáneamente sobre la placa; al salir uno, debe permanecer activa si el otro sigue dentro.
5. Soltar la placa mientras el jugador ocupa el umbral: la puerta debe mantenerse abierta. Salir completamente del umbral: debe cerrar. Volver al umbral durante el cierre: debe reabrir sin atravesar la cápsula. Comprobar que desde cierre completo la proximidad sola no vuelve a abrirla.
6. Esperar a que termine o cancelar la reproducción mientras solo el Echo pisa la placa. Confirmar que desaparece, la placa se limpia y la puerta cierra si el umbral está libre. Revisar que el jugador conserva WASD, salto y cámara, y que el circuito S01 sigue transitable.
7. Revisar Console sin excepciones ni referencias faltantes. Ajustar en Inspector volumen de `PressurePlate`, `thresholdSize`, `openOffset` y `moveSpeed` solo si la geometría observada lo requiere; anotar cualquier cambio y repetir los casos de cierre seguro.

La placa detecta la posición raíz de las entidades; los casos de borde con saltos o cuerpos parciales en el umbral siguen siendo comprobaciones útiles de regresión.

## S03-A: historial y Rewind de mecanismos

El núcleo no se instala en `Prototype` ni `Laboratory` durante S03-A. Unity 6000.3.22f1 compiló y pasó **30/30 pruebas EditMode del proyecto** (9 de Rewind y 21 anteriores). Las nuevas pruebas cubren ventana y capacidad, interpolación temporal de posición/rotación, transiciones, suspensión del registro durante el retroceso, límite del historial, reanudación desde la pose recuperada, descarte del futuro, reinicio y entradas insuficientes.

**Integración prevista en S03-A y preparada por S03-B:** el rotor de prueba y RMB ya están montados; los pasos siguientes sirven para su validación en Play Mode.

1. Crear un mecanismo cinemático concreto derivado de `RewindableMechanism`; concentrar su avance normal en `AdvanceNormal`, sin un segundo `Update` que mueva el mismo Transform durante Rewind.
2. Verificar en Inspector duración de historial (máximo cinco segundos), muestras por segundo y velocidad de retroceso. Dejarlo avanzar al menos cinco segundos y comprobar que el límite de retroceso no excede la ventana.
3. Iniciar y detener Rewind sobre ese único mecanismo; observar interpolación suave con distintos FPS. Soltar a mitad de trayectoria y verificar que continúa desde la pose recuperada, sin saltar a la pose que tenía antes de rebobinar.
4. Cambiar el sentido del movimiento normal tras soltar Rewind y volver a rebobinar: la rama futura antigua no debe reaparecer. Repetir con historial vacío/corto, reinicio y desactivación durante el retroceso.
5. Comprobar regresión del jugador, Echo y placa/puerta en `Prototype` y revisar la consola.

S03-A no añadió controles de entrada, selección ni un mecanismo concreto a una escena. S03-B proporcionó el rotor y el control para esa comprobación.

## S03-B: selección de cámara y RMB

**Verificación ejecutada:** Unity 6000.3.22f1 compiló, `S03RewindInstaller` añadió `Player/Rewind` con `<Mouse>/rightButton` y enlazó `RewindInteractor` en `Player.prefab`. En `Prototype`, el objeto `S03-B Rewind Test Rotor` gira sin Rigidbody y tiene su `BoxCollider` en `Rotor Visual and Collider`, un hijo del mecanismo. Pasaron **41/41 pruebas EditMode del proyecto** (11 nuevas de S03-B y 30 anteriores), con selección por collider hijo, obstáculos y alcance, bloqueo durante pulsación, historial insuficiente, liberación, destrucción, desactivación, exclusividad y referencias de escena/prefab. El menú `Echoryx/S03-B/Install Rewind Interaction` evita duplicar la acción, el componente y el rotor.

**Validación manual:** el usuario confirmó 10/10 pruebas de S03-B en Play Mode. Los pasos siguientes se conservan para regresión tras integrar la plataforma.

1. Abrir `Prototype`, entrar en Play Mode y apuntar el centro de la cámara al rotor violeta situado a un lado del suelo inicial. Dejarlo girar al menos un segundo; la marca central debe mostrarse cian. Antes de disponer de historial suficiente, debe aparecer ámbar.
2. Mantener RMB: la marca debe pasar a violeta y el rotor debe girar hacia atrás. Mover el ratón para orbitar la cámara mientras RMB sigue pulsado; el rotor inicial debe continuar siendo el objetivo aunque la cámara deje de apuntarle.
3. Soltar RMB a mitad del retroceso: el rotor debe seguir girando normalmente desde la orientación recuperada, sin volver a la anterior. Repetir cerca del límite de cinco segundos y volver a rebobinar para comprobar que no reaparece la rama futura descartada.
4. Apuntar a otro objeto o a una pared delante del rotor: no debe seleccionarse el rotor a través del obstáculo. Alejarse más allá de `maximumDistance` y confirmar que no se selecciona. Mantener RMB sobre suelo u objeto incompatible, mover la mira al rotor sin soltar: no debe cambiarse de objetivo hasta una nueva pulsación.
5. Desactivar el rotor o su componente mientras se rebobina y comprobar que la interacción se libera sin excepciones; volver a activarlo y exigir una nueva pulsación. Comprobar WASD, salto, Q, E y órbita de cámara junto al rotor y el rompecabezas Echo.
6. Revisar Console y los parámetros `maximumDistance`, `targetLayers`, `historySeconds`, `samplesPerSecond` y `rewindSpeed` en Inspector. Restablecer ajustes temporales tras la prueba.

El rotor permite validar la interacción, la selección mediante collider hijo y el retroceso de rotación. La plataforma móvil y sus colisiones de gameplay corresponden a S03-C; S03-B no demuestra aún ese caso.

## S03-C: plataforma móvil y cruce

**Verificación:** el usuario confirmó 11/11 pruebas manuales de transporte, colisión y continuación de la primera versión. El ajuste final conserva el `BoxCollider` sólido y el transporte, y cambia el recorrido a un solo viaje de 7 m a 3,5 m/s. Un acceso largo impide alcanzar el muelle antes del primer viaje; el extremo de llegada queda fuera del alcance de salto desde la salida, incluso considerando coyote time. El viaje dura unos dos segundos y cabe en el historial configurado de cinco. Unity 6000.3.22f1 guardó la escena mediante `S03PlatformInstaller`, compiló el proyecto y pasó **48/48 pruebas EditMode**: llegada y detención, recuperación y continuación, espera prolongada, política de transporte, geometría de salto, acceso, línea de visión y regresiones existentes. Estas pruebas no sustituyen la nueva validación jugable en Play Mode.

**Política de espera:** al llegar, `RewindMovingPlatform` detiene tanto el movimiento como el avance de su propio historial. Las poses del trayecto no se sustituyen por poses idénticas durante una espera larga. RMB puede devolverla hasta el muelle de salida; al soltar, reanuda el avance desde el progreso recuperado. La política solo afecta a esta plataforma, no al rotor ni al tiempo del jugador o Echo. Si se cambia la velocidad o la duración del historial en Inspector, comprobar de nuevo que el trayecto completo cabe en la ventana.

**Regresión en Play Mode de Unity Editor:** el usuario confirmó posteriormente S03 completo y validado en `fc73688`; conservar los pasos siguientes para comprobarlo tras S04-A.

1. Abrir `Prototype`, iniciar Play Mode y seguir `Approach Floor` desde el costado del suelo inicial hasta `Departure Dock`. Sin pulsar RMB, observar que la plataforma ya llegó al destino al alcanzar el muelle; se desplaza desde la salida durante los primeros dos segundos y después se detiene. Esperar al menos 10 segundos: no debe regresar.
2. Sin usar RMB, intentar alcanzar la plataforma detenida desde el borde de salida con el salto normal y el coyote time; no debe ser posible. Tampoco debe poderse saltar directamente al muelle de llegada. Apuntar desde el muelle a la superficie verde azulado en destino: debe aparecer la marca cian de selección.
3. Mantener RMB sobre la plataforma hasta devolverla al muelle de salida. Soltar, subir caminando y permanecer quieto durante el avance; debe transportar al jugador con colisión estable. Saltar o caminar al muelle de llegada y tocar `Arrival Marker`.
4. Reiniciar Play Mode, dejarla detenida en destino durante más de cinco segundos y repetir RMB. Debe poder recuperarse hasta el origen pese a la espera. Repetir tras una segunda llegada. Soltar RMB a mitad del retroceso: debe avanzar desde esa posición sin saltar a la ubicación anterior.
5. Repetir RMB con el jugador apoyado en la plataforma y saltar durante el transporte: no debe teleportarse, resbalar, atravesar la superficie ni perder el control aéreo. Probar si es posible con FPS bajos y altos.
6. Recorrer el circuito S01, resolver Q/E en el rompecabezas Echo y usar RMB sobre el rotor. Revisar Console durante el cruce y la regresión: sin errores ni referencias faltantes. S04-A incorpora ahora R y reaparición al caer para repetir este recorrido.

**Transporte:** solo se ofrece movimiento si el `CharacterController` estaba grounded y sus pies se encontraban sobre la cara superior de la plataforma antes de moverla. `PlayerMotor` combina ese delta con caminata, gravedad y salto en una sola llamada a `CharacterController.Move`. El jugador no se rebobina ni se parenta; al saltar o perder apoyo deja de recibir transporte.

## S04-A: checkpoints y reaparición

`S04CheckpointInstaller` generó los recursos mediante APIs de Unity (menú `Echoryx/S04-A/Install Checkpoints`, sin duplicar un montaje existente). Unity 6000.3.22f1 compiló y pasó **61/61 pruebas EditMode**: las 48 existentes y 13 de S04-A. Cubren punto inicial, selección y activación repetida, entrada espacial, checkpoint destruido, ámbito, orden de fases, solicitudes agrupadas, peligro, velocidad/salto/transporte, contrato de restauración, liberación Rewind sin limpiar historial, entradas mantenidas y referencias de escena/R. El usuario confirmó S01–S03 completos y validados antes de esta entrega. El usuario confirmó la validación manual de S04-A.

**Regresión manual de S04-A (el reinicio de habilidades se amplía en S04-B):**

1. Abrir `Prototype`, comprobar el objeto `S04-A Checkpoints`, sus referencias Player, Initial Spawn, Initial Room, cámara y liberador Rewind. Antes de pisar un checkpoint, moverse/saltar y pulsar R: debe regresar al punto inicial sin velocidad ni salto automático. Mantener R no debe producir reinicios repetidos.
2. Pisar el marcador naranja `Checkpoint Room A` junto a la salida del suelo inicial hacia el circuito S01 (z = 4). Debe pasar a verde. Salir y volver a entrar; no debe teletransportarse ni reiniciar mecanismos. Alejarse y pulsar R: debe regresar a `Room A/Respawn Point`.
3. Pisar `Checkpoint Room B` al inicio de `Approach Floor` (x = 5,5; z = -4), antes del muelle. B debe quedar verde y A naranja. Pulsar R y comprobar la nueva pose; volver a A y confirmar que vuelve a sustituir a B.
4. En Play Mode, cambiar desde Inspector la posición o rotación de `Reset Probe Room A` y `Reset Probe Room B`. Activar A y pulsar R: solo A debe recuperar su pose inicial. Repetir con B. Desde S04-B, antes de cualquier checkpoint R aplica el ámbito A; Echo, rotor y plataforma están inscritos según la sección siguiente.
5. Caer por el hueco Rewind o salir de una superficie. Al atravesar `Fall Respawn Zone` (por debajo de y = -5), debe volver al checkpoint activo. Repetir antes del primer checkpoint y durante un salto; no debe acumularse gravedad ni aparecer un salto pendiente al aterrizar.
6. Reaparecer manteniendo WASD, Espacio o RMB: la acción mantenida debe exigir soltarse antes de operar otra vez. Comprobar que una nueva pulsación funciona. Mantener RMB sobre un mecanismo y pulsar R: debe liberarse el objetivo, sin volver a iniciar Rewind hasta soltar RMB. Desde S04-B, si pertenece a la sala activa también recupera su pose inicial. Probar también R y Q/E simultáneos.
7. Subir a la plataforma, saltar y pulsar R, o caer desde ella: al reaparecer no debe recibir su desplazamiento pendiente. Volver a subir y comprobar transporte normal. La cámara debe seguir al jugador tras reaparecer sin barrer el trayecto de caída; conservar la orientación de órbita.
8. Repetir S01, Q/E, placa/puerta y RMB sobre rotor/plataforma. S04-B cancela cualquier captura/clon y restaura los mecanismos temporales de la sala activa; aplicar las comprobaciones detalladas siguientes. Revisar Console durante todas las pruebas: sin errores ni referencias faltantes.

Estas verificaciones cubren interacción real, colisión, cámara y regresión jugable; no se sustituyen por pruebas EditMode.

## S04-B: reinicio coordinado

**Verificado automáticamente:** Unity 6000.3.22f1 compiló y pasó **74/74 EditMode** (61 anteriores y 13 nuevas). Las nuevas cubren cancelación inmediata de captura, datos preparados y clon; caída; reinicio directo/repetido; bloqueo del checkpoint durante una solicitud; liberación RMB; limpieza de historial y exclusividad; plataforma en origen con progreso cero y continuación; omisión del transporte al restaurar; suspensión entre fases; limpieza de ocupación y retención de puerta; aislamiento real entre salas; y referencias/listas de Prototype. Las pruebas anteriores de protección de entradas mantenidas siguen pasando. El instalador se ejecutó dos veces; la segunda ejecución dejó la escena idéntica (hash comprobado), sin duplicar participantes. Unity generó los nuevos `.meta`. Los tests invocan las operaciones/lifecycle en EditMode con reloj controlado: no prueban contactos físicos ni la sensación de cámara en una partida.

**Configuración:** Prototype ya está guardada mediante APIs de Unity. Para replicar el montaje sobre S04-A, usar `Echoryx/S04-B/Install Coordinated Reset`. En `S04-A Checkpoints`, verificar `Echo Reset → EchoAttemptReset → Player/EchoPlayback`; el liberador Rewind conserva `Player/RewindInteractor`. `Initial Room` del manager referencia A, conservando el punto inicial. A registra su cubo, placa, puerta y rotor; B registra su cubo y plataforma. Cada adaptador debe estar bajo el ámbito de la lista que lo contiene. En otra escena, registrar explícitamente los adaptadores equivalentes y situar la reaparición fuera de puertas y peligros; no añadir un Echo global a cada sala.

**Validación manual de S04-B:** el usuario confirmó 11/11 comprobaciones. Conservar estos pasos para regresión.

1. Antes de pisar checkpoints, iniciar Q, caminar y pulsar R durante la captura. En ese mismo intento debe quedar `Available`, con cero muestras y sin grabación preparada; después de reaparecer E no debe reproducir nada. Q debe iniciar una captura nueva cuando se haya soltado. Repetir activando A y B, y cayendo durante la captura.
2. Grabar Q/Q, dejar datos preparados y reiniciar con R y con caída. E no debe recuperar la grabación anterior. Crear un clon con E y repetir: debe desaparecer inmediatamente, dejar la placa sin Echo y permitir Q otra vez. Repetir antes de agotar sus cinco segundos y cerca del final de reproducción.
3. Activar A, mantener RMB sobre el rotor y pulsar R. La marca de retroceso debe desaparecer; el rotor recupera su orientación inicial y gira normalmente desde ella. Al comienzo solo debe existir la nueva muestra inicial; su historial vuelve a crecer. RMB mantenido no debe iniciar otra sesión sin soltarlo. Apuntar a otro mecanismo tras soltarlo para comprobar exclusividad liberada.
4. Activar B, dejar la plataforma en destino y mantener RMB hasta mitad del recorrido. Pulsar R con el jugador encima o provocar una caída: plataforma en origen, progreso/historial nuevos, jugador en B sin delta de plataforma ni salto residual. El nuevo viaje se detiene en destino; RMB y la espera prolongada siguen funcionando sobre el historial de este intento.
5. En A, activar la placa con jugador y Echo simultáneamente; abrir la puerta y situarse en el umbral. R debe eliminar el clon, vaciar la ocupación, cerrar la puerta y reaparecer en suelo seguro. Volver a la placa: debe funcionar normalmente y no conservar apertura retenida ni ocupantes fantasma. Repetir por caída con el clon en la placa.
6. Probar aislamiento: con B activo, variar rotor y puerta de A y usar R. No deben recuperar poses iniciales ni borrar historiales por contrato. Con A activo, observar la plataforma de B en destino, usar R y comprobar que permanece allí con el historial anterior. Al eliminar Echo global o mover al jugador, una placa ajena puede desactivarse por ausencia real: es detección normal, no restauración de esa sala.
7. Mantener WASD, Espacio, Q, E o RMB durante R/caída: exigir estado neutro antes de operar otra vez; mantener R no debe repetir reinicios. Una pulsación nueva debe funcionar. Repetir solicitudes y ciclos Q/E/RMB/R en distintos frames, sin acciones residuales ni dos clones/sesiones.
8. Recorrer S01, resolver Echo y cruzar con Rewind sin reiniciar; comprobar cámara, aterrizaje, salto, transporte y protección normal del umbral. Revisar Console: sin errores ni referencias faltantes. Repetir esta revisión de Console después de cualquier cambio.

**Resultado posterior de S04-C:** se reprodujo el atajo de B mediante caminar/saltar tras R, sin RMB. La corrección final de S04 reubica el checkpoint B; `HistoricalRoomBDockSpawnReproducesShortcut` conserva una configuración histórica solo en memoria para contrastarla con las pruebas del acceso corregido.

## S04-C: sala combinada

**Verificación automatizada ejecutada:** Unity 6000.3.22f1 compiló y pasó **82/82 EditMode** (74 anteriores y ocho nuevos) y **3/3 PlayMode** automatizados en Editor batchmode sin renderizado. Hubo dos soluciones completas tras reiniciar, sin errores de gameplay inesperados en los casos ejecutados. Repetir el instalador dejó Prototype idéntica (hash comprobado), sin duplicados. Esto no equivale a la validación manual del montaje visible.

Los ocho nuevos casos EditMode comprueban referencias y orden de participantes, checkpoint propio y habilidades globales únicas, tiempo mínimo de acceso tras R, alcance de salto/coyote time, margen de cierre y reproducción, paredes/techo, volumen de entrada y seguridad de reaparición, colliders sólidos y línea de selección, y restauración del ámbito nuevo sin borrar el historial del rotor ajeno. Se actualizó la comprobación S04-A para admitir el tercer checkpoint, manteniendo las comprobaciones de A/B.

Tres casos PlayMode cargan `Prototype` y utilizan el controlador y colisiones reales:

- B (montaje histórico): reaparecer, caminar, saltar al viaje recién iniciado y cruzar sin usar Rewind. El caso original se conserva como fixture histórica; los casos nuevos verifican el montaje corregido.
- Sala combinada: fallar sin Echo y con solo Rewind; repetir R; grabar cinco segundos con Q, reproducir con E, esperar en suelo seguro y fallar al intentar el hueco con carrera/salto y solo Echo. Después completa la sala dos veces tras R, usando el raycast y RMB reales, subiendo con Rewind mantenido y viajando hasta salida.
- Reinicio: R con clon y sesión RMB activos, protección de RMB mantenido, historial nuevo, caída durante grabación y aislamiento de la plataforma B.

El recorrido automatizado usa teclado/ratón virtuales, una base de movimiento y cámara de selección de prueba, y 60 Hz simulados; no demuestra la lectura visual, la órbita manual ni todas las rutas o dispositivos posibles. El fallo de los primeros ensayos de entrada se corrigió en el harness: batchmode no tiene Game View enfocada. Los ajustes de entrada para las pruebas se aplican a una copia temporal y se restauran; la configuración de producción permanece intacta.

**Configuración:** la sala ya está guardada en Prototype. Menú `Echoryx/S04-C/Install Combined Puzzle` para incorporarla sobre S04-B; se niega a duplicar/reemplazar una sala existente. En `S04-A Checkpoints/S04-C Combined Puzzle`, verificar checkpoint → manager existente / ámbito propio / Respawn Point, puerta → Combined Echo Plate, plataforma → marcadores propios / PlayerPlatformCarrier existente, y lista de ámbito con placa, puerta y plataforma. La zona de caída referencia al mismo manager. La salida es un marcador provisional, sin transición de escena.

**Ejecución reproducible:** en Unity Test Runner ejecutar las suites EditMode y PlayMode. Por CLI, con el Editor de la versión fijada y sin otra instancia abierta sobre el proyecto:

```bash
# Ajustar esta ruta si Unity Hub está instalado en otra ubicación.
echoryx_editor="/home/raphinha/Unity/Hub/Editor/6000.3.22f1/Editor/Unity"
"$echoryx_editor" -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testResults /tmp/echoryx-s04c-edit.xml -logFile /tmp/echoryx-s04c-edit.log
"$echoryx_editor" -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform PlayMode -testResults /tmp/echoryx-s04c-play.xml -logFile /tmp/echoryx-s04c-play.log
```

Las pruebas PlayMode utilizan la escena guardada desde Editor; no están diseñadas como validación de un build standalone. La ejecución completa de EditMode sigue incluyendo las pruebas de S01–S04-B.

**Solución manual y regresión de S04-C:** el usuario confirmó 11/11 comprobaciones manuales; conservar estos pasos para regresión.

1. Abrir Prototype, entrar en Play Mode y seguir el puente derecho desde Start Floor hasta x = 22, z = -1. Comprobar que el checkpoint naranja cambia a verde y sustituye A/B; no debe haber otro checkpoint detrás de la puerta. Revisar referencias y Console.
2. Pisar la placa naranja y grabar 4–5 s con Q. Alejarse al lado de entrada de la puerta azul, pulsar E, cruzar y detenerse en Safe Departure Floor (z local 13,5–18). Dejar expirar Echo: el jugador permanece sobre suelo seguro, la placa se limpia y la puerta cierra sin atraparlo. Repetir dejando expirar Echo mientras ocupa el umbral: conserva la protección existente.
3. Desde Rewind Aim Mark, apuntar a la plataforma verde azulado en destino y mantener RMB hasta origen. Mantenerlo mientras se sube caminando, soltar ya apoyado y viajar al muelle de llegada. Caminar hasta Combined Exit Marker verde. La órbita debe dejar seleccionar la plataforma dentro de 12 m sin golpear paredes/techo ni bloquear accidentalmente el raycast.
4. Reiniciar e intentar sin Q/E: correr desde la placa, saltar, rozar el umbral, rodear las paredes o acceder desde los circuitos vecinos. La puerta debe impedirlo. Intentar usar solo RMB: la puerta no es un objetivo Rewind y no debe abrirse.
5. Usar Echo para pasar la puerta, esperar al menos 20 s y tratar de cruzar el hueco sin RMB, con carrera, salto normal y coyote time. No debe alcanzarse la plataforma detenida ni el muelle final; caer debe reaparecer en el checkpoint de entrada. La plataforma no regresa esperando.
6. Pulsar R repetidamente desde entrada, puerta, suelo intermedio y plataforma. Siempre debe reaparecer antes de ambas barreras: la plataforma comienza su viaje de 2 s y llega antes de que se pueda alcanzarla. No deben sobrevivir captura, grabación preparada, clon, sesión RMB, ocupación, retención de puerta, transporte o historial anterior. Repetir manteniendo WASD, Espacio, Q, E y RMB: exigir soltarlos antes de actuar de nuevo.
7. Repetir la solución completa al menos tres veces, también fallando con Echo activo y cayendo desde la plataforma. Probar subir, permanecer, saltar y rebobinar con el jugador encima, a 30/60/120 FPS. Sin saltos de posición, caídas inesperadas o cierre atravesando al jugador.
8. Con el checkpoint combinado activo, observar A/B antes y después de R: sus historiales/poses no se restauran. Repetir la regresión de S01, rompecabezas Echo anterior, rotor y cruce B; comprobar que Laboratory no cambió. En B, comprobar el acceso corregido según la sección siguiente: R devuelve al inicio del acceso y no permite embarcar en el viaje inicial.

**Alcance:** la comprobación humana de S04-C fue confirmada por el usuario. Las nuevas modificaciones B necesitan su propia regresión visual; la automatización no cubre todas las rutas ni diferentes FPS. La necesidad de ambas habilidades está respaldada por geometría y los recorridos automatizados indicados; no se declara una exploración exhaustiva de toda ruta posible.

## Corrección final S04: acceso B tras reinicio

**Causa y cambio:** la reaparición original (6; 0,05; -20), en el muelle, eliminaba el acceso largo de S03-C. R restauraba la plataforma y dejaba al jugador a tiempo de embarcar en su viaje inicial. Unity Editor trasladó únicamente `Checkpoint Room B` a (5,5; 0; -4) y su `Respawn Point` a (5,5; 0,05; -4), sobre el acceso existente. El contacto más temprano con la plataforma requiere más de 2,37 s a 6 m/s descontando radio/skin del personaje, frente al recorrido de 2 s. Saltar o cortar esquinas no reduce esa separación mínima sobre Z. El salto hasta la plataforma detenida continúa fuera del alcance validado. No se modificaron velocidad, plataforma, transporte, ventana de historial, semántica de R, participantes ni otras salas.

**Pruebas ejecutadas:** Unity 6000.3.22f1 pasó **83/83 EditMode** y **5/5 PlayMode** con la escena corregida. Antes del cambio, la prueba original cruzó sin RMB; la nueva comprobación de acceso falló porque aún faltaban 5,37 m de viaje al llegar al muelle. Tras el ajuste:

- `RoomBRespawnHasSafeApproachLongerThanInitialPlatformTrip`: margen mínimo de acceso, apoyo seguro y propiedad del ámbito B.
- `HistoricalRoomBDockSpawnReproducesShortcut`: el antiguo spawn se aplica solo en memoria; el mismo R, salto, colisiones y transporte reales aún reproducen el atajo histórico. El fixture no se guarda.
- `RoomBResetAccessOutlastsInitialTrip`: activa el checkpoint mediante el volumen real; dos reinicios con R y uno por zona de caída; aproximación sin espera deliberada; salto de borde/coyote sin embarcar ni cruzar; reaparición consistente y ámbito combinado intacto.
- `RoomBRemainsRecoverableAfterLongWaitAndRepeatsWithRmb`: dos cruces completos tras R, espera de 10 s, raycast y RMB reales, embarque con RMB mantenido, liberación sin salto de posición y transporte hasta llegada.
- Los dos casos PlayMode combinados existentes siguen pasando: necesidad de ambas habilidades, dos soluciones, limpieza de Echo/Rewind, entradas mantenidas y aislamiento. Las 82 pruebas EditMode previas también pasan.

Los casos PlayMode usan motor, colliders y entradas reales con cámaras de prueba y tiempo simulado a 60 Hz. No equivalen a comprobar visualmente la cámara de producción ni todos los FPS. Los logs incluyen mensajes no fatales del servicio de licencias al arrancar; no hubo fallos de compilación ni logs de gameplay inesperados en las suites. EditMode incluye errores esperados capturados por pruebas del contrato de ámbito.

**Integridad del cambio:** repetir el ajuste dejó el hash de Prototype idéntico. Comparada con la copia previa a esta corrección, la escena solo cambia dos posiciones; Packages, ProjectSettings, prefabs, EchoCapture, RewindHistory y componentes de movimiento no cambiaron. `git diff --check` sobre todos los cambios S04 detecta espacios finales de serialización Unity ya presentes antes de este ajuste; la comparación de esta corrección y los documentos no introduce otros. Se conserva el formato del Editor.

**Montaje:** Prototype ya está guardada. Para aplicar el ajuste a una copia que aún conserve el checkpoint antiguo, ejecutar `Echoryx/S04/Fix Room B Reset Access`. Usa APIs de Unity sobre los objetos existentes; el instalador S04-A también crea ahora B en estas posiciones. No volver a generar todo el circuito.

**Regresión manual pendiente para este ajuste:**

1. Abrir Prototype en Play Mode. Pisar el marcador B al comienzo del ramal (x = 5,5; z = -4). Debe quedar verde; avanzar al muelle y pulsar R. Reaparecer en ese marcador, con movimiento limpio y cámara estable. Repetir desde llegada y desde la plataforma.
2. Tras R, soltar controles y recorrer el acceso a máxima velocidad; intentar cortar esquinas y saltar para alcanzar el viaje inicial. La plataforma debe llegar antes del embarque. Repetir R varias veces: ningún reinicio debe adelantar al jugador al muelle.
3. Sin RMB, esperar en salida al menos 10–20 s e intentar saltar desde el borde, con carrera y coyote time, hacia la plataforma detenida o el muelle final. No debe cruzarse; la caída debe devolver al mismo checkpoint lejano y reiniciar el viaje.
4. Volver al muelle, esperar más de cinco segundos y apuntar a la plataforma: debe seguir seleccionable. Mantener RMB hasta origen, subir mientras sigue pulsado y soltar ya apoyado. Debe continuar desde ahí, transportar al jugador y permitir llegar al marcador de llegada.
5. Repetir la solución tras R y tras una caída. Soltar RMB a mitad del retroceso y rebobinar con el jugador encima; confirmar continuación, colisión y transporte sin saltos. Probar 30/60/120 FPS y revisar Console.
6. Con B activo, comprobar que R no restaura A ni la sala combinada. Repetir S01, Echo, rotor y la solución combinada con su propio checkpoint; sus controles y funcionamiento deben mantenerse. No cerrar S04 por la sola automatización: confirmar esta regresión visual del acceso B.

## Criterios manuales por slice

- **S00:** los pasos anteriores; verificar estructura y documentos; ninguna escena debe contener mecánicas de S01+.
- **S01:** recorrer suelo, rampas, bordes y obstáculos con teclado/mando acordado; inspeccionar seguimiento de cámara y colisiones.
- **S02:** iniciar Echo manualmente, registrar menos de cinco segundos y más de cinco segundos, comprobar límite, reproducción y comportamiento sin historial.
- **S03:** alterar objetos habilitados y no habilitados, usar Rewind y comprobar restauración y aislamiento; combinar con Echo.
- **S04:** activar checkpoint, reiniciar/fallar antes y después de activarlo; comprobar posición y regla de persistencia acordada.
- **S05–S07:** completar niveles en secuencia; probar entrada/salida, checkpoints, casos de fallo y desafío final.
- **S08:** ejecutar regresión en Editor, crear build para plataforma aprobada y abrirla en entorno objetivo; revisar consola y transiciones.

Registrar dispositivo, plataforma, escena, pasos, resultado y errores. Las pruebas futuras deben cubrir límites/casos vacíos además del recorrido feliz; no considerar una compilación de scripts como validación de jugabilidad.
