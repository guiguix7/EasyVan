"""Gera docs/apresentacao-final.pptx com a identidade visual do EasyVan.

Uso: python docs/apresentacao/gerar.py
Requer: python-pptx (pip install python-pptx)
"""
from pathlib import Path

from pptx import Presentation
from pptx.dml.color import RGBColor
from pptx.enum.text import MSO_ANCHOR, PP_ALIGN
from pptx.util import Emu, Inches, Pt

RAIZ = Path(__file__).resolve().parents[2]
IMG = RAIZ / "docs" / "img"
SAIDA = RAIZ / "docs" / "apresentacao-final.pptx"

PETROLEO = RGBColor(0x0F, 0x4C, 0x5C)
PETROLEO_ESCURO = RGBColor(0x0B, 0x37, 0x44)
PETROLEO_CLARO = RGBColor(0xE8, 0xF1, 0xF3)
AMBAR = RGBColor(0xF5, 0xA5, 0x24)
AMBAR_CLARO = RGBColor(0xFF, 0xF4, 0xDE)
FUNDO = RGBColor(0xF4, 0xF6, 0xF6)
SUPERFICIE = RGBColor(0xFF, 0xFF, 0xFF)
TEXTO = RGBColor(0x1F, 0x29, 0x33)
SECUNDARIO = RGBColor(0x52, 0x61, 0x6B)
TENUE = RGBColor(0x68, 0x77, 0x81)
LINHA = RGBColor(0xDC, 0xE3, 0xE7)
VERDE = RGBColor(0x1B, 0x87, 0x3F)
VERDE_CLARO = RGBColor(0xE6, 0xF4, 0xEA)

FONTE = "Segoe UI"
LARG = Inches(13.333)
ALT = Inches(7.5)

prs = Presentation()
prs.slide_width = LARG
prs.slide_height = ALT
BRANCO = prs.slide_layouts[6]


def slide():
    return prs.slides.add_slide(BRANCO)


def retangulo(sl, x, y, w, h, cor, linha=None, raio=None):
    from pptx.enum.shapes import MSO_SHAPE

    forma = sl.shapes.add_shape(
        MSO_SHAPE.ROUNDED_RECTANGLE if raio else MSO_SHAPE.RECTANGLE,
        Inches(x), Inches(y), Inches(w), Inches(h))
    if raio:
        forma.adjustments[0] = raio
    forma.fill.solid()
    forma.fill.fore_color.rgb = cor
    if linha:
        forma.line.color.rgb = linha
        forma.line.width = Pt(1)
    else:
        forma.line.fill.background()
    forma.shadow.inherit = False
    from pptx.oxml.ns import qn

    for estilo in forma._element.findall(qn("p:style")):
        forma._element.remove(estilo)
    return forma


def texto(sl, x, y, w, h, conteudo, tamanho=16, cor=TEXTO, negrito=False,
          alinhamento=PP_ALIGN.LEFT, espaco=1.15, italico=False, caps=False):
    caixa = sl.shapes.add_textbox(Inches(x), Inches(y), Inches(w), Inches(h))
    quadro = caixa.text_frame
    quadro.word_wrap = True
    quadro.margin_left = 0
    quadro.margin_right = 0
    quadro.margin_top = 0
    quadro.margin_bottom = 0
    linhas = conteudo if isinstance(conteudo, list) else [conteudo]
    for i, linha in enumerate(linhas):
        p = quadro.paragraphs[0] if i == 0 else quadro.add_paragraph()
        p.alignment = alinhamento
        p.line_spacing = espaco
        run = p.add_run()
        run.text = linha.upper() if caps else linha
        run.font.name = FONTE
        run.font.size = Pt(tamanho)
        run.font.bold = negrito
        run.font.italic = italico
        run.font.color.rgb = cor
    return caixa


def cabecalho(sl, eyebrow, titulo, descricao=None):
    retangulo(sl, 0, 0, 13.333, 7.5, SUPERFICIE)
    texto(sl, 0.9, 0.55, 6, 0.3, "EasyVan", 12, PETROLEO, True)
    retangulo(sl, 0.9, 0.88, 0.42, 0.045, AMBAR)
    texto(sl, 1.45, 0.8, 8, 0.3, eyebrow, 10.5, TENUE, True, caps=True)
    texto(sl, 0.9, 1.16, 11.4, 0.7, titulo, 30, TEXTO, True)
    if descricao:
        texto(sl, 0.9, 1.78, 10.9, 0.5, descricao, 14.5, SECUNDARIO)
    texto(sl, 0.9, 7.02, 8, 0.3, "EasyVan · Gestão de transporte por vans", 9.5, TENUE)
    texto(sl, 11.0, 7.02, 1.45, 0.3, f"{len(prs.slides._sldIdLst):02d}", 9.5, TENUE, alinhamento=PP_ALIGN.RIGHT)


