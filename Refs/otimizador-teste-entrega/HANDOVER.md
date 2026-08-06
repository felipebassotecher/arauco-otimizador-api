# otimizador-teste — pacote de entrega

Spike do otimizador de pedidos da Arauco: OR-Tools CP-SAT em .NET rodando sobre a
**carteira real** do banco do ADC. Serviu para aprender escala, qualidade do dado e
formulação **antes** de escrever a PoC de verdade.

> ⚠️ **Contém dado real da Arauco** — `dados/` traz nome e id de clientes. Trate como
> dado de cliente da consultoria: não subir em repositório compartilhado, não anexar
> em e-mail solto, não colar em ferramenta externa.

---

## Leia isto primeiro: o que este pacote é e o que ele não é

**Nada aqui é reaproveitado pela PoC.** É spike descartável, por decisão: sem testes,
sem EF Core, sem camadas. O valor está nos **documentos de análise**, não no código.
Se você começar copiando classes para a PoC, o enquadramento já saiu errado.

O que este repositório entrega:

1. **59 decisões registradas com porquê e evidência medida** — a maior parte se aplica
   direto à PoC.
2. **7 perguntas abertas para o negócio** — não bloqueiam código, mas três mudam a
   leitura de qualquer resultado. Quanto antes forem levadas, melhor.
3. **Um mapa de armadilhas já pagas** — várias custaram horas de depuração porque
   produziam resultado *plausível e errado*, sem erro nenhum.

---

## O que tem no zip

| Item | O que é |
|---|---|
| `otimizador-teste.bundle` | O repositório inteiro, com histórico. Não é um `.zip` de arquivos — é um bundle do git |
| `dados/saida-adc.zip` | A extração do ADC, feita na máquina Windows. **Sem isso nada roda** |
| `dados/referencia-fase1.json` | Baseline de regressão da fase 1 (108.519 m³ alocados) |

Os dois arquivos de `dados/` estão **fora do versionamento de propósito** e por isso
vêm à parte — o `.gitignore` do repo cobre os padrões que carregam nome de cliente.

---

## Montando do zero

### Pré-requisitos

- **.NET SDK 8.0** (testado em 8.0.423) — o `Google.OrTools` 9.15.6755 vem via NuGet
- **Node 20+** (testado em 24.12) — só para a UI
- **Python 3.11+** — só se você for regerar a extração do ADC

### Passo a passo

```bash
# 1. clonar do bundle
git clone otimizador-teste.bundle otimizador-teste
cd otimizador-teste

# 2. colocar os dados no lugar (eles NAO vem do git)
cp .../dados/saida-adc.zip .
cp .../dados/referencia-fase1.json engine/
unzip -o saida-adc.zip -d saida-adc-extraido

# 3. conferir que subiu certo — regressão contra o baseline
cd engine && dotnet run -- --referencia referencia-fase1.json
# esperado: ~108.5 mil m3, desvio dentro de 0,5%
```

Se esse último comando fechar dentro da tolerância, está tudo no lugar.

### Rodando os cenários

```bash
cd engine
dotnet run                                                    # simulado, alvo 0,8
dotnet run -- --config cenario-real.json --saida r.json       # real, janela W45-52
dotnet run -- --config cenario-real-cru.json --saida r.json   # capacidade real intocada
```

Config comentada campo a campo em `engine/config.exemplo.json`. As configurações da
fase 2 (carreta, critérios, mix CIF/FOB, espalhamento, carteira sintética, limite de
recebimento) estão em `analise/CONTINUIDADE.md`.

### A aplicação (dois processos)

```bash
cd api && dotnet run --no-launch-profile --urls http://localhost:5175
cd web && npm install && npm run dev        # http://localhost:5173
```

O primeiro `GET /api/dataset` demora ~10 s: carrega os Parquet e cacheia.

---

## Ordem de leitura

Não re-derive o que já está registrado.

