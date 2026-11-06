using System.ComponentModel.DataAnnotations;
using EasyVan.Services;

namespace EasyVan.Models
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class TelefoneBrasilAttribute : ValidationAttribute
    {
        public TelefoneBrasilAttribute()
        {
            ErrorMessage = "Informe um telefone brasileiro válido com DDD (10 ou 11 dígitos).";
        }

        public override bool IsValid(object? value)
        {
            return value is null || ValidacaoBrasil.TelefoneValido(value.ToString());
        }
    }
}
