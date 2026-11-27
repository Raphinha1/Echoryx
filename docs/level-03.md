# Level_03_Paradojas

Escena `Assets/_Echoryx/Scenes/Level_03_Paradojas.unity`, tercer nivel del catálogo (id `level-03-paradojas`, «Paradojas»). Es el **Laboratorio de Paradojas** y el objetivo es desbloquear el acceso al reactor. Es el nivel más difícil, pensado para 5–8 minutos.

**Cómo se regenera:** con `Echoryx > Level 03 > Build Paradojas`. El menú añade el nivel al `LevelCatalog` justo después de `level-02-sincronia` y ejecuta `Sync Build Settings`.

**Qué reutiliza:**
- Prefabs de nivel: placa, puerta, `Grate`, plataforma, peligro, meta y pistas.
- Arte Kenney, atmósfera y perfil de volumen de los niveles 1 y 2.
- Cerramiento de paredes y fosos con `LevelEnclosure`.
- Ayudantes del constructor del nivel 2, que pasan de `private` a `internal` sin cambiar su comportamiento.

No se ha tocado código de juego: `PlayerMotor`, Echo, Rewind y checkpoints siguen igual.

**Piezas nuevas,** montadas con componentes existentes:
- **Puente:** un `DoorController` cuyo panel es un tablero de 14 m. «Abierto» significa elevado hasta el suelo. Su umbral está muy por debajo, así que no espera al jugador: se hunde en cuanto la placa queda libre.
- **Elevadores:** son `RewindMovingPlatform` de un solo viaje, que bajan 3 m dentro de un pozo letal y llevan una placa encima. Esa placa no tiene colisionador propio, así que se pisa la plataforma.

## Medidas que usa el diseño

- **Jugador:** 6 m/s, salto de 1,65 m y alcance horizontal de unos 4,4 m. Todos los fosos miden más.
- **Echo:**
  - Graba como máximo 5 s.
  - Se reproduce en el mismo sitio y al mismo ritmo, desde donde empezó la grabación.
  - Al terminar, la grabación vuelve a estar disponible para otra E.
  - R conserva la última grabación terminada.
- **Rewind:**
  - Apunta con el centro de la cámara hasta 12 m.
  - Una vez sujeto, no hace falta seguir apuntando.
  - Retrocede como mucho los últimos 5 s de movimiento.
  - Mientras se mantiene el botón en el punto más antiguo, el mecanismo se queda quieto.
  - La compuerta y la reja cerradas bloquean la puntería.
- **Tiempos:** se miden en `Level03ParadojasPlayChecks`, que juega cada tramo con input real a 60 fps (líneas `L3TIME` del log). Primero prueba la trampa tentadora y comprueba que falla; después, la solución. El tiempo cero es la pulsación de E, salvo que se diga otra cosa. El margen exigido en la prueba es de 0,9 s.

| Tramo | Medida | Margen |
|---|---|---|
| 1 Rewind | viaje de 3,88 s dentro de 5 s de historial | 1,12 s |
| 1 Echo | cruza la puerta a 0,53 s del Echo de 5 s | 4,47 s |
| 2 | puente arriba a 1,28 s, cruzado a 3,90 s, Echo acaba a 5,03 s | 1,13 s |
| 3 | viaje rebobinado de 3,78 s dentro de 5 s de historial | 1,22 s |
| 4 | reja cruzada a 2,12 s | 2,88 s |
| 5a | puerta cruzada a 1,05 s | 3,95 s |
| 5b | bajo la compuerta a 3,60 s | 1,40 s |

En los tramos 4 y 5a el reloj no es lo difícil: Rewind sostiene el elevador indefinidamente, así que el margen es amplio a propósito. La dificultad está en entender la fase.

## 1. Recordatorio (z −2…28)

Primero, un foso de 10 m (z 6…16). La plataforma recorre 6,8 m a 1,75 m/s y, cuando llegas, ya está aparcada al otro lado. Después, una placa (z = 18,5) y su puerta (z = 25,5), a 7 m.

