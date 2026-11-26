# Auditoría de limpieza

Fecha: 2026-11-26. Rama `limpieza`, commit `70304f0`, Unity `6000.3.29f1`. **No se borró, movió ni modificó nada del proyecto**; el único archivo nuevo es este informe. No hay commits.

## 1. Resumen

| Grupo | Contenido principal | Tamaño en disco (con `.meta`) | ¿Requiere decisión? |
|---|---|---:|---|
| A puro (L1–L3) | Plantilla URP y restos de Kenney | 25,1 MiB | No |
| Clúster SuperSoldier (L4a–c) | Personaje, clips, labs, código y tests | 85,1 MiB | Sí: arrastra código, tests y atribución |
| B (L5–L8) | Docs del SuperSoldier, Quaternius, instaladores y `Laboratory` | 17,1 MiB | Sí |
| **Total L1–L8** | | **≈127 MiB** | |

L5 (≈10,5 MiB) está en `docs/`; el resto (≈117 MiB) está en `Assets/`, que hoy ocupa unos 132 MiB. **El `.git` (107 MiB) no se reduce** al borrar, porque el historial conserva los blobs.

Cosas que no coinciden con lo esperado:

1. `docs/credits.md` **no menciona** al SuperSoldier, a CC BY 4.0 ni a Mixamo. La atribución vive solo en `ATTRIBUTION.md`, `USAGE.md` y los `provenance.json`, que están dentro de las carpetas candidatas a borrar.
2. `Assets/Scenes/SampleScene.unity` ya no existe (la carpeta está vacía). En cambio, `Assets/Settings/SampleSceneProfile.asset` **está vivo**: lo usa `Prototype`.
3. De Cybernet Warrior **no quedan assets**; solo su registro documental.
4. Los visuales de cápsula desactivados no son archivos: son objetos dentro de prefabs vivos y hay tests que exigen que existan. Van en B.
5. El SuperSoldier es seguro para el juego, pero **no para el proyecto tal como está**: hay código y tests ligados, un builder vivo abre uno de sus laboratorios, y un test de código vivo está dentro de su fixture.
6. Build Settings tiene `Prototype` en el índice 0 y `Level_01_Tutorial` en el 1: **un build arrancaría en `Prototype`**.
7. `ProjectVersion.txt` indica `6000.3.29f1`, pero `AGENTS.md`, `README.md` y `MEMORY.md` aún dicen `6000.3.22f1`. `AGENTS.md` ordena usar esa versión a los agentes.

## 2. Método y verificación

- **Grafo estático:** referencias GUID en el YAML y en los `.meta` (incluye `externalObjects` de los FBX).
- **Grafo de Unity:** `AssetDatabase.GetDependencies` de cada asset. Añadió 274 aristas FBX/OBJ → PNG de Kenney (texturas resueltas por nombre), invisibles para el grafo estático, además de 8 internas de TMP. No hubo aristas que solo viera el grafo estático (sin contar scripts).
- **Código:** cruce de tipos C#, rutas en strings, `MenuItem`, `Shader.Find`, `LoadSceneInPlayMode` y asmdefs. No se encontraron `Resources.Load`, Addressables ni `SceneManager.LoadScene` en el código C# fuera de TextMesh Pro.
- **Raíces vivas:** `Level_01_Tutorial`, `Prototype`, `Player`, `EchoActor`, `RobotVisual`, `RobotEchoVisual`, los assets referenciados por ProjectSettings (RP assets, URP Global Settings, `InputSystem_Actions`) y `Assets/Settings/`. `SampleSceneProfile` no se forzó como raíz: resulta alcanzable desde `Prototype`.
- **Unity en batchmode:** el Editor estaba abierto, así que todo se ejecutó sobre **una copia** en el scratchpad de la sesión, con `Library` reimportada desde cero y `-nographics`. Al terminar, los hashes de `Assets/`, `docs/`, `Packages/` y `ProjectSettings/` del original coincidían con los tomados antes de crear la copia.

