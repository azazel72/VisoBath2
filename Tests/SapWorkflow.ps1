$ErrorActionPreference = 'Stop'
$projectPath = Split-Path $PSScriptRoot -Parent
$binPath = Join-Path $projectPath 'bin\Debug'
Add-Type -TypeDefinition @'
public static class SapTestAssemblyResolver {
    public static string DirectoryPath;
    public static void Install() {
        System.AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
            string path = System.IO.Path.Combine(DirectoryPath, new System.Reflection.AssemblyName(args.Name).Name + ".dll");
            return System.IO.File.Exists(path) ? System.Reflection.Assembly.LoadFrom(path) : null;
        };
    }
}
'@
[SapTestAssemblyResolver]::DirectoryPath = $binPath
[SapTestAssemblyResolver]::Install()
foreach ($dependency in @('System.Buffers.dll','System.Memory.dll','System.Runtime.CompilerServices.Unsafe.dll','System.Threading.Tasks.Extensions.dll','Microsoft.Bcl.AsyncInterfaces.dll','System.Text.Encodings.Web.dll','System.Text.Json.dll')) {
    [void][Reflection.Assembly]::LoadFrom((Join-Path $binPath $dependency))
}
[void][Reflection.Assembly]::LoadFrom((Join-Path $binPath 'System.Data.SQLite.dll'))
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $binPath 'VisoBath.exe'))
$sqlite = $assembly.GetType('VisoBath.ConectorSQLite')
$sap = $assembly.GetType('VisoBath.Conectores.ConectorSAP')
$flags = [Reflection.BindingFlags]'Static,Public,NonPublic'
function Assert($condition, $message) { if (!$condition) { throw $message } }
function CallDb($name, [object[]]$arguments) {
    for ($i = 0; $i -lt $arguments.Length; $i++) { $arguments[$i] = $arguments[$i].PSObject.BaseObject }
    return ,($sqlite.GetMethod($name, $flags).Invoke($null, $arguments))
}
function NewOrder($code, $count) {
    $order = New-Object VisoBath.Albaran
    $order.numeroAlbaran = $code
    $order.FijarBultos($count)
    $order.CrearPaletsPendientes()
    return $order
}
function SaveMeasurement($order, $number) {
    $pallet = New-Object VisoBath.Palet
    $pallet.numeroAlbaran = $order.numeroAlbaran
    $pallet.numero = $number
    $pallet.peso = 100 + $number
    $pallet.volumen = 200 + $number
    $pallet.alto = 1100
    $pallet.ancho = 1200
    $pallet.largo = 1300
    Assert (CallDb 'GuardarPaletMedido' @($order, $pallet)) 'No se guardo la medicion'
    $order.AgregarPalet($pallet)
}
$testPath = Join-Path ([IO.Path]::GetTempPath()) ('VisoBath-SAP-' + [Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $testPath)
$previousLocation = Get-Location
$previousDirectory = [Environment]::CurrentDirectory
try {
    Set-Location $testPath
    [Environment]::CurrentDirectory = $testPath
    $connection = New-Object System.Data.SQLite.SQLiteConnection 'Data Source=visobath.sqlite;Version=3;'
    $connection.Open()
    $command = $connection.CreateCommand()
    # Esquema creado por el propio proyecto, sin ejecutar la ventana ni contactar con SAP.
    foreach ($field in @('crearTablaAlbaranes','crearTablaPalets','crearTablaConfiguracion','crearTablaRegistros')) {
        $command.CommandText = $sqlite.GetField($field, $flags).GetRawConstantValue()
        [void]$command.ExecuteNonQuery()
    }
    $connection.Close()
    $a = NewOrder 'TEST-A' 2
    $b = NewOrder 'TEST-B' 3
    Assert (CallDb 'GuardarPedidoSAP' @($a)) 'No se guardo pedido A'
    Assert (CallDb 'GuardarPedidoSAP' @($b)) 'No se guardo pedido B'
    Assert ($a.bultoActual -eq 0 -and $a.estado -eq 0 -and $a.ListadoPalets().Count -eq 2) 'Pendientes incorrectos'
    SaveMeasurement $a 1
    SaveMeasurement $b 1
    Assert ($a.estado -eq 0 -and $b.estado -eq 0) 'Pedido finalizado prematuramente'
    $loaded = New-Object VisoBath.Albaranes
    $loaded.Agregar((CallDb 'CargarAlbaranes' @()))
    $loaded.RepartirPalets((CallDb 'CargarPalets' @()))
    $a = $loaded.Buscar('TEST-A')
    $b = $loaded.Buscar('TEST-B')
    Assert ($a.bultoActual -eq 1 -and $b.bultoActual -eq 1) 'Se perdio progreso al recargar'
    Assert ($a.ObtenerPalet(1).alto -eq 1100 -and $b.ObtenerPalet(3).peso -eq 0) 'Dimensiones/pendientes no persistidos'
    SaveMeasurement $a 2
    Assert ($a.estado -eq 1 -and $b.estado -eq 0) 'Finalizacion por pedido incorrecta'
    SaveMeasurement $b 2
    SaveMeasurement $b 3
    Assert ($b.estado -eq 1 -and $b.bultoActual -eq 3) 'No se completo el ultimo palet'
    $notification = New-Object VisoBath.Notificacion -ArgumentList 'test-token', $b
    $payload = $sap.GetMethod('SerializarNotificacion', $flags).Invoke($null, @($notification.PSObject.BaseObject)) | ConvertFrom-Json
    Assert ($payload.num_palets -eq 3 -and $payload.palets.Count -eq 3) 'No se enviarian todos los palets'
    Assert (($payload.palets[0].PSObject.Properties.Name -join ',') -eq 'numero,hora,peso,volumen') 'Dimensiones incluidas en JSON de SAP'
    $missing = NewOrder 'NO-EXISTE' 1
    $pallet = New-Object VisoBath.Palet
    $pallet.numero = 1
    $pallet.numeroAlbaran = $missing.numeroAlbaran
    $pallet.peso = 100
    $pallet.volumen = 200
    Assert (!(CallDb 'GuardarPaletMedido' @($missing, $pallet))) 'No se detecto pedido inexistente'
    $connection.Open()
    $command.CommandText = "SELECT COUNT(*) FROM palets WHERE numeroAlbaran='NO-EXISTE'"
    Assert ($command.ExecuteScalar() -eq 0) 'La transaccion no hizo rollback'
    $connection.Close()
    $form = New-Object VisoBath.Gestor
    try {
        Assert ($form.ClientSize.Width -eq 1203 -and $form.ClientSize.Height -eq 729) 'Cambio el tamano de ventana'
        Assert ($form.Controls.Find('tVersion', $true)[0].Text -eq 'B v3.003') 'Version incorrecta'
        $logo = $form.Controls.Find('logoSAP', $true)[0]
        Assert ($null -ne $logo.Image -and $logo.Width -eq 64 -and $logo.Height -eq 28) 'Logo no disponible'
        Assert (!( $form.Controls.Find('tipoConectorCombo', $true)[0].Visible )) 'Selector visible'
    } finally { $form.Dispose() }
    Write-Output 'OK: pendientes, pedidos intercalados, recarga, dimensiones locales, ultimo palet, JSON SAP y rollback.'
} finally {
    Set-Location $previousLocation
    [Environment]::CurrentDirectory = $previousDirectory
    if ($connection) { $connection.Dispose() }
    [System.Data.SQLite.SQLiteConnection]::ClearAllPools()
}
