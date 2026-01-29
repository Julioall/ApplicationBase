# Hierarquia de Categorias do Moodle

## Visão Geral

Este documento descreve a estrutura hierárquica de categorias do Moodle implementada no sistema, que permite organizar as Unidades Curriculares (CourseUnits) de acordo com a estrutura organizacional do SENAI/SESI.

## Estrutura Hierárquica

O Moodle organiza as categorias em uma estrutura de árvore com até 4 níveis de profundidade (depth):

```
Path exemplo: /84/87/6375/6406

Depth 1: Instituição (ID 84 = SENAI)
   └── Depth 2: Escola (ID 87 = Escola SENAI Vila Canaã)
         └── Depth 3: Curso (ID 6375 = Operador de Computador)
               └── Depth 4: Turma/Evento (ID 6406 = 1003121 - Operador de Computador - 00003/2025)
                     └── CourseUnits (Unidades Curriculares - cursos do Moodle)
```

### Níveis da Hierarquia

| Depth | Nome | Descrição | Exemplo |
|-------|------|-----------|---------|
| 1 | **Instituição** | Entidade principal (SENAI, SESI, etc.) | SENAI |
| 2 | **Escola** | Unidade física da instituição | Escola SENAI Vila Canaã |
| 3 | **Curso** | Programa de formação oferecido | Operador de Computador |
| 4 | **Turma/Evento** | Instância específica do curso | 1003121 - Operador de Computador - 00003/2025 |

## Modelo de Dados

### CourseUnit

O documento `CourseUnit` armazena os dados de cada Unidade Curricular com os seguintes campos de hierarquia:

```csharp
// Campos derivados da hierarquia de categorias do Moodle
public string? InstitutionName { get; set; }     // Nome da instituição (depth 1)
public int? InstitutionMoodleId { get; set; }    // ID no Moodle

public string? SchoolName { get; set; }          // Nome da escola (depth 2)
public int? SchoolMoodleId { get; set; }         // ID no Moodle

public string? CourseName { get; set; }          // Nome do curso (depth 3)
public int? CourseMoodleId { get; set; }         // ID no Moodle

public string? EventName { get; set; }           // Nome da turma/evento (depth 4)
public int? EventMoodleId { get; set; }          // ID no Moodle
```

### MoodleCategory

As categorias são armazenadas localmente no RavenDB para cache:

```csharp
public class MoodleCategory
{
    public string? Id { get; set; }              // moodle-categories/{MoodleId}
    public int MoodleId { get; set; }            // ID original do Moodle
    public string Name { get; set; }             // Nome da categoria
    public int ParentId { get; set; }            // ID da categoria pai
    public int Depth { get; set; }               // Nível na hierarquia (1-4)
    public string? Path { get; set; }            // Caminho completo (ex: /84/87/6375/6406)
    public long LastSyncedAt { get; set; }       // Timestamp da última sincronização
}
```

## Fluxo de Sincronização

O job `MoodleSyncHangfireJob` extrai a hierarquia seguindo este fluxo:

1. **Busca cursos do usuário** via `core_enrol_get_users_courses`
2. **Identifica categorias diretas** dos cursos (CategoryId)
3. **Busca categorias locais** do cache RavenDB
4. **Busca categorias faltantes** do Moodle via `core_course_get_categories`
5. **Extrai IDs da hierarquia** parseando o campo `Path` de cada categoria
6. **Busca categorias da hierarquia** que ainda não estão no cache
7. **Mapeia a hierarquia completa** para cada CourseUnit usando a posição no Path

### Exemplo de Parsing do Path

```csharp
// Path: /84/87/6375/6406
var pathIds = categoryInfo.Path.Split('/', StringSplitOptions.RemoveEmptyEntries)
    .Select(s => int.TryParse(s, out var id) ? id : 0)
    .Where(id => id > 0)
    .ToList();

// pathIds = [84, 87, 6375, 6406]
// Índice 0 → Instituição (depth 1)
// Índice 1 → Escola (depth 2)
// Índice 2 → Curso (depth 3)
// Índice 3 → Turma/Evento (depth 4)
```

## Cache Local

As categorias são armazenadas localmente no RavenDB para:

- **Reduzir chamadas ao Moodle** - Só busca categorias que ainda não existem
- **Melhorar performance** - Lookup local é mais rápido
- **Manter histórico** - Categorias permanecem mesmo se removidas do Moodle

### Estratégia de Batch

Para evitar o limite de 500 requisições por sessão do RavenDB:

1. Todas as categorias são carregadas em uma única operação `LoadAsync(ids)`
2. Atualizações são feitas em memória (RavenDB tracked automaticamente)
3. Novas categorias são inseridas via `StoreAsync` em batch

## Campos Legados (Deprecated)

Os seguintes campos foram marcados como `[Obsolete]` mas mantidos para compatibilidade:

| Campo Antigo | Substituído Por |
|--------------|-----------------|
| `SchoolNameDerived` | `EventName` ou `SchoolName` |
| `ProgramNameDerived` | `CourseName` |

## API Endpoints

Os seguintes endpoints estão disponíveis para navegar pela hierarquia de categorias:

### Endpoints de Hierarquia

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/api/moodle/hierarchy/institutions` | Lista todas as instituições (depth 1) |
| GET | `/api/moodle/hierarchy/institutions/{id}/schools` | Lista escolas de uma instituição (depth 2) |
| GET | `/api/moodle/hierarchy/schools/{id}/courses` | Lista cursos de uma escola (depth 3) |
| GET | `/api/moodle/hierarchy/courses/{id}/events` | Lista eventos/turmas de um curso (depth 4) |
| GET | `/api/moodle/hierarchy?path=/84/87/6375/6406` | Retorna hierarquia completa a partir do path |

### Exemplo de Resposta - Hierarquia Completa

```json
{
  "institution": {
    "moodleId": 84,
    "name": "SENAI",
    "depth": 1
  },
  "school": {
    "moodleId": 87,
    "name": "Escola SENAI Vila Canaã",
    "depth": 2
  },
  "course": {
    "moodleId": 6375,
    "name": "Operador de Computador",
    "depth": 3
  },
  "event": {
    "moodleId": 6406,
    "name": "1003121 - Operador de Computador - 00003/2025",
    "depth": 4
  },
  "isComplete": true
}
```

## Modelos Auxiliares

### MoodleCategoryHierarchy

Modelo que representa a hierarquia completa de categorias:

```csharp
public class MoodleCategoryHierarchy
{
    public MoodleCategoryLevel? Institution { get; set; }  // Depth 1
    public MoodleCategoryLevel? School { get; set; }       // Depth 2
    public MoodleCategoryLevel? Course { get; set; }       // Depth 3
    public MoodleCategoryLevel? Event { get; set; }        // Depth 4
    public bool IsComplete { get; }                         // Todos os níveis preenchidos
}

public class MoodleCategoryLevel
{
    public int MoodleId { get; set; }
    public string Name { get; set; }
    public int Depth { get; set; }
}
```

## Índice RavenDB

O índice `MoodleCategories_ByDepthAndParent` otimiza as consultas hierárquicas:

```csharp
public class MoodleCategories_ByDepthAndParent : AbstractIndexCreationTask<MoodleCategory>
{
    public MoodleCategories_ByDepthAndParent()
    {
        Map = categories => from c in categories
                            select new
                            {
                                c.MoodleId,
                                c.Name,
                                c.Depth,
                                c.ParentId,
                                c.Path,
                                c.LastSyncedAt
                            };
    }
}
```

---

**Última atualização:** 28/01/2026
