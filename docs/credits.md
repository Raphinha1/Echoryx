# Créditos y licencias

## Arte de terceros (CC0)

| Pack | Autor | Licencia | Ubicación | Uso en el proyecto |
|---|---|---|---|---|
| Modular Space Kit 1.0 | [Kenney](https://www.kenney.nl) | Creative Commons Zero (CC0 1.0) | `Assets/_Echoryx/Art/Environment/kenney_modular-space-kit_1.0/` | Paredes, pisos, marcos de puerta, plataforma móvil y estructuras del fondo de `Level_01_Tutorial`. |
| Space Station Kit | [Kenney](https://www.kenney.nl) | Creative Commons Zero (CC0 1.0) | `Assets/_Echoryx/Art/Environment/kenney_space-station-kit/` | Props con moderación: bloque de casco/escalón (`floor`), pantalla de pared (`display-wall-wide`), asteroides (`rocks`, `skip-rocks`), contenedor (`container-tall`) y la placa (`floor-panel`). |

Licencia completa de cada pack: archivo `License.txt` dentro de su carpeta; texto legal en <https://creativecommons.org/publicdomain/zero/1.0/>. CC0 no exige atribución; se acredita igualmente por cortesía.

El pack Quaternius (CC0), usado solo en laboratorios visuales, fue retirado; sigue en el historial de Git.

## Personaje (Unity Asset Store)

| Paquete | Licencia | Ubicación | Uso en el proyecto |
|---|---|---|---|
| Animated Low Poly Robot Free (Unity Asset Store) | Unity Asset Store EULA (<https://unity.com/legal/as-terms>) | `Assets/FreeLowPolyRobot/` (referenciado en sitio, sin copiar archivos fuera de `Assets/`) | Modelo `ModularRobots.fbx` y sus clips (`StaticIdle`, `Walk`, `Run`, `Jump_Start`, `Jump_Air`, `Jump_Landing`) para el Player (`RobotVisual`) y el Echo (`RobotEchoVisual`). |

El paquete no incluye archivo de licencia ni de créditos; consulta la ficha del Asset Store para el autor y los términos vigentes. **No se versiona en Git**: el repositorio es público y la EULA del Asset Store no permite redistribuir los archivos fuente. Para abrir el proyecto, importa el paquete desde el Asset Store en `Assets/FreeLowPolyRobot/` (los prefabs lo referencian por GUID). No se usan sus scripts (`ModularRobotRandomizer` y `MatOffset` leen el Input antiguo) ni sus escenas.

## Assets retirados

### SCI-FI SuperSoldier (retirado)

Se usó durante el desarrollo como protagonista de prueba y **fue retirado** del proyecto; el juego usa ahora el robot descrito en *Personaje*. Sus archivos **siguen en el historial de Git**, y las capturas y vídeos de la evidencia de desarrollo, [archivada](archivo/README.md), muestran el modelo, por lo que la atribución se mantiene.

| Obra | Autor | Licencia |
|---|---|---|
| [SCI-FI SuperSoldier - Game-ready [ANIMATED]](https://sketchfab.com/3d-models/sci-fi-supersoldier-game-ready-animated-09e0bd9c9715463584cd847d9b30a140) | SihaGames | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) |
| [Sci-Fi Helmet](https://sketchfab.com/3d-models/sci-fi-helmet-785f5cfd848940b1a4a0b8ddeefc9ef1) (casco del modelo, retexturizado según el autor del conjunto) | Udon-San | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) |

Cambios hechos en Echoryx: ajustes de importación Humanoid en Unity, remapeo de materiales a URP, escala y orientación de presentación, y escenas y controller de validación. El FBX y los cuatro PNG originales se conservaron byte a byte. La atribución no implica que los autores respalden este proyecto.

Estuvo en `Assets/_Echoryx/Art/Characters/SihaGamesSuperSoldier/`; esa carpeta incluía `ATTRIBUTION.md` y `provenance.json` (enlaces y SHA-256 por archivo), recuperables desde el historial.

## Audio

### Música

Autor: **MintoDog**, en OpenGameArt.

| Archivo (`Assets/_Echoryx/Audio/`) | Uso | Fuente | Licencia |
|---|---|---|---|
| `sci-fi_puzzle_title_bpm120.ogg` | Menú | <https://opengameart.org/content/sci-fi-puzzle-title> | CC0 |
| `sci-fi_puzzle_in-game_2_bpm95.ogg` | Niveles | <https://opengameart.org/node/180660> | CC0 |

### Efectos

El resto de los `.ogg` de esa carpeta (salvo `rewind-C`, ver abajo), de **Kenney Vleugels** ([Kenney.nl](https://kenney.nl)), packs **Digital Audio**, **Impact Sounds**, **Interface Sounds**, **Sci-Fi Sounds** y **UI Audio**. Licencia **CC0**, confirmada en el `License.txt` de cada pack.

CC0 no exige atribución; se acredita igualmente por cortesía (música y efectos).

`rewind-C.ogg` es un sonido sintetizado por el proyecto Echoryx (`Tools/audio/synth_rewind.py`), sin material ni licencia externos.

## Fuentes

| Fuente | Autor | Licencia | Uso |
|---|---|---|---|
| [Orbitron](https://github.com/theleagueof/orbitron) | Matt McInerney. Copyright 2018 The Orbitron Project Authors, con Reserved Font Name «Orbitron» | [SIL Open Font License 1.1](https://openfontlicense.org) | Títulos del menú, la pausa y el fin de nivel |
| [Exo 2](https://github.com/googlefonts/Exo-2.0) | Natanael Gama. Copyright 2013 The Exo 2 Project Authors | [SIL Open Font License 1.1](https://openfontlicense.org) | Resto del texto de esos menús |

Los `.ttf` están en `Assets/_Echoryx/Fonts/`, junto a `Orbitron-OFL.txt` y `Exo2-OFL.txt` con el texto completo de cada licencia; la pantalla de créditos los muestra al final. Los TMP Font Assets de `Assets/_Echoryx/Resources/Fonts/` se generan a partir de ellos (`Echoryx/Fonts/Create TMP Fonts`).

## Código propio del pase de arte

- `Assets/_Echoryx/Art/Shaders/EchoryxGradientSkybox.shader`: skybox con degradado y estrellas, escrito para este proyecto (sin texturas de terceros).
- Materiales `Assets/_Echoryx/Materials/Art/`: materiales URP propios que reutilizan las texturas `colormap.png` de cada kit.
