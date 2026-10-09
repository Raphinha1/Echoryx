# S05-B3 — locomoción Humanoid aislada del SuperSoldier

## Alcance y estado aprobado

El SuperSoldier estaba aprobado técnica y visualmente por el usuario antes de este slice. Se prepara `SuperSoldierLocomotionLab` a partir del laboratorio B2, conservando B2 y las escenas de juego. Unity 6000.3.22f1, URP 17.3.0 y las dependencias se mantienen. No hay integración con PlayerMotor, movimiento físico, controles nuevos, Echo ni modificación de materiales.

S05-B3 está **aprobado técnica y visualmente** por el usuario: Idle, Walk, Run, Jump, Fall, Land, contacto y transiciones. La animación se mantiene en un laboratorio aislado; la sincronización con PlayerMotor todavía no se implementa y corresponde a S05-B4. El candidato Cybernet Warrior de S05-B1 fue retirado; su historial y hashes están en `character-visual-assets.md` y `s05-b1-retirement-manifest.json`. S05-B2/3 permanecen independientes del Player y de las escenas jugables.

## Fuentes y condiciones de uso

Los cinco originales se encontraron directamente en `~/Descargas`, sin ZIP ni texturas asociados. Son FBX binarios 7700 de X Bot con **65 huesos**, un take `mixamo.com` y 315 curvas por archivo. Son exportaciones de animación sin mallas, skinning, materiales ni texturas: el skinning de destino sigue siendo el del SuperSoldier.

| Original | Clip importado | Duración | FPS | Bucle |
|---|---|---:|---:|---|
| X Bot@Walking.fbx | Walk | 1,033 s | 30 | Sí |
| X Bot@Running.fbx | Run | 0,700 s | 30 | Sí |
| X Bot@Jumping.fbx | Jump anterior, conservado solo como referencia | 1,900 s (take completo) | 30 | No |
| Jumping Up.fbx | Fuente del Jump actual (`JumpInPlace.anim`) | 0,833333 s (frames 0–25) | 30 | No |
| X Bot@Falling Idle.fbx | Fall | 0,700 s | 30 | Sí, mientras siga en el aire |
| X Bot@Falling To Landing.fbx | Land | 1,067 s | 30 | No |
| Idle.fbx existente | IdleLoop | 1,967 s | 30 | Sí |

Los cinco FBX originales se conservan sin modificar en `Art/Animations/SuperSoldier/Mixamo`. Para S05-B3-Jump Replacement se copió el FBX nuevo del usuario a `Art/Animations/SuperSoldier/MixamoReplacement/Jumping Up.fbx`; la copia coincide byte a byte con Descargas. El importador de Unity informa 66 huesos de referencia y cero mallas/materiales/texturas; es un clip de animación solamente. [provenance.json](../Assets/_Echoryx/Art/Animations/SuperSoldier/provenance.json) registra SHA-256, tamaños, procedencia, licencia, rango y política de raíz. No se conocen las opciones exactas ni las fechas de descarga anteriores; no se inventan. No hubo descargas adicionales.

