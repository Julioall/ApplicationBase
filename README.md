# Application Base - Aplicação Web com Angular e .NET
![Tela de login](https://github.com/user-attachments/assets/195dccf8-56eb-4e14-a244-11930e5a9dc2)


Este repositório contém o código-fonte do **Application Base**, um projeto que serve como uma base para futuros desenvolvimentos. O projeto segue as melhores práticas de desenvolvimento de software, incluindo:

- **Arquitetura Onion:** Estrutura em camadas que promove a separação de preocupações, facilitando a manutenção e evolução do código.
- **Injeção de Dependência:** Facilita a gestão de dependências entre classes, resultando em um código mais modular e testável.
- **Padrão de Repositório:** Abstração da lógica de acesso a dados, tornando o código mais limpo e desacoplado.

## Tecnologias Utilizadas

- **Frontend:** Angular 18
- **Backend:** .NET 8
- **Banco de Dados:** RavenDB

## Configuração do Ambiente

Para utilizar o projeto, é necessário configurar as seguintes variáveis de ambiente. Para facilitar essa configuração, foi desenvolvido um executável que requer que a política de execução de scripts esteja definida como **RemoteSigned** ou similar. Caso prefira, você pode definir essas variáveis manualmente:

## Configuração do Ambiente

Para utilizar o projeto, é necessário configurar as seguintes variáveis de ambiente. Para facilitar essa configuração, foi desenvolvido um executável que requer que a política de execução de scripts esteja definida como **RemoteSigned** ou similar. Caso prefira, você pode definir essas variáveis manualmente:

### Variáveis de Ambiente

1. **JWT_AUDIENCE**
   - **Descrição:** Define a audiência para o qual o token JWT (JSON Web Token) é emitido.
   - **Exemplo:** 
     ```bash
     JWT_AUDIENCE="meuapp.com"
     ```

2. **JWT_ISSUER**
   - **Descrição:** Especifica quem emitiu o token JWT. Isso é útil para validação.
   - **Exemplo:**
     ```bash
     JWT_ISSUER="MeuAppIssuer"
     ```

3. **JWT_SIGNING_KEY**
   - **Descrição:** Chave secreta usada para assinar o token JWT, garantindo sua integridade e autenticidade.
   - **Exemplo:**
     ```bash
     JWT_SIGNING_KEY="minhachavesecreta"
     ```

4. **RAVENDBSETTINGS_DATABASE_NAME**
   - **Descrição:** Nome do banco de dados no RavenDB onde os dados serão armazenados.
   - **Exemplo:**
     ```bash
     RAVENDBSETTINGS_DATABASE_NAME="MeuBancoDeDados"
     ```

5. **RAVENDBSETTINGS_URLS**
   - **Descrição:** URLs do servidor RavenDB para conectar ao banco de dados.
   - **Exemplo:**
     ```bash
     RAVENDBSETTINGS_URLS="http://localhost:8080"
     ```

6. **RAVENDBSETTINGS_CERTIFICATE_SUBJECT**
   - **Descrição:** Assunto do certificado usado para autenticação no RavenDB, se aplicável. Você pode usar usar sem certificado, mas ainda é preciso definir a variavel.
   - **Exemplo:**
     ```bash
     RAVENDBSETTINGS_CERTIFICATE_SUBJECT="CN=MeuCertificado"
     ```

## Estrutura do Projeto

O projeto é dividido em várias camadas, cada uma com sua responsabilidade específica:

1. **Domain:** Contém as entidades e interfaces que definem o núcleo do negócio.
2. **Service:** Inclui os casos de uso e serviços da aplicação.
3. **Infrastructure:** Implementações concretas de repositórios e serviços de infraestrutura, além do acesso a dados.
4. **Client:** Interface de usuário construída com Angular 18.
5. **API:** Camada de serviços backend construída com .NET 8.
6. **Test:** Camada de testes unitários.

## Camada de Testes

A camada de testes inclui:

- **RavenDB Embedded:** Utilizado para simular o banco de dados em testes, permitindo a execução de testes sem a necessidade de um servidor externo.

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues ou pull requests.

## Licença

Este projeto está licenciado sob a [Licença MIT](LICENSE).

---

Agradecemos por considerar o **Application Base** como a base para seus projetos!
