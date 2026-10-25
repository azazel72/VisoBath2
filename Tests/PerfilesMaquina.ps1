$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Add-Type -Path @((Join-Path $root 'MME\PerfilMaquina.cs'), (Join-Path $root 'MME\Medidas2000.cs'))
$flags = [Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$perfilType = ([AppDomain]::CurrentDomain.GetAssemblies() | ForEach-Object { $_.GetType('VisoBath.MME.PerfilMaquina') } | Where-Object { $_ })
$medidasType = $perfilType.Assembly.GetType('VisoBath.MME.Medidas2000')
function Assert($condition, $message) { if (!$condition) { throw $message } }
foreach ($modo in @('A','B')) {
    $perfil = $perfilType.GetMethod('Crear', $flags).Invoke($null, @($modo))
    $angulo = $perfilType.GetProperty('AnguloCalculo').GetValue($perfil, $null)
    Assert ([Math]::Abs($angulo - (-76.5 * [Math]::PI / 180)) -lt 1e-12) "Orientacion incorrecta: $modo"
    $inicio = $perfilType.GetProperty('AnguloInicial').GetValue($perfil, $null)
    Assert ($inicio -eq $(if ($modo -eq 'A') { -765000 } else { 135000 })) "Sector incorrecto: $modo"
    $medidas = [Activator]::CreateInstance($medidasType, $flags, $null, @([int]153, $perfil), $null)
    # Cuatro secciones idénticas con un único retorno válido de 2300 mm.
    for ($s = 0; $s -lt 4; $s++) {
        $trama = New-Object byte[] 788
        [BitConverter]::GetBytes([int]2300).CopyTo($trama, 76)
        $arguments = New-Object object[] 1
        $arguments[0] = $trama.PSObject.BaseObject
        [void]$medidasType.GetMethod('AgregarDatos').Invoke($medidas, $arguments)
    }
    $result = $medidasType.GetMethod('Calcular').Invoke($medidas, @())
    $eje = $(if ($modo -eq 'A') { 1500 } else { 1520 })
    $radio = $eje - [Math]::Cos($angulo) * 2300
    Assert ([Math]::Abs($result[0] - (2470 + [Math]::Sin($angulo) * 2300)) -lt 0.01) "Altura incorrecta: $modo"
    Assert ([Math]::Abs(($result[3] - $result[2]) - 2 * $radio) -lt 0.01) "Anchura incorrecta: $modo"
}
Write-Output 'OK: sectores A/B, compensacion de 90 grados y geometria sintetica.'


