using IPScan.Models;

namespace IPScan.Services.Export;

public interface IResultExporter
{
    string FileExtension { get; }
    string FileFilter { get; }
    Task ExportAsync(string filePath, IReadOnlyList<ScanResult> results, CancellationToken cancellationToken = default);
}

