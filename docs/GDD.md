# ECHORYX

## Game Design Document

**Versión:** 1.0 (actualizada al estado implementado, 27 de noviembre de 2026). La versión 0.2 es el documento de diseño original; los cambios entre ambas se registran en la sección 16.

**Tipo:** Videojuego para un jugador  
**Género:** Acción, plataformas y acertijos temporales en tercera persona  
**Motor:** Unity 6, Universal Render Pipeline (URP)  
**Plataforma:** PC (Windows como objetivo de distribución; desarrollo compatible con Linux)  
**Duración objetivo:** 15–25 minutos  
**Calendario de producción:** 8 semanas  
**Estado del nombre:** Nombre de trabajo seleccionado; disponibilidad comercial y marcaria pendiente de verificar.

---

## 1. Visión del producto

### 1.1 Premisa

Echoryx es un videojuego de acción y plataformas en tercera persona ambientado en una instalación científica donde un experimento ha fracturado la estabilidad temporal.

El protagonista, un robot de pruebas equipado con un sistema tecnológico experimental, debe escapar del complejo utilizando dos habilidades:

- **Echo:** registrar sus movimientos y reproducirlos mediante una copia holográfica de sí mismo.
- **Rewind:** retroceder el historial temporal de determinados objetos del escenario.

Los desafíos combinan desplazamiento, saltos, sincronización y resolución de acertijos. La dificultad aumenta conforme el jugador aprende a utilizar ambas habilidades de manera independiente y conjunta.

### 1.2 Fantasía del jugador

El jugador debe sentirse capaz de manipular su entorno mediante la comprensión del tiempo. Su principal herramienta no es la fuerza, sino la capacidad de preparar acciones, reproducirlas y modificar situaciones que parecían irreversibles.

### 1.3 Pilares de diseño

**Cooperar con el propio pasado.** El jugador utiliza acciones previamente grabadas como apoyo para superar obstáculos.

**Modificar el entorno.** Los objetos temporales pueden regresar a estados anteriores, creando nuevas oportunidades.

**Dominar el movimiento.** Los saltos, el posicionamiento y la sincronización son fundamentales.

**Aprender mediante experimentación.** Los errores deben ser recuperables y los desafíos deben ofrecer información visual suficiente.

### 1.4 Público objetivo

Jugadores interesados en plataformas 3D, acertijos de lógica espacial, ciencia ficción y experiencias cortas basadas en mecánicas distintivas.

### 1.5 Principios de producción

- Calidad y estabilidad antes que cantidad de contenido.
- Un número reducido de mecánicas con múltiples aplicaciones.
- Reutilización de componentes, escenarios y recursos.
- Introducción progresiva de conceptos.
- Ausencia de dependencias en servicios externos.
- Una partida completa verificable desde el menú inicial hasta los créditos.

---

## 2. Mundo y narrativa

### 2.1 Contexto

El Complejo Echoryx es una instalación de investigación dedicada a estudiar la resonancia temporal: la posibilidad de conservar y recuperar estados anteriores de la materia.

Durante una prueba del reactor principal, una sobrecarga provoca una anomalía que altera diferentes sectores de la instalación. Las puertas de seguridad se bloquean, las plataformas de transporte dejan de funcionar correctamente y ciertos mecanismos comienzan a comportarse de manera anormal.

El protagonista recupera la conciencia después del accidente y descubre que su sistema experimental conserva dos funciones que siguen operativas: Echo y Rewind.

### 2.2 Protagonista

**Identidad:** Unidad robótica de pruebas del Complejo Echoryx.

**Apariencia:** Robot humanoide de estilo caricaturesco, con acabado ámbar dorado y ojos luminosos blancos.

**Rol narrativo:** Participó en las pruebas del sistema temporal y ahora debe alcanzar el núcleo para contener la anomalía.

**Personalidad:** No se desarrollará mediante diálogos extensos. Su identidad se transmitirá principalmente a través de su diseño y las circunstancias del escenario.

### 2.3 Conflicto

La anomalía temporal amenaza con desestabilizar por completo el complejo. El sistema de contención automática no puede reiniciarse mientras permanezcan activas las fallas en los sectores principales.

El protagonista debe atravesar tres sectores de investigación, restaurar el acceso al reactor y activar manualmente el protocolo de estabilización.

### 2.4 Estructura narrativa

**Inicio:** El protagonista despierta en una cámara de seguridad. Un mensaje de emergencia indica que debe llegar al reactor central.

**Desarrollo:** Cada sector revela una aplicación distinta de la tecnología temporal y muestra las consecuencias del accidente.