| Estado de la copia (lotes acumulativos) | EditMode | PlayMode |
|---|---:|---:|
| Línea base | 120/120 | 35/35 |
| Tras L1–L3 (una sola ejecución) | 120/120 | 35/35 |
| Tras L4a (con los cambios de código de §6) | 117/117 | 29/29 |
| Tras L4b | 113/113 | 18/18 |
| Tras L4c | 113/113 | 17/17 |
| Tras L6 | 113/113 | 15/15 |
| Tras L7 + L8 (una sola ejecución) | 113/113 | 15/15 |
| Tras reejecutar los builders vivos | 113/113 | 15/15 |

L5 es solo documentación y no se simuló. L1, L2, L3, L7 y L8 **no se probaron por separado**: al aplicarlos de verdad hay que pasar las suites tras cada uno.

Sobre la copia podada:

- No quedó ninguna referencia GUID de un superviviente a un archivo borrado, y no faltó ningún asset vivo (aparte de `InitTestScene`, excluida de la copia).
- `Level01TutorialBuilder.Build`, `Level01ArtPass.Apply`, `RobotVisualBuilder.BuildLab` y `RobotVisualBuilder.Integrate` terminaron con éxito, en ese orden.

**No verificado:** aspecto visual (`-nographics`), un build de player y los menús ejecutados a mano en el Editor.

## 3. Categoría A: sin referencias desde nada vivo

Prefijo `E/` = `Assets/_Echoryx/`. Los tamaños incluyen `.meta`.

### A puro: sin impacto en tests ni compilación

| Lote | Ruta | Tamaño | Por qué no se usa | Menciones (tests / docs / créditos / código) |
|---|---|---:|---|---|
| L0 | `Assets/InitTestScene2516a125-….unity` (+ `.meta`) | 6 KB | Resto del Test Runner (23-nov), sin referencias | Ninguna. Está ignorado por Git: **borrarlo no genera commit**. Hacerlo sin pruebas en curso |
| L1 | `Assets/TutorialInfo/` | ~45 KB (con la fila siguiente, ~50 KB) | Plantilla URP; solo se referencia a sí misma | Su propio `ReadmeEditor` (`InitializeOnLoad`). Sin tests ni docs |
| L1 | `Assets/Readme.asset` | incluido | Usa `Readme.cs` y un icono de `TutorialInfo`; borrar juntos | Ninguna |
| L1 | `Assets/Scenes/` + `Scenes.meta` | <1 KB | Carpeta vacía; Git solo versiona el `.meta` | `ProjectSettings.asset` conserva el string `templateDefaultScene: Assets/Scenes/SampleScene.unity`; `testing.md` y `architecture.md` citan `SampleScene` |
| L2 | En ambos kits Kenney: `Models/GLB format/`, `Models/OBJ format/`, `Models/Textures/`, `Previews/`, `Preview*.png`, `Sample.png`, `Overview.html`, `*.url` | 19,6 MiB | Formatos duplicados y material promocional; el juego solo usa FBX | `credits.md` cita las carpetas raíz, que se conservan. Sin tests ni código |
| L3 | 122 FBX sin uso en `…/Models/FBX format/` de ambos kits | 5,5 MiB | No están en ninguna escena ni prefab, y `Level01ArtPass` no los pide | Igual que L2 |

**Se conserva de Kenney:** los dos `License.txt`, los dos `FBX format/Textures/colormap.png` y los 15 FBX que coinciden exactamente con los que usa el pase de arte:

- Modular: `corridor`, `gate`, `room-large`, `room-small`, `room-wide`, `stairs-wide`, `template-floor-big`, `template-floor-layer`, `template-wall`.
- Station: `container-tall`, `display-wall-wide`, `floor`, `floor-panel`, `rocks`, `skip-rocks`.

Si el arte necesita otra pieza en el futuro, habrá que recuperarla de Git o descargarla de kenney.nl.

### A*: sin uso en el juego, pero atado a código y tests

Cada fila debe ir **en el mismo commit** que sus dependientes de B.

