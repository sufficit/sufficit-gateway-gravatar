# Sufficit Gateway Gravatar

> **Worktrees (padrão Sufficit):** toda árvore de trabalho deste projeto (humanos ou agentes de IA) deve ser criada dentro da pasta do próprio projeto: `git worktree add .worktrees/<nome>`. A pasta `.worktrees/` é ignorada pelo git (`.gitignore` → `**/.worktrees/`) e nunca deve ser versionada ou criada fora da raiz do repositório.


Cliente HTTP tipado da Sufficit para as APIs públicas do Gravatar (avatar e
perfil), com suporte aos hashes MD5 (legado) e SHA-256 (atual).

`GravatarClient` é a fachada do provedor: consulta de avatar por e-mail ou por
hash, consulta de perfil público (`/{hash}.json`) e uma busca combinada que
informa se um e-mail possui presença no Gravatar.

## Responsabilidades

- normalizar e-mails (trim + lowercase) e calcular os hashes MD5 e SHA-256
  exigidos pelo Gravatar;
- baixar o avatar (`/avatar/{hash}`) com tamanho configurável e detecção de
  ausência (`default=404` → retorno nulo, sem imagem placeholder);
- expor metadados da resposta (`Last-Modified`, `Content-Type`) para cache
  do consumidor;
- desserializar o perfil público (displayName, preferredUsername, fotos,
  contas) com `System.Text.Json`;
- combinar avatar + perfil em uma única consulta (`SearchByEmailAsync`);
- ser consumível via DI (`AddSufficitGatewayGravatar`) ou manualmente com um
  `HttpClient` e options estáticas, para hosts legados.

## Uso via DI

```csharp
services.AddSufficitGatewayGravatar(configuration);
// resolve IGravatarClient (typed client, HttpClientName = "Sufficit.Gateway.Gravatar")
```

Configuração (todas com defaults, seção opcional):

```json
{
  "Sufficit": {
    "Gateway": {
      "Gravatar": {
        "AvatarSize": 230,
        "UserAgent": "SufficitGravatarGateway/1.0"
      }
    }
  }
}
```

## Uso manual (hosts sem IHttpClientFactory)

```csharp
var options = new GravatarOptions(); // defaults
var client = new GravatarClient(new HttpClient(), options);
var avatar = await client.GetAvatarByEmailAsync("user@example.com");
```

## Endpoints representativos expostos pelo sufficit-endpoints

| Rota | Descrição |
|---|---|
| `GET /gateway/gravatar/avatar?email=...` | bytes da imagem (404 quando inexistente) |
| `GET /gateway/gravatar/avatar/{hash}` | bytes da imagem por hash |
| `GET /gateway/gravatar/profile?email=...` | perfil público em JSON |
| `GET /gateway/gravatar/profile/{hash}` | perfil público por hash |
| `GET /gateway/gravatar/search?email=...` | presença combinada (avatar + perfil) |

## Licença

MIT No Attribution — ver [LICENSE](LICENSE).
