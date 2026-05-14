// Controlador para gerenciar os usuários (Controller)
using Microsoft.AspNetCore.Mvc;
using EasyVan.Models;

namespace EasyVan.Controllers
{
    public class UsuariosController : Controller
    {
        private static readonly List<Usuarios> usuarios = new()
        {
            new Usuarios
            {
                Id = 1,
                Nome = "João",
                Email = "joao@example.com",
                RoleManager = "Admin",
                PasswordHasher = "123456"
            }
        };

        private static int proximoId = 2;

        public IActionResult Index()
        {
            return View(usuarios);
        }

        public IActionResult Details(int id)
        {
            var usuario = usuarios.FirstOrDefault(u => u.Id == id);
            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        public IActionResult Create()
        {
            if (!ModelState.IsValid)
            {
               return View("~/Views/Home/Index.cshtml");
            }
/*
            if (string.IsNullOrWhiteSpace(ModelState["RoleManager"]?.AttemptedValue))
            {
                ModelState["RoleManager"].AttemptedValue = "Aluno";
            }
*/
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Usuarios usuario)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/Pages/cadastro.cshtml", usuario);
            }

            if (string.IsNullOrWhiteSpace(usuario.RoleManager))
            {
                usuario.RoleManager = "Aluno";
            }

            usuario.Id = proximoId++;
            usuarios.Add(usuario);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            var usuario = usuarios.FirstOrDefault(u => u.Id == id);
            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Usuarios usuario)
        {
            if (!ModelState.IsValid)
            {
                return View(usuario);
            }

            var existente = usuarios.FirstOrDefault(u => u.Id == usuario.Id);
            if (existente == null)
            {
                return NotFound();
            }

            existente.Nome = usuario.Nome;
            existente.Email = usuario.Email;
            existente.RoleManager = usuario.RoleManager;
            existente.PasswordHasher = usuario.PasswordHasher;

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int id)
        {
            var usuario = usuarios.FirstOrDefault(u => u.Id == id);
            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var usuario = usuarios.FirstOrDefault(u => u.Id == id);
            if (usuario != null)
            {
                usuarios.Remove(usuario);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}