| Lote | Ruta | Tamaño | Por qué no se usa | Menciones |
|---|---|---:|---|---|
| L4a | `E/Scenes/PlayerAnimationIntegrationLab.unity` | 41 KB | Fuera de Build Settings; el Player vivo usa el robot | Tests: `PlayerAnimationIntegrationTests`, `PlayerAnimationIntegrationChecks`. Docs: README, MEMORY, informe, `level-01-tutorial`. Código: `S05PlayerAnimationLabSetup`, **`RobotVisualBuilder.HideRobotInSuperSoldierLab`** |
| L4a | `E/Prefabs/Characters/SuperSoldierVisual.prefab`, `E/Art/Animations/SuperSoldier/PlayerLocomotion.controller` | ~9 KB | Solo los usa ese lab | Mismos tests y builder; informe |
| L4a | `E/Scripts/Player/PlayerAnimationDriver.cs` (driver antiguo) | 2 KB | Solo está en ese lab. Compila en el asmdef vivo `Echoryx.Player` | Los dos tests anteriores, `S05PlayerAnimationLabSetup`; README, MEMORY, informe, `level-01-tutorial` |
| L4b | `E/Art/Animations/SuperSoldier/` restante: `IdleLoop.anim`, `JumpInPlace.anim`, `SuperSoldierLocomotion.controller`, `Mixamo/` (5 FBX), `MixamoReplacement/`, `USAGE.md`, `provenance.json` | 3,7 MiB | Solo para los labs del SuperSoldier | Tests: `SuperSoldierLocomotionImportTests`, `JumpFallTransitionChecks`. Docs: `supersoldier-locomotion.md` y reportes JSON, `character-visual-assets`, `supersoldier-validation`, informe, README. Código: `S05LocomotionLabSetup`, `S05JumpReplacementSetup` |
| L4c | `E/Art/Characters/SihaGamesSuperSoldier/` (`Idle.fbx` con su Avatar, 4 PNG, controller, atribución) | 81,2 MiB | Solo lo usan los tres labs y `SuperSoldierVisual` | Tests: `SuperSoldierAnimationPlayCheck`, además de los de L4a y L4b. Docs: README, MEMORY, `testing`, `development-plan`, informe, `supersoldier-*`, manifiesto B1. **CC BY 4.0, ver §7** |
| L4c | `E/Materials/Characters/SihaGamesSuperSoldier/` (7 materiales) | 28 KB | Remapeo de `Idle.fbx` | `S05SuperSoldierValidation` |

Notas:

- `X Bot@Jumping.fbx` no lo usa ningún controller, pero `SuperSoldierLocomotionImportTests` lo comprueba.
- **Cybernet Warrior:** nada que borrar en `Assets/`. Quedan `docs/character-visual-assets.md` y `docs/s05-b1-retirement-manifest.json` (B).
- Las seis imágenes `docs/images/s05-b3-{idleloop,walk,run,jump,fall,land}.png` (1,3 MiB) no aparecen por nombre en ningún documento. `SuperSoldierLocomotionPlayChecks` las genera con una ruta concatenada. Van en L5.
- Los 2 materiales `LevelCheckpoint` y `LevelHazard` aparecen sin referencias, pero **no son A**: el builder vivo los crea y asigna (§5).

## 4. Categoría B: probable, requiere tu decisión

