# Capturas del README

Genera las imágenes de `docs/img/`. No forma parte del juego ni de las suites de pruebas: `capture.sh` copia
`ReadmeCaptures.cs` a `Assets/_Echoryx/Tests/PlayMode/Level/` solo mientras captura y lo borra al terminar.
No guarda cambios en escenas ni en assets.

Requisitos: Unity 6000.3.29f1, paquete del robot importado, GPU con OpenGL y el Editor cerrado.

```bash
Tools/readme-media/capture.sh /tmp/capturas            # todas
Tools/readme-media/capture.sh /tmp/capturas Level3     # Menu, Level1AndEcho, Rewind o Level3
```

## Cómo captura

- Juega las escenas reales con teclado y ratón virtuales a 60 fps simulados y renderiza la cámara de juego a 1280x720.
- Coloca al jugador y fija la órbita de la cámara para encuadrar. En Rewind apunta con una cámara auxiliar, como las pruebas.
- Para incluir la UI, pasa en memoria los Canvas de *Overlay* a *Screen Space - Camera*; por eso recibe el
  posprocesado y puede verse algo distinta que en el juego. El retículo de Rewind (IMGUI) no aparece.
- Usa progreso y preferencias en memoria: el selector se ve como en una partida nueva.

## Selección y compresión

El script genera varias candidatas. Revísalas antes de copiarlas. Las publicadas:

| `docs/img/` | Candidata |
|---|---|
| `menu.png`, `niveles.png` | `menu`, `niveles` |
| `nivel1.png` | `nivel1_1` |
| `echo.png` | `echo_2` |
| `rewind.png` | `rewind_1_a` |
| `nivel3.png` | `nivel3_puente_0_a` |

```bash
magick <candidata>.png -strip -dither FloydSteinberg -colors 256 PNG8:docs/img/<nombre>.png
```
