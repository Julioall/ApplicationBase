namespace Application.Domain.Model.Moodle
{
    /// <summary>
    /// Representa a hierarquia de categorias do Moodle conforme a estrutura organizacional.
    /// Path exemplo: /84/87/6375/6406 → Instituição/Escola/Curso/Turma
    /// </summary>
    public class MoodleCategoryHierarchy
    {
        /// <summary>
        /// Depth 1 - Instituição (ex: SENAI, SESI)
        /// </summary>
        public MoodleCategoryLevel? Institution { get; set; }

        /// <summary>
        /// Depth 2 - Escola/Unidade (ex: Escola SENAI Vila Canaã)
        /// </summary>
        public MoodleCategoryLevel? School { get; set; }

        /// <summary>
        /// Depth 3 - Curso/Programa (ex: Operador de Computador)
        /// </summary>
        public MoodleCategoryLevel? Course { get; set; }

        /// <summary>
        /// Depth 4 - Turma/Evento (ex: 1003121 - Operador de Computador - 00003/2025)
        /// </summary>
        public MoodleCategoryLevel? Event { get; set; }

        /// <summary>
        /// Indica se a hierarquia está completa (todos os níveis preenchidos)
        /// </summary>
        public bool IsComplete => Institution != null && School != null && Course != null && Event != null;

        /// <summary>
        /// Cria uma hierarquia a partir de uma lista de categorias ordenadas por depth
        /// </summary>
        public static MoodleCategoryHierarchy FromCategories(IEnumerable<MoodleCategory> categoriesOrderedByDepth)
        {
            var hierarchy = new MoodleCategoryHierarchy();
            var categories = categoriesOrderedByDepth.ToList();

            foreach (var cat in categories)
            {
                var level = new MoodleCategoryLevel
                {
                    MoodleId = cat.MoodleId,
                    Name = cat.Name,
                    Depth = cat.Depth
                };

                switch (cat.Depth)
                {
                    case 1:
                        hierarchy.Institution = level;
                        break;
                    case 2:
                        hierarchy.School = level;
                        break;
                    case 3:
                        hierarchy.Course = level;
                        break;
                    case 4:
                        hierarchy.Event = level;
                        break;
                }
            }

            return hierarchy;
        }

        /// <summary>
        /// Cria uma hierarquia a partir de um path e um dicionário de categorias
        /// </summary>
        public static MoodleCategoryHierarchy FromPath(string? path, IReadOnlyDictionary<int, MoodleCategory> categories)
        {
            var hierarchy = new MoodleCategoryHierarchy();

            if (string.IsNullOrWhiteSpace(path))
                return hierarchy;

            var pathIds = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();

            for (int i = 0; i < pathIds.Count && i < 4; i++)
            {
                var catId = pathIds[i];
                if (!categories.TryGetValue(catId, out var cat))
                    continue;

                var level = new MoodleCategoryLevel
                {
                    MoodleId = cat.MoodleId,
                    Name = cat.Name,
                    Depth = i + 1 // depth é baseado na posição no path
                };

                switch (i)
                {
                    case 0:
                        hierarchy.Institution = level;
                        break;
                    case 1:
                        hierarchy.School = level;
                        break;
                    case 2:
                        hierarchy.Course = level;
                        break;
                    case 3:
                        hierarchy.Event = level;
                        break;
                }
            }

            return hierarchy;
        }
    }

    /// <summary>
    /// Representa um nível da hierarquia de categorias do Moodle
    /// </summary>
    public class MoodleCategoryLevel
    {
        /// <summary>
        /// ID da categoria no Moodle
        /// </summary>
        public int MoodleId { get; set; }

        /// <summary>
        /// Nome da categoria
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Profundidade na hierarquia (1-4)
        /// </summary>
        public int Depth { get; set; }
    }
}
