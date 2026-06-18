using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using EasyVan.Models;

namespace Gerenciamento_de_Van.Controllers
{
    public class VansController : Controller
    {
        private static readonly List<Van> _vans = new List<Van>
        {
            new Van { Id = 1, Plate = "ABC-1234", Driver = "João Silva", Capacity = 12, Route = "Centro - Universidade", Schedule = "07:00 / 17:30", Status = "Operacional", Description = "Van para transporte de alunos da universidade." },
            new Van { Id = 2, Plate = "DEF-5678", Driver = "Maria Oliveira", Capacity = 10, Route = "Zona Leste - Centro", Schedule = "06:30 / 18:00", Status = "Manutenção", Description = "Van em manutenção preventiva. Retorno previsto em 3 dias." },
            new Van { Id = 3, Plate = "TUF-6769", Driver = "Aurudo da Silva", Capacity = 15, Route = "Centro - Distrito Industrial", Schedule = "08:00 / 19:00", Status = "Operacional", Description = "Van de maior capacidade para trajetos longos." }
        };

        public IActionResult Index()
        {
            return View(_vans);
        }

        public IActionResult Details(int id)
        {
            var van = _vans.FirstOrDefault(v => v.Id == id);
            if (van == null) return NotFound();
            return View(van);
        }
    }
}
