$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $root 'bin\Debug\VisoBath.exe'))
$flags = [Reflection.BindingFlags]'Instance,Public,NonPublic'
function CheckSequence($mode, $messages, $expected) {
    $type = $assembly.GetType("VisoBath.Conectores.ProtocoloMaquina$mode")
    $protocol = [Activator]::CreateInstance($type, $true)
    $method = $type.GetMethod('Interpretar', $flags)
    for ($i = 0; $i -lt $messages.Count; $i++) {
        $message = New-Object VisoBath.NotificacionPLC
        $message.accion = $messages[$i][0]
        $message.valor = $messages[$i][1]
        $arguments = New-Object object[] 1
        $arguments[0] = $message.PSObject.BaseObject
        $result = $method.Invoke($protocol, $arguments).ToString()
        if ($result -ne $expected[$i]) { throw "Modo $mode paso ${i}: $result, esperado $($expected[$i])" }
    }
}
CheckSequence 'B' @(@('vuelta',99),@('inicio',0),@('peso',150),@('vuelta',12),@('vuelta',12),@('vuelta',12),@('vuelta',12)) @('Ninguna','Iniciar','Peso','Ninguna','Capturar','Completar','Ninguna')
CheckSequence 'A' @(@('inicio',0),@('vuelta',1),@('vuelta',1),@('vuelta',2),@('vuelta',2),@('peso',150),@('vuelta',3),@('vuelta',4)) @('Iniciar','Ninguna','Ninguna','Capturar','Ninguna','Peso','Completar','Ninguna')
CheckSequence 'A' @(@('inicio',0),@('vuelta',0),@('vuelta',0),@('vuelta',0)) @('Iniciar','Ninguna','Capturar','Completar')
CheckSequence 'A' @(@('inicio',0),@('vuelta',2),@('paro',0),@('vuelta',3),@('inicio',0),@('vuelta',1)) @('Iniciar','Capturar','Cancelar','Ninguna','Iniciar','Ninguna')
CheckSequence 'A' @(@('inicio',0),@('vuelta',2),@('vuelta',4)) @('Iniciar','Capturar','Cancelar')
CheckSequence 'A' @(@('inicio',0),@('vuelta',3)) @('Iniciar','Cancelar')
Write-Output 'OK: protocolo B original; A con contador, pulsos, duplicados, paro, reinicio y vueltas perdidas.'