**Clímax:** El protagonista alcanza el núcleo, donde debe combinar sus dos habilidades para activar el sistema de contención. En la versión actual, el juego concluye al abrir el acceso al núcleo al final del tercer nivel; el desafío final queda como ampliación futura.

**Final:** El reactor se estabiliza, las alarmas cesan y se abre una salida de emergencia. El protagonista abandona la instalación.

### 2.5 Narrativa ambiental

La historia se comunicará mediante:

- Mensajes breves en terminales holográficas.
- Señalización y nombres de sectores.
- Alarmas, iluminación y cambios ambientales.
- Una introducción corta y una secuencia final controlada por el juego.

No se requieren personajes secundarios, actuaciones de voz ni cinemáticas complejas.

---

## 3. Estructura de jugabilidad

### 3.1 Bucle principal

1. Entrar en una zona.
2. Identificar el objetivo y los obstáculos.
3. Explorar las posibilidades de Echo y Rewind.
4. Preparar y ejecutar una solución.
5. Superar la sección.
6. Activar un checkpoint.
7. Avanzar hacia un desafío de mayor complejidad.

### 3.2 Cámara

Cámara en tercera persona con seguimiento suave del protagonista, rotación mediante ratón y control de colisiones con el entorno.

La cámara debe permitir observar el personaje, los mecanismos relevantes y los puntos de aterrizaje.

### 3.3 Movimiento

El personaje podrá caminar, correr, saltar y controlar parcialmente su dirección durante el salto.

Se implementarán:

- Movimiento relativo a la orientación de la cámara.
- Aceleración y desaceleración controladas.
- Gravedad y detección de suelo.
- Coyote time y jump buffering.
- Interacción estable con plataformas móviles.
- Reaparición desde checkpoints.

No habrá doble salto, dash, escalada ni combate en la primera versión.

---

## 4. Habilidad Echo

### 4.1 Propósito

Echo permite registrar una secuencia de movimientos del protagonista y reproducirla mediante una copia temporal que actúa de manera independiente.

### 4.2 Reglas

- Duración máxima de grabación: 5 segundos.
- La grabación puede detenerse anticipadamente.
- El jugador decide cuándo iniciar la reproducción.
- Solo puede existir una grabación preparada y un clon activo.
- La grabación anterior se reemplaza cuando se completa una nueva.
- El clon reproduce posiciones, orientación y eventos interactivos compatibles.
- El clon no recibe daño, no combate y no toma decisiones propias.
- Al terminar su secuencia, desaparece.
- La grabación completada se conserva cuando el jugador reaparece en un checkpoint y puede reproducirse varias veces, hasta que se complete una nueva.

### 4.3 Estados

**Disponible → Grabando → Preparado → Reproduciendo → Disponible**

El HUD mostrará claramente el estado actual y el tiempo de grabación restante.

### 4.4 Interacciones permitidas

Echo podrá activar placas de presión y sensores temporales que acepten explícitamente clones.

No podrá activar cualquier interacción del escenario de manera automática. Cada mecanismo deberá indicar si acepta al jugador, a Echo o a ambos.

### 4.5 Restricciones de diseño

La reproducción debe ser suficientemente consistente para resolver los desafíos diseñados.

Los mecanismos destinados a Echo no dependerán de simulaciones físicas complejas ni de la aplicación de fuerzas por parte del clon.

---

## 5. Habilidad Rewind

### 5.1 Propósito

Rewind permite recuperar estados anteriores de objetos específicos del entorno.

### 5.2 Reglas

- El jugador apunta hacia un objeto compatible.
- Debe encontrarse dentro de la distancia de interacción (12 m en la versión actual).
- Mantener pulsado el control de Rewind inicia el retroceso.
- Historial objetivo: 5 segundos.
- Solo se rebobina un objeto a la vez.
- El objeto detiene temporalmente su comportamiento normal.
- Al finalizar, continúa desde el estado restaurado.
- No afecta al jugador, al clon ni al mundo completo.
- Los objetos incompatibles no pueden seleccionarse.

### 5.3 Objetos compatibles

La primera versión admitirá:

- Plataformas móviles con recorridos definidos.
- Elevadores con recorridos programados. Las plataformas colapsables no se incluyeron en la versión actual.
- Elementos mecánicos desplazables mediante animaciones programadas.

No se incluirá rebobinado genérico de cuerpos rígidos ni de simulaciones físicas arbitrarias.

### 5.4 Información visual

Los objetos compatibles tendrán indicadores discretos. Cuando el jugador apunte a uno, aparecerá un contorno o resaltado.

