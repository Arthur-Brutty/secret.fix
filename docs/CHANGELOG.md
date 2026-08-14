# Changelog

## v0.4 — estabilidade e funções reais

### Implementado

- estado compartilhado e persistente em `%LocalAppData%\SecretFix\settings.json`;
- cache de views para preservar a sessão durante toda a navegação;
- detecção conservadora de mouse/teclado por HID, VID e PID, com fallback Generic;
- assets Generic próprios e imagens oficiais normalizadas para Logitech G Pro X Superlight 2 e Razer Viper V3 Pro;
- launcher FiveM com processo existente, caminhos padrão, caminho salvo e seleção manual;
- presets Basic, Medium, High e Custom da Mira com atualização imediata do overlay;
- Display sem aplicação automática e sem mensagens falsas de sucesso;
- gates centralizados CORE/PULSE/APEX e janela VIEW PLANS;
- carregamento tolerante a backup/configuração JSON inválidos;
- testes de VID/PID, dispositivos, settings, presets, gates e backup.

### Segurança

- nenhum recurso de injeção, manipulação de memória, bypass, cheat ou macro de combate;
- ajustes não suportados de Display permanecem explicitamente indisponíveis;
- opções de Serviços permanecem preferências experimentais e não alteram o Windows nesta versão.

## 2026-08-12 — Repositório canônico

### Decidido
- projeto passa a ter fonte organizada para GitHub/Codex;
- um único aplicativo com planos CORE, PULSE e APEX;
- Account com dados de licença/plano/dispositivo/versão;
- design preto + vermelho;
- animações de hover e seleção preservadas;
- imagens maiores/melhores de mouse e teclado;
- funções de sistema sempre com backup/restore.

### Próximo passo
- usar Codex para iterar sobre a UI e implementar os módulos por prioridade.
