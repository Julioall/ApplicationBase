namespace Application.Shared.Background;

/// <summary>
/// Interface para o job de importação de educação no Hangfire.
/// </summary>
public interface IEducationImportJob
{
    /// <summary>
    /// Processa um arquivo de importação de educação.
    /// </summary>
    Task ProcessImportAsync(string importId, CancellationToken cancellationToken = default);
}
