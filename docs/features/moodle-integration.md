# Moodle Integration

Este documento descreve a integração com o Moodle usando a terminologia oficial da plataforma.

## Terminologia Moodle

A aplicação usa entidades que mapeiam diretamente para conceitos do Moodle:

| Entidade do Sistema | Conceito Moodle | Descrição |
|---------------------|-----------------|-----------|
| `MoodleCategory` | Category | Categoria principal (ex: Escola/Unidade) |
| `MoodleCourseCategory` | Course Category | Subcategoria de cursos (ex: Programa) |
| `MoodleCohort` | Cohort | Grupo de alunos (ex: Turma) |
| `MoodleCourse` | Course | Curso/Unidade Curricular |
| `MoodleSyncStatus` | - | Status de sincronização com o Moodle |
| `StudentCoursePerformance` | - | Desempenho do aluno em um curso |
| `StudentCourseMap` | Enrollment | Matrícula do aluno no curso |

## Estrutura de Dados

### Backend (C#)

#### Domain Models (`Application.Domain/Model/Moodle/`)

- `MoodleCategory.cs` - Categoria principal
- `MoodleCourseCategory.cs` - Subcategoria de cursos
- `MoodleCohort.cs` - Turma/Cohort
- `MoodleCourse.cs` - Curso
- `MoodleSyncStatus.cs` - Status de sincronização
- `MoodleCohortCourseMap.cs` - Mapeamento turma→curso
- `StudentCourseMap.cs` - Matrícula de aluno
- `StudentCoursePerformance.cs` - Desempenho do aluno

#### Interfaces (`Application.Domain/Interface/Moodle/`)

- `IMoodleRepository.cs` - Repositório de dados Moodle
- `IStudentCoursePerformanceRepository.cs` - Repositório de desempenho

#### Services (`Application.Service/Service/Moodle/`)

- `MoodleService.cs` - Serviço de negócio para Moodle

#### Controllers (`Application.Web/Controllers/`)

- `MoodleController.cs` - API REST para Moodle

### Frontend (TypeScript)

#### Models (`Application.Client/src/app/model/`)

- `moodle-category.ts`
- `moodle-course-category.ts`
- `moodle-cohort.ts`
- `moodle-course.ts`
- `moodle-sync-status.ts`
- `student-course-dto.ts`
- `course-search-query.ts`

#### Services (`Application.Client/src/app/service/moodle/`)

- `moodle.service.ts` - Cliente HTTP para API Moodle

## Endpoints API

### GET `/api/moodle/categories`
Retorna lista de categorias.

### GET `/api/moodle/course-categories?categoryId={id}`
Retorna subcategorias de uma categoria.

### GET `/api/moodle/cohorts?courseCategoryId={id}`
Retorna turmas de uma subcategoria.

### GET `/api/moodle/courses?cohortId={id}`
Retorna cursos de uma turma.

### GET `/api/moodle/courses/search`
Busca paginada de cursos.

**Query Parameters:**
- `search` - Termo de busca
- `cohortId` - Filtro por turma
- `courseCategoryId` - Filtro por subcategoria
- `PageNumber` - Página atual
- `PageSize` - Itens por página

### GET `/api/moodle/courses/students?eadId={id}`
Retorna alunos matriculados em um curso com dados de desempenho.

### GET `/api/moodle/sync-status`
Retorna status da última sincronização.

### POST `/api/moodle/sync/trigger`
Dispara sincronização manual com o Moodle.

## Índices RavenDB

- `MoodleCohorts_ByCategoryAndCourseCategory`
- `MoodleCohortCourseMaps_ByCohort`
- `StudentCourseMaps_ByCourse`
- `MoodleCourseSearchIndex`

## Modo Somente Leitura

A integração opera em **modo somente leitura**. Todas as operações que modificariam dados no Moodle estão desabilitadas. Os dados são sincronizados periodicamente do Moodle para cache local no RavenDB.

## Traduções

As chaves de tradução estão disponíveis sob o namespace `moodle.*`:

- `moodle.header.*` - Cabeçalhos da UI
- `moodle.syncStatus.*` - Status de sincronização
- `moodle.sync.*` - Ações de sincronização
- `moodle.filters.*` - Filtros da UI
- `moodle.labels.*` - Labels de campos
- `moodle.errors.*` - Mensagens de erro
- `moodle.courseDetail.*` - Detalhes do curso

## Migração da Nomenclatura Anterior

A nomenclatura anterior (`Education*`) foi mantida para compatibilidade mas está depreciada:

| Anterior | Novo (Moodle) |
|----------|---------------|
| `School` | `MoodleCategory` |
| `ProgramDocument` | `MoodleCourseCategory` |
| `ClassDocument` | `MoodleCohort` |
| `CourseUnit` | `MoodleCourse` |
| `ClassUcMap` | `MoodleCohortCourseMap` |
| `StudentUcMap` | `StudentCourseMap` |
| `StudentUcPerformance` | `StudentCoursePerformance` |
| `EducationSyncStatus` | `MoodleSyncStatus` |
