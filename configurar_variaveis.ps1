function Set-ExecutionPolicyIfNeeded {
    $currentPolicy = Get-ExecutionPolicy
    if ($currentPolicy -eq 'Restricted') {
        Write-Host "A política de execução atual é 'Restricted'."
        Write-Host "Alterando a política de execução para 'RemoteSigned'..."
        
        try {
            Set-ExecutionPolicy RemoteSigned -Scope CurrentUser -Force
            Write-Host "Política de execução alterada para 'RemoteSigned'."
        } catch {
            Write-Host "Erro ao alterar a política de execução: $_"
            exit
        }
    } else {
        Write-Host "A política de execução atual é '$currentPolicy'."
    }
}

function Check-Administrator {
    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object System.Security.Principal.WindowsPrincipal($identity)
    
    if (-not $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Write-Host "Este script deve ser executado como administrador."
        Write-Host "Por favor, clique com o botão direito no PowerShell e selecione 'Executar como administrador'."
        exit
    }
}

function Set-EnvironmentVariable {
    # Entrada do usuário para variáveis que podem mudar com frequência
    $ravenDbUrls = Read-Host -Prompt "Digite a URL do RavenDB (ex: http://localhost:8080)"
    $databaseName = Read-Host -Prompt "Digite o nome do banco de dados (ex: AppBanco)"
    $certificateSubject = Read-Host -Prompt "Digite o assunto do certificado (ex: CN=YourCertificateSubject)"
    $jwtIssuer = Read-Host -Prompt "Digite o JWT Issuer (ex: myapp@mydomain.com)"
    $jwtAudience = Read-Host -Prompt "Digite o JWT Audience (ex: myapp_users)"

    # Gerar a chave de assinatura JWT
    Add-Type -AssemblyName System.Security
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $bytes = New-Object Byte[] 32  # 256 bits = 32 bytes
    $rng.GetBytes($bytes)
    $jwtSigningKey = [Convert]::ToBase64String($bytes)

    # Definindo as variáveis de ambiente
    [System.Environment]::SetEnvironmentVariable("RAVENDBSETTINGS_URLS", $ravenDbUrls, [System.EnvironmentVariableTarget]::User)
    [System.Environment]::SetEnvironmentVariable("RAVENDBSETTINGS_DATABASE_NAME", $databaseName, [System.EnvironmentVariableTarget]::User)
    [System.Environment]::SetEnvironmentVariable("RAVENDBSETTINGS_CERTIFICATE_SUBJECT", $certificateSubject, [System.EnvironmentVariableTarget]::User)

    [System.Environment]::SetEnvironmentVariable("JWT_ISSUER", $jwtIssuer, [System.EnvironmentVariableTarget]::User)
    [System.Environment]::SetEnvironmentVariable("JWT_AUDIENCE", $jwtAudience, [System.EnvironmentVariableTarget]::User)
    [System.Environment]::SetEnvironmentVariable("JWT_SIGNING_KEY", $jwtSigningKey, [System.EnvironmentVariableTarget]::User)

    Write-Host "Variáveis de ambiente configuradas com sucesso!"
    Write-Host "JWT_SIGNING_KEY: $jwtSigningKey"
}

Check-Administrator
Set-ExecutionPolicyIfNeeded
Set-EnvironmentVariable

# Aguarde o usuário pressionar qualquer tecla
Read-Host -Prompt "Pressione Enter para sair"