| Lote | Ruta | Tamaño | Por qué | Menciones | Recomendación |
|---|---|---:|---|---|---|
| L4a | `Scripts/Game/Editor/S05PlayerAnimationLabSetup.cs`; `Tests/Editor/Game/PlayerAnimationIntegrationTests.cs`; `Tests/PlayMode/Game/Characters/PlayerAnimationIntegrationChecks.cs` | ~30 KB | Construyen o prueban A* | MEMORY, informe, `testing` | Borrar, **rescatando cobertura viva** (§6) |
| L4b | `Scenes/SuperSoldierLocomotionLab.unity`; `Scripts/Game/VisualLabs/` (`LocomotionAnimationPreview`, en el asmdef runtime `Echoryx.Game`); `S05LocomotionLabSetup.cs`; `S05JumpReplacementSetup.cs`; `SuperSoldierLocomotionImportTests.cs`; `JumpFallTransitionChecks`, `JumpPreparationChecks`, `SuperSoldierLocomotionDiagnostics`, `SuperSoldierLocomotionPlayChecks` | ~130 KB | Solo sirven a la locomoción del SuperSoldier | `supersoldier-locomotion.md`, reportes JSON, `architecture`, `testing` | Borrar |
| L4c | `Scenes/SuperSoldierValidationLab.unity`; `S05SuperSoldierValidation.cs`; `SuperSoldierAnimationPlayCheck.cs` | ~45 KB | Validación del import | `supersoldier-validation.md`, `architecture`, `testing` | Borrar |
| L5 | `docs/supersoldier-*.{md,json}`; `docs/images/s05-b2-*`, `s05-b3-*`, `s05-b42-*`; `docs/informe-avance-proyecto.md` | 10,5 MiB | Evidencia e informe de un personaje retirado. El informe está obsoleto (SuperSoldier, 116 tests) | README enlaza el informe, `supersoldier-locomotion.md` y las imágenes `b42-run` y `b42-platform-idle`; `testing`, `architecture`, `development-plan` y `visual-assets` enlazan los demás | Decide si es entregable académico. Si no, borrar y actualizar enlaces |
| L5 (opcional) | `docs/character-visual-assets.md`, `docs/s05-b1-retirement-manifest.json` | 6 KB | Registro de Cybernet Warrior (CC BY 4.0, retirado) | `architecture`, `supersoldier-locomotion`, `visual-assets`, MEMORY, `testing`, `development-plan` | **Conservar**: cuesta 6 KB y da trazabilidad. No entra en los tamaños ni en la simulación |
| L6 | `Scenes/VisualReferenceRoom.unity`, `Scenes/VisualAssetLab.unity`, `Prefabs/Environment/` (5), `Art/Environment/Quaternius/`, `Materials/Environment/` (9) | 6,4 MiB | Quaternius no se usa en el nivel | `credits`, README, MEMORY, GDD, `architecture`, `testing`, `visual-assets`, `visual-reference-room`, informe | Borrar si no se va a reutilizar el kit. CC0, sin obligación de atribuir |
| L6 | `S05ReferenceRoomBuilder.cs`, `S05VisualAssetsSetup.cs`, `VisualReferenceRoomPlayChecks.cs` | ~45 KB | Solo construyen o prueban lo anterior | `architecture`, `testing`, `visual-assets` | Junto con la fila anterior |
| L6 | `docs/visual-assets*`, `docs/visual-reference-room*`, `docs/images/s05-a1-*`, `s05-a2-*` | ~1,0 MiB | Documentan esos labs | README (2 imágenes y enlace), informe, `testing`, `architecture`, `development-plan`, GDD | Junto con L6 |
| L7 | `Scripts/Echo/Editor/`, `Scripts/Interactions/Editor/`, `Scripts/Rewind/Editor/` (con asmdefs); `Scripts/Game/Editor/` restante (3 `S04*` + asmdef); `Scripts/Player/Editor/S01PrototypeBuilder.cs` | ~60 KB | Instaladores de una sola vez para `Prototype` y los prefabs, ya aplicados. No son "solo de A": operan sobre contenido vivo. Ningún test usa sus tipos | `architecture` y `testing` | Borrar si no necesitas regenerar `Prototype`. `Player.Editor` y su asmdef **se quedan** por `RobotVisualBuilder` |
| L8 | `Scenes/Laboratory.unity` | 9 KB | Sin referencias ni tests; fuera de Build Settings | `development-plan` (aceptación de S00), MEMORY, `architecture`, `testing`, `visual-*`, `supersoldier-validation` | Borrar si no importa conservar la evidencia de S00 |
| Sin lote | `Scenes/RobotIntegrationLab.unity` | 16 KB | Es un laboratorio, pero prueba **código vivo** | `RobotLocomotionPlayChecks` (5 tests: salto en el mismo frame, velocidad de pies, pared…); `RobotVisualBuilder` lo genera; `level-01-tutorial` | **Conservar** |
| Sin lote | Objetos desactivados `Capsule Placeholder` (`Player.prefab`) y `Hologram Visual` (`EchoActor.prefab`) | ~0 | No se ven | `RobotLocomotionTests` (líneas 73 y 81) usa `Find(...).gameObject`; `RobotVisualBuilder` (líneas 57–58 y 306) los busca por nombre; `S05PlayerAnimationLabSetup` y `level-01-tutorial` | **Conservar**. Quitarlos exige cambiar prefabs (con APIs del Editor), builder y tests, y no libera espacio |

