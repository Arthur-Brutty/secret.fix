# AGENTS.md — secret.fix

Este arquivo é o contexto principal para agentes de código (Codex).

## Missão

Construir `secret.fix`, um aplicativo Windows moderno para centralizar ajustes de input e performance voltados a FiveM, mantendo transparência técnica, reversibilidade e boa UX.

## Produto

Planos definidos:

- CORE: MouseFix e ajustes essenciais de mouse/input.
- PULSE: tudo do CORE + TecladoFix + FiveM + automações.
- APEX: tudo do PULSE + Flick/Aim Trainer, benchmark, Display, diagnóstico e ferramentas avançadas.

O aplicativo é único. A licença determina quais recursos ficam disponíveis.

## Regras técnicas

1. Target: Windows 10/11 x64, .NET 8, WPF/C#.
2. Toda mudança persistente no Windows deve ter leitura do estado anterior e possibilidade de restauração.
3. Não afirmar ganhos de latência sem medição.
4. Não forçar polling rate que o hardware/driver não suporta.
5. Não implementar DLL injection, cheats, aim assist, macro de tiro, bypass de anti-cheat ou bypass de proibição de servidor.
6. Não guardar keys, tokens, segredos, HWIDs crus ou credenciais no Git.
7. A camada de licenciamento deve ser abstraída por interface; durante desenvolvimento usar implementação mock/local.
8. Recursos desconhecidos do software de referência não devem ser inventados como se fossem equivalentes.
9. Prefira APIs documentadas do Windows e deixe recursos experimentais explicitamente marcados.
10. UI: preto + vermelho, pouco verde/nenhum verde; hover e seleção animados; textos sempre dentro dos cards; imagens de mouse grandes e imagens de teclado nítidas.

## Design

Referências visuais estão em `docs/reference/`.

Direção aprovada:

- sidebar à esquerda;
- fundo quase preto;
- vermelho para seleção/hover/estado ativo;
- cards discretos com bordas finas;
- imagem grande do mouse/teclado no centro;
- carrossel de dispositivos com foto e nome dentro de cada card;
- animação suave de entrada e hover;
- usuário/plano no rodapé da sidebar.

## Account

Campos planejados:

- Username
- License (mascarada)
- Plan
- Expiration
- Device
- App Version
- Upgrade Plan
- Status
- Last Login
- Device Bind
- Support ID

## Processo de trabalho

Antes de implementar uma função de sistema:

1. identificar exatamente o que será alterado;
2. documentar valor atual e valor pretendido;
3. criar backup/snapshot;
4. aplicar;
5. validar;
6. permitir restauração;
7. registrar resultado no log.

Para tarefas de UI, preserve a arquitetura e faça mudanças granulares em vez de reescrever o aplicativo inteiro.
