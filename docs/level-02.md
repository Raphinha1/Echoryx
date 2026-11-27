# Level_02_Sincronia

Escena `Assets/_Echoryx/Scenes/Level_02_Sincronia.unity`, segundo nivel del catálogo (id `level-02-sincronia`, «Sincronía»). Se regenera con `Echoryx > Level 02 > Build Sincronía`, que además la añade al `LevelCatalog` después del tutorial y ejecuta `Sync Build Settings`. Reutiliza los prefabs de nivel (con su arte), las piezas Kenney, la atmósfera y el perfil de volumen del nivel 1. Pieza nueva: `Prefabs/Level/Grate.prefab`, un `DoorController` con panel translúcido (barras naranjas y campo lila) que bloquea el paso **y** la puntería de Rewind.

Único cambio de código de juego: `DoorController` admite placas adicionales (`extraPlates`) que deben estar pulsadas a la vez que la principal; vacío por defecto, así que las puertas existentes no cambian. No se modificó `PlayerMotor`, Echo, Rewind ni checkpoints.

## Medidas que usa el diseño

- Jugador: 6 m/s, salto de 1,65 m de altura, alcance horizontal de unos 4,4 m.
- Echo: graba como máximo 5 s y se reproduce desde la pose en la que empezó la grabación, esté donde esté el jugador al pulsar E.
- Rewind: apunta con el centro de la cámara hasta 12 m y, una vez sujeto, no necesita seguir apuntando.
- Plataforma de la sala 2: 5,4 m a 2,4 m/s (2,25 s), rebobinado ×1,2 (1,9 s). Es 1,6 veces más rápida que en la primera versión.
- Plataforma de la sala 3: 6,5 m a 3,2 m/s, rebobinado ×0,7. El viaje rebobinado va a 2,24 m/s, 1,6 veces más rápido que en la primera versión.

Los tiempos de cada paso se midieron en `Level02SincroniaPlayChecks`, que juega cada sala con input real a 60 fps. El tiempo cero es la pulsación de E.

## Sala 1 — Dos placas (z −2…14)

Una puerta a z = 14 exige dos placas a la vez: una justo delante (z = 12,6) y otra al fondo de un pozo de 2,6 m del que no se sale saltando. El pozo mide 2,7 × 7 m, con la placa en el extremo lejano para que la cámara tenga sitio detrás del jugador.

- **Tentador y erróneo:** grabar al Echo en la placa de la puerta y bajar tú al pozo. La puerta se abre, pero tú estás atrapado abajo.
- **Solución:**
  1. Baja al pozo, ponte en la placa y pulsa **Q**. Empieza a grabar ya encima de la placa y deja que se agoten los 5 s, o detén la grabación.
  2. Pulsa **R**. Vuelves al inicio y la grabación terminada se conserva. Si la grabación seguía en curso al pulsar R, se pierde.
  3. Ponte en la placa de la puerta y pulsa **E**. El Echo aparece en el pozo y la puerta se abre en 0,55 s.
  4. Cruza el umbral a los 1,05 s y continúa hasta el checkpoint de z = 17.
- **Lectura de la placa:** usa el mismo indicador que en el nivel 1 (naranja libre, verde contigo, cian con el Echo), pero en esta placa es más intenso, emite más partículas y reacciona desde 10 m. Además, una luz cálida ilumina el fondo del pozo.
- **Margen:** 3,95 s. Es una sala de introducción: la dificultad es entender que R conserva la grabación, no el reloj.

## Sala 2 — La reja (z 14…39)

Una reja en el borde del foso (z = 30,15) solo se abre mientras alguien pisa su placa, que está a 9 m (z = 21). Cerrada, impide pasar y también apuntar con Rewind a la plataforma, que se ve aparcada al otro lado. Desde la placa, la plataforma queda fuera del alcance de Rewind.

- **Tentador y erróneo:** pisar la placa tú mismo. La reja se abre, pero se cierra en cuanto la dejas, mucho antes de que llegues. Apuntar a la plataforma con la reja cerrada tampoco funciona.
- **Solución:**
  1. En la placa de la reja, pulsa **Q** ya encima y deja que se agoten los 5 s.
  2. Ve hasta el borde, delante de la reja.
  3. Pulsa **E**. El Echo pisa la placa y la reja sube.
  4. Mantén **clic derecho** sobre la plataforma desde 0,32 s. Vuelve al borde a los 2,17 s.
  5. Sin soltar, súbete. Cruzas la reja a los 2,45 s. Suelta y viaja.
  6. El Echo deja la placa a los 5,02 s.