def cartao(sl, x, y, w, h, titulo, corpo, numero=None, cor_numero=PETROLEO):
    retangulo(sl, x, y, w, h, SUPERFICIE, LINHA, 0.06)
    topo = y + 0.28
    if numero:
        texto(sl, x + 0.3, topo - 0.03, 0.5, 0.3, numero, 10.5, cor_numero, True)
        topo += 0.26
    texto(sl, x + 0.3, topo, w - 0.6, 0.5, titulo, 14.5, TEXTO, True, espaco=1.05)
    texto(sl, x + 0.3, topo + 0.52, w - 0.6, h - (topo - y) - 0.7, corpo, 11.5, SECUNDARIO, espaco=1.2)


def imagem(sl, caminho, x, y, w, h, rotulo=None):
    """Insere a imagem ajustada dentro da caixa, sem distorcer."""
    from PIL import Image

    with Image.open(caminho) as im:
        iw, ih = im.size
    escala = min(w / iw, h / ih)
    nw, nh = iw * escala, ih * escala
    px, py = x + (w - nw) / 2, y + (h - nh) / 2
    sl.shapes.add_picture(str(caminho), Inches(px), Inches(py), Inches(nw), Inches(nh))
    if rotulo:
        texto(sl, x, y + h + 0.12, w, 0.3, rotulo, 10, TENUE)


# ---------------------------------------------------------------- 1. Capa
sl = slide()
retangulo(sl, 0, 0, 13.333, 7.5, PETROLEO)
retangulo(sl, 0, 0, 0.16, 7.5, AMBAR)
texto(sl, 0.95, 0.75, 6, 0.4, "EASYVAN", 13, AMBAR, True)
texto(sl, 0.95, 1.55, 9.3, 1.6, "A operação da van,\norganizada.", 54, SUPERFICIE, True, espaco=1.02)
texto(sl, 0.95, 3.55, 7.4, 0.9,
      "Sistema web de gestão de transporte por vans escolares e universitárias: "
      "linhas, viagens, presença e alertas operacionais.", 17, PETROLEO_CLARO, espaco=1.3)
retangulo(sl, 0.95, 4.72, 5.1, 0.035, AMBAR)
texto(sl, 0.95, 5.0, 6.0, 0.3, "INTEGRANTES", 10, AMBAR, True)
texto(sl, 0.95, 5.32, 6.4, 1.5, [
    "Guilherme Pereira Andraz",
    "Davi Gato Grijó da Silva",
    "Victor Hugo Volpi Pereira",
    "Caio Candiani Regio",
    "Pedro Ailton dos Santos Cavalieri",
    "Enzo Miguel Bertoluci Ovido",
], 12.5, PETROLEO_CLARO, espaco=1.42)
texto(sl, 9.55, 5.32, 3.1, 1.4,
      ["ASP.NET Core MVC · .NET 10", "PostgreSQL / Supabase", "Railway", "Novembro de 2026"],
      11.5, PETROLEO_CLARO, espaco=1.6)

# ---------------------------------------------------------------- 2. Problema
sl = slide()
cabecalho(sl, "Contexto", "A operação ainda vive no grupo de WhatsApp",
          "Transporte escolar e universitário é rotina diária — e a informação crítica fica dispersa.")
cartao(sl, 0.9, 2.35, 3.7, 1.95, "Confirmação dispersa",
       "Quem vai hoje? Cada resposta chega por mensagem, áudio ou não chega. O motorista sai sem saber quem esperar.", "01")
cartao(sl, 4.82, 2.35, 3.7, 1.95, "Capacidade no escuro",
       "Sem contagem consolidada, a van pode sair acima da capacidade ou com assentos vazios.", "02")
cartao(sl, 8.74, 2.35, 3.7, 1.95, "Nenhum registro",
       "Faltas, atrasos e mudanças de ponto de embarque não deixam histórico para conferir depois.", "03")