- **Tentador y erróneo:**
  - Saltar el foso: desde el borde hasta la plataforma aparcada hay 6,9 m.
  - Pisar la placa y correr hacia la puerta: se cierra en 0,35 s y tardas más de un segundo en llegar.
- **Solución:**
  1. Desde el borde, mantén **clic derecho** sobre la plataforma. Vuelve en 1,9 s y se queda quieta mientras sigues pulsando.
  2. Súbete sin soltar. Al **soltar**, su recorrido te lleva al otro lado en 3,88 s.
  3. En la placa, pulsa **Q** y deja que se agoten los 5 s.
  4. Ve hasta la puerta y pulsa **E**. Ábrela y crúzala. Checkpoint en z = 28.

## 2. El Echo sostiene el puente (z 28…50,5)

Hay un foso de 14 m (z 34…48). En el fondo descansa el tablero del puente, 6,3 m por debajo, y sube a 5 m/s mientras alguien pisa su placa (z = 31,5).

- **Tentador y erróneo:** pisar la placa y echar a correr. El tablero empieza a hundirse en cuanto sales de la placa y caes al foso. La prueba lo comprueba: se cuenta un intento.
- **Solución:**
  1. En la placa, pulsa **Q** y deja que se agoten los 5 s.
  2. Ve hasta el borde y pulsa **E**. El puente tarda 1,28 s en subir si estaba abajo del todo.
  3. Crúzalo de inmediato: llegas al otro lado a los 3,90 s. El Echo termina a los 5,03 s y el puente se hunde detrás de ti.

## 3. Rewind recupera la plataforma (z 50,5…63,5)

Hay un foso de 8,5 m (z 53…61,5). La plataforma llega desde el otro lado (5,3 m a 1,4 m/s) y queda aparcada junto a ti. Su recorrido normal ya ha terminado.

- **Tentador y erróneo:** subirse y saltar hacia el otro lado. Quedan 5,4 m: caes al foso.
- **Solución:**
  1. Súbete y mantén **clic derecho** apuntando a la plataforma. Rewind la devuelve por donde vino y te lleva encima en 3,78 s.
  2. Bájate en el otro lado y suelta. Checkpoint en z = 63,5.

## 4. Fase: el elevador (z 63,5…84)

Junto al pasillo hay un pozo de 2,8 × 3 m (z 68…71). El elevador empieza a ras del suelo y baja 3 m en 3,75 s. En el fondo hay un peligro: desde 1,5 m de profundidad caer es mortal, así que nadie puede quedarse sobre el elevador aparcado.

Encima del elevador va la placa de una reja que cierra el pasillo (z = 80). Cuando llegas, el elevador ya está abajo.

- **Idea:** el Echo repite tu recorrido en el mismo sitio y al mismo ritmo, pero el elevador no lo espera. Cuando pulsas E, la placa ya no está donde la pisaste. Hay que devolverla a esa fase con Rewind **antes** de pulsar E.
- **Tentador y erróneo:**
  1. Subir el elevador con Rewind, pisar su placa y correr a la reja. Se cierra mucho antes de llegar, porque hay 10 m.
  2. Grabarte en la placa, dejar que el elevador se hunda y pulsar E. El Echo aparece arriba, flotando sobre el pozo, y la placa está abajo: la reja no se abre en ningún momento.
- **Solución:**
  1. Desde el pasillo, junto al pozo, mantén **clic derecho** sobre el elevador. Sube en 2,5 s y se queda arriba mientras pulsas.
  2. Sin soltar, súbete a la placa y pulsa **Q**. Deja que se agoten los 5 s.
  3. Bájate al pasillo. Si soltaste, vuelve a subirlo con clic derecho y mantenlo.
  4. Con el elevador arriba, pulsa **E** y corre a la reja **sin soltar**. La cruzas a los 2,12 s. Checkpoint en z = 84.

**Por qué el viaje es de ida:** se pactó un ciclo único de ida y vuelta (A→B→A). Se cambió por un viaje de ida que termina en una zona letal. Una plataforma que vuelve a su origen queda quieta en un sitio alcanzable, y entonces bastaría con grabarse de pie sobre ella aparcada para resolver la sala sin Rewind. Con el viaje de ida que acaba en el peligro:
- la única forma de pisar la placa es sostener el elevador con Rewind;
- la única forma de que el Echo la pise es devolverlo a la misma fase.

