using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using static jogo.Mapa;
using System.Threading;
using System.Security.Cryptography.Pkcs;
using jogo;

namespace jogo
{
    public static class Preset
    {
        public static void EsperarSegundos(int s)
        {
            Thread.Sleep(s * 1000);
        }
        public static Queue<string> MensagensPendentes { get; private set; } = new Queue<string>();
        public static void DefinirMensagem(string msg)
        {
            if (!string.IsNullOrWhiteSpace(msg))
            {
                MensagensPendentes.Enqueue(msg);
            }
        }
        public static void MostrarMensagemSeHouver()
        {
            while (MensagensPendentes.Count > 0)
            {
                string msg = MensagensPendentes.Dequeue();
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("==================================");
                Console.WriteLine(msg);
                Console.WriteLine("==================================");
                Console.ResetColor();
            }
        }
        public static void MsgReceberItem(string nome, string descricao, int quantidade)
        {
            DefinirMensagem($"Você conseguiu {nome} (x{quantidade})\n=== Descrição ===\n{descricao}");
        }
        public static void Aviso(string aviso)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(aviso);
            Console.ResetColor();
            Preset.EsperarSegundos(2);
        }

    }
    public enum TipoCelula
    {
        Inimigo,
        Caminho,
        Obstaculo,
        Vazio,
        Item,
        Bau
    }
    public enum EstiloCelula
    {
        Agua,
        Fogo,
        Chefao,
        Pedra,
        Normal,
        Especial
    }

    public class Celula
    {
        public TipoCelula Tipo { get; protected set; }
        public EstiloCelula Estilo { get; protected set; }
        public string Simbolo { get; protected set; }
        public bool Bloqueia { get; protected set; }
        public virtual void Interagir()
        { }

        public Celula(TipoCelula tipo = TipoCelula.Caminho, EstiloCelula estilo = EstiloCelula.Normal, string simbolo = "", bool bloqueia = false)
        {
            Tipo = tipo;
            Estilo = estilo;
            Bloqueia = bloqueia;
            if (simbolo == "")
            {
                if (Tipo == TipoCelula.Inimigo)
                {
                    if (Estilo == EstiloCelula.Chefao)
                        Simbolo = "🟥";
                    else
                        Simbolo = "⬛";
                }
                else if (Tipo == TipoCelula.Caminho)
                {
                    if (Estilo == EstiloCelula.Agua)
                        Simbolo = "🟦";
                    else
                        Simbolo = "⬜";
                }
                else if (Tipo == TipoCelula.Obstaculo)
                {
                    if (Estilo == EstiloCelula.Pedra)
                        Simbolo = "🟫";
                    else
                        Simbolo = "⬛";
                }
                else if (Tipo == TipoCelula.Item)
                {
                    if (Estilo == EstiloCelula.Especial)
                        Simbolo = "💰";
                    else
                        Simbolo = "⭐";
                }
                else if (Tipo == TipoCelula.Bau)
                {
                    Simbolo = "📦";
                }
                else
                {
                    Simbolo = "⬜";
                }
            }
            else
            {
                Simbolo = simbolo;
            }
        }
    }

    public class Bau : Celula
    {
        private Jogador player;
        private List<Item> itemDentro = new List<Item>();
        private bool aberto = false;

        public Bau(Jogador player, params Item[] itens) : base(TipoCelula.Bau, EstiloCelula.Normal)
        {
            foreach (var item in itens)
            {
                itemDentro.Add(item);
            }
            this.player = player;
        }

        public override void Interagir()
        {
            if (aberto == false)
            {
                List<string> itens = new List<string> { "Você conseguiu os seguintes itens:" };
                foreach (Item item in itemDentro)
                {
                    player.PegarItem(item);
                    itens.Add($"{item.Nome} (x{item.Quantidade})\n{item.Descricao}");
                }
                Preset.DefinirMensagem(String.Join("\n\n", itens));
                aberto = true;
            }
            else
            {
                Preset.DefinirMensagem("Este bau já foi aberto");
            }
        }
    }

    public class ItemLargado : Celula
    {
        private Jogador player;
        private Item item;
        private string pegar;

        public ItemLargado(Jogador Player, Item Item) : base(TipoCelula.Item, EstiloCelula.Normal, Item.Simbolo)
        {
            player = Player;
            item = Item;
        }

        public override void Interagir()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"===== {item.Nome} =====\nQuantidade:{item.Quantidade}\n{item.Descricao}\n=====");
            for (int i = 0; i < item.Nome.Length; i++)
            {
                Console.Write("=");
            }
            Console.Write("=======\n");
            Console.ResetColor();

            Console.WriteLine($"Pegar item? s/n");
            pegar = Console.ReadLine().ToLower();
            if (pegar == "s" || pegar == "sim")
            {
                player.PegarItem(item);
                Mapa.grid[player.novoY, player.novoX] = new Celula(TipoCelula.Caminho, EstiloCelula.Normal);
            }
        }
    }

    public class Bloqueio : Celula
    {
        private Jogador player;
        public Bloqueio(Jogador Player) : base(TipoCelula.Obstaculo, EstiloCelula.Pedra, bloqueia: true)
        {
            player = Player;
        }
        public override void Interagir()
        {
            player.podeAndar = false;
        }
    }

    public class InimigoGatilho : Celula
    {
        int vida;
        int vidaMaxima;
        int x;
        int y;
        string nome;
        Jogador p;

        public InimigoGatilho(Jogador Player, string nome, int vida, int vidaMaxima, int x, int y) : base(TipoCelula.Inimigo, EstiloCelula.Normal)
        {
            this.nome = nome;
            this.vida = vida;
            this.vidaMaxima = vidaMaxima;
            this.nome = nome;
            this.x = x;
            this.y = y;
            p = Player;
        }
        public override void Interagir()
        {
            InimigoPersegue i = new InimigoPersegue(p, nome, vida, vidaMaxima, x, y);
            Inimigos.Add(i);
            i.StartThreads();
            Mapa.grid[p.novoY, p.novoX] = new Celula(TipoCelula.Caminho, EstiloCelula.Normal);
        }
    }

    public class GerarInimigo : Celula
    {
        private List<InimigoPersegue> inimigos = new List<InimigoPersegue> { };
        Jogador p;

        public GerarInimigo(Jogador Player, params InimigoPersegue[] inimigos) : base(TipoCelula.Inimigo, EstiloCelula.Normal)
        {
            foreach (InimigoPersegue i in inimigos)
            {
                this.inimigos.Add(i);
            }
            p = Player;
        }

        public override void Interagir()
        {
            foreach (InimigoPersegue i in inimigos)
            {
                new InimigoPersegue(i.p, i.Nome, i.Vida, i.VidaMaxima, i.X, i.Y);
                Inimigos.Add(i);
                i.StartThreads();
            }
            Mapa.grid[p.novoY, p.novoX] = new Celula(TipoCelula.Caminho, EstiloCelula.Normal);
        }
    }

    public class Item
    {
        public string Nome { get; protected set; }
        public string Descricao { get; protected set; }
        public bool Empilhavel { get; protected set; }
        public int Quantidade { get; protected set; }
        public string Simbolo { get; protected set; }

        public Item(string nome, bool empilhavel, string descricao = "", int quantidade = 1, string simbolo = "")
        {
            Nome = nome;
            Descricao = descricao;
            Quantidade = quantidade;
            Empilhavel = empilhavel;
            Simbolo = simbolo;
        }
        public virtual void Usar(Jogador player)
        {
            if (Empilhavel)
            {
                Quantidade--;
                if (Quantidade <= 0)
                { player.RemoverItem(this); }
            }

        }
        public void Adicionar(int quantidade)
        {
            Quantidade += quantidade;
        }
    }

    public class PocaoDeCura : Item
    {
        public int QuantidadeDeCura { get; protected set; }
        public string Nivel { get; protected set; }

        public PocaoDeCura(int quantidadeDeCura, string nivel, int quantidade = 1) : base($"Poção de Cura {nivel}", true, $"Uma poção que regenera {quantidadeDeCura} de vida", quantidade, "🌡")
        {
            QuantidadeDeCura = quantidadeDeCura;
            Nivel = nivel;
        }
        public override void Usar(Jogador player)
        {
            base.Usar(player);
            player.Vida += QuantidadeDeCura;
        }
    }

    public static class Mapa
    {
        public static int tamanho = 9;
        public static Celula[,] grid = new Celula[tamanho, tamanho];
        public static List<InimigoPersegue> Inimigos = new List<InimigoPersegue> { };


        public static void Inicializar(Jogador player)
        {
            for (int y = 0; y < tamanho; y++)
            {
                for (int x = 0; x < tamanho; x++)
                {
                    grid[y, x] = new Celula(TipoCelula.Caminho, EstiloCelula.Normal);
                }
            }
            //posicoes
            grid[1, 0] = new Bau(player, new PocaoDeCura(50, "pequena"), new PocaoDeCura(70, "arrox"));
            grid[0, 5] = new ItemLargado(player, new PocaoDeCura(60, "teste", 6));
            grid[0, 2] = new GerarInimigo(player, new InimigoPersegue(player, "Perseguidor", 59, 59, 6, 6), new InimigoPersegue(player, "Perseguidora", 59, 59, 4, 2));
            grid[2, 3] = new Bloqueio(player);

        }
        public static bool PosicaoValida(int X, int Y)
        {
            return X >= 0 && Y >= 0 && X < tamanho && Y < tamanho;
        }

        public static void MostrarMapa(Jogador player)
        {
            for (int y = 0; y < tamanho; y++)
            {
                for (int x = 0; x < tamanho; x++)
                {
                    if (Inimigos.Any(i => i.X == x && i.Y == y))
                    {
                        Console.Write("🟥" + " ");
                    }
                    else if (x == player.X && y == player.Y)
                    {
                        Console.Write("🔳" + " ");
                    }
                    else
                    {
                        Console.Write(grid[y, x].Simbolo + " ");
                    }
                }
                Console.WriteLine();
            }
        }

        public static void IniciarBatalha(Jogador p, InimigoPersegue i)
        {

        }
    }

    public class InimigoPersegue : IVida
    {
        public string Nome { get; set; }
        public int X;
        public int Y;
        public int NovoX;
        public int NovoY;
        public Jogador p;
        public Thread Tperseguicao;
        public Thread Tplayerserapego;
        public static bool ThreadsProntas = false;
        private List<dir> DirecoesPrioritarias = new List<dir> { };
        private int vidaMaxima = 100;
        public int VidaMaxima
        {
            get { return vidaMaxima; }
            set
            {
                if (value <= 0)
                {
                    value = 1;
                }
                vidaMaxima = value;
            }
        }
        private int vida = 100;
        public int Vida
        {
            get { return vida; }
            set
            {
                if (value > vidaMaxima)
                {
                    value = vidaMaxima;
                }
                else if (value < 0)
                {
                    value = 0;
                }
                vida = value;
            }
        }

        public InimigoPersegue(Jogador p, string nome, int vida, int vidaMaxima, int x, int y)
        {
            Nome = nome;
            X = x;
            Y = y;
            VidaMaxima = vidaMaxima;
            Vida = vida;
            this.p = p;
        }

        public void StartThreads()
        {
            if (!ThreadsProntas)
            {
                Tperseguicao = new Thread(() => Program.Perseguicao(p, Program.cts.Token));
                Tperseguicao.Start();
                Tplayerserapego = new Thread(() => Program.PlayerSerPego(p, Program.cts.Token));
                Tplayerserapego.Start();
                ThreadsProntas = true;
            }
        }

        public enum dir
        {
            cima,
            baixo,
            esquerda,
            direita,
            meio
        }
        public dir OndeIr()
        {
            DirecoesPrioritarias.Clear();
            if (p.Y < Y) DirecoesPrioritarias.Add(dir.cima);
            if (p.Y > Y) DirecoesPrioritarias.Add(dir.baixo);
            if (p.X > X) DirecoesPrioritarias.Add(dir.direita);
            if (p.X < X) DirecoesPrioritarias.Add(dir.esquerda);

            dir direcaoEscolhida = DirecoesPrioritarias[new Random().Next(DirecoesPrioritarias.Count)];
            return direcaoEscolhida;
        }
        public void Mover(dir dir)
        {
            NovoX = X;
            NovoY = Y;
            switch (dir)
            {
                case dir.cima:
                    NovoY--;
                    break;
                case dir.baixo:
                    NovoY++;
                    break;
                case dir.esquerda:
                    NovoX--;
                    break;
                case dir.direita:
                    NovoX++;
                    break;
                default:
                    X = NovoX;
                    break;
            }
            if (PosicaoValida(NovoX, NovoY) && !grid[NovoY, NovoX].Bloqueia)
            {
                X = NovoX;
                Y = NovoY;
            }
        }
        public void Batalhar(Jogador player)
        {
            Console.Clear();
            Console.WriteLine($"====== Batalha Contra {Nome} ======");
            Console.WriteLine($"Sua Vida: {player.Vida}\nSua Energia: {player.Energia}");
        }

    }

    public class Jogador : IVida
    {
        public string Nome { get; set; }
        private int vidaMaxima = 100;
        public int VidaMaxima
        {
            get { return vidaMaxima; }
            set
            {
                if (value <= 0)
                {
                    value = 1;
                }
                vidaMaxima = value;
            }
        }
        private int vida = 100;
        public int Vida
        {
            get { return vida; }
            set
            {
                if (value > vidaMaxima)
                {
                    value = vidaMaxima;
                }
                else if (value < 0)
                {
                    value = 0;
                }
                vida = value;
            }
        }
        private int energia = 100;
        public int Energia
        {
            get { return energia; }
            set
            {
                if (value > energiaMaxima)
                {
                    value = energiaMaxima;
                }
                else if (value < 0)
                {
                    value = 0;
                }
                energia = value;
            }
        }
        private int energiaMaxima = 100;
        public int EnergiaMaxima
        {
            get { return energiaMaxima; }
            set
            {
                if (value <= 0)
                {
                    value = 1;
                }
                energiaMaxima = value;
            }
        }
        public bool podeAndar = true;
        public int X { get; private set; } = 0;
        public int Y { get; private set; } = 0;
        public int novoX;
        public int novoY;
        public string Senha { get; set; }
        public string Dica;
        public string RespostaDaDica;
        public List<Item> inventario { get; private set; } = new List<Item> { };
        public void Mover(string direcao)
        {
            novoX = X;
            novoY = Y;
            switch (direcao.ToLower())
            {
                case "w": // cima
                    novoY--;
                    break;
                case "s": // baixo
                    novoY++;
                    break;
                case "a": // esquerda
                    novoX--;
                    break;
                case "d": // direita
                    novoX++;
                    break;
            }
            if (PosicaoValida(novoX, novoY))
            {
                grid[novoY, novoX].Interagir();
                if (podeAndar)
                {
                    X = novoX;
                    Y = novoY;
                }
                else
                {
                    Preset.DefinirMensagem("Não é possivel andar para este local");
                    podeAndar = true;
                }
            }
            else
            {
                Console.WriteLine("Irmão, não dá pra passar pra fora do mapa");
                Preset.EsperarSegundos(2);
            }
        }

        public void MexerNoInventario()
        {
            Console.Clear();
            MostrarInventario();
            Console.WriteLine($"{inventario.Count + 1} – Sair");
            string escolha = Console.ReadLine();
            int entrada = 0;

            try
            {
                entrada = int.Parse(escolha);
                if (int.Parse(escolha) <= QuantidadeDeItensNoInventario())
                {
                    Console.Clear();
                    MostrarItem(entrada - 1);

                    Console.WriteLine("1 - usar\n2 - remover\n3 - largar");
                    escolha = Console.ReadLine();
                    if (escolha == "1")
                    {
                        UsarItem(entrada - 1);
                    }
                    else if (escolha == "2")
                    {
                        RemoverItem(inventario[entrada - 1]);
                    }
                    else if (escolha == "3")
                    {
                        grid[Y, X] = new ItemLargado(this, inventario[entrada - 1]);
                        RemoverItem(inventario[entrada - 1]);
                    }
                }
            }
            catch
            {
                Preset.Aviso("Tem que digitar número, pô!");
            }
        }

        public void PegarItem(Item item)
        {
            if (item.Empilhavel)
            {
                var itemEncontrado = inventario.FirstOrDefault(i => i.Nome.ToLower() == item.Nome.ToLower());
                if (itemEncontrado != null)
                {
                    itemEncontrado.Adicionar(item.Quantidade);
                }
                else
                {
                    inventario.Add(item);
                }
            }
            else
            {
                inventario.Add(item);
            }
        }
        public void RemoverItem(Item item)
        {
            inventario.Remove(item);
        }

        public void Status()
        {
            Console.WriteLine($"Nome: {Nome}\nVida: {Vida}\r");
            //Inventario
            if (inventario.Count == 0) Console.WriteLine("O inventario esta vazio");
            else if (inventario.Count == 1) Console.WriteLine("O inventario tem 1 item");
            else Console.WriteLine($"O inventario tem {inventario.Count} itens");
        }
        public void MostrarInventario()
        {
            for (int i = 0; i < inventario.Count; i++)
            {
                var item = inventario[i];
                Console.Write($"{i + 1} – {item.Nome}");
                if (item.Empilhavel)
                {
                    Console.Write($"(x{item.Quantidade})\n");
                }
                else { Console.WriteLine(); }
            }
        }
        public void MostrarItem(int indice)
        {
            Item i = inventario[indice];
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"===== {i.Nome} =====\n{i.Descricao}\n=====");
            for (int l = 0; l < i.Nome.Length; l++)
            {
                Console.Write("=");
            }
            Console.Write("=======\n");
            Console.ResetColor();
        }
        public void UsarItem(int indice)
        {
            inventario[indice].Usar(this);
        }

        public int QuantidadeDeItensNoInventario()
        {
            return inventario.Count();
        }

        public void Save()
        {
            Directory.CreateDirectory($"saves/{Nome}");
            string caminho = $"saves/{Nome}/{Nome}save.txt";

            File.WriteAllText(caminho, $"{Nome}\n{Vida}@");
            foreach (Item item in inventario)
            {
                //   Item itm = new Item("nome", "empilhavel?", "descricao");
                File.AppendAllText(caminho, $"{item.GetType().Name},,");
                if (item is PocaoDeCura pocao)
                {
                    File.AppendAllText(caminho, $"{pocao.QuantidadeDeCura},,{pocao.Nivel},,{pocao.Quantidade}\n");
                }
            }
            File.AppendAllText(caminho, $"@{Senha}\n{Dica}\n{RespostaDaDica}@");

        }
        public void Carregar()
        {
            string[] linhas = File.ReadAllLines($"saves/{Nome}/{Nome}save.txt");
            string arquivo = String.Join(";;", linhas);
            string[] partes = arquivo.Split("@");
            string[] partePlayer = partes[0].Split(";;");
            string[] parteInventario = partes[1].Split(";;");
            string[] parteSenha = partes[2].Split(";;");

            Nome = partePlayer[0];
            Vida = int.Parse(partePlayer[1]);

            try
            {
                Senha = parteSenha[0];
                Dica = parteSenha[1];
                RespostaDaDica = parteSenha[2];
            }
            catch { }

            inventario.Clear();
            for (int i = 0; i < parteInventario.Length; i++)
            {
                string[] infoItem = parteInventario[i].Split(",,");
                if (infoItem[0] == "PocaoDeCura")
                {
                    PegarItem(new PocaoDeCura(int.Parse(infoItem[1]), infoItem[2], int.Parse(infoItem[3])));
                }

            }
        }
    }


    public static class Program
    {
        static bool InimigoSolto = false;
        static bool InimigoPegou = false;
        static readonly object console = new object();
        public static CancellationTokenSource cts = new CancellationTokenSource();
        public static void Main()
        {

            Jogador player1 = new Jogador();
            Jogador save = new Jogador();
            Console.Clear();
            string escolha = "reescrever";
            while (escolha == "reescrever")
            {
                Console.Write("Qual seu nome?\n:");
                player1.Nome = Console.ReadLine();

                while (string.IsNullOrWhiteSpace(player1.Nome))
                {
                    Console.Write("Digite um nome válido:\n:");
                    player1.Nome = Console.ReadLine();
                }

                if (File.Exists($"saves/{player1.Nome}/{player1.Nome}save.txt"))
                {
                    save.Nome = player1.Nome;
                    save.Carregar();
                    Console.Write("\nEncontramos dados salvos em seu nome, quer continuar?\n\ndigite reescrever para colocar outro nome\n:");
                    escolha = Console.ReadLine().ToLower();

                    if (!(escolha == "reescrever"))
                    {
                        if (string.IsNullOrEmpty(save.Senha))
                        {
                            player1.Carregar();
                            Console.Clear();
                            Console.WriteLine("Você não tem uma senha\ncrie uma ou deixe vazio pra continuar");
                            player1.Senha = Console.ReadLine();
                            if (!string.IsNullOrEmpty(player1.Senha))
                            {
                                Console.Clear();
                                Console.WriteLine("Crie uma dica pra caso esquecer da senha\ne precisar entrar de forma alternativa\nde preferência a algo que só você sabe\n");
                                Console.Write("Dica:");
                                player1.Dica = Console.ReadLine().ToLower();
                                Console.Write("\nResposta:");
                                player1.RespostaDaDica = Console.ReadLine().ToLower();
                            }
                        }
                        else
                        {
                            int tentativa = 0;
                            string senha = "";
                            do
                            {
                                tentativa++;
                                Console.Clear();
                                Console.Write("Digite sua senha\n:");
                                senha = Console.ReadLine();
                                if (senha == save.Senha)
                                {
                                    Console.Clear();
                                    Console.WriteLine("s ou sim – continuar\nqualquer coisa – novo jogo");
                                    escolha = Console.ReadLine();
                                    if (escolha == "s" || escolha == "sim") player1.Carregar();
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("senha incorreta");
                                    Console.ResetColor();
                                    Thread.Sleep(500);
                                    if (tentativa > 2)
                                    {
                                        Console.Clear();
                                        Console.Write($"tentativas esgotadas\ntentando entrar de forma alternativa\n\nDica: {save.Dica}\nDigite a resposta\n:");
                                        escolha = Console.ReadLine();
                                        if (escolha == save.RespostaDaDica)
                                        {
                                            player1.Carregar();
                                            break;
                                        }
                                        else
                                        {
                                            Console.Clear();
                                            escolha = "reescrever";
                                            break;
                                        }
                                    }
                                }
                            } while (senha != save.Senha);
                        }
                    }
                }
                else
                {
                    escolha = "";
                    Console.Clear();
                    Console.WriteLine("Você não tem uma senha\ncrie uma ou deixe vazio pra continuar");
                    player1.Senha = Console.ReadLine();
                    if (!string.IsNullOrEmpty(player1.Senha))
                    {
                        Console.Clear();
                        Console.WriteLine("Crie uma dica pra sua senha alternativa\n");
                        Console.Write("Dica:");
                        player1.Dica = Console.ReadLine().ToLower();
                        Console.Write("\nSenha alternativa: ");
                        player1.RespostaDaDica = Console.ReadLine().ToLower();
                    }
                }
                Console.Clear();
            }

            Console.Clear();
            player1.Status();

            Inicializar(player1);
            Mapa.MostrarMapa(player1);
            while (true)
            {
                Console.WriteLine($"wasd – Mover \ni – Inventario\nsave - Salvar Jogo");
                Console.WriteLine("sair – Sair do jogo");
                string acao = Console.ReadLine().ToLower();
                if (acao == "w" || acao == "a" || acao == "s" || acao == "d")
                {
                    lock (console)
                    {
                        player1.Mover(acao);
                    }
                }
                else if (acao == "i")
                {
                    lock (console)
                    {
                        player1.MexerNoInventario();
                    }
                }
                else if (acao == "save")
                {
                    player1.Save();
                }
                else if (acao == "sair")
                {
                    lock (console)
                    {
                        Console.Clear();
                        Console.Write("salvar antes de sair? s ou save\n:");
                        escolha = Console.ReadLine().ToLower();
                        if (escolha == "s" || escolha == "save")
                        {
                            player1.Save();
                            Console.Write("jogo salvo com sucesso");
                            Thread.Sleep(2000);
                        }
                        cts.Cancel();
                        return;
                    }
                }
                else
                {
                    if (Inimigos.Count == 0)
                    {
                        Console.WriteLine("num entendi, tenta de novo");
                        Preset.EsperarSegundos(1);
                    }
                }

                lock (console)
                {
                    Console.Clear();
                    player1.Status();
                    MostrarMapa(player1);
                    Preset.MostrarMensagemSeHouver();
                }
            }// fim do while do jogo
        } //fim do Main

        public static void Perseguicao(Jogador p, CancellationToken token)
        {
            while (!token.IsCancellationRequested && Inimigos.Count > 0)
            {

                if (Inimigos.FirstOrDefault(i => p.X == i.X && p.Y == i.Y) == null)
                {
                    int inimigosMexendo = 0;
                    token.WaitHandle.WaitOne(5000);
                    if (!InimigoPegou)
                    {
                        lock (console)
                        {
                            foreach (InimigoPersegue i in Inimigos)
                            {
                                i.Mover(i.OndeIr());
                                if (!grid[i.NovoY, i.NovoX].Bloqueia)
                                {
                                    inimigosMexendo++;
                                }
                            }
                            if (inimigosMexendo != 0)
                            {
                                Console.Clear();
                                p.Status();
                                MostrarMapa(p);
                                Preset.MostrarMensagemSeHouver();
                                InimigoSolto = true;
                            }
                        }
                    }
                }
            }
            if (Inimigos.Count == 0) { InimigoPersegue.ThreadsProntas = false; }
        }

        public static void PlayerSerPego(Jogador p, CancellationToken token)
        {
            InimigoPersegue i;
            while (!token.IsCancellationRequested && Inimigos.Count > 0)
            {

                token.WaitHandle.WaitOne(500);
                i = Inimigos.FirstOrDefault(ini => ini.X == p.X && ini.Y == p.Y);
                if (i != null)
                {
                    if (!InimigoPegou)
                    {
                        lock (console)
                        {
                            InimigoPegou = true;
                            i.Batalhar(p);
                            Inimigos.Remove(i);
                            InimigoPegou = false;
                            token.WaitHandle.WaitOne(5000);
                        }
                    }
                }
            }
            if (Inimigos.Count == 0) { InimigoPersegue.ThreadsProntas = false; }
        }

    } // fim do Program
}// fim do namespace