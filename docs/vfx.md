# Efectos visuales

`Scripts/Vfx/` (ensamblado `Echoryx.Vfx`). `GameVfx` se añade al ejecutar a las escenas con `CheckpointManager`; no hay cambios en escenas ni en la lógica del juego. Solo lee estado público y `LevelStats.Completed`; no hizo falta añadir eventos.

| Efecto | Se dispara con | Color |
|---|---|---|
| Aura y estela al grabar | `EchoRecorder.State == Recording` | Cian |
| Echo: destello y partículas que se condensan | aparece `EchoPlayback.ActiveActor` | Cian |
| Echo: se deshace | desaparece el actor (no al reaparecer) | Cian |
| Rewind: estelas y brillo sobre el mecanismo | `RewindableMechanism.Mode == Rewinding` | Violeta y lila |
| Checkpoint: anillos y partículas; luego brillo suave | cambia `CheckpointManager.ActiveCheckpoint` | Naranja, dorado y lila |
| Meta: explosión y destello | `LevelStats.Completed` | Dorado, naranja y lila |
| Polvo al aterrizar | caída de 1 m o más (un salto normal lo supera) | Lila grisáceo |

- **Dirección del Rewind:** las estelas siguen el desplazamiento del mecanismo mientras se rebobina, es decir, **hacia atrás respecto a su movimiento normal**.
- **Silueta del Echo:** se toma de las posiciones de los huesos, sin leer los datos de la malla.
- **Pooling:** un `ParticleSystem` por tipo y escena, con capacidad fija, alimentado con `Emit()`. Usan tiempo escalado, así que la pausa los congela.
- **Materiales:** propios, URP Particles/Unlit, en `Resources/Vfx/` (`Echoryx/VFX/Create Materials`); texturas generadas por el proyecto en `Art/Effects/`.
- **Colores:** en `VfxPalette`. El cian es exclusivo del Echo.
