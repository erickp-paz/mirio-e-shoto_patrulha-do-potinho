# Backlog — Mirio & Shoto: Patrulha do Potinho

## Em andamento
- [x] Sprites 2D de Mirio e Shoto (8 direções)
- [x] Shoto jogável
- [ ] Tela de seleção de personagem + atributos distintos + crítico (etapa 1)

## Próximas etapas (ordem combinada)
1. Amostras de pixel art no estilo do Fuga FB (insetos, cenário) para aprovação
2. Cooperativo local 2P (WASD + setas/controle; reviver o parceiro parando alguns segundos onde ele caiu; escolha de quem joga com quem)

## Melhorias para outras sessões
- Menu de configurações: brilho, volume (música/efeitos), mapeamento de botões (teclado/controle), acessibilidade
- Suporte a toque para a versão mobile (joystick virtual, botões na tela)
- Otimização: object pooling (insetos, projéteis, números flutuantes), reduzir alocações por frame, atlas de sprites, limitar insetos ativos, perfilar no Profiler, substituir IMGUI do HUD por UI Toolkit/uGUI
- Sprites dos demais personagens (insetos, potinho, barricadas de almofada) e cenários em pixel art
- Remapeamento de teclas deve definir livremente quais teclas cada jogador usa no co-op

## Pedidos recentes (30/10)
- [x] Velocidade dos insetos reduzida; entradas com disco maior, feixe de luz e rótulo maior
- [x] Insetos em pixel art: formiga, saúva, mosquito, barata, grilo, besouro; Mosca-Rainha substitui o besouro-rinoceronte
- [x] Pelúcia (bicho sorteado por partida) no lugar da almofada
- [x] Mirio e Shoto com tamanho proporcional (Mirio ~17% maior)
- [ ] Redesenhar o cenário (melhorar visual da sala)
- [ ] Redesenhar a tela de escolha de habilidades (hoje muito simples)
- [ ] Animação de andar e 2–4 direções nos insetos (hoje 1 pose espelhada)
- [ ] Mosca comum como inseto regular (a mosca-rainha é só o chefe)

## Ajustes de 30/10 (tarde)
- [x] Ordem de tamanho: formiguinha < saúva = mosquito < mosca < varejeira < barata; chefe = Barata Gigante (substitui a mosca gigante)
- [x] Petiscos em pixel art (peixinho, sardinha, lata de atum)
- [x] Andar para a esquerda usa os quadros da direita espelhados (cartelas tinham gatos virados para o lado errado)
- [x] Gatos menores e proporcionais (tela de seleção usa a mesma escala do jogo)
- [ ] Rever balanceamento: níveis sobem rápido demais (XP por nível)
- [ ] Remover PNGs não usados em Resources/Insects (besouro, grilo, mosca_rainha)
- [ ] PRIORIDADE (balanceamento): garantir que dá para vencer a primeira fase (hoje ninguém venceu). Revisar dano base por arma, crescimento de dano por nível, XP por nível e curva de spawn; testar vitória com cada gato
