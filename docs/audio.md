# Audio

## Dónde asignar los clips

`Assets/_Echoryx/Resources/AudioLibrary.asset` (Inspector). Cada entrada tiene una lista **Clips** (uno, o varios para elegir al azar) y un **Volume**. Cualquier entrada puede quedar vacía: no suena y no falla.

| Entrada | Cuándo suena |
|---|---|
| Footsteps | Cada 1,7 m andando en el suelo |
| Jump / Land | Despegue / aterrizaje tras un salto o caída |
| Pressure Plate | La placa pasa a pulsada |
| Door Open / Door Close | La puerta empieza a abrirse / a cerrarse |
| Checkpoint / Goal | Checkpoint nuevo / nivel completado |
| Echo Record («Echo grabando») | **En bucle** mientras se graba |
| Echo Play («Echo reproduciendo») | **En bucle** mientras se reproduce |
| Echo End («Echo terminar») | Una vez, al acabar la reproducción |
| Rewind | En bucle mientras se rebobina (usar un clip que haga loop) |
| Ui Click / Ui Hover | Botones de menú: clic o Enter / puntero encima o navegación con teclado o mando |
| Menu Music / Level Music | En bucle en `MainMenu` / en los niveles del catálogo, con fundidos: la anterior baja en 1,5 s y la nueva sube en 2 s |

`Echoryx/Audio/Assign Clips` asigna los clips de `Assets/_Echoryx/Audio/` y sus volúmenes. Sobrescribe esas entradas, incluida la música (Streaming, volumen 0,5, en bucle).

## Volúmenes

`Assets/_Echoryx/Audio/EchoryxMixer.mixer`: grupos `Master`, `Music` y `SFX`, con `MasterVolume`, `MusicVolume` y `SFXVolume` expuestos (dB), por ejemplo `mixer.SetFloat("MusicVolume", -10f)` desde `Start` o después. En pausa la música baja al 35 % en las fuentes de audio, sin tocar el mixer.

Los bucles (Rewind y los dos del Echo; usar clips que repitan sin cortes) entran y salen con un fade de 0,1 s. Paran al terminar, cancelar o reaparecer. En pausa se detienen en su posición y continúan al reanudar; el de Rewind termina porque la pausa suelta el Rewind.

## Piezas (`Scripts/Audio/`)

- `AudioManager`: persistente entre escenas; efectos, bucle de rewind y música con fundidos.
- `GameAudioEvents`: se añade a las escenas con `CheckpointManager`. Lee estado público y `LevelStats.Completed`. Tras un respawn (R, caída) toma el estado nuevo sin sonidos.
- `UiButtonSound`: se añade a cada botón creado por el flujo (`UiEvents.ButtonCreated`).
- `Editor/AudioAssetsBuilder` (`Echoryx/Audio/Create Mixer and Library`): crea lo que falte, sin sobrescribir. Usa la API interna del Editor porque Unity no ofrece una pública para crear mixers.

Fuera de `Scripts/Audio/` solo se añadieron `DoorController.Panel` (lectura) y `UiEvents.ButtonCreated`; no cambia la lógica de motor, Echo, Rewind ni checkpoints. Todo el audio es 2D.

## Rewind inaudible (diagnóstico)

El clip estaba asignado (volumen 0,55, grupo SFX) y, en una prueba rebobinando con el botón derecho, el bucle arrancaba y entregaba señal. La causa medida está en el archivo: casi toda la energía de `rewind.ogg` está por debajo de 150 Hz (media total −9,7 dB; −30,4 dB por encima de 300 Hz, unos 20 dB menos que los bucles del Echo), así que en altavoces pequeños casi no se oye. `Assign Clips` usa `rewind-audible.ogg`, derivado con:

```
ffmpeg -i rewind.ogg -af "highpass=f=180:p=2,highpass=f=180:p=2,volume=18dB,alimiter=limit=0.84:attack=2:release=40:level=false" -c:a libvorbis -q:a 6 rewind-audible.ogg
```
