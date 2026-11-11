using Microsoft.AspNetCore.Mvc;
using ExpenseTracker.Services;

namespace ExpenseTracker.Controllers;

public class ImportController : Controller
{
    private readonly ImportService _importer;

    public ImportController(ImportService importer) => _importer = importer;

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFileCollection files)
    {
        var results = new List<ImportSummary>();

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            using var reader  = new StreamReader(file.OpenReadStream());
            var content       = await reader.ReadToEndAsync();
            var summary       = await _importer.ImportFileAsync(content, file.FileName);
            results.Add(summary);
        }

        return View("Results", results);
    }
}
