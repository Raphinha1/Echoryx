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

**Verificación automatizada ejecutada:** Unity 6000.3.22f1 compiló los scripts, `S02InteractionInstaller` guardó los cambios de prefabs y `Prototype` mediante APIs del editor y pasaron **21/21** pruebas EditMode del proyecto (9 de S02-C y 12 existentes). Las pruebas nuevas cubren activación, desactivación, ocupación simultánea, desaparición del clon, limpieza, volumen espacial, protección del umbral y referencias de prefabs/escena. El menú `Echoryx/S02-C/Install Echo Puzzle` evita duplicar el ramal si ya existe. La jugabilidad aún requiere Play Mode.

**Pendiente en Unity Editor:**

1. Abrir `Prototype` y localizar `S02-C Echo Puzzle` al lado izquierdo de `Start Floor`. Comprobar `Echo Pressure Plate`, `Echo Door/Door Panel` con `BoxCollider`, corredor y `Exit Marker`. Confirmar que `S01 Test Circuit` conserva sus plataformas y que `Laboratory` no cambió.
2. Entrar en Play Mode. Avanzar hasta la placa: debe cambiar de naranja a verde y abrir la puerta. Salir de la placa sin Echo; la placa debe apagarse y la puerta cerrar. Acercarse a la puerta cerrada: debe impedir el paso y no abrirse por proximidad.
3. Volver a la placa, pulsar Q, permanecer sobre ella entre 3 y 5 segundos y pulsar Q para terminar. Salir, acercarse al lado de entrada de la puerta y pulsar E. El Echo debe aparecer sobre la placa, activarla y permitir cruzar hasta `Exit Marker`. La reproducción se consume una vez; E de nuevo no debe crear otro clon.
4. Repetir con una grabación en la que el jugador abandone la placa antes de terminar: la placa debe desactivarse cuando el Echo salga de su volumen. Repetir con el jugador y Echo simultáneamente sobre la placa; al salir uno, debe permanecer activa si el otro sigue dentro.
5. Soltar la placa mientras el jugador ocupa el umbral: la puerta debe mantenerse abierta. Salir completamente del umbral: debe cerrar. Volver al umbral durante el cierre: debe reabrir sin atravesar la cápsula. Comprobar que desde cierre completo la proximidad sola no vuelve a abrirla.
6. Esperar a que termine o cancelar la reproducción mientras solo el Echo pisa la placa. Confirmar que desaparece, la placa se limpia y la puerta cierra si el umbral está libre. Revisar que el jugador conserva WASD, salto y cámara, y que el circuito S01 sigue transitable.
7. Revisar Console sin excepciones ni referencias faltantes. Ajustar en Inspector volumen de `PressurePlate`, `thresholdSize`, `openOffset` y `moveSpeed` solo si la geometría observada lo requiere; anotar cualquier cambio y repetir los casos de cierre seguro.

No se ha ejecutado Play Mode de S02-C en esta entrega. La placa detecta la posición raíz de las entidades; los casos de borde con saltos o cuerpos parciales en el umbral requieren observación manual.

## Criterios manuales por slice

- **S00:** los pasos anteriores; verificar estructura y documentos; ninguna escena debe contener mecánicas de S01+.
- **S01:** recorrer suelo, rampas, bordes y obstáculos con teclado/mando acordado; inspeccionar seguimiento de cámara y colisiones.
- **S02:** iniciar Echo manualmente, registrar menos de cinco segundos y más de cinco segundos, comprobar límite, reproducción y comportamiento sin historial.
- **S03:** alterar objetos habilitados y no habilitados, usar Rewind y comprobar restauración y aislamiento; combinar con Echo.
- **S04:** activar checkpoint, reiniciar/fallar antes y después de activarlo; comprobar posición y regla de persistencia acordada.
- **S05–S07:** completar niveles en secuencia; probar entrada/salida, checkpoints, casos de fallo y desafío final.
- **S08:** ejecutar regresión en Editor, crear build para plataforma aprobada y abrirla en entorno objetivo; revisar consola y transiciones.

Registrar dispositivo, plataforma, escena, pasos, resultado y errores. Las pruebas futuras deben cubrir límites/casos vacíos además del recorrido feliz; no considerar una compilación de scripts como validación de jugabilidad.
