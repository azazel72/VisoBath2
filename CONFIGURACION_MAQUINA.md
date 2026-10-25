La aplicación lee `configuracion.json` junto a `VisoBath.exe` al arrancar.
Añadir `"configuracion_maquina": "A"` para la máquina antigua o `"B"`
para la actual y reiniciar. Si falta la propiedad, se utiliza B. La selección
se conserva al guardar la configuración de impresora.

| Parámetro | A | B |
| --- | --- | --- |
| IP LiDAR | 192.78.70.118 | 192.78.70.173 |
| start_angle (1/10000 grados) | -765000 | 135000 |
| Corrección por montaje | 0 grados | -90 grados |
| Distancia al eje | 1500 mm | 1520 mm |

Ambos perfiles conservan la altura de instalación de 2470 mm, 720 puntos por
giro y un sector de 153 puntos. El sector solicitado difiere 90 grados; la
corrección de montaje deja ambos en el sistema de coordenadas empleado por
el cálculo antiguo. B conserva el cálculo existente.

El transporte PLC sigue siendo TCP en el puerto 5000, con un JSON por conexión
y cierre de conexión como fin de mensaje: `{"accion":"vuelta","valor":2}`.
No se cambia el programa del PLC.

La interpretación A se deduce del código disponible y de la indicación de que
el PLC cuenta vueltas; no se ha verificado con hardware:

- `inicio` prepara la medición y reinicia la secuencia.
- `peso` actualiza el peso, como hasta ahora.
- `vuelta` con valor positivo representa el contador acumulado desde inicio:
  1 espera, 2 comienza a registrar, 3 termina y calcula. Los duplicados y
  contadores anteriores se ignoran. Si se pierde el límite de captura, se cancela.
- `vuelta` con valor cero conserva el protocolo original de una notificación
  por vuelta: segunda notificación inicia la captura y tercera la termina.
- `paro` cancela sin calcular y exige un nuevo `inicio`.

Se asume que el PLC ya traduce las entradas físicas de los sensores a estas
acciones. No se han inventado nombres de mensajes para las tres entradas:
no aparecen en ninguna de las versiones del código consultadas. El nombre
`paro` y el uso de `valor` como contador son suposiciones explícitas.

B reproduce la secuencia original, ignorando el valor recibido en `vuelta`.
ERP, etiquetas y cálculo de volumen mantienen el flujo existente.

Validación sin hardware: compilar y ejecutar `Tests/ProtocoloMaquina.ps1`
y `Tests/PerfilesMaquina.ps1` con Windows PowerShell.
