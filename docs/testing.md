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

**Pendiente en Unity Editor:**

1. Abrir `Prototype`, comprobar que `Main Camera` tiene `CinemachineBrain`, `Player Cinemachine Camera` sigue a `Camera Orbit Target`, y el prefab `Player` tiene el asset de acciones asignado.
2. Entrar en Play Mode. Con WASD avanzar, retroceder y desplazarse lateralmente; girar la cámara con ratón y repetir. Verificar que la dirección de movimiento sigue la cámara y que la cápsula gira con suavidad hacia el movimiento.
3. Pulsar Espacio en suelo; comprobar ascenso, caída y aterrizaje. Ajustar `moveSpeed`, `acceleration`, `deceleration`, `airControl`, `turnSpeed`, `jumpHeight`, `gravity`, `coyoteTime` y `jumpBuffer` en el Inspector para confirmar efecto. Restablecer los valores tras la prueba si no se desea conservar ajustes.
4. Saltar justo después de dejar un borde (coyote time) y pulsar Espacio justo antes de aterrizar (jump buffering). Repetir fuera de cada ventana para comprobar que no ocurre el salto.
5. Orbitar frente a un bloque y acercarse a una pared; comprobar que la cámara evita atravesarla. Ajustar sensibilidad de ratón en `Camera Orbit Target` y radio/filtro de colisión en `Player Cinemachine Camera`.
6. Recorrer `Start Floor`, `Platform 1`, `Platform 2`, `Platform 3` y `Finish Floor`, pasando los obstáculos. Inspeccionar consola durante la prueba y anotar cualquier error.
7. Confirmar que `Laboratory` abre sin cambios funcionales.

La física de plataformas móviles queda para una integración posterior; este circuito solo usa geometría estática.

## Criterios manuales por slice

- **S00:** los pasos anteriores; verificar estructura y documentos; ninguna escena debe contener mecánicas de S01+.
- **S01:** recorrer suelo, rampas, bordes y obstáculos con teclado/mando acordado; inspeccionar seguimiento de cámara y colisiones.
- **S02:** iniciar Echo manualmente, registrar menos de cinco segundos y más de cinco segundos, comprobar límite, reproducción y comportamiento sin historial.
- **S03:** alterar objetos habilitados y no habilitados, usar Rewind y comprobar restauración y aislamiento; combinar con Echo.
- **S04:** activar checkpoint, reiniciar/fallar antes y después de activarlo; comprobar posición y regla de persistencia acordada.
- **S05–S07:** completar niveles en secuencia; probar entrada/salida, checkpoints, casos de fallo y desafío final.
- **S08:** ejecutar regresión en Editor, crear build para plataforma aprobada y abrirla en entorno objetivo; revisar consola y transiciones.

Registrar dispositivo, plataforma, escena, pasos, resultado y errores. Las pruebas futuras deben cubrir límites/casos vacíos además del recorrido feliz; no considerar una compilación de scripts como validación de jugabilidad.
