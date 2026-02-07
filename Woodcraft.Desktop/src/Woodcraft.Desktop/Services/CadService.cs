using System.Text.Json;
using Microsoft.Extensions.Logging;
using Woodcraft.Core.Interfaces;
using Woodcraft.Core.Models;

namespace Woodcraft.Desktop.Services;

/// <summary>
/// CAD service implementation using Python bridge.
/// </summary>
public class CadService : ICadService
{
    private readonly IPythonBridge _bridge;
    private readonly ILogger<CadService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public bool IsConnected => _bridge.IsConnected;

    public CadService(IPythonBridge bridge, ILogger<CadService> logger)
    {
        _bridge = bridge;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _bridge.StartAsync(cancellationToken);
    }

    public async Task DisconnectAsync()
    {
        await _bridge.StopAsync();
    }

    public async Task<CutListResult> GenerateCutListAsync(
        double stockLength,
        double stockWidth,
        string material = "plywood",
        double thickness = 0.75,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating cut list for {Length}x{Width} {Material}", stockLength, stockWidth, material);

        var response = await _bridge.CallToolRawAsync("generate_cutlist", new
        {
            stock_length = stockLength,
            stock_width = stockWidth,
            stock_material = material,
            stock_thickness = thickness
        }, cancellationToken);

        var result = JsonSerializer.Deserialize<CutListResponse>(response, _jsonOptions);
        return result?.Result ?? new CutListResult();
    }

    public async Task<string> GenerateCutListSvgAsync(
        double stockLength,
        double stockWidth,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating cut list SVG");

        var response = await _bridge.CallToolRawAsync("generate_cutlist_svg", new
        {
            stock_length = stockLength,
            stock_width = stockWidth
        }, cancellationToken);

        var result = JsonSerializer.Deserialize<SvgResponse>(response, _jsonOptions);
        return result?.SvgPath ?? string.Empty;
    }

    public async Task<BillOfMaterials> GenerateBOMAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating bill of materials");

        var response = await _bridge.CallToolRawAsync("generate_bom", new
        {
            output_format = "json"
        }, cancellationToken);

        var result = JsonSerializer.Deserialize<BOMResponse>(response, _jsonOptions);
        return result?.Bom ?? new BillOfMaterials();
    }

    public async Task<string> ExportStepAsync(string? partId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting STEP file");

        var response = await _bridge.CallToolRawAsync("export_step", new
        {
            part_id = partId
        }, cancellationToken);

        var result = JsonSerializer.Deserialize<ExportResponse>(response, _jsonOptions);
        return result?.Path ?? string.Empty;
    }

    public async Task<string> ExportStlAsync(string? partId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting STL file");

        var response = await _bridge.CallToolRawAsync("export_stl", new
        {
            part_id = partId
        }, cancellationToken);

        var result = JsonSerializer.Deserialize<ExportResponse>(response, _jsonOptions);
        return result?.Path ?? string.Empty;
    }

    public async Task<string> GenerateDrawingAsync(
        string partId,
        string[] views,
        string format = "svg",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating drawing for part {PartId}", partId);

        var response = await _bridge.CallToolRawAsync("generate_drawing", new
        {
            part_id = partId,
            views = views,
            output_format = format
        }, cancellationToken);

        var result = JsonSerializer.Deserialize<ExportResponse>(response, _jsonOptions);
        return result?.Path ?? string.Empty;
    }

    public async Task<Dictionary<string, object>> CalculateLumberAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating lumber requirements");

        var response = await _bridge.CallToolRawAsync("calculate_lumber", null, cancellationToken);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(response, _jsonOptions)
            ?? new Dictionary<string, object>();
    }

    public async Task<ValidationResult> ValidateDesignAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating design");

        var response = await _bridge.CallToolRawAsync("validate_design", null, cancellationToken);
        var result = JsonSerializer.Deserialize<ValidationResponse>(response, _jsonOptions);

        return new ValidationResult
        {
            IsValid = result?.Valid ?? false,
            Issues = result?.Issues?.Select(i => new ValidationIssue
            {
                Severity = i.Severity,
                Message = i.Message,
                PartId = i.PartId
            }).ToList() ?? []
        };
    }

    // Response types
    private class CutListResponse
    {
        public string Status { get; set; } = string.Empty;
        public CutListResult? Result { get; set; }
    }

    private class SvgResponse
    {
        public string Status { get; set; } = string.Empty;
        public string SvgPath { get; set; } = string.Empty;
    }

    private class BOMResponse
    {
        public string Status { get; set; } = string.Empty;
        public BillOfMaterials? Bom { get; set; }
    }

    private class ExportResponse
    {
        public string Status { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
    }

    private class ValidationResponse
    {
        public bool Valid { get; set; }
        public List<ValidationIssueDto>? Issues { get; set; }
    }

    private class ValidationIssueDto
    {
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? PartId { get; set; }
    }
}