## 5. Categoría C: conservar

| Elemento | Motivo |
|---|---|
| `Level_01_Tutorial`, `Prefabs/Level/`, `Player`, `EchoActor`, `RobotVisual`, `RobotEchoVisual` | Raíces vivas |
| `Prototype.unity` | Escena válida, en Build Settings y cargada por varios tests EditMode y `CombinedPuzzlePlayChecks` |
| `RewindTestRotor`, `AttemptResetTransform`, `FrameRateTest` | Solo `Prototype` los tiene en escena; viven mientras viva. `FrameRateTest` parece un diagnóstico temporal (sin namespace): para quitarlo hay que editar `Prototype` |
| `PressurePlateEvents`, `ResolvedPlanarMotion` y el resto de scripts runtime no listados en A/B | Usados por prefabs, escenas o código vivo |
| `Level01TutorialBuilder`, `Level01ArtPass`, `RobotVisualBuilder` | Builders vivos |
| `Materials/Level/` completo | `Level01TutorialBuilder.BuildMaterials` crea o actualiza los 9, incluidos `LevelCheckpoint` y `LevelHazard`; borrarlos solo haría que reaparecieran |
| `Materials/Art/`, `Materials/Robot/`, los `.mat` sueltos de `Materials/`, `Art/Effects/`, `Art/Shaders/`, `Art/Animations/Robot/`, `Settings/Level01VolumeProfile.asset` | Alcanzables desde las raíces. El skybox además se busca con `Shader.Find` |
| `Assets/Settings/` completo, incluido `SampleSceneProfile.asset` | URP; el nombre de este último engaña |
| `Assets/InputSystem_Actions.inputactions` | Escenas, prefabs, ProjectSettings, builders y tests |
| `Assets/TextMesh Pro/` | Paquete importado. TMP carga por `Resources` y por nombre; no conviene podarlo por GUID |
| `Assets/FreeLowPolyRobot/` (ignorado por Git) | `ModularRobots.fbx`, materiales y shader están vivos. Hay demos sin uso, pero es un paquete de terceros no versionado: sin commit posible |
| Todos los asmdefs salvo los de L7; tests no listados en A/B | Cubren código vivo |
| `Art/Environment/kenney_*/License.txt` | Licencia de los assets usados |
| `Audio/.gitkeep` | Estructura reservada |
| `AGENTS.md`, README, MEMORY, `docs/{GDD,architecture,development-plan,testing,credits,level-01-tutorial}.md`, `ProjectSettings/` | Vigentes, aunque con partes obsoletas (§8) |

## 6. Riesgos

1. **Builder vivo roto por L4a.** `RobotVisualBuilder.Integrate()` (menú `Echoryx/Robot/2…`) llama a `HideRobotInSuperSoldierLab()`, que abre el lab por string sin comprobar que exista. El compilador no lo detecta. Hay que quitar la llamada y el método en el mismo commit; en la copia se hizo así y el menú terminó con éxito.
2. **Test de código vivo escondido.** `TransportAndContactsDoNotInventOwnSpeed` prueba `ResolvedPlanarMotion`, que usa `PlayerMotor.PlanarSpeed` (y de él, `RobotAnimationDriver`). Antes de borrar su fixture, moverlo a `Tests/Editor/ResolvedPlanarMotionTests.cs` (asmdef `Echoryx.Player.Tests`). Se simuló y pasa.
3. **Cobertura PlayMode que desaparece con L4a** aunque ese resumen salga verde. `PlayerAnimationIntegrationChecks` ejercita el motor real en: velocidad y rumbo en diagonal y al cambiar de dirección, empuje y deslizamiento contra pared, transporte en plataforma excluido de `PlanarSpeed`, y transporte bloqueado. El test EditMode rescatado cubre el cálculo, y el lab del robot cubre la pared desde la animación, pero **no hay en los tests PlayMode restantes ninguna comprobación de `PlanarSpeed` sobre plataforma**. Recomendado: portar esos casos a `RobotLocomotionPlayChecks` antes de L4a (el lab del robot necesitaría una plataforma, o cargar `Level_01`).
4. **L6 quita `VisualReferenceRoomPlayChecks`**, que prueba colisiones, seguimiento, órbita y deoclusión de cámara. `CameraDoesNotPassThroughWallsOrTheClosedDoor` cubre la deoclusión en el nivel; no comprobé que la órbita tenga un equivalente.
5. **Tests que fallarían o no compilarían si se borra solo A\*:** los fixtures de L4a–L4c cargan esos assets por ruta o usan `PlayerAnimationDriver`. `LocomotionAnimationPreview`, `S05PlayerAnimationLabSetup` y los tests PlayMode dependen entre sí por tipo o escena. Respetar la composición de los lotes.
6. **Menús que dejan de tener sentido:** `S05-A1`, `A2`, `B2`, `B3` y `B4.2` reconstruyen contenido de A o B y desaparecen en L4 y L6. Los menús `S01`–`S04` desaparecen en L7.
7. **Orden obligatorio:** `Materials/Environment/` lo comparten los labs del SuperSoldier, sus builders y Quaternius. **L6 debe ir después de L4a–c** (los FBX de Quaternius remapean a esos materiales).
8. **Los tests ensucian el árbol.** En la línea base con `-nographics`, PlayMode reescribió `docs/supersoldier-locomotion-play-report.json`. Con gráficos, varios fixtures también escriben PNG en `docs/images/`. Tras cada ejecución en L1–L4a, revisar `git status` y descartar esos cambios antes del commit. Desde L4b desaparece quien escribió en esta prueba; tras L4 y L6 desaparecen los fixtures que escriben en `docs/`. Además, `JumpFallTransitionChecks` y `JumpPreparationChecks` escriben en `/tmp`.
9. **Asset no versionado:** los tests y el nivel dependen de `Assets/FreeLowPolyRobot/`. Un clon limpio necesita importarlo antes de poder dar las suites en verde.
10. **Kenney:** el grafo estático por sí solo no ve las texturas que Unity resuelve por nombre. Conservar los dos `colormap.png` de `FBX format/Textures/` (los de `Models/Textures/` y los de GLB/OBJ no se usan).

## 7. Licencias y créditos

- **SCI-FI SuperSoldier (SihaGames) y casco (Udon-San), CC BY 4.0.** Al retirar los archivos, las versiones y builds futuros que ya no incluyan el material dejan de necesitar esos créditos. **No es correcto decir que la licencia deja de aplicar por completo:**
  - El repositorio `Raphinha1/Echoryx` es **público** y el FBX y las texturas seguirán en el historial, en `main` y en la etiqueta `pre-limpieza`, que también está en `origin`. Se siguen distribuyendo.
  - Las capturas y vídeos `s05-b2/b3/b42` muestran el modelo. Mientras se conserven, mantener la atribución.
  - En L4c, **antes de borrar `ATTRIBUTION.md`**, añadir a `docs/credits.md` una sección "Assets retirados (presentes en el historial y en las capturas)" con ambos autores, sus enlaces, la licencia y los cambios hechos.
  - No propongo reescribir el historial; es una decisión aparte y destructiva.