retangulo(sl, 0.9, 4.6, 11.54, 1.45, PETROLEO_CLARO, None, 0.05)
texto(sl, 1.25, 4.82, 10.9, 1.0,
      "\"O administrador precisa decidir de manhã, com informação de ontem.\"", 18, PETROLEO, True, italico=True)
texto(sl, 1.25, 5.42, 10.9, 0.4,
      "O problema não é falta de van: é falta de um lugar único onde a operação do dia esteja visível e atualizada.", 12, SECUNDARIO)

# ---------------------------------------------------------------- 3. Solução
sl = slide()
cabecalho(sl, "Solução", "Um centro de operações para o dia da van",
          "O EasyVan substitui a coordenação por mensagens por um fluxo com regras explícitas e dados persistidos.")
etapas = [
    ("1", "O Admin monta a linha", "Nome, origem, destino, dias, horários e alunos com ponto de embarque."),
    ("2", "O sistema gera as viagens", "Ocorrências por data e sentido, idempotentes, para 14 dias úteis."),
    ("3", "O aluno confirma", "\"Vou\" ou \"Não vou\" até o prazo de cada sentido."),
    ("4", "O motorista executa", "Passageiros agrupados por ponto, pendentes separados e rota no Google Maps."),
]
for i, (num, titulo, corpo) in enumerate(etapas):
    x = 0.9 + i * 2.95
    retangulo(sl, x, 2.4, 2.62, 2.5, SUPERFICIE, LINHA, 0.06)
    retangulo(sl, x + 0.28, 2.62, 0.5, 0.5, AMBAR if i == 3 else PETROLEO, None, 0.5)
    texto(sl, x + 0.28, 2.7, 0.5, 0.35, num, 13, SUPERFICIE, True, alinhamento=PP_ALIGN.CENTER)
    texto(sl, x + 0.28, 3.3, 2.1, 0.6, titulo, 13, TEXTO, True, espaco=1.1)
    texto(sl, x + 0.28, 3.92, 2.1, 0.9, corpo, 10.5, SECUNDARIO, espaco=1.18)
texto(sl, 0.9, 5.3, 11.5, 0.9,
      "Tudo é calculado a partir do banco — não existe planilha paralela, lista em memória ou tela demonstrativa.", 14, PETROLEO, True)

# ---------------------------------------------------------------- 4. Funcionalidades
sl = slide()
cabecalho(sl, "Escopo implementado", "O que o sistema faz hoje",
          "Seis módulos funcionais, todos persistidos e cobertos por testes automatizados.")
modulos = [
    ("Frota e cadastros", "Vans, motoristas e alunos com validação de placa, CNH e telefone, além de busca e filtros."),
    ("Linhas recorrentes", "Dias, sentidos e horários, alunos com ponto de embarque e alocação de van e motorista."),
    ("Viagens e presença", "Geração idempotente, ciclo de estados, confirmação do aluno e marcação do motorista."),
    ("Centro de Operações", "Ocupação, distribuição das viagens e alertas A1–A6 calculados por regra de negócio."),
    ("Rotas e Maps", "Roteirização no servidor, agrupamento por ponto e links segmentados para o Google Maps."),
    ("Acesso e auditoria", "Login por cookie, três perfis, trilha de operações e tema claro/escuro."),
]
for i, (titulo, corpo) in enumerate(modulos):
    x = 0.9 + (i % 3) * 3.94
    y = 2.3 + (i // 3) * 1.85
    retangulo(sl, x, y, 3.7, 1.6, SUPERFICIE, LINHA, 0.06)
    retangulo(sl, x + 0.28, y + 0.3, 0.09, 0.26, AMBAR)
    texto(sl, x + 0.52, y + 0.28, 2.9, 0.4, titulo, 14, TEXTO, True)
    texto(sl, x + 0.52, y + 0.72, 2.95, 0.8, corpo, 10.5, SECUNDARIO, espaco=1.18)
texto(sl, 0.9, 6.15, 11.5, 0.4,
      "Fora do MVP por decisão de escopo: GPS em tempo real, otimização automática, cobrança e app nativo.", 11, TENUE, italico=True)

# ---------------------------------------------------------------- 5. Tecnologias
sl = slide()
cabecalho(sl, "Stack", "Tecnologias utilizadas",
          "Nenhuma dependência nova foi adicionada: a stack é a mesma do início do projeto.")
chips = [
    ("Backend", "C# · .NET 10 · ASP.NET Core MVC"),
    ("Interface", "Razor Views · Bootstrap · jQuery"),
    ("Estilo", "CSS próprio com tokens e tema claro/escuro"),
    ("Dados", "EF Core 10 · SQLite (dev) · PostgreSQL (prod)"),
    ("Hospedagem", "Railway (contêiner) · Supabase (banco)"),
    ("Qualidade", "xUnit · 143 testes automatizados"),
]
for i, (grupo, valor) in enumerate(chips):
    x = 0.9 + (i % 2) * 5.85
    y = 2.4 + (i // 2) * 1.35
    retangulo(sl, x, y, 5.55, 1.05, SUPERFICIE, LINHA, 0.08)
    texto(sl, x + 0.35, y + 0.2, 1.4, 0.3, grupo, 9.5, TENUE, True, caps=True)
    texto(sl, x + 0.35, y + 0.5, 4.85, 0.4, valor, 13, TEXTO, True)
texto(sl, 0.9, 6.35, 11.5, 0.4, "Sem React, Next.js, Tailwind ou bibliotecas de UI adicionais.", 11, TENUE, italico=True)

# ---------------------------------------------------------------- 6. Arquitetura
sl = slide()
cabecalho(sl, "Arquitetura", "Do navegador ao PostgreSQL",
          "Aplicação MVC renderizada no servidor; regra de negócio isolada em serviços.")
caixas = [
    (0.9, 2.55, 2.5, "Navegador", "Razor + Bootstrap\nCookie de sessão"),
    (3.95, 2.55, 2.5, "Controllers", "Entrada, autorização\ne validação"),
    (7.0, 2.55, 2.5, "Services", "Regras, prazos,\nalertas e roteirização"),
    (10.05, 2.55, 2.4, "EF Core", "DbContext com filtro\npor organização"),
]
for x, y, w, titulo, corpo in caixas:
    retangulo(sl, x, y, w, 1.45, SUPERFICIE, LINHA, 0.07)
    texto(sl, x + 0.25, y + 0.25, w - 0.5, 0.35, titulo, 14, TEXTO, True)
    texto(sl, x + 0.25, y + 0.68, w - 0.5, 0.6, corpo, 10.5, SECUNDARIO, espaco=1.2)
for x in (3.42, 6.47, 9.52):
    texto(sl, x, 3.02, 0.5, 0.4, "→", 20, AMBAR, True, alinhamento=PP_ALIGN.CENTER)
retangulo(sl, 10.05, 4.62, 2.4, 1.5, PETROLEO, None, 0.07)
texto(sl, 10.3, 4.92, 1.95, 0.8, ["PostgreSQL", "Supabase"], 13, SUPERFICIE, True, espaco=1.35)
texto(sl, 10.95, 4.1, 0.6, 0.4, "↓", 18, AMBAR, True, alinhamento=PP_ALIGN.CENTER)
retangulo(sl, 0.9, 4.62, 5.15, 1.5, SUPERFICIE, LINHA, 0.06)
texto(sl, 1.2, 4.84, 4.6, 0.35, "Serviços em segundo plano", 13, TEXTO, True)
texto(sl, 1.2, 5.27, 4.55, 0.7,
      "Geração de viagens a cada 15 minutos e recálculo de rota quando o conjunto de passageiros muda.",
      10.5, SECUNDARIO, espaco=1.2)
retangulo(sl, 6.3, 4.62, 3.45, 1.5, SUPERFICIE, LINHA, 0.06)
texto(sl, 6.6, 4.84, 2.9, 0.35, "Hospedagem", 13, TEXTO, True)
texto(sl, 6.6, 5.27, 2.9, 0.7, "Contêiner no Railway lendo a porta da plataforma e o banco no Supabase.",
      10.5, SECUNDARIO, espaco=1.2)

# ---------------------------------------------------------------- 7. Banco
sl = slide()
cabecalho(sl, "Modelo de dados", "Entidades e relacionamentos",
          "16 migrations aplicadas; o banco de produção tem o mesmo schema do ambiente local.")
texto(sl, 0.9, 2.3, 5.6, 0.35, "Núcleo operacional", 12, PETROLEO, True, caps=True)
linhas = [
    ("Organizacao", "raiz do isolamento por organização", 0.0),
    ("Linha", "dias, sentidos, origem e destino", 0.34),
    ("LinhaPassageiro", "aluno + ponto de embarque", 0.68),
    ("LinhaVan", "van e motorista alocados", 0.68),
    ("Viagem", "ocorrência por data e sentido", 0.34),
    ("PresencaViagem", "confirmado, ausente ou sem resposta", 0.68),
]
for i, (nome, desc, recuo) in enumerate(linhas):
    y = 2.72 + i * 0.63
    retangulo(sl, 0.9 + recuo, y, 3.0 - recuo, 0.46, PETROLEO_CLARO, None, 0.12)
    texto(sl, 1.04 + recuo, y + 0.11, 2.8 - recuo, 0.3, nome, 11, PETROLEO, True)
    texto(sl, 4.15, y + 0.13, 2.45, 0.3, desc, 10, SECUNDARIO)
texto(sl, 7.0, 2.3, 5.4, 0.35, "Integridade garantida no banco", 12, PETROLEO, True, caps=True)
regras = [
    "Chaves estrangeiras restritivas preservam o histórico.",
    "Um vínculo ativo por aluno e sentido, com data de início e fim.",
    "Uma presença por viagem e aluno.",
    "Geração idempotente: índice único impede viagem duplicada.",
    "Filtro global por OrganizacaoId em todas as consultas.",
]
for i, regra in enumerate(regras):
    y = 2.72 + i * 0.72
    retangulo(sl, 7.0, y + 0.11, 0.1, 0.1, AMBAR, None, 0.5)
    texto(sl, 7.3, y, 5.1, 0.6, regra, 11, SECUNDARIO, espaco=1.15)
texto(sl, 7.0, 6.35, 5.2, 0.4, "Migrations versionadas em Migrations/ e aplicadas no boot.", 10, TENUE, italico=True)

# ---------------------------------------------------------------- 8. CRUD
sl = slide()
cabecalho(sl, "CRUD", "Criar, consultar, editar e excluir",
          "Cada operação passa por ViewModel, validação no servidor, antiforgery e autorização.")
operacoes = [
    ("Create", "Formulário → validação → entidade → SaveChanges.", "Placa única, CNH válida, senha de 6 a 20 caracteres."),
    ("Read", "Listagens e detalhes consultam o banco com filtro por organização.", "Busca e filtros combináveis em /Vans."),
    ("Update", "Edição carrega o registro, valida o vínculo e regrava.", "Troca de linha encerra o vínculo anterior."),
    ("Delete", "Exclusão só quando não há dependências.", "Admin não exclui a si nem outro administrador."),
]
for i, (nome, fluxo, regra) in enumerate(operacoes):
    x = 0.9 + i * 2.95
    retangulo(sl, x, 2.35, 2.62, 3.25, SUPERFICIE, LINHA, 0.05)
    retangulo(sl, x + 0.28, 2.6, 1.05, 0.32, PETROLEO, None, 0.4)
    texto(sl, x + 0.28, 2.66, 1.05, 0.25, nome, 9.5, SUPERFICIE, True, alinhamento=PP_ALIGN.CENTER)
    texto(sl, x + 0.28, 3.1, 2.1, 1.1, fluxo, 10.5, SECUNDARIO, espaco=1.2)
    retangulo(sl, x + 0.28, 4.35, 2.06, 0.02, LINHA)
    texto(sl, x + 0.28, 4.5, 2.1, 1.0, regra, 10, TEXTO, espaco=1.18)
retangulo(sl, 0.9, 5.85, 11.54, 0.75, VERDE_CLARO, None, 0.1)
texto(sl, 1.15, 6.05, 11.0, 0.4,
      "Persistência real: nenhuma listagem usa dados em memória ou JSON local.", 12, VERDE, True)

# ---------------------------------------------------------------- 9. Interface
sl = slide()
cabecalho(sl, "Interface", "Capturas reais do sistema",
          "Layout responsivo de 320 px a 1920 px, com contraste AA e foco visível.")
imagem(sl, IMG / "tela-centro-operacoes.png", 0.9, 2.25, 7.1, 3.6, "Centro de Operações (Administrador)")
imagem(sl, IMG / "tela-vans.png", 8.3, 2.25, 4.15, 3.6, "Frota com busca e filtros")
texto(sl, 0.9, 6.3, 11.5, 0.5,
      "Tema claro e escuro, tabelas com rolagem própria no celular e nenhuma rolagem horizontal da página.", 11, SECUNDARIO)

# ---------------------------------------------------------------- 10. Segurança
sl = slide()
cabecalho(sl, "Segurança", "Autenticação, permissões e dados",
          "Controles aplicados no backend — a interface apenas reflete o que o servidor permite.")
itens = [
    ("Senhas", "PBKDF2-SHA256 com 600 mil iterações, salt por usuário e comparação em tempo constante."),
    ("Sessão", "Cookie HttpOnly, Secure e SameSite Lax, revalidação do usuário a cada requisição."),
    ("Manter conectado", "Cookie persistente de até 30 dias; sem a opção, encerra com o navegador."),
    ("Perfis", "Administrador, Motorista e Aluno; cada ação é autorizada por papel no servidor."),
    ("Cadastro", "Somente o Admin cria contas. Não existe cadastro público."),
    ("Superfície", "Antiforgery em todo POST, rate limit no login, consultas parametrizadas pelo EF Core."),
    ("Dados", "Filtro por OrganizacaoId, RLS no Supabase e segredos apenas em variáveis de ambiente."),
    ("LGPD", "Coleta mínima, sem PII em logs e endereço residencial fora da rota enviada ao Maps."),
]
for i, (titulo, corpo) in enumerate(itens):
    x = 0.9 + (i % 2) * 5.85
    y = 2.3 + (i // 2) * 1.1
    retangulo(sl, x, y, 0.09, 0.45, AMBAR if i % 2 else PETROLEO)
    texto(sl, x + 0.28, y - 0.02, 4.9, 0.3, titulo, 12, TEXTO, True)
    texto(sl, x + 0.28, y + 0.32, 5.1, 0.7, corpo, 10, SECUNDARIO, espaco=1.15)

# ---------------------------------------------------------------- 11. Demonstração
sl = slide()
cabecalho(sl, "Demonstração", "Roteiro de cinco minutos",
          "Sequência sugerida para a banca, com os três perfis.")
passos = [
    ("Login", "Entrar como Administrador e mostrar o Centro de Operações com dados reais."),
    ("Cadastro", "Criar uma linha com origem, destino, dias e alunos; mostrar validação de erro."),
    ("Geração", "Abrir Viagens e localizar as ocorrências geradas para os próximos dias úteis."),
    ("Aluno", "Entrar como aluno e confirmar ida e volta; mostrar o bloqueio após o prazo."),
    ("Motorista", "Abrir a viagem, conferir passageiros por ponto e abrir o trecho no Google Maps."),
    ("Auditoria", "Voltar ao Admin e mostrar o registro da operação na atividade recente."),
]
for i, (titulo, corpo) in enumerate(passos):
    x = 0.9 + (i % 3) * 3.94
    y = 2.3 + (i // 3) * 1.85
    retangulo(sl, x, y, 3.7, 1.6, SUPERFICIE, LINHA, 0.06)
    texto(sl, x + 0.28, y + 0.24, 0.6, 0.35, f"0{i + 1}", 12, AMBAR, True)
    texto(sl, x + 0.95, y + 0.26, 2.5, 0.35, titulo, 13, TEXTO, True)
    texto(sl, x + 0.28, y + 0.75, 3.15, 0.75, corpo, 10.5, SECUNDARIO, espaco=1.18)

# ---------------------------------------------------------------- 12. Encerramento
sl = slide()
retangulo(sl, 0, 0, 13.333, 7.5, PETROLEO)
retangulo(sl, 0, 0, 0.16, 7.5, AMBAR)
texto(sl, 0.95, 1.5, 8, 0.4, "EASYVAN", 13, AMBAR, True)
texto(sl, 0.95, 2.05, 9.5, 1.0, "Obrigado.", 54, SUPERFICIE, True)
texto(sl, 0.95, 3.2, 8.6, 1.1,
      "Um sistema pequeno o suficiente para ser explicado e completo o suficiente para operar de verdade.",
      18, PETROLEO_CLARO, espaco=1.3)
retangulo(sl, 0.95, 4.45, 5.0, 0.035, AMBAR)
texto(sl, 0.95, 4.75, 6.5, 0.35, "REPOSITÓRIO", 10, AMBAR, True)
texto(sl, 0.95, 5.08, 7.0, 0.4, "github.com/guiguix7/EasyVan", 15, SUPERFICIE, True)
texto(sl, 8.2, 4.75, 4.2, 0.35, "APLICAÇÃO", 10, AMBAR, True)
texto(sl, 8.2, 5.08, 4.4, 0.9, "Railway + Supabase\npostgres · us-west-2", 13, PETROLEO_CLARO, espaco=1.3)

prs.save(SAIDA)
print(f"gerado: {SAIDA} ({len(prs.slides._sldIdLst)} slides)")