La [FAQ oficial de Adobe Mixamo](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html) permite uso sin regalías en proyectos personales, comerciales y sin fines de lucro, incluidos videojuegos. **No son CC BY ni CC0**: están sujetos a [Adobe General Terms](https://www.adobe.com/go/terms) y [Mixamo Additional Terms](https://wwwimages2.adobe.com/content/dam/cc/en/legal/servicetou/Mixamo-Addl-Terms-en_US-20210623.pdf). El documento adicional publicado con vigencia 2021 prohíbe emplear contenido/salidas del servicio para crear, entrenar, probar o mejorar sistemas AI/ML; no contiene una licencia Creative Commons ni una cláusula específica de distribución standalone. No se deduce de ese PDF una autorización general de redistribución de una biblioteca de assets. Esta etapa los conserva para el proyecto; los FBX descargados no incluyen un aviso de créditos/licencia. El [aviso local de uso](../Assets/_Echoryx/Art/Animations/SuperSoldier/USAGE.md) conserva los enlaces y el alcance. Los créditos CC BY de SihaGames/Udon-San del modelo siguen separados.

## Revisión de S05-B3 tras el rechazo manual

Las primeras pruebas (2/2 dirigidas, 83/83 EditMode y 10/10 PlayMode) comprobaban raíz inmóvil, cambios de huesos y estados alcanzables. **No demostraban calidad de retargeting ni contacto durante todo el ciclo.** La revisión inicial rechazó Walk/Run/Jump/Land, transiciones y contacto; las correcciones posteriores fueron aprobadas manualmente para Walk, Run, Fall, Land y el contacto de botas. El Jump anterior siguió pendiente y se reemplazó en S05-B3-Jump Replacement. Jumping Up fue aprobado visualmente por el usuario, igual que Fall y Land. Jump→Fall fue corregido y aprobado posteriormente; se detectó una regresión de flotación durante la preparación, corregida en la sección final. S05-B4 permanece fuera del alcance de este hito y es el siguiente paso pendiente.

### Diagnóstico reproducido, sin Blend Tree ni transiciones

`SuperSoldierLocomotionDiagnostics` carga únicamente el laboratorio, desactiva su presentador y evalúa cada clip con `AnimationClipPlayable`/reloj manual. Mide toda la malla deformada a 60 Hz, huesos, orientación corporal con `HumanPoseHandler` y raíz; genera ocho poses desde frente y perfil por clip. Se reutiliza `BakeMesh(mesh, true)` para compensar escala de las piezas, sin modificar mallas.

La ejecución inicial `/tmp/echoryx-b3-fix-red.xml` falló en Land: mínimo de suela **−0,0424 m**. El [informe anterior](supersoldier-locomotion-diagnosis-before.json) conserva los ciclos iniciales, no solo un snapshot. Las comparaciones posteriores cambiaron la referencia del Avatar y después la orientación, conservando modelo/materiales/escala. Una inspección temporal del Editor comprobó las poses de referencia; esa herramienta se retiró y la comprobación útil se conserva como test EditMode, sin reflection en el código final.

| Fallo | Causa confirmada / evidencia | Corrección |
|---|---|---|
| Walk: pie incorrecto | Avatars fuente calculados con referencias articulares procedentes de poses animadas, distintas entre clips. El mismo FBX usa referencias de codo/tobillo coherentes al generar el Avatar sin importar animación. El defecto aparece también sin controller. | Regenerar la referencia fuente desde los transforms de reposo del FBX y mantenerla al recuperar clips; Foot IK de Mecanim para locomoción. |
| Run: articulaciones extrañas | Misma calibración fuente. Con la referencia corregida, el máximo de suela baja de 0,236 a 0,085 m antes de Foot IK, manteniendo originales, Avatar destino y velocidad 1×. | Misma corrección de referencia; Foot IK. No cambiar anatomía ni escala. |
| Jump anterior (diagnóstico histórico) | El take completo tenía 57 frames: preparación, despegue, caída y recuperación. Recortar 10–24 redujo la duración pero aún dejaba parte de la caída. La demo con pausa larga permitió inspeccionarlo, pero no corrigió ese contenido. | Reemplazado exclusivamente por Jumping Up (frames 0–25), un clip ascendente que termina aún elevado; consultar resultados abajo. |
| Land / contacto | El take incluye caída previa: hips Y fuente comienza en 154,95 cm y llega a altura de contacto cerca del frame 8. La proyección/offset anterior producía penetración al inicio y elevación al final. | Importar frames 8–32; Y en pose para compresión/recuperación sobre suelo, offset 0 y Foot IK. |
| Giros entre estados | Root Rotation basada en Original dejaba sesgos de ~28° Jump, ~43° Fall y ~31° Land aunque el transform raíz no girara. | Based Upon Body Orientation, rotación extraída y sin aplicación de root motion. Fall conserva su take/bucle; su referencia se hace coherente con el conjunto. |
| Mezclas / parámetros | Umbrales 2/6 no correspondían a los ciclos fuente; el setter manual saltaba inmediatamente a Speed. No existía reproducción individual accesible. | Umbrales derivados de `averageSpeed.magnitude` (~1,70/4,37), clips a 1×, suavizado de Speed en una única ruta y modo individual exclusivo. |
| Aterrizaje / interrupciones | El controller anterior no tenía interrupciones para cambios de apoyo durante una mezcla. No se confirmó un ciclo repetido de Land causado por los FBX: no era looping. | Interrupciones desde Destination, prioridad para perder apoyo y retorno condicionado a Grounded, Exit Time 0,95 / duración fija 0,12 s. Prueba de contactos repetidos y contacto durante Jump→Fall. |

**Hipótesis descartadas:** el controller por sí solo no explica defectos que aparecen en reproducción individual; el escalado/orientación del modelo tampoco, porque se conservan; no hay evidencia de un FBX irrecuperable o de rigging requerido en el destino. El Avatar destino y los cinco originales permanecen idénticos por hash. `Avatar.isValid` no bastaba para garantizar una buena referencia fuente; no se observaron errores de Avatar en la importación final. La revisión visual posterior del usuario aprobó el resultado final de locomoción; las pruebas automatizadas siguen describiendo cobertura, no sustituyen esa aprobación.

### Importación actual y raíz

Cada X Bot conserva `Human` + `CreateFromThisModel`, Avatar propio, `sourceAvatar=null`. No se usa Copy From Other Avatar para el SuperSoldier. La herramienta `Prepare` vacía solo la descripción de referencia fuente, genera el Avatar con `importAnimation=false`, vuelve a activar clips manteniendo esa referencia y configura los takes mediante `ModelImporter`. Los cinco skeletons de referencia resultantes coinciden en posiciones/rotaciones; no cambia la jerarquía FBX ni se crean huesos.

| Clip actual | Frames fuente | Duración | Loop | Y | Offset |
|---|---|---:|---|---|---:|
| IdleLoop | Original B2 | 1,967 s | Sí | Original sin modificar | Original |
| Walk | 0–31 | 1,033 s | Sí | Bake Into Pose | 0 |
| Run | 0–21 | 0,700 s | Sí | Bake Into Pose | 0 |
| Jump | Derivado JumpInPlace de Jumping Up, 0–25 | 0,833333 s | No | XZ/heading extraídos; Y horneada solo para compresión original, sin trayectoria ascendente | 0 |
| Fall | 0–21 | 0,700 s | Sí | Extraída, Feet | −0,20 |
| Land | 8–32 | 0,800 s | No | Bake Into Pose, Feet | 0 |

La configuración inicial de Jumping Up horneaba Y: pelvis 0,977 m al inicio, mínimo de preparación 0,559 m, máximo 1,588 m y fin 1,586 m. Aunque el impulso individual fue aprobado, esa altura era incompatible con Fall. La corrección histórica Transition extraía toda Y y conservaba la referencia Original: pelvis inicial 0,977 m y final 0,874 m, manteniendo las rotaciones y el tramo completo. La trayectoria ascendente permanece en los datos de raíz extraídos (velocidad media Y 0,776 m/s); Apply Root Motion sigue OFF y el GameObject no se desplaza. La política actual, detallada al final, recupera la compresión de preparación sin recuperar la trayectoria ascendente. El laboratorio sigue mostrando un salto **en el sitio**: no conserva la subida global de 0,61 m dentro de los huesos. La altura física seguirá perteneciendo a PlayerMotor en una integración futura; no se implementa una parábola en esta etapa.

IdleLoop se mantiene byte a byte; root a y=0, escala 0,986568 y orientación de presentación 180° tampoco cambian. La fuente completa de Jump/Land sigue en el FBX; el recorte es reversible en el importer, sin modificar sus originales. Jump utiliza ahora una copia derivada por la API de Unity que filtra únicamente la curva de altura corporal; los demás clips siguen importados directamente.

### Animator y reproducción exclusiva

`SuperSoldierLocomotion.controller` conserva su GUID y cuatro estados: Locomotion (Blend Tree Idle/Walk/Run), Jump, Fall y Land. Speed/Grounded/VerticalSpeed siguen siendo los únicos parámetros. Tree: Idle=0, Walk≈1,70, Run≈4,37; playback 1×. Foot IK de Mecanim habilitado en Locomotion/Land; no añade raycasts ni IK manual de escenario.

Jump entra sin suelo y velocidad vertical positiva; Fall al ápice o por caída; el contacto lleva a Land. La transición Jump→Fall sigue condicionada por no estar grounded y `VerticalSpeed < 0.01`, sin Exit Time; la demo mantiene velocidad ascendente durante los 0,833333 s del impulso y luego pasa a caída. La mezcla Jump→Fall ahora dura 0,18 s; las demás conservan 0,12 s. Así la secuencia automática deja terminar el clip sin imponer espera artificial; en un futuro motor real, su señal de caída deberá corresponder a la duración/curva del impulso. Land no hace bucle ni transiciones hacia sí mismo; vuelve a Locomotion con Grounded y Exit Time 0,95. Perder apoyo se evalúa antes de esa salida. Todas las transiciones conservan interrupciones desde Destination; evita reiniciar continuamente el fade por repetir una condición del estado origen. Speed se suaviza 0,12 s, también cuando se usa el setter manual. Las pruebas de 30/120 Hz verifican estados/interrupciones, no calidad visual a todos los FPS.

`LocomotionAnimationPreview` ofrece tres fuentes exclusivas:

- Automatic Demo: secuencia de presentación con tiempo calculado desde Jump/Land. Jump reproduce por su duración real de 0,833333 s a 1×; al terminar comienza la mezcla a Fall. Playback Rate escala conjuntamente el reloj de demo y Animator.speed para inspeccionar también la transición a 0,5×/0,25×; en parámetros manuales el Animator vuelve a 1×. No añade retención artificial. La pausa/scrub solo ocurre en Individual Clip. Usa las velocidades fuente del Tree.
- Manual: Automatic Demo desactivado, Individual Clip=None; Inspector alimenta los tres parámetros.
- Individual Clip: Playable directo sin controller ni transiciones. Seleccionarlo apaga la demo; Playback Rate 0,25–2; Pause Clip + Normalized Clip Time permiten recorrer fotogramas. Los clips no looping se detienen al final. Al salir/desactivar el componente se destruye el graph y se restaura/reinicia el controller.

No hay nuevos controles de gameplay, cambios de escenas jugables ni integración con PlayerMotor. No hay parámetros manuales ejecutándose simultáneamente con un Playable individual.

### Verificación actual

Los resultados actuales se registran en [importación](supersoldier-locomotion-import-report.json), [poses](supersoldier-locomotion-play-report.json) y [ciclos completos](supersoldier-locomotion-cycle-report.json). Las mediciones siguientes proceden del destino real con las mismas opciones Foot IK que usa la previsualización:

| Clip | Suela mínima / máxima de toda la malla durante el ciclo (m) | Máximo sesgo corporal | Deriva raíz |
|---|---|---:|---:|
| IdleLoop | 0,002 / 0,006 | 0,2° | 0 m |
| Walk | 0,009 / 0,028 | 0,1° | 0 m |
| Run | −0,013 / 0,098 | 0,7° | 0 m |
| Jump anterior (clip de referencia) | 0,047 / 0,224 | 1,1° | 0 m |
| Fall | 0,058 / 0,080 | 1,7° | 0 m |
| Land | −0,008 / 0,005 | 7,5° | 0 m |

Run tiene una fase aérea legítima; no se fuerza suela=0 todo el tiempo. La pequeña penetración máxima de Run (~13 mm) y Land (~8 mm), así como la proyección aérea fija, requieren aceptación visual. Los números de malla no equivalen a un solver de pies ni prueban ausencia de deslizamiento con desplazamiento real.

Capturas nuevas: ocho momentos, orden de lectura por filas; dos filas de frente y dos de perfil. Se inspeccionaron marcha, brazos, piernas, compresión y recuperación; las capturas y métricas no sustituyen reproducción continua.

- [Idle](images/s05-b3-cycle-idleloop.png), [Walk](images/s05-b3-cycle-walk.png), [Run](images/s05-b3-cycle-run.png)
- [Jump](images/s05-b3-cycle-jump.png), [Fall](images/s05-b3-cycle-fall.png), [Land](images/s05-b3-cycle-land.png)

**Resultados históricos de S05-B3-Jump Replacement (anteriores a Transition):** la importación/validación de Editor confirmó Avatar Humanoid válido, clip no loop, 0,833333 s/30 fps/frames 0–25, sin deriva del GameObject y con Y horneada a huesos. La inspección PlayMode muestreó la pelvis durante todo el clip: subida efectiva 0,611 m tras la preparación, máximo 1,588 m y descenso final 0,002 m; no incluye una fase significativa de caída/aterrizaje. La prueba del Animator midió Jump **0,833333 s**, normalized time **1,140** al entrar en Fall, peso máximo del clip **1,000**, una entrada a Jump, una a Fall y una a Land. La interpolación individual confirmó duraciones 0,833/1,667/3,333 s a 1×/0,5×/0,25×. Unity 6000.3.22f1: **87/87 EditMode y 16/16 PlayMode pasaron** en las suites completas con `-nographics`; las pruebas dirigidas adicionales fueron 4/4 EditMode y 7/7 PlayMode. XML: `/tmp/echoryx-s05-jump-all-edit.xml` y `/tmp/echoryx-s05-jump-all-play.xml`. No hubo errores de compilación ni fallos de test. El Editor registró avisos de conexión/licencia del entorno y timeout de Unity Connect; no impidieron importar ni completar las suites. `-nographics` no valida legibilidad visual; la aceptación manual final de B3 quedó confirmada por el usuario posteriormente.

**Preservación:** 481/490 archivos previos idénticos por hash; las nueve diferencias son exactamente los archivos B3 listados abajo; cero eliminaciones y cuatro archivos nuevos bajo Assets (dos tests y sus meta). Cinco FBX originales idénticos a Descargas. Todas las escenas existentes, modelos, materiales, gameplay, Packages y ProjectSettings intactos. [Integridad de esta corrección](supersoldier-locomotion-correction-integrity.json).

### Archivos de esta corrección y preservación

Modificados deliberadamente: cinco `.fbx.meta` de Mixamo, `SuperSoldierLocomotion.controller`, `S05LocomotionLabSetup.cs`, `LocomotionAnimationPreview.cs`, `SuperSoldierLocomotionPlayChecks.cs`. Nuevos: `SuperSoldierLocomotionDiagnostics.cs` y `SuperSoldierLocomotionImportTests.cs`, con sus `.meta` generados por Unity. Documentación: este informe, README/MEMORY/architecture/development-plan/testing, informes de importación/poses/ciclos/integridad y capturas B3.

Al consolidar el checkout, el candidato B1 se retiró; B2 y B3 son las entregas de personaje a conservar. La corrección no cambia `SuperSoldierLocomotionLab.unity`, Player, motores, cámara, Echo/Rewind/checkpoints, escenarios, FBX/PNG ni materiales. No hay commit. El informe original de integridad B3 se conserva como evidencia histórica; la revisión cuenta con `supersoldier-locomotion-correction-integrity.json`.

### Validación manual que cerró S05-B3

1. Salir de Play si estaba abierto, esperar que Unity termine de importar/compilar, abrir `SuperSoldierLocomotionLab.unity` y volver a Play, seleccionar `SuperSoldier Locomotion Preview`. Comprobar Console y Root Motion OFF. No ejecutar Prepare mientras se inspecciona una escena con cambios propios sin guardar.
2. Seleccionar Individual Clip=IdleLoop, Walk y Run por separado. Comprobar ambas botas, tobillos, rodillas, brazos, caderas y casco desde frente/lado/espalda en Scene View. A 1× deben mantener avance visual hacia +Z local del personaje; verificar no hay giro lateral. El personaje marcha en el sitio deliberadamente.
3. Seleccionar Jump y comprobar Playback Rate=1,0, 0,5 y 0,25. Su duración debe ser aproximadamente 0,833 s, 1,667 s y 3,333 s respectivamente. Activar Pause Clip y fijar Normalized Clip Time en 0, 0,25, 0,5, 0,75 y 1; la pose debe permanecer quieta hasta cambiar el valor. Verificar la pose de preparación/impulso en varios puntos; la compresión original se conserva y la trayectoria ascendente está eliminada en la copia derivada. Repetir Land para contacto/compresión/recuperación. No exigir altura física de salto en este laboratorio sin motor.
4. Individual Clip=None y Automatic Demo=false. Probar Speed=0/1,70/3,04/4,37; esperar un segundo tras cada cambio. Probar ascenso Grounded=false/VerticalSpeed=4; ápice/caída con 0 y −4; contacto Grounded=true/VerticalSpeed=0. Land debe ocurrir una vez y volver a locomoción aunque Grounded siga true.
5. Repetir tres contactos; caer sin Jump; perder suelo durante Land; volver a Grounded durante la mezcla Jump→Fall. Confirmar no hay bucles de Land, estados bloqueados ni sacudidas. Comparar con Animator abierto.
6. Activar Automatic Demo (con Individual Clip=None). Después de Run, Jump debe ejecutarse por 0,833 s a 1×, terminar su impulso y enlazar a Fall; Land debe ocurrir una sola vez. Repetir individual y verificar que la demo no compite por el Animator. Cambiar a manual y comprobar que restaura el controller. Revisar de nuevo Idle/Fall aprobados.
7. Observar pies y blends en reproducción continua a otros FPS. Registrar cualquier articulación extraña o contacto excesivo con clip, porcentaje y vista; no cerrar el hito solo por los XML.

El usuario aprobó manualmente el conjunto locomotor después de validar la corrección de preparación de Jump. La calibración con salto físico, sincronización exacta con PlayerMotor y deslizamiento siguen pendientes para S05-B4. No se modifica Player en B3.


## Corrección histórica S05-B3-Transition — Jump → Fall

El usuario confirmó una bajada brusca y un cambio repentino de postura. Se reprodujo con el **Animator real y la demo real**, a 60 muestras por segundo, a 1× y 0,5×. La prueba nueva falló primero por un cambio vertical de 0,844 m en la mezcla y, tras corregir únicamente Y, por el cambio angular de pelvis de 2,719°/frame. No se usa un cambio de huesos o un estado alcanzable como sustituto de continuidad.

### Causas y contraste

- **Altura confirmada:** Jump tenía Bake Into Pose Y ON y Based Upon Feet; Fall ya extraía Y con Feet y offset −0,20. Jump terminaba a 1,586 m y Fall empezaba a 0,751 m. Al cambiar pesos se eliminaban ~0,84 m de altura de pose, aunque el GameObject permanecía inmóvil. No era un teletransporte del transform, un error de FPS ni una interrupción prematura: la mezcla empezaba al terminar el clip, normalizedTime ≈1,000, con peso Jump=1/Fall=0, y progresaba gradualmente.
- **Postura confirmada:** diferencia de pelvis 19,55° entre finales/inicios; Spine/Chest solo 0,68°/0,39° locales, piernas superiores 0,44°/2,00°, rodillas 0,98°/1,06°. El torso y las piernas heredaban ~18–19° **globales** por la inclinación de pelvis. Se conserva esa diferencia de pose y se suaviza su cambio con una mezcla breve de 0,18 s. No se corrige rotando huesos a mano ni cambiando el rig.
- Extraer Y con **Feet** dejaba Jump a 0,471 m y producía una subida de ~0,27 m hacia Fall: los pies recogidos redefinían la altura. **Center of Mass** extraído dejaba pelvis −0,136 m; horneado dejaba 0,576 m al final y conservaba desplazamiento interno. **Original** horneado seguía a 1,586 m, por lo que cambiar solo Based Upon no resolvía el ascenso horneado. **Original extraído** deja 0,874 m y no cambia las rotaciones. No se modifica la configuración aprobada de Fall.
- Buscar otra fase de entrada en Fall dio una diferencia mínima cerca de normalizedTime 0,048, sin eliminar la diferencia vertical. Se conserva offset=0; no se recortan ni retemporizan los clips.

### Cambios mínimos

En esa corrección, solo Jump fuente: Root Transform Position Y **Bake Into Pose OFF, Based Upon Original, Offset 0** (`lockRootHeightY=false`, `keepOriginalPositionY=true`, `heightFromFeet=false`). Frames 0–25, 30 fps, Humanoid, Avatar, orientación/XZ, duración y FBX originales permanecen iguales. Root Motion permanece OFF. El ascenso fuente se extrae; ya no se presenta como desplazamiento global del esqueleto en el laboratorio.

Solo Jump→Fall: duración fija **0,18 s**, sin Exit Time, offset 0, mismas condiciones `!Grounded && VerticalSpeed < 0.01`, mismas interrupciones Destination. Esto limita la inclinación sin retener Jump, esperar un segundo ni congelar la pose. Fall→Land y Land→Locomotion conservan sus opciones y 0,12 s. La demo cambia a caída al acabar los 0,833333 s; a menor velocidad de inspección se escalan juntos demo y Animator, sin Time.timeScale global.

| Medición de la demo real a 1× | Antes | Corregido |
|---|---:|---:|
| Pelvis antes de la mezcla (global Y) | 1,5866 m | 0,8750 m |
| Pelvis al terminar la mezcla (global Y) | 0,7423 m | 0,7367 m |
| Cambio vertical en la mezcla | 0,8443 m | 0,1383 m |
| Paso máximo de pelvis por frame, 60 Hz | 0,1176 m | 0,0141 m |
| Paso máximo de rotación local medida | 2,719° | 1,811° |
| Paso máximo de rotación global medida | — | 2,029° |
| Inicio/fin de mezcla desde comienzo demo | 8,850 / 8,983 s | 8,850 / 9,033 s |
| Entradas Jump/Fall/Land | 1/1/1 | 1/1/1 |
| Deriva del GameObject | 0 m | 0 m |

A 0,5×: paso máximo de pelvis 0,00716 m y angular local/global 0,906°/1,034°; cambio total 0,1381 m. La diferencia residual de altura de unos 14 cm es cambio de pose aéreo, distribuido en la mezcla, no eliminación de los 61 cm de trayectoria. No se oculta con un offset del modelo, un desplazamiento de GameObject ni root motion. La calidad perceptual final de esa diferencia requiere revisión del usuario.

El [informe medido](supersoldier-jump-fall-transition-report.json) incluye posiciones **locales, relativas a la raíz y globales**, quaternions locales/globales de pelvis, Spine, Chest y ambas piernas/rodillas, tiempos, normalizedTime actual/destino y pesos por muestra, además de los cinco contrastes de configuración de Y. Las rotaciones del extremo Jump antes/después son idénticas; los FBX y demás importadores permanecen idénticos por hash.

### Evidencia y repetición visual

Se renderizó la demo completa Jump→Fall→Land en Unity/URP con OpenGL real, sin `-nographics`, a 1× y 0,5×. [Vídeo comparativo a velocidad normal](images/s05-b3-jump-fall-1x.mp4) (izquierda antes/derecha corregido; cada lado tiene frente/perfil), [corregido a 0,5×](images/s05-b3-jump-fall-05x.mp4) y [fotogramas consecutivos comparados](images/s05-b3-jump-fall-poses.png). Los vídeos se codificaron a 60 fps, sin interpolar ni ralentizar artificialmente el de 1×. Las capturas son del laboratorio real y no de una reconstrucción.

1. Abrir `Assets/_Echoryx/Scenes/SuperSoldierLocomotionLab.unity`; Play; seleccionar `SuperSoldier Locomotion Preview`. Individual Clip=None, Automatic Demo=true, **Playback Rate=1**. Root Motion OFF.
2. Después de Run, comprobar el impulso en el sitio durante 0,833333 s y la mezcla Jump→Fall durante ~0,18 s. El cuerpo debe inclinarse gradualmente, sin la caída de ~84 cm. La pelvis conserva un ajuste de pose de ~14 cm; valorar si resulta aceptable en reproducción normal, de frente y perfil.
3. Animator abierto: al comenzar la mezcla Jump llega a normalizedTime≈1 y ambos clips participan; Fall se hace dominante sin reiniciar Jump. Land ocurre una sola vez y vuelve a Locomotion.
4. Repetir con Playback Rate=0,5 y 0,25 para inspeccionar; **volver a 1** para aceptación. No usar Pause Clip, scrub ni saltar directamente de Individual Jump a Individual Fall para juzgar la mezcla: ese modo omite el controller.
5. Individual Clip=Jump y luego Fall permiten comprobar cada pose por separado; activar de nuevo la demo restablece el controller. Confirmar Console sin errores.

No se modifican clips Fall/Idle/Walk/Run/Land, escenas de juego, Player ni gameplay; no se hace commit ni se avanza a S05-B4. El usuario confirmó posteriormente la continuidad Jump→Fall. Esa configuración produjo la regresión de preparación diagnosticada a continuación.


### Pruebas ejecutadas y archivos afectados por Transition

Unity 6000.3.22f1: **8/8 PlayMode dirigidas**, **87/87 EditMode completas**, **17/17 PlayMode completas**, además de **1/1 prueba de transición con renderizado OpenGL**, que recorre 1× y 0,5×. XML: `/tmp/echoryx-transition-targeted-play.xml`, `/tmp/echoryx-transition-all-edit.xml`, `/tmp/echoryx-transition-all-play.xml`, `/tmp/echoryx-transition-graphics-final.xml`. La reproducción inicial falló por altura en `/tmp/echoryx-transition-red.xml`; el contraste Original Y sin cambiar mezcla falló por el límite angular en `/tmp/echoryx-transition-pose-red.xml`. Las regresiones incluyen clips individuales, tasas 1/0,5/0,25, cambio de parámetros, aterrizajes repetidos a 30/120 Hz y las salas anteriores. Ninguna prueba automatizada sustituye la aceptación visual final del usuario.

`JumpFallTransitionChecks` mide el Animator real; comprueba pesos simultáneos Jump/Fall, una entrada por fase, duración breve de mezcla, deriva nula, cambio vertical total <0,18 m, paso de pelvis <0,04 m y paso angular local/global <2,25°/3° por 1/60 s. La fuente original y los importadores aprobados siguen idénticos. [Semántica oficial de Bake Into Pose y referencias Y](https://docs.unity3d.com/6000.0/Documentation/Manual/RootMotion.html).

Modificados en esta corrección: `MixamoReplacement/Jumping Up.fbx.meta` (solo tres opciones de Y), `SuperSoldierLocomotion.controller` (solo duración de Jump→Fall), `S05JumpReplacementSetup.cs`, `S05LocomotionLabSetup.cs`, `LocomotionAnimationPreview.cs`, `SuperSoldierLocomotionImportTests.cs`, `SuperSoldierLocomotionPlayChecks.cs` y `provenance.json`. Nuevo: `JumpFallTransitionChecks.cs`/meta generado por Unity. Documentación: README, MEMORY, este documento, informes de reemplazo/importación/poses/ciclos actualizados, informe de transición y tres evidencias visuales nuevas (`jump-fall-1x.mp4`, `jump-fall-05x.mp4`, `jump-fall-poses.png`). El montaje del laboratorio también permanece idéntico. Las herramientas temporales de contraste se retiraron; no se retienen adaptadores de huesos ni correcciones en LateUpdate.


## Corrección S05-B3 — flotación durante la preparación de Jump

### Causa confirmada y separación de responsabilidades

La extracción completa de Y con referencia Original eliminaba **tanto la trayectoria aérea como la compresión descendente de preparación**. Las piernas seguían flexionándose, pero la pelvis quedaba casi a altura de pie, levantando las botas hasta **35,4 cm** mientras aún debía estar preparando el impulso. El defecto se reprodujo individualmente sin controller y también en la demo real: no era una transición, una deriva del GameObject ni un error del Avatar. Añadir altura física no resolvería esa separación prematura: elevaría todavía más al personaje.

Se midió a 60 Hz el GameObject, la raíz visual (`hips.parent`), la pelvis, ambos tobillos y el mínimo Y de todas las mallas deformadas. Tobillo y suela no son la misma medida. GameObject y raíz visual conservaron Y=0 y posición XZ constante en todos los fotogramas. La fuente contiene un descenso de `RootT.y` de 0,91845 a ~0,5903 unidades Humanoid, seguido de una subida hasta 1,56541. La extracción completa quitaba ambos; no había desplazamiento físico ni root motion aplicado.

| Jump individual, tiempo desde inicio | Pelvis antes / después (m) | Tobillo izquierdo antes / después (m) | Tobillo derecho antes / después (m) | Suela antes / después (m) |
|---|---|---|---|---|
| 0,167 s | 0,959 / 0,834 | 0,230 / 0,105 | 0,229 / 0,104 | +0,130 / +0,005 |
| 0,333 s | 0,918 / 0,559 | 0,455 / 0,095 | 0,462 / 0,102 | +0,353 / −0,006 |
| 0,833 s | 0,874 / 0,874 | 0,497 / 0,497 | 0,471 / 0,471 | +0,267 / +0,267 |

En preparación (0,05–0,40 s): máximo de suela individual **0,353830 → 0,008337 m**, mínimo corregido −0,007061 m. En automático: máximo **0,353830 → 0,024255 m**, incluyendo el breve fade de la pose aérea aprobada de Run; después del fade coincide con el individual. Las rotaciones originales no cambian.

### Corrección mínima y alternativas contrastadas

Se conserva el FBX y su importador aprobados **byte a byte**, incluido Bake Y OFF / Original / offset 0. Se genera mediante `AnimationUtility` una copia Humanoid `Art/Animations/SuperSoldier/JumpInPlace.anim`, conservando duración **0,833333 s**, rango completo, músculos, rotaciones y demás curvas. Solo `RootT.y` se limita a `min(Y original(t), Y original(0))`: conserva el descenso de preparación y elimina la trayectoria positiva de vuelo. Se muestrea a 60 Hz con tangentes ClampedAuto. La copia hornea esa Y corporal con Original/offset 0, sin loop. El estado Jump usa esa copia; las transiciones y parámetros no cambian. El modo individual carga la misma copia. No hay claves articulares creadas a mano, offsets en LateUpdate, desplazamiento del actor, IK de suelo ni cambios de escala.

Esto no es una compensación vertical fija: la compresión pertenece a la pose, relativa a la raíz. Una prueba desplaza externamente la raíz +2 m durante todo el clip y confirma que pelvis y ambos pies se trasladan exactamente +2 m. Es evidencia de ausencia de anclaje al suelo o compensación mundial; **no valida aún el salto del CharacterController**.

Contrastes aislados, restaurados después de medir:

- Extracción completa Original: máximo prematuro 35,4 cm; reproduce la regresión.
- Activar Foot IK sin recuperar compresión: máximo 38,5 cm; no resuelve el defecto.
- Hornear toda Y con Feet: preparación correcta (~8 mm), pero pelvis final 1,586 m; reintroduce la caída brusca hacia Fall. Descartado.
- Copia con extracción completa Original: mismo defecto; duplicar sin cambiar política no sirve.
- Retener solo compresión original: preparación ~8 mm y pelvis final **0,874 m**, idéntica a la transición aprobada. Seleccionado.

No se añaden desplazamiento físico de demostración, offsets permanentes, congelamientos ni esperas. El laboratorio y su presentador permanecen sin cambios. Para regenerar únicamente el derivado y su referencia existe el menú `Echoryx/S05-B3/Replace Jump With Jumping Up`; no es necesario ejecutarlo para inspeccionar los assets ya preparados.

### Continuidad conservada y limitación aérea explícita

La demo real a 1× conserva mezcla Jump→Fall de 8,850 a 9,033 s, pelvis 0,875→0,737 m, máximo paso de pelvis **0,0141 m/frame**, orientación local/global máxima **1,811°/2,029° por frame**, deriva de raíz 0 y entradas Jump/Fall/Land **1/1/1**. A 0,5×: 0,00716 m/frame y 0,906°/1,034°. Son las mismas mediciones de la transición aprobada: la corrección actúa en preparación, no en su extremo final ni en Fall→Land.

**Después del despegue el laboratorio sigue sin trayectoria física.** Las poses aéreas en el sitio pueden dejar puntas por debajo del suelo de referencia (mínimo de malla aproximadamente −0,1475 m alrededor de 0,57 s) y terminan con pies recogidos a +0,267 m. Ese comportamiento aéreo ya estaba en la configuración anterior y se conserva; no se oculta usando un offset. El motor real aportará altura física, pero su sincronización, altura disponible y contacto real aún requieren B4. La reparación elimina el levantamiento prematuro durante preparación y fue aprobada visualmente como parte del cierre B3. No certifica la simulación completa con CharacterController; eso se valida en B4.

### Pruebas y evidencia

Unity 6000.3.22f1: **3/3 PlayMode dirigidas con renderizado OpenGL real**, **87/87 EditMode completas** y **19/19 PlayMode completas**. Suites completas con `-nographics`, para preservar capturas anteriores. XML: `/tmp/echoryx-float-targeted-green.xml`, `/tmp/echoryx-float-all-edit.xml`, `/tmp/echoryx-float-all-play.xml`. La regresión falló antes de corregir por elevación prematura en `/tmp/echoryx-float-red.xml`. Los tests nuevos controlan ambos pies, suelas durante preparación, raíz constante y compatibilidad relativa con movimiento externo. Los existentes mantienen pesos, continuidad de pelvis/orientación, tasas 1/0,5/0,25, transiciones y aterrizajes únicos/repetidos. EditMode verifica que **todas las curvas excepto RootT.y** coinciden con la fuente. Sin errores de compilación ni fallos finales; avisos de licencia/Unity Connect del entorno no impidieron las ejecuciones.

[Mediciones por fotograma y políticas contrastadas](supersoldier-jump-preparation-report.json), [vídeo real comparativo a 1×](images/s05-b3-jump-preparation-1x.mp4) (izquierda anterior/derecha corregido; frente y perfil), [poses de preparación](images/s05-b3-jump-preparation-poses.png). El vídeo procede del Animator/URP real, grabado a 60 fps; la referencia anterior usa un OverrideController temporal en memoria, sin alterar assets aprobados.

### Validación manual final

1. Abrir `Assets/_Echoryx/Scenes/SuperSoldierLocomotionLab.unity`, entrar en Play y seleccionar `SuperSoldier Locomotion Preview`. Confirmar Apply Root Motion OFF y Console sin errores.
2. Individual Clip=Jump, Playback Rate=1. Observar de frente/perfil: ambas botas permanecen cerca del suelo mientras se flexionan piernas y baja pelvis; distinguir esa preparación de las poses posteriores de vuelo sin desplazamiento físico.
3. Activar Pause Clip y recorrer Normalized Clip Time=0, 0,20, 0,40 y 0,55. En ~0,40 (0,333 s) la pelvis debe estar baja, con botas plantadas; no suspendidas ~35 cm. Repetir a 0,5×/0,25× y volver a 1×.
4. Individual Clip=None, Automatic Demo=true, Playback Rate=1. Revisar Run→Jump, preparación→impulso→Fall→Land. Jump dura 0,833333 s, la mezcla a Fall 0,18 s y Land ocurre una sola vez. La postura aérea no implica altura física del actor en este laboratorio.
5. Comprobar transform del actor y raíz visual inmóviles, Fall→Land continuo y clips previamente aprobados sin cambios. Estas instrucciones sirven para reproducir la aceptación ya realizada. La siguiente etapa es S05-B4, pendiente; no forma parte de este laboratorio.

### Archivos de esta reparación

Nuevo `JumpInPlace.anim`/meta y `JumpPreparationChecks.cs`/meta. Modificados: controller (solo Motion de Jump), `S05JumpReplacementSetup.cs`, `S05LocomotionLabSetup.cs`, `SuperSoldierLocomotionImportTests.cs`, `SuperSoldierLocomotionPlayChecks.cs`, `JumpFallTransitionChecks.cs` (captura comparativa opcional en memoria), `provenance.json`, informes de importación/reemplazo/poses/ciclos/transición y documentación README/MEMORY/este documento. Nuevos informe de preparación, vídeo y imagen. Escenas, FBX, importadores, materiales, paquetes, Player y sistemas jugables permanecen intactos respecto al inicio de esta reparación. No hay commit.

**Preservación comprobada contra el inicio de esta reparación:** 532/546 archivos previos idénticos por SHA-256, 14 diferencias deliberadas y cero eliminaciones de archivos previos. Todos los FBX/importadores, escenas (incluidas VisualReferenceRoom y las jugables), materiales, scripts de gameplay, paquetes, ajustes y evidencias visuales anteriores permanecen idénticos. `git diff --check` solo señala espacios preexistentes en VisualReferenceRoom; no se corrigen en esta tarea. [Registro de integridad](supersoldier-jump-preparation-integrity.json).
