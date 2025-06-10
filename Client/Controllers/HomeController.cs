using Client.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Client.Controllers;

public class HomeController : Controller
{
    private readonly IConfiguration _config;
    private readonly ILogger<HomeController> _logger;

    // Gộp lại thành 1 constructor duy nhất
    public HomeController(ILogger<HomeController> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult About()
    {
        // Đọc key từ appsettings.secrets.json
        ViewBag.GoogleMapsApiKey = _config["GoogleMaps:ApiKey"];
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}