| # | Documento | O que contém |
|---|---|---|
| 1 | **`analise/DECISOES.md`** | **59 decisões, 6 premissas, 7 perguntas abertas. Comece aqui** |
| 2 | `analise/ACHADOS.md` | O que os dados do ADC revelaram (escala, defasagem, elegibilidade × capacidade) |
| 3 | `analise/ENGINE.md` | Cenários rodados, explicabilidade, e os defeitos encontrados construindo |
| 4 | `analise/CONTINUIDADE.md` | O que está pronto, o que ficou pela metade, o que fazer a seguir |
| 5 | `CLAUDE.md` | **Mapa de armadilhas.** Se você usa Claude Code, entra em contexto sozinho |

Marcadores das decisões: 🔒 firmado com evidência medida · 🟡 assumido, funciona, não
confirmado · 🔴 aberto, depende de terceiro.

---

## Três coisas que eu destacaria antes de você começar a PoC

### 1. A escolha de planta é menor do que parecia

**71,9% do volume tem uma única planta viável**, 13,7% tem duas. Vender "o otimizador
escolhe a planta" como valor central seria impreciso. O que ele decide de fato, nesta
carteira, é **quando** e **o que fica de fora**. Isso muda como a PoC é apresentada.

### 2. As perguntas abertas são entrega, não pendência

`DECISOES.md §6`. A mais frágil de todas: **a capacidade do tático está mesmo em m³?**
Foi assumida só por ordem de grandeza. Se for chapas ou toneladas, todo gráfico de
capacidade é ficção. Vale levar ao negócio antes de construir em cima.

### 3. O hint do CP-SAT é a fonte de metade dos bugs

**Hint incompleto ou infactível é descartado inteiro, em silêncio.** O solver escreve
uma linha no log e segue como se não houvesse hint; o resultado piora muito e nada
acusa erro. Aconteceu **cinco vezes** neste projeto. O caso mais caro: 108.519 → 39.574 m³.

Por isso existem duas salvaguardas no código, e elas precisam continuar valendo se você
portar a formulação:

- `Greedy.Validar()` — confere a solução gulosa contra as restrições antes de virar hint
- Contagem de completude contra o **proto** do modelo, não contra soma manual

E uma sutileza que só apareceu na última mudança: **restrição sem variável nova não é
coberta pela contagem de completude** — ela fica verde enquanto o hint é descartado por
infactibilidade. Nesse caso o dever é fazer o greedy respeitar a restrição enquanto
constrói **e** acrescentar uma checagem no `Validar()`.

O resto das armadilhas está em `CLAUDE.md`, seção "Armadilhas já pagas".

---

## Já respondido para a PoC

O **D-27 da PoC** (granularidade da capacidade) está resolvido: leaf
`(Centro, LinhaProduto, Semana)`, com `tipoAlocacao = 1`. Ver **D-05**.

E três aprendizados que poupam retrabalho direto:

1. **A escala numérica do modelo é decisória, não detalhe de implementação** (D-13).
   Volume em centésimos + prioridade 0..100 fazia o objetivo chegar a ~10¹³ e o CP-SAT
   gastava o orçamento inteiro no presolve **sem iniciar a busca**.
2. **Itens menores que um lote viram atribuição pura** — sem variável inteira nem
   big-M. Vale para 60% da carteira e é parte do que torna a instância tratável (D-12).
3. **O pre-flight não é opcional** (D-19). Sem separar o inalocável estrutural, 14,6%
   do volume vira ruído que parece decisão do otimizador.

---

## Se os dados vencerem

O snapshot é de **novembro/2025**. Uma extração nova é barata: copiar
`extract/adc_standalone.py` para uma máquina Windows com acesso ao SQL Server e rodar

```
python adc_standalone.py --server <server> --database ADC --trusted
```

Traga a pasta de volta e repita o passo 2. Detalhes no `README.md`.

> `extract/adc_standalone.py` é **gerado**, não editar à mão. Mexeu num `.sql` ou no
> `run_adc.py`? Rode `python extract/build_standalone.py`.