Durante Rewind se utilizarán partículas, distorsión visual o estelas violetas, acompañadas de una indicación de progreso temporal.

### 5.5 Seguridad

Los objetos rebobinables utilizarán recorridos y estados previsibles. Los desafíos evitarán situaciones que requieran atravesar al jugador con geometría sólida.

Si las pruebas revelan conflictos de colisiones, se ajustará el diseño del mecanismo antes de ampliar la complejidad física del sistema.

---

## 6. Combinación de habilidades

Echo y Rewind son independientes y pueden funcionar simultáneamente.

Un desafío combinado puede requerir que Echo mantenga activado un interruptor mientras el jugador rebobina una plataforma y aprovecha su posición restaurada para cruzar.

La combinación debe resultar comprensible a partir de las reglas ya aprendidas. No se introducirán excepciones ocultas ni cambios arbitrarios en el comportamiento de las habilidades.

---

## 7. Interacciones y obstáculos

### 7.1 Placas de presión

Se activan mientras un actor válido permanece dentro de su área.

Pueden configurarse para aceptar al jugador, al clon o a ambos.

### 7.2 Puertas

Se abren al recibir una señal de activación. Pueden permanecer abiertas mientras exista la señal o durante un intervalo definido.

### 7.3 Plataformas móviles

Recorren trayectorias predefinidas. Algunas serán compatibles con Rewind.

### 7.4 Rejas y compuertas

Las rejas translúcidas permanecen abiertas mientras una placa está activa. Las compuertas colgantes se cierran cuando deja de recibirse la señal y bloquean el paso. Las plataformas colapsables previstas inicialmente no se incluyeron.

### 7.5 Puentes y elevadores

El nivel 3 incluye un puente que sube desde un foso mientras una placa permanece activa y elevadores de un solo viaje que deben devolverse a su fase inicial con Rewind. Las barreras energéticas previstas inicialmente no se incluyeron.

### 7.6 Zonas de peligro

Provocarán la reaparición del jugador en el último checkpoint. Se utilizarán principalmente caídas al vacío y superficies energizadas claramente señalizadas.

---

## 8. Diseño de niveles

### Nivel 1 — Cámara de Resonancia (Tutorial)

**Mecánicas principales:** Echo y, al final del nivel, introducción de Rewind.

**Objetivo narrativo:** Abandonar la cámara de pruebas y recuperar el acceso al sistema de transporte.

**Secuencia:**

1. Introducción al movimiento y salto.
2. Activación del primer checkpoint.
3. Demostración guiada de la grabación Echo.
4. Apertura de una puerta mediante una placa de presión.
5. Desafío de coordinación: grabar una secuencia, reproducirla y cruzar una compuerta antes de que el clon termine.
6. Introducción a Rewind: recuperar una plataforma móvil para cruzar un foso con zona de peligro, y acceso al siguiente sector.

**Duración objetivo:** 3–5 minutos.

**Criterio de finalización:** El jugador comprende cómo grabar, preparar y reproducir Echo sin instrucciones adicionales.

### Nivel 2 — Sector de Ruptura (Sincronía)

**Mecánicas principales:** Echo y Rewind combinadas.

**Objetivo narrativo:** Recuperar el paso hacia las instalaciones de investigación.

**Secuencia:**

1. Sala 1 (el pozo): grabar dentro de un pozo y reproducir la grabación sobre la placa que abre la puerta; la grabación se conserva al reaparecer.
2. Sala 2 (la reja): el Echo mantiene abierta una reja translúcida mientras el jugador rebobina, a través de ella, una plataforma lejana.
3. Cruce sobre la plataforma recuperada hasta el otro lado.
4. Sala 3 (la compuerta): una compuerta colgante bloquea el paso; el Echo la mantiene abierta mientras el jugador viaja sobre una plataforma rebobinada.
5. Desafío de precisión que exige elegir el momento adecuado para grabar, reproducir y rebobinar.
6. Acceso al laboratorio principal.

**Duración objetivo:** 4–6 minutos.

**Criterio de finalización:** El jugador utiliza Rewind para modificar posiciones y estados del escenario de manera intencional.

### Nivel 3 — Laboratorio de Paradojas

**Mecánicas principales:** Echo y Rewind.

**Objetivo narrativo:** Desbloquear el acceso al reactor central.

**Secuencia:**

