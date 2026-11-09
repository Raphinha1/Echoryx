# Créditos y licencias

## Arte de terceros (CC0)

| Pack | Autor | Licencia | Ubicación | Uso en el proyecto |
|---|---|---|---|---|
| Modular Space Kit 1.0 | [Kenney](https://www.kenney.nl) | Creative Commons Zero (CC0 1.0) | `Assets/_Echoryx/Art/Environment/kenney_modular-space-kit_1.0/` | Paredes, pisos, marcos de puerta, plataforma móvil y estructuras del fondo de `Level_01_Tutorial`. |
| Space Station Kit | [Kenney](https://www.kenney.nl) | Creative Commons Zero (CC0 1.0) | `Assets/_Echoryx/Art/Environment/kenney_space-station-kit/` | Props con moderación: bloque de casco/escalón (`floor`), pantalla de pared (`display-wall-wide`), asteroides (`rocks`, `skip-rocks`), contenedor (`container-tall`) y la placa (`floor-panel`). |

Licencia completa de cada pack: archivo `License.txt` dentro de su carpeta; texto legal en <https://creativecommons.org/publicdomain/zero/1.0/>. CC0 no exige atribución; se acredita igualmente por cortesía.

El pack Quaternius presente en `Art/Environment/Quaternius/` **no** se usa en `Level_01_Tutorial`.

## Personaje (Unity Asset Store)

| Paquete | Licencia | Ubicación | Uso en el proyecto |
|---|---|---|---|
| Animated Low Poly Robot Free (Unity Asset Store) | Unity Asset Store EULA (<https://unity.com/legal/as-terms>) | `Assets/FreeLowPolyRobot/` (referenciado en sitio, sin copiar archivos fuera de `Assets/`) | Modelo `ModularRobots.fbx` y sus clips (`StaticIdle`, `Walk`, `Run`, `Jump_Start`, `Jump_Air`, `Jump_Landing`) para el Player (`RobotVisual`) y el Echo (`RobotEchoVisual`). |

El paquete no incluye archivo de licencia ni de créditos; consulta la ficha del Asset Store para el autor y los términos vigentes. **No se versiona en Git**: el repositorio es público y la EULA del Asset Store no permite redistribuir los archivos fuente. Para abrir el proyecto, importa el paquete desde el Asset Store en `Assets/FreeLowPolyRobot/` (los prefabs lo referencian por GUID). No se usan sus scripts (`ModularRobotRandomizer` y `MatOffset` leen el Input antiguo) ni sus escenas.

## Código propio del pase de arte

- `Assets/_Echoryx/Art/Shaders/EchoryxGradientSkybox.shader`: skybox con degradado y estrellas, escrito para este proyecto (sin texturas de terceros).
- Materiales `Assets/_Echoryx/Materials/Art/`: materiales URP propios que reutilizan las texturas `colormap.png` de cada kit.
