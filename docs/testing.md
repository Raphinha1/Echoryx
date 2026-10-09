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

**Validación específica pendiente en Play Mode de Unity Editor:**

1. Abrir `Prototype`, iniciar Play Mode y seguir `Approach Floor` desde el costado del suelo inicial hasta `Departure Dock`. Sin pulsar RMB, observar que la plataforma ya llegó al destino al alcanzar el muelle; se desplaza desde la salida durante los primeros dos segundos y después se detiene. Esperar al menos 10 segundos: no debe regresar.
2. Sin usar RMB, intentar alcanzar la plataforma detenida desde el borde de salida con el salto normal y el coyote time; no debe ser posible. Tampoco debe poderse saltar directamente al muelle de llegada. Apuntar desde el muelle a la superficie verde azulado en destino: debe aparecer la marca cian de selección.
3. Mantener RMB sobre la plataforma hasta devolverla al muelle de salida. Soltar, subir caminando y permanecer quieto durante el avance; debe transportar al jugador con colisión estable. Saltar o caminar al muelle de llegada y tocar `Arrival Marker`.
4. Reiniciar Play Mode, dejarla detenida en destino durante más de cinco segundos y repetir RMB. Debe poder recuperarse hasta el origen pese a la espera. Repetir tras una segunda llegada. Soltar RMB a mitad del retroceso: debe avanzar desde esa posición sin saltar a la ubicación anterior.
5. Repetir RMB con el jugador apoyado en la plataforma y saltar durante el transporte: no debe teleportarse, resbalar, atravesar la superficie ni perder el control aéreo. Probar si es posible con FPS bajos y altos.
6. Recorrer el circuito S01, resolver Q/E en el rompecabezas Echo y usar RMB sobre el rotor. Revisar Console durante el cruce y la regresión: sin errores ni referencias faltantes. Si el jugador cae al hueco, reiniciar Play Mode; aún no hay checkpoint.

**Transporte:** solo se ofrece movimiento si el `CharacterController` estaba grounded y sus pies se encontraban sobre la cara superior de la plataforma antes de moverla. `PlayerMotor` combina ese delta con caminata, gravedad y salto en una sola llamada a `CharacterController.Move`. El jugador no se rebobina ni se parenta; al saltar o perder apoyo deja de recibir transporte.

## Criterios manuales por slice

- **S00:** los pasos anteriores; verificar estructura y documentos; ninguna escena debe contener mecánicas de S01+.
- **S01:** recorrer suelo, rampas, bordes y obstáculos con teclado/mando acordado; inspeccionar seguimiento de cámara y colisiones.
- **S02:** iniciar Echo manualmente, registrar menos de cinco segundos y más de cinco segundos, comprobar límite, reproducción y comportamiento sin historial.
- **S03:** alterar objetos habilitados y no habilitados, usar Rewind y comprobar restauración y aislamiento; combinar con Echo.
- **S04:** activar checkpoint, reiniciar/fallar antes y después de activarlo; comprobar posición y regla de persistencia acordada.
- **S05–S07:** completar niveles en secuencia; probar entrada/salida, checkpoints, casos de fallo y desafío final.
- **S08:** ejecutar regresión en Editor, crear build para plataforma aprobada y abrirla en entorno objetivo; revisar consola y transiciones.

Registrar dispositivo, plataforma, escena, pasos, resultado y errores. Las pruebas futuras deben cubrir límites/casos vacíos además del recorrido feliz; no considerar una compilación de scripts como validación de jugabilidad.