1. Tramo 1: recordatorio de ambas habilidades; rebobinar una plataforma para cruzar un foso y pisar una placa que abre una puerta.
2. Tramo 2: el Echo sostiene un puente largo que sube desde un foso mientras el jugador cruza.
3. Tramo 3: Rewind recupera una plataforma necesaria para continuar.
4. Tramo 4: primera combinación obligatoria de Echo y Rewind; el Echo debe pisar una placa situada sobre un elevador de un solo viaje, por lo que hay que devolver el elevador a su fase inicial con Rewind antes de reproducir la grabación.
5. Tramo 5: acertijo final de múltiples etapas (puerta doble y compuerta colgante), con un checkpoint antes de la sección más exigente.
6. Apertura del acceso al núcleo.

**Duración objetivo:** 5–8 minutos.

**Criterio de finalización:** El jugador puede planificar una secuencia que involucre ambas habilidades.

### Desafío final — Núcleo Temporal (fuera de la versión actual)

**Objetivo narrativo:** Estabilizar el reactor.

El jugador debe activar tres dispositivos de contención en una secuencia de plataformas y mecanismos temporales.

El desafío incluirá una cuenta regresiva local y una combinación final de Echo y Rewind.

Si falla, regresará al checkpoint del inicio del desafío, sin repetir los niveles anteriores.

Al completarlo, se reproducirá una secuencia breve de estabilización y aparecerá la pantalla de victoria.

**Duración objetivo:** 3–6 minutos.

---

## 9. Progresión y dificultad

La progresión se basará en cuatro etapas:

**Aprendizaje:** El jugador utiliza una habilidad en condiciones seguras.

**Práctica:** Debe aplicarla con mayor precisión.

**Combinación:** Debe coordinar distintas acciones y mecanismos.

**Dominio:** Debe resolver una secuencia completa bajo una restricción temporal moderada.

La dificultad no aumentará mediante enemigos o penalizaciones severas.

Los checkpoints se colocarán antes de desafíos largos y después de superar secciones significativas.

---

## 10. Interfaz y controles

### 10.1 Controles iniciales

| Acción | Control |
| :-- | :-- |
| Movimiento | WASD |
| Cámara | Ratón |
| Salto | Espacio |
| Echo: iniciar/detener grabación | Q |
| Echo: reproducir | E |
| Rewind | Mantener botón derecho del ratón |
| Reiniciar desde checkpoint | R |
| Pausa | Esc |

Controles adicionales implementados: Retroceso reinicia el nivel; H oculta o muestra el recordatorio de controles; en mando, el botón Start abre la pausa. Los controles podrán ajustarse durante pruebas de usabilidad.

### 10.2 HUD

El HUD mostrará únicamente la información necesaria:

- Estado de Echo.
- Duración disponible o grabada, mediante una barra de 5 segundos.
- Indicador de objeto compatible con Rewind.
- Progreso del rebobinado.
- Instrucciones contextuales.
- Cuenta regresiva en los desafíos que la requieran (no incluida en la versión actual).

No habrá barras de salud, inventario ni sistemas de puntuación obligatorios.

### 10.3 Menús

La versión actual incluye menú principal con selector de niveles (mejor tiempo, intentos y usos de Rewind por nivel), pausa, opciones, créditos, pantalla de fin de nivel y opción de reinicio.

Las opciones cubren volumen de música y efectos, pantalla completa, resolución y reinicio del progreso guardado. La sensibilidad del ratón no se incluyó.

---

## 11. Dirección artística

### 11.1 Estilo

Ciencia ficción estilizada con geometría relativamente sencilla, superficies metálicas, paneles luminosos y elementos holográficos.

Se priorizará una apariencia coherente sobre el realismo; el resultado final adopta una estética caricaturesca de bajo polígono con paleta lila y naranja.

### 11.2 Paleta visual

- Fondos: lila y violeta, con detalles en naranja.
- Echo: cian.
- Rewind: violeta.
- Peligros: naranja y rojo.
- Elementos interactivos: iluminación clara y consistente.

### 11.3 Protagonista

Robot humanoide con animaciones de reposo, caminar, correr, salto y caída.

Se priorizará un modelo reutilizable con rig humanoide y animaciones compatibles, evitando la creación de un personaje completo desde cero.

### 11.4 Efectos

Echo utilizará transparencia, estelas y partículas cian.

Rewind utilizará partículas violetas, rastros de movimiento y efectos de inversión temporal.

Los efectos deberán comunicar el estado de las habilidades sin dificultar la visibilidad de las plataformas.

---

## 12. Dirección sonora

Se utilizará música ambiental electrónica de ciencia ficción, con intensidad moderada durante la exploración y mayor tensión en el desafío final.

Efectos prioritarios:

