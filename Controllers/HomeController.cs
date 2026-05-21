using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Gerenciamento_de_Van.Models;
using EasyVan.Models;

namespace Gerenciamento_de_Van.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Aqui você pode validar o usuário contra banco ou simular autenticação
        return RedirectToAction("Index", "Usuarios");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Detalhes()
    {
        return View();
    }

    public IActionResult Pages(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        return View($"Pages/{id}");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
