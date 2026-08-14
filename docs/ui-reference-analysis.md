# UI reference analysis — secret.fix v0.4

## Fonte e método

- Vídeo: `docs/reference/precisionfix-v3.mp4`.
- `ffprobe`: H.264, 1920 × 1080, 60 fps, duração de 88,932 s; áudio AAC presente no arquivo de referência.
- `ffmpeg`: 18 frames extraídos em intervalos de 5 s, mais uma sequência detalhada entre 50 s e 78 s em intervalos de 2 s.
- Páginas comparadas: MouseFix (~10–18 s e ~85 s), TecladoFix (~20 s), FiveM (~25 s), Flick (~30–38 s), Sensi (~40–52 s), Mira (~54–68 s), Serviços (~70 s) e Display (~72–82 s).

## Estrutura observada

O aplicativo de referência é uma janela desktop compacta, aproximadamente 16:9, com sidebar fixa estreita à esquerda e conteúdo denso à direita. A sidebar ocupa perto de um quinto da janela. O conteúdo usa fundo quase preto, cards com bordas finas e baixo contraste e uma cor de destaque reservada a seleção, estado ativo e avisos. No `secret.fix`, essa linguagem permanece preto/vermelho e não replica marca ou assets proprietários.

As páginas evitam grandes vazios e dashboards genéricos. Títulos ficam no topo, controles se alinham em linhas ou colunas curtas e o usuário/plano permanece no rodapé da sidebar. Hover e transições devem ser rápidos (aproximadamente 120–200 ms) e discretos.

## MouseFix

- O mouse central aparece inteiro, vertical, com proporção preservada e altura visual próxima a metade da área útil da página.
- O hero não toca as bordas e não usa um retângulo de imagem excessivamente largo; o canvas deve ser estável entre modelos, com o produto centralizado dentro dele.
- As opções formam duas colunas laterais leves ao redor do mouse.
- O seletor inferior é uma faixa horizontal de cards compactos. Cada card mantém foto, fabricante e modelo dentro dos limites.
- Seleção usa borda de destaque; hover usa pequena elevação/escala sem deslocamento agressivo.

## TecladoFix

- O teclado é exibido inteiro e centralizado em um canvas horizontal, com largura suficiente para leitura do produto e sem crop.
- As opções permanecem nas laterais, preservando a mesma hierarquia do MouseFix.
- Cards de teclado precisam ser mais largos que os de mouse e usar imagens nítidas com margem consistente.

## FiveM

- Há um card principal de launcher com ação evidente no canto direito e uma lista compacta de opções abaixo.
- O botão de abrir o FiveM deve ser uma ação real; estados de processo/caminho ficam visíveis sem sugerir otimizações não implementadas.

## Mira e Serviços

- Mira usa card de introdução, aviso, seletor do overlay e controles em linhas compactas. Mudanças de preset devem alterar parâmetros reais, não apenas o estilo do botão.
- Serviços aparece como lista simples de toggles. O estado ativo é fácil de ler e não deve desaparecer ao navegar.

## Display

- Display usa cards de preset no topo e sliders horizontais abaixo, em um único painel denso.
- Presets e sliders conservam valores, mas nenhuma alteração real deve ocorrer somente ao abrir a página.
- Onde não houver API genérica segura para saturação/temperatura/gamma, a UI deve dizer `Experimental` ou `Não suportado`, sem confirmação falsa de sucesso.

## Decisões para v0.4

- Preservar login, galáxia, fullscreen, sidebar e identidade preto/vermelho existentes.
- Normalizar o canvas dos assets para manter escala visual semelhante, além de usar `Stretch=Uniform`.
- Não usar logo nem foto de outro produto como substituto de um modelo anunciado; itens sem asset fiel usam visual Generic explícito.
- Conectar as páginas ao estado compartilhado/persistente em vez de depender da instância da view.
- Manter cards compactos, borda vermelha selecionada, hover com escala leve e textos integralmente dentro do card.
