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

No se ejecutaron validaciones del Editor durante la preparación de S00. Esta lista es pendiente y no representa resultados observados.

## Criterios manuales por slice

- **S00:** los pasos anteriores; verificar estructura y documentos; ninguna escena debe contener mecánicas de S01+.
- **S01:** recorrer suelo, rampas, bordes y obstáculos con teclado/mando acordado; inspeccionar seguimiento de cámara y colisiones.
- **S02:** iniciar Echo manualmente, registrar menos de cinco segundos y más de cinco segundos, comprobar límite, reproducción y comportamiento sin historial.
- **S03:** alterar objetos habilitados y no habilitados, usar Rewind y comprobar restauración y aislamiento; combinar con Echo.
- **S04:** activar checkpoint, reiniciar/fallar antes y después de activarlo; comprobar posición y regla de persistencia acordada.
- **S05–S07:** completar niveles en secuencia; probar entrada/salida, checkpoints, casos de fallo y desafío final.
- **S08:** ejecutar regresión en Editor, crear build para plataforma aprobada y abrirla en entorno objetivo; revisar consola y transiciones.

Registrar dispositivo, plataforma, escena, pasos, resultado y errores. Las pruebas futuras deben cubrir límites/casos vacíos además del recorrido feliz; no considerar una compilación de scripts como validación de jugabilidad.