- Inicio y fin de grabación Echo.
- Aparición y desaparición del clon.
- Activación de Rewind.
- Apertura de puertas.
- Activación de checkpoints.
- Saltos, aterrizajes y pasos.
- Alarmas del laboratorio.
- Estabilización del reactor.

Los recursos de terceros deberán tener licencias compatibles con el proyecto y sus créditos correspondientes. La versión actual incluye música de menú y de nivel con transición progresiva, y efectos de Echo, Rewind (sonido sintetizado propio), puertas, checkpoint, meta, pasos, salto y aterrizaje. Las alarmas del laboratorio y la secuencia de estabilización no se incluyeron.

---

## 13. Alcance y prioridades

### Obligatorio (P0)

- Movimiento y cámara funcionales.
- Echo completo.
- Rewind completo para objetos controlados.
- Interacciones y obstáculos esenciales.
- Checkpoints.
- Tres niveles (el desafío final queda fuera de la versión actual).
- HUD y controles comprensibles.
- Menú, pausa, reinicio y victoria.
- Build ejecutable y estable.

### Importante (P1)

- Animaciones y efectos pulidos.
- Audio y música.
- Tutoriales contextuales.
- Opciones básicas.
- Narrativa ambiental.
- Guardado del nivel desbloqueado entre sesiones.

### Opcional (P2)

- Coleccionables.
- Estadísticas de finalización (implementadas: mejor tiempo, intentos y usos de Rewind).
- Desafíos adicionales.
- Logros.
- Efectos visuales avanzados.

### Fuera de alcance

Combate, enemigos con IA, multijugador, árbol de habilidades, mundo abierto, generación procedural, manipulación global del tiempo y simulación física temporal generalizada.

---

## 14. Criterios de aceptación

El juego estará listo para entrega cuando:

1. Pueda iniciarse y completarse sin utilizar el editor de Unity.
2. Echo reproduzca correctamente las acciones necesarias para los desafíos.
3. Rewind funcione consistentemente con los objetos diseñados.
4. Ambas habilidades puedan utilizarse conjuntamente.
5. Los checkpoints permitan recuperar la partida tras fallar.
6. Todos los niveles tengan inicio, objetivo y salida claramente identificables.
7. No existan errores conocidos que impidan avanzar.
8. Los controles y estados de las habilidades sean comprensibles.
9. Los recursos externos estén correctamente atribuidos.
10. Exista una versión ejecutable y documentación mínima de instalación y controles.

---

## 15. Riesgos y decisiones de control

**Riesgo:** inconsistencias en Echo.  
**Mitigación:** reproducción de transformaciones y eventos controlados, evitando física compleja.

**Riesgo:** colisiones durante Rewind.  
**Mitigación:** objetos con trayectorias programadas y escenarios diseñados para evitar interpenetraciones.

**Riesgo:** exceso de contenido.  
**Mitigación:** niveles cortos, componentes reutilizables y prohibición de añadir nuevas mecánicas principales después de la cuarta semana.

**Riesgo:** integración artística.  
**Mitigación:** seleccionar pronto un modelo humanoide compatible y trabajar inicialmente con geometría provisional.

**Riesgo:** retrasos.  
**Mitigación:** conservar como prioridad una partida completa y funcional, reduciendo elementos P1 y P2 antes que eliminar mecánicas fundamentales.

---

## 16. Control de cambios

Este GDD será la referencia de diseño del proyecto.

Las modificaciones que afecten mecánicas principales, duración, estructura de niveles o alcance deberán registrarse y evaluarse respecto al calendario de producción.

Los valores numéricos, como la duración de Echo y Rewind, son objetivos iniciales sujetos a pruebas de jugabilidad.

**Cambios de la versión 1.0 (27 de noviembre de 2026), para reflejar el juego implementado:**

- El protagonista pasa de un investigador humano a un robot humanoide de estilo caricaturesco (secciones 1.1, 2.1, 2.2 y 11.3).
- La grabación de Echo se conserva tras reaparecer y puede reproducirse varias veces hasta completar una nueva (sección 4.2).
- Las plataformas colapsables y las barreras energéticas se sustituyen por rejas, compuertas, puentes y elevadores (sección 7).
- El nivel 1 introduce Echo y Rewind; los niveles 2 (Sincronía) y 3 (Paradojas) combinan ambas habilidades (sección 8).
- El desafío final Núcleo Temporal y la cuenta regresiva quedan fuera de la versión actual (secciones 8, 10.2 y 13).
- Se incorporan selector de niveles con estadísticas, opciones, créditos y reinicio de progreso; se omite la sensibilidad del ratón (sección 10.3).
- La paleta final es lila y naranja, con protagonista en ámbar dorado (sección 11.2).
