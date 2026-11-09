using System.Reflection;
using EasyVan.Models;
using Gerenciamento_de_Van.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace EasyVan.Tests;

public sealed class LinhaNovaLinkTests
{
    [Fact]
    public void BotaoNovaLinhaApontaParaCreateSemCapturaPorRotaDeAtributo()
    {
        var view = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Views", "Home", "Administrador.cshtml"));

        Assert.Contains("asp-action=\"Create\" asp-controller=\"Linhas\"", view);
        Assert.Contains("Nova linha", view);
        Assert.NotNull(typeof(LinhasController).GetMethod(nameof(LinhasController.Create), new[] { typeof(CancellationToken) }));
        Assert.Empty(typeof(LinhasController).GetCustomAttributes<RouteAttribute>());
    }
}
