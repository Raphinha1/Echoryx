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

## Sonido del Rewind

`rewind-A.ogg`, `rewind-B.ogg` y `rewind-C.ogg` son **sonidos sintetizados por nosotros** para Echoryx con `Tools/audio/synth_rewind.py` (Python y NumPy, sin muestras ni material de terceros): **no dependen de ninguna licencia externa**. Duran 2 s, rondan −18 LUFS, no tienen energía apreciable bajo 300 Hz y son periódicos (el buffer es circular, así que no hay salto en la unión; cada ráfaga y tic lleva fundidos cortos).

| Menú `Echoryx/Audio/Rewind/…` | Archivo | Sonido |
|---|---|---|
| A - Cinta (**por defecto**) | `rewind-A.ogg` | Ráfagas rápidas de ruido de 1–6 kHz que se van abriendo hacia arriba |
| B - Mecanismo | `rewind-B.ogg` | Tics secos, unos 16 por segundo en patrón tic-tac, con un brillo suave |
| C - Whoosh | `rewind-C.ogg` | Cuatro barridos ascendentes de ruido con eco corto y un tono apenas perceptible |

Los menús cambian solo la entrada **Rewind** del `AudioLibrary` (volumen 0,8). `Assign Clips` vuelve a A.

Antecedentes: `rewind.ogg` (original de Kenney, sin modificar) casi no se oía en altavoces pequeños por tener la energía en los graves; la copia filtrada `rewind-audible.ogg` sonaba como una sirena. Ambos se conservan, pero ya no se usan.
