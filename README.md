# Echoryx

[![Unity](https://img.shields.io/badge/Unity-6000.3.29f1-blue.svg)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP%2017.3.0-orange.svg)](https://unity.com/srp/universal-render-pipeline)
[![Tests](https://img.shields.io/badge/Tests-130%20Passed%20(100%25)-brightgreen.svg)](docs/testing.md)
[![License](https://img.shields.io/badge/License-Academic%20Project-lightgrey.svg)]()

**Echoryx** es un videojuego 3D de plataformas y resolución de rompecabezas temporales desarrollado en **Unity 6** con **Universal Render Pipeline (URP)**. El jugador controla a un robot y utiliza dos habilidades temporales complementarias —la grabación y reproducción de acciones pasadas (**Echo**) y la reversión cinemática de objetos interactivos (**Rewind**)— para navegar salas modulares y superar obstáculos sincronizados.

---

## Capturas del Desarrollo (archivo)

Etapas anteriores del proyecto: la sala visual y el protagonista de estas imágenes ya fueron retirados (ver [créditos](docs/credits.md) y [archivo](docs/archivo/README.md)).

| Sala de Referencia Sci-Fi (URP) | Locomoción del Protagonista |
|:---:|:---:|
| ![Sala de referencia visual sci-fi](docs/archivo/images/s05-a2-reference-room.png) | ![SuperSoldier corriendo](docs/archivo/images/s05-b42-run.png) |

| Vista en Tercera Persona | Plataforma Móvil con Físicas Desacopladas |
|:---:|:---:|
| ![Tercera persona](docs/archivo/images/s05-a2-third-person.png) | ![SuperSoldier sobre plataforma](docs/archivo/images/s05-b42-platform-idle.png) |

---

## Requisitos y Apertura

- **Unity Editor:** `6000.3.29f1` (consistente con `ProjectSettings/ProjectVersion.txt`).
- **Apertura:** Abrir la raíz del proyecto desde **Unity Hub**. Unity Package Manager resolverá automáticamente las dependencias declaradas en `Packages/manifest.json`.
- **Paquetes Principales Integrados:**
  - Universal Render Pipeline (`com.unity.render-pipelines.universal` @ `17.3.0`)
  - Input System (`com.unity.inputsystem` @ `1.20.1`)
  - Cinemachine (`com.unity.cinemachine` @ `3.1.7`)
  - Test Framework (`com.unity.test-framework` @ `1.6.0`)
- **Robot (se importa aparte):** el paquete *Animated Low Poly Robot Free* (Unity Asset Store, de Juanpmh) **no está en Git** por la EULA del Asset Store. Antes de jugar o ejecutar pruebas, impórtalo desde el Package Manager en `Assets/FreeLowPolyRobot/`; sin él faltan el modelo y las animaciones del Player y del Echo.

---

## Controles del Videojuego

| Acción | Tecla / Control | Descripción |
|---|---|---|
| **Moverse** | `W` `A` `S` `D` | Movimiento planar relativo a la orientación de la cámara |
| **Orbitar Cámara** | `Ratón` | Rotación orbital en tercera persona con Cinemachine |
| **Saltar** | `Espacio` | Salto vertical con *Coyote Time* y *Jump Buffering* |
| **Grabar Echo** | `Q` | Graba hasta 5 segundos de acciones (posición y rotación a 20 Hz) |
| **Reproducir Echo** | `E` | Instancia un clon holográfico que replica la trayectoria grabada |
| **Rebobinar (Rewind)** | `Click Derecho (RMB)` *(mantener)* | Rebobina en el tiempo el mecanismo o plataforma apuntada |
| **Reiniciar Sala** | `R` | Devuelve al jugador al checkpoint activo y reinicia los mecanismos de la sala |

---

## Mecánicas Principales

### 1. Grabación y Clon Temporal (Echo)
Al presionar `Q`, el sistema comienza a registrar las poses y rotaciones del jugador durante un máximo de 5 segundos. Al pulsar `E`, un actor holográfico cian reproduce el recorrido exacto. Esta mecánica permite resolver retos asíncronos (por ejemplo, pararse sobre una placa de presión que abre una compuerta distante, grabar el tiempo necesario, correr hacia la puerta y ejecutar el clon para que mantenga la placa activada).

### 2. Reversión Cinemática de Objetos (Rewind)
El jugador puede apuntar con el ratón hacia objetos móviles (como plataformas flotantes) y mantener presionado `RMB`. El objeto retrocede fotograma a fotograma por su trayectoria histórica. Al soltar `RMB`, el mecanismo reanuda su movimiento normal hacia adelante desde el punto temporal recuperado, permitiendo cruzar abismos o recuperar plataformas que ya habían partido.

### 3. Checkpoints y Restauración Determinista de Salas
El juego divide los desafíos en salas independientes. Si el jugador cae al abismo o pulsa `R`, el sistema restaura el estado exacto de la sala en curso (posiciones iniciales de plataformas, compuertas, placas y cancela cualquier grabación o clon activo), garantizando que los puzles no queden en estados irrecuperables.

### 4. Puzle Combinado (Sala S04-C)
Integra ambas habilidades en secuencia coordinada:
1. Superar una compuerta pesada mediante **Echo**.
2. Llegar a una zona segura intermedia.
3. Rebobinar con **Rewind** una plataforma móvil que se encuentra al otro lado del abismo para poder abordarla y cruzar al muelle final.

---

## Pipeline Visual y Animación Humanoide

- **Entorno Sci-Fi Modular:** Piezas CC0 de Kenney (Modular Space Kit y Space Station Kit) con materiales URP propios; ver [créditos](docs/credits.md).
- **Protagonista:** Robot animado (paquete *Animated Low Poly Robot Free*, rig Generic); ver [créditos](docs/credits.md).
- **Locomoción y Física Desacopladas:** [`PlayerMotor`](Assets/_Echoryx/Scripts/Player/PlayerMotor.cs) mantiene la autoridad absoluta del movimiento físico, mientras que [`RobotAnimationDriver`](Assets/_Echoryx/Scripts/Player/RobotAnimationDriver.cs) y [`ResolvedPlanarMotion`](Assets/_Echoryx/Scripts/Player/ResolvedPlanarMotion.cs) calculan la cadencia y transiciones del Animator aislando el transporte de plataformas móviles, evitando que el personaje camine en falso cuando la plataforma lo transporta.

---

## Catálogo de Escenas

Las escenas principales se encuentran en `Assets/_Echoryx/Scenes/`:

- **`Level_01_Tutorial.unity`:** Primera escena del build. Nivel tutorial de tres salas que enseña Echo y Rewind ([detalle](docs/level-01-tutorial.md)).
- **`Prototype.unity`:** Circuito de pruebas de movimiento, puzzle de Echo, cruce con Rewind y sala combinada S04-C.
- **`RobotIntegrationLab.unity`:** Laboratorio del robot; lo usan las pruebas PlayMode de locomoción.

---

## Pruebas y Calidad de Software

El proyecto se rige por Desarrollo Guiado por Pruebas (TDD) y cuenta con una batería completa de pruebas automatizadas ejecutables en Unity Editor y batchmode:

```bash
# Ejecutar pruebas EditMode desde terminal:
/ruta/hacia/Unity -batchmode -quit -projectPath . -runTests -testPlatform editmode -testResults editmode-results.xml

# Ejecutar pruebas PlayMode desde terminal:
/ruta/hacia/Unity -batchmode -quit -projectPath . -runTests -testPlatform playmode -testResults playmode-results.xml
```

- **EditMode Tests:** 113 pasadas / 0 fallidas (100%)
- **PlayMode Tests:** 17 pasadas / 0 fallidas (100%)
- **Total:** **130 pruebas automatizadas aprobadas**

Para más detalles, consulta la [documentación de pruebas](docs/testing.md).

---

## Documentación del Proyecto

- 📄 [**Informe General de Avance (con capturas y análisis técnico)**](docs/archivo/informe-avance-proyecto.md)
- 🎮 [Game Design Document (GDD)](docs/GDD.md)
- 🏛️ [Arquitectura de Software y Ensamblados](docs/architecture.md)
- 📅 [Plan de Desarrollo y Roadmap](docs/development-plan.md)
- 🧪 [Guía de Pruebas y Validación](docs/testing.md)
- 🎨 [Archivo: pipeline de assets Quaternius (retirado)](docs/archivo/visual-assets.md)
- 🚶 [Archivo: locomoción del SuperSoldier (retirado)](docs/archivo/supersoldier-locomotion.md)
