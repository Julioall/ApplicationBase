param(
    [string]$Password = 'changeme-ravendb-cert',
    [string[]]$DnsNames = @('localhost', 'raven-db'),
    [int]$YearsValid = 1
)

$ErrorActionPreference = 'Stop'

$certPath = Join-Path $PSScriptRoot 'ravendb.pfx'
$cerPath = Join-Path $PSScriptRoot 'ravendb.cer'
$dns = $DnsNames | Where-Object { $_ } | ForEach-Object { $_.Trim() } | Where-Object { $_ }

if (-not $dns) {
    $dns = @('localhost', 'raven-db')
}

Write-Host "Gerando certificado RavenDB para SANs: $($dns -join ', ')"

if (Test-Path $certPath) {
    Remove-Item $certPath -Force
}

if (Test-Path $cerPath) {
    Remove-Item $cerPath -Force
}

$securePassword = ConvertTo-SecureString -String $Password -AsPlainText -Force

$certificate = New-SelfSignedCertificate `
    -Subject 'CN=localhost' `
    -DnsName $dns `
    -NotAfter (Get-Date).AddYears($YearsValid) `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -CertStoreLocation 'Cert:\CurrentUser\My' `
    -KeyUsage DigitalSignature, KeyEncipherment `
    -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.1,1.3.6.1.5.5.7.3.2') `
    -FriendlyName 'RavenDB Dev'

Export-PfxCertificate -Cert $certificate -FilePath $certPath -Password $securePassword | Out-Null
Export-Certificate -Cert $certificate -FilePath $cerPath | Out-Null

Write-Host "PFX salvo em $certPath (senha: $Password)"
Write-Host "CER salvo em $cerPath"
Write-Host "Instale o CER no store 'Root' do usuario atual se for acessar via host (certutil -user -addstore Root certs\\ravendb.cer)."
