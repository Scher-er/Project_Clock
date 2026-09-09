import re

with open('Services/PdfParserService.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('''            // 1.5) Primeiro, checa dicionário De/Para
            // Assumiremos EmpresaId = null por enquanto porque no ImportarPdf ainda não sabemos a empresa.
            // A empresa só é cadastrada depois no Planilhamento.
            var dePara = await _listagens.BuscarMapeamentoDeParaAsync(descNormalizada, null);
            if (dePara != null && dePara.ContaPadraoId > 0)
            {
                var contaMatch = contas.FirstOrDefault(c => c.Id == dePara.ContaPadraoId);
                if (contaMatch != null)
                {
                    r.ContasMapeadas.Add(new ContaMapeada
                    {
                        ContaPadraoId = contaMatch.Id,
                        DescricaoOriginal = descricao,
                        Valor = valor.Value
                    });
                    continue;
                }
            }

            // 2) Busca conta padrAo com maior similaridade
            var descNormalizada = Normalizar(descricao);''',
'''            var descNormalizada = Normalizar(descricao);

            // 1.5) Primeiro, checa dicionário De/Para
            // Assumiremos EmpresaId = null por enquanto porque no ImportarPdf ainda não sabemos a empresa.
            // A empresa só é cadastrada depois no Planilhamento.
            var dePara = await _listagens.BuscarMapeamentoDeParaAsync(descNormalizada, null);
            if (dePara != null && dePara.ContaPadraoId > 0)
            {
                var contaMatch = contas.FirstOrDefault(c => c.Id == dePara.ContaPadraoId);
                if (contaMatch != null)
                {
                    if (!r.ContasMapeadas.ContainsKey(contaMatch.Id))
                    {
                        r.ContasMapeadas[contaMatch.Id] = valor.Value;
                    }
                    continue;
                }
            }

            // 2) Busca conta padrão com maior similaridade''')

with open('Services/PdfParserService.cs', 'w', encoding='utf-8') as f:
    f.write(code)
