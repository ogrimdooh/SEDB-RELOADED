param()

# Caminhos
$sourceDir = "E:\JUNIOR\DESENVOLVIMENTO\SPACEENGINNERS\Apps\SEDB-RELOADED\SEDiscordBridge\bin\Debug"
$destDir = "D:\SEServer\Plugins"
$zipName = "SEDiscordBridge-Custom.zip"
$destZip = Join-Path -Path $destDir -ChildPath $zipName

Write-Host "Fonte: $sourceDir"
Write-Host "Destino: $destZip"

if (-not (Test-Path -Path $sourceDir)) {
    Write-Error "Pasta de origem não encontrada: $sourceDir"
    exit 1
}

# Cria pasta de destino se necessário
if (-not (Test-Path -Path $destDir)) {
    Write-Host "Criando pasta de destino: $destDir"
    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
}

try {
    # Remove arquivo zip existente (para garantir substituição)
    if (Test-Path -Path $destZip) {
        Write-Host "Removendo arquivo existente: $destZip"
        Remove-Item -Path $destZip -Force
    }

    # Compacta o conteúdo da pasta (somente arquivos e subpastas dentro de Debug)
    Write-Host "Compactando conteúdo de $sourceDir para $destZip"
    Compress-Archive -Path (Join-Path $sourceDir "*") -DestinationPath $destZip -Force

    Write-Host "Zip criado com sucesso: $destZip"
    exit 0
}
catch {
    Write-Error "Falha ao criar o zip: $_"
    exit 1
}