Además, no hace falta código nuevo.

## 5. Acertijo final

### Etapa 1 — Dos placas (z 84…101,4)

Un segundo elevador, idéntico (pozo en z 89…92), lleva una de las dos placas que exige la puerta de z = 99. La otra está justo delante de ella (z = 97,6), a 7 m del elevador.

- **Tentador y erróneo:**
  - Pisar solo tu placa.
  - Grabar en el elevador, dejarlo hundirse y pulsar E desde tu placa: fuera de fase, la placa del elevador está abajo.
- **Solución:**
  1. Sube el elevador con Rewind, mantenlo y grábate 5 s en su placa (**Q**).
  2. Bájate. Con el elevador sujeto arriba, ve a la placa de la puerta.
  3. Pulsa **E**. Con el Echo arriba y tú en tu placa, la puerta se abre. Crúzala; su umbral la mantiene abierta mientras estás debajo.

Hay un checkpoint en z = 101,4, justo antes de la etapa más exigente.

### Etapa 2 — La compuerta colgante (z 101,4…124)

Hay un foso de 9 m (z 107…116). La plataforma hace su viaje de ida desde el muelle (z = 108,6) hasta el otro lado (z = 114,4): 5,8 m a 1,3 m/s, 4,46 s. Una compuerta cuelga sobre el foso en z = 112,8: deja pasar la plataforma por debajo, pero no a quien va encima. Su placa está en z = 103,4.

La plataforma aparca **detrás** de la compuerta. Cerrada, la compuerta no deja apuntar a través. Desde la placa, la plataforma queda a más de 12 m de la cámara.

- **Tentador y erróneo:** pulsar E para abrir la compuerta, traer la plataforma con Rewind, subirse y soltar. Entre abrir, rebobinar (2,8 s), subirse y viajar, el Echo se acaba antes de llegar a la compuerta: te empuja al foso.
- **Solución,** la única que lleva a la meta:
  1. En la placa de la compuerta, pulsa **Q** y deja que se agoten los 5 s.
  2. En el muelle, pulsa **E**. El Echo abre la compuerta y ya puedes apuntar.
  3. Mantén **clic derecho** sobre la plataforma: vuelve al muelle y se queda allí mientras pulsas.
  4. Súbete sin soltar y **espera a que termine el Echo**. La compuerta se cierra, pero la plataforma está de este lado.
  5. Pulsa **E** otra vez. El Echo repite la grabación y la compuerta se abre.
  6. **Suelta** el clic derecho. El viaje normal te lleva bajo la compuerta abierta a los 3,60 s y te deja en el otro lado.
  7. Entra en la meta (z = 120): el acceso al reactor queda desbloqueado.

## Pistas

Las pistas explican reglas, nunca la solución:

- **Intro:** Laboratorio de Paradojas. Objetivo: desbloquear el acceso al reactor.
- **Rewind:**
  - Mantener clic derecho hace retroceder un mecanismo por el camino que ya recorrió, como máximo 5 s.
  - Al soltar, sigue su recorrido.
  - Rewind lleva consigo a quien esté encima.
  - Mientras lo mantienes, el mecanismo no avanza.
- **Echo:**
  - Q graba hasta 5 s.
  - E lo repite desde el punto donde empezaste a grabar.
  - El Echo repite tu recorrido en el mismo sitio y al mismo ritmo, pero el mundo no lo espera.
  - Puede repetir su grabación otra vez cuando termina.
- **Mecanismos:**
  - El puente solo está arriba mientras alguien pisa su placa.
  - Una placa sobre una plataforma viaja con ella.
  - La puerta doble exige sus dos placas a la vez.
  - La compuerta se abre con su placa. Cerrada, no deja apuntar a través de ella y, si te alcanza, te empuja fuera.
- **R:** te devuelve al último checkpoint y conserva tu última grabación terminada.