- **Margen:** 2,57 s; antes de acelerar la plataforma era 1,43 s. Sigue siendo necesario que el Echo sujete la placa: si la pisas tú, la reja se cierra al irte (comprobado). Una grabación que empieza a unos 2,5 s de la placa, o pulsar E en la placa y caminar después los 9 m hasta la reja, ya no deja tiempo.

La zona anti-aplastamiento de la reja está sobre el foso, no delante. Si estuviera delante, quedarse bajo la reja abierta la mantendría abierta para siempre.

## Sala 3 — La compuerta (z 39…64)

La plataforma de esta sala viaja del lado lejano al cercano y queda aparcada junto al jugador. Sobre el foso cuelga una compuerta (z = 52,4) cuyo borde inferior queda a 0,4 m: deja pasar la plataforma, pero no a quien va encima. La abre una placa (z = 41,6). Rewind devuelve la plataforma por donde vino y lleva al jugador al otro lado.

- **Tentador y erróneo:** subirse y rebobinar sin Echo. La compuerta cerrada empuja al jugador fuera de la plataforma y cae al foso; está comprobado. Pisar la placa tú mismo tampoco sirve, porque no puedes estar a la vez sobre la plataforma.
- **Solución:**
  1. En la placa de la compuerta, pulsa **Q** ya encima y deja que se agoten los 5 s.
  2. Súbete a la plataforma aparcada.
  3. Pulsa **E** y mantén **clic derecho** apuntando a la plataforma.
  4. Pasas bajo la compuerta abierta a los 2,43 s, de pie en el centro de la plataforma, y llegas al otro lado a los 3,00 s.
  5. Bájate sin soltar, suelta y ve a la meta (z = 60,5).
- **Margen:** 2,57 s hasta dejar atrás la compuerta. Con la plataforma más rápida, el margen pasó de 1,22 a 2,57 s; sigue por encima de 1 s, así que no se ajustó nada más. La compuerta tampoco baja sobre alguien que ya está debajo, lo que da algo más de margen real. La sala sigue exigiendo el Echo: sin él, la compuerta te empuja al foso (comprobado).

## Cerramiento

El nivel es una sección de estación cerrada: no se coloca el decorado flotante del nivel 1. Detrás de las cuatro paredes perimetrales hay paneles opacos que van del fondo de los fosos a lo alto de los muros y sobresalen 30 cm por cada extremo. Las cuatro esquinas tienen pilares que solapan ambos paneles. Como sus medidas salen de las piezas de pared generadas, ninguna rendija entre piezas deja ver el exterior. No tienen collider, así que no afectan al jugador ni a la cámara. Los revestimientos del pozo y de los fosos se solapan 6 cm en cada esquina. Cada foso es una caja cerrada, con paneles laterales, de extremo y fondo bajo el campo de peligro. El pozo tiene sus cuatro paredes revestidas. El casco bajo la sala 1 queda por debajo del fondo del pozo. Todo lo genera el builder, así que se conserva al reconstruir la escena. Se revisó con capturas desde la cámara real de cada sala, incluido el pozo, girando 360° a tres inclinaciones. Para que cualquier hueco saltara a la vista, se pintó de magenta el fondo exterior: solo aparece por encima de las paredes, porque el nivel no tiene techo, igual que el nivel 1.

## Pistas

Solo explican reglas:

- La puerta necesita sus dos placas a la vez, y una placa cuenta mientras algo la pisa.
- Del pozo no se sale saltando, y R conserva la última grabación terminada.
- La reja solo se abre mientras alguien pisa su placa y, cerrada, no deja pasar ni apuntar.
- El Echo repite lo grabado desde donde empezaste a grabar, durante 5 s como máximo.
- Rewind devuelve un mecanismo por el camino que recorrió y lleva a quien esté encima.
- La compuerta se abre con su placa y, si te alcanza cerrada, te empuja fuera.

## Sin verificar

- Aspecto visual: no lo vi en el Editor.
- Puntería con la cámara real: las pruebas apuntan con una cámara colocada donde estaría la de juego. En la sala 2, la plataforma aparcada queda a unos 11 m de la cámara, cerca del límite de 12 m.
- Partida a mano y build.
