# AuxCodex

![AuxCodex](https://i.imgur.com/BuCQbJr.jpeg)

O AuxCodex é um aplicativo Windows em WinForms executado na bandeja do sistema. Ele organiza pastas, projetos e sessões e permite configurar a execução de OpenAI e de provedores adicionais por meio de arquivos BAT e templates.

## Principais recursos

- menu hierárquico na bandeja do Windows;
- pastas e projetos que podem ser criados, editados, movidos e removidos da configuração;
- múltiplas sessões independentes por projeto;
- OpenAI como provedor principal;
- provedores adicionais configuráveis por catálogo;
- templates BAT globais e geração/edição de BATs;
- diretório de trabalho do projeto (`ProjectDirectory`);
- ResumeKey independente por sessão e provedor;
- abertura opcional da URL do GPT;
- execução normal ou elevada quando configurada;
- persistência em JSON;
- log operacional local em `AuxCodex.log`.

## Tecnologias

- C#
- .NET 9
- Windows Forms
- `net9.0-windows`

## Requisitos

- Windows;
- .NET 9 SDK para compilação;
- runtime compatível com a forma de publicação escolhida;
- ferramentas externas referenciadas pelos templates BAT, quando aplicável.

## Compilação

Na raiz da solução:

```text
dotnet build AuxCodex.sln --nologo
```

O executável compilado fica no diretório de saída do projeto conforme a configuração utilizada.

## Execução

Execute o binário compilado no Windows. O programa permanece na bandeja do sistema e o menu principal é aberto pelo clique esquerdo no ícone.

Também é possível indicar uma pasta de configuração isolada:

```text
AuxCodex.exe --config-dir "C:\Projetos\AuxCodex-Teste"
```

Sem `--config-dir`, a configuração é lida na pasta base da execução. O arquivo local `config.json` não deve ser compartilhado.

## Configuração

A configuração é armazenada em JSON e pode ser mantida pelas telas do aplicativo. Projetos possuem uma pasta de trabalho, sessões e configurações individuais de OpenAI e provedores adicionais. Cada sessão mantém suas próprias ResumeKeys e caminhos de BAT.

O arquivo `config.example.json` contém somente dados fictícios para referência. Não copie credenciais, tokens ou ResumeKeys reais para ele.

## Provedores

OpenAI aparece como provedor principal. A tela **Provedores** permite configurar o template global de OpenAI e cadastrar provedores adicionais, como DeepSeek, GLM ou outros definidos pelo usuário.

Os templates globais não devem conter chaves específicas de uma sessão nem credenciais.

## Templates BAT

Quando um BAT novo ou ausente é composto pelo aplicativo, o resultado segue conceitualmente este formato:

```text
d:
<Pasta do projeto>
<template do provedor parametrizado>
```

O template é combinado com a pasta do projeto e a ResumeKey da sessão/provedor em memória. BATs existentes podem conter personalizações e não devem ser substituídos cegamente.

## Log

`AuxCodex.log` é criado na pasta do executável e sobrescrito a cada inicialização. Durante a execução, novos eventos são acrescentados ao mesmo arquivo. O logger foi projetado para não registrar ResumeKeys, chaves de API, tokens ou o conteúdo integral de BATs e templates.

## Estrutura do projeto

- `Forms`: telas WinForms de projetos, sessões, movimentação e provedores;
- `Models`: modelos de configuração, projetos, sessões e provedores;
- `Services`: persistência, migração, composição de BAT, menu, execução e logging;
- `Utils`: validações e recursos compartilhados;
- `TesteManual`: roteiro e materiais locais de homologação; dados de teste não devem ser versionados.

## Segurança

- não versione `config.json` local;
- não publique BATs gerados ou personalizados;
- não coloque ResumeKeys, API keys, tokens ou senhas no repositório;
- revise templates antes de compartilhá-los;
- trate caminhos e URLs presentes na configuração como dados potencialmente privados;
- mantenha arquivos locais de log fora do controle de versão.

## Status

O projeto está em desenvolvimento e homologação manual. A composição de BAT novo/ausente foi homologada; outras validações visuais e operacionais ainda dependem do ambiente e do usuário.
