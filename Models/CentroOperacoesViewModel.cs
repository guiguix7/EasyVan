using System;
using System.Collections.Generic;
using EasyVan.Models;
using EasyVan.Services;

namespace EasyVan.Models
{
    public class CentroOperacoesViewModel
    {
        // --- Indicadores gerais ---
        public int TotalVans { get; set; }
        public int VansOperacionais { get; set; }
        public int VansManutencao { get; set; }
        public int VansOutros { get; set; }
        public int TotalMotoristas { get; set; }
        public int MotoristasAtivos { get; set; }
        public int TotalAlunos { get; set; }
        public int AlunosAtivos { get; set; }
        public int TotalRotas { get; set; }
        public int RotasAtivas { get; set; }

        // --- Viagens por status ---
        public int ViagensPlanejadas { get; set; }
        public int ViagensConfirmadas { get; set; }
        public int ViagensAgendadas { get; set; }
        public int ViagensEmAndamento { get; set; }
        public int ViagensFinalizadas { get; set; }
        public int ViagensCanceladas { get; set; }
        public int ViagensHoje { get; set; }

        // --- Listas para exibição ---
        public List<Viagem> ProximasViagens { get; set; } = new();
        public List<Van> VansEmManutencao { get; set; } = new();
        public List<LogOperacao> AtividadeRecente { get; set; } = new();

        // --- Alertas operacionais ---
        public List<ValidacaoOperacional.ResultadoValidacao> Alertas { get; set; } = new();
        public int TotalAlertas => Alertas.Count;
        public int AlertasImpeditivos => Alertas.Count(a => a.Gravidade == "Impeditivo");
        public int AlertasAvisos => Alertas.Count(a => a.Gravidade == "Aviso");

        public bool TemAlertas => Alertas.Any();
    }
}