- **Mixamo:** no es CC; aplican los términos de Adobe que cita `USAGE.md`. Los FBX también quedan en el historial público. Conviene que revises si esos términos permiten esa redistribución; **no lo he determinado**. `USAGE.md` solo dice que la redistribución como biblioteca de assets queda fuera del alcance del trabajo.
- **Cybernet Warrior (CC BY 4.0):** el registro dice que sus recursos se retiraron. Consulté en `git log --all` las 24 rutas `Assets/…` que extraje del manifiesto y ninguna aparece en el historial.
- **Quaternius (CC0):** sin obligación. Tras L6, quitar la frase de `credits.md` que dice que el pack está presente.
- **Kenney (CC0)** y **robot (EULA del Asset Store, no versionado):** sin cambios.

## 8. Orden propuesto de borrado

Un lote = un commit. Borrar desde el Editor, o eliminar cada asset **con su `.meta`**. Entre lotes: EditMode y PlayMode en verde y `git status` revisado.

| # | Commit | Archivos sin `.meta` | Libera | EditMode / PlayMode esperado | Requiere |
|---|---|---:|---:|---|---|
| L0 | (sin commit) borrar `InitTestScene*` local | 1 | 6 KB | — | — |
| L1 | Plantilla URP y `Assets/Scenes` | 5 | 0,05 MiB | 120 / 35 | — |
| L2 | Kenney: formatos duplicados y previews | 570 | 19,6 MiB | 120 / 35 | — |
| L3 | Kenney: FBX sin uso | 122 | 5,5 MiB | 120 / 35 | — |
| — | (recomendado) portar la cobertura del motor del riesgo 3 | — | — | sube según los tests portados | Aprobación |
| L4a | Integración antigua del SuperSoldier + editar `RobotVisualBuilder` + rescatar el test | 7 | 0,08 MiB | 117 / 29 | Aprobación |
| L4b | Locomoción y Mixamo | 20 | 3,8 MiB | 113 / 18 | L4a |
| L4c | Personaje, materiales, lab de validación + créditos | 18 | 81,3 MiB | 113 / 17 | L4b |
| L5 | Docs e imágenes del SuperSoldier + enlaces | — | 10,5 MiB | sin cambio (no simulado) | L4c |
| L6 | Quaternius y sus labs, con sus docs | 40 en `Assets/`, más docs | 6,5 + 1,0 MiB | 113 / 15 | L4c |
| L7 | Instaladores S01–S04 y asmdefs vacíos | 13 | 0,06 MiB | 113 / 15 | L4c (comparte `Game/Editor`) |
| L8 | `Laboratory.unity` | 1 | 0,01 MiB | 113 / 15 | — |

Si portas tests antes de L4a, los recuentos PlayMode posteriores suben en esa cantidad.

**Documentación a actualizar** en el lote correspondiente:

- `README.md`: protagonista, capturas, catálogo de escenas, "116 tests", versión de Unity, Test Framework (`2.0.1` frente a `1.6.0` en el manifest) y enlaces `file:///home/raphinha/…`.
- `MEMORY.md`, `testing.md`, `architecture.md`: labs, instaladores y comandos `-executeMethod` retirados.
- `development-plan.md`: mención de `Laboratory` y estado de S05.
- `level-01-tutorial.md`: aún dice que el jugador es una cápsula (líneas 44 y 64) y que `PlayerAnimationDriver` sigue intacto.
- `GDD.md` (Quaternius), `credits.md` (§7) y la versión de Unity en `AGENTS.md`.

## 9. Decisiones pendientes

1. ¿Apruebas L4a–c con sus tests y código? ¿Porto antes la cobertura del motor (riesgo 3)?
2. ¿La evidencia S05 y el informe de avance son entregables académicos (L5 y docs de L6)?
3. ¿Quaternius se descarta definitivamente (L6)?
4. ¿Se necesita regenerar `Prototype` (L7) o conservar `Laboratory` (L8)?
5. ¿`Level_01_Tutorial` debe ser la primera escena del build? No es parte de la limpieza, y `Level01TutorialBuilder.AddToBuildSettings` conviene revisarlo al cambiarlo.
