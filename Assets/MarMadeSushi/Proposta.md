# Aula 11Jogo
# Relatório Técnico – Projeto de Redes e Multijogador com Unity
## Capa
Nome do Projeto: Mar Made Sushi
Nome do Aluno: Gonçalo Santana de Miranda Pulido Valente
Curso: Desenvolvimento de Jogos Digitais
Data de Entrega: 
Numero de Aluno: 40981

## Introdução
Breve descrição do objetivo do projeto, tipo de jogo criado e tecnologias envolvidas.

## Ideia de jog

Tipo de jogo (FPS, estratégia, por turnos...): Party Game - Estratégia.
Quantos jogadores suporta? 1 a 4 jogadores.
Objetivo principal do jogo: Servir clientes num restaurante para manter uma boa reputação.
Mecânicas principais:
    - Cozinhar;
    - Servir os clientes;
    - Pescar;

## Arquitetura de Rede
#### Sistema utilizado:
☐ Photon | ☐ Mirror | x Netcode | ☐ UTP | ☐ Outro: ___________

#### Tipo de arquitetura:
x Peer-to-peer
☐ Host-client
☐ Servidor dedicado

#### Como os jogadores se ligam entre si?
Inicialmente era com o uso de Relay, em que os jogadoes clicavam num unico butao para criar um lobby com um codigo gerado. Os outros jogadores teriam de inserir esse codigo e clicar num botão para se juntarem. Mas infelizmente as instalações onde fiz os testes não permitem esse tipo de ligações, tive de mudar para websockets com conecções por IP e portas. O host cria um lobby num unico clique de um botão e no ecrã aparece um IP. Depois os outros jogadores inserem esse ip no ecrã e pressinam no butão de entrar.

## Sincronização
#### O que está sincronizado? (ex: posição dos jogadores, vida, disparos, objetos)
Posição, animações, objetos, clientes, e cenarios.

#### Como foi feita a sincronização?
A sincronização foi feita com varias funcionalidades que o Netcode For Gameobject oferece. Como por exemplo, sincronização automatica de posições e animações entre clientes. Mas tambem foram desenvolvidos varios scripts para manter a sincronização entre objectos, clientes, cenarios, etc.

#### Por RPCs?
Sim, foi usado RPCs para enviar informações de clientes para o servidor, ou até mesmo cliente para servidor e para outros clientes.

#### Por NetworkVariable?
NetworkVariable foi uma grande ajuda pois facilita muito a sincronização entre clientes sem ter de passar pelo servidor manualmente. Por exemplo, para indicar qual o pedido do cliente que está sentado na mesa, digo atravez de uma networkvariable e por sua vez todos os outros jogados tambem ficam atualizados sobre o pedido.

#### Com PhotonView?
Nao usei Photon no meu projeto.

#### Foram usados snapshots, predição, interpolação?
Foi usado interpolação para posições dos jogadores.

## Segurança e validação
#### Que mecanismos foram implementados para evitar erros/trapaças? (ex: verificação de velocidade, limite de dano, controlo no servidor)
Nenhum de momento.

#### Foi usada encriptação ou filtragem de pacotes?
O unity Netcode For Gameobjects já faz essa encriptação e filtragem por nós desenvolvedores.

## Interface e feedback
#### Que elementos visuais foram usados para mostrar estados? (vida, pontuação, notificações, turnos, etc.)
Pontuação, reações com balões de conversa e vários sprites diferentes. Por exemplo, o tacho de arroz quando não contem nada usa um sprite vazio, quando contem arroz fica a borbulhar e quando cozido fica com arroz a transbordar.

#### Como é feito o feedback das ações dos jogadores?
Com alteração de sprites ou spawn de objectos.

## Testes realizados
#### Foram feitos testes entre computadores? 
Sim, houve varios testes entre computadores na mesma rede e fora.

#### Quantos jogadores simultâneos foram testados?
Conseguimos juntar 12 jogadores em simultâneo.

#### Foram simuladas más ligações?
Não.

#### Algum erro grave foi detetado e corrigido?
Sim, principalmente a sincronização entre objetos foi de certeza um erro grave com muita dificuldade na sua solução.



## Desafios enfrentados
#### Quais foram os principais problemas técnicos encontrados?
Sincronização entre objetos e trabalhar com permições entre jogadores e o servidor.

#### Como foram resolvidos?
Para enfrentar estes problemas tive que criar um Network Object Manager que permites os jogadores enviarem mensagens para o servidor a dizer que querem adicionar ou remover um certo objeto.

#### Que partes ficaram por terminar (se houver)?
Nenhuma.

## Conclusão
#### O que aprenderam com o projeto?
Aprendi muito sobre como usar o Netcode for Gameobject e permições entre jogadores e servidor.

#### O que fariam diferente numa nova versão?
Recriava os meus scripts pois como estava em faze de aprendizagem reparo agora que poderia ter feito de forma diferente e melhor.

## Anexos
Capturas de ecrã do jogo em funcionamento

Printscreen do código mais relevante


Link para repositório (GitHub, Drive...)
https://github.com/Valente-Coding/ModularMultiplayer

Vídeo de demonstração (se aplicável)

