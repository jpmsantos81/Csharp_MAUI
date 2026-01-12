
using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using AntsProj;

namespace AntsProj
{

    public class Tile
    {
        public string Simbolo;
        public bool Bloqueia;

        public Tile(string simbolo = "⬜", bool bloqueia = false)
        {
            Simbolo = simbolo;
            Bloqueia = bloqueia;
        }
        public virtual void Interagir(Formiga f) { }
    }

    public class Bloqueio : Tile
    {
        public Bloqueio() : base("🟫", true) { }
    }

    public class Deposito : Tile
    {
        public List<Item> Recursos = new List<Item>();

        public Deposito(string simbolo = "🔲", params Item[] recursos) : base(simbolo)
        {
            foreach (Item i in recursos)
            {
                Recursos.Add(i);
            }
        }
        public override void Interagir(Formiga f)
        {
            Item i;

            if (f.ComFome && Recursos.Where(i => i.MataFome).Count() > 0)
            {
                i = Recursos.Where(i => i.MataFome).First();
                f.PegarItem(i);
            }
            if (f.ComSede && Recursos.Where(i => i.MataSede).Count() > 0)
            {
                i = Recursos.Where(i => i.MataSede).First();
                f.PegarItem(i);
            }
        }
    }


    public class Item
    {
        public string Nome { get; protected set; }
        public string Descricao { get; protected set; }
        public bool Empilhavel { get; protected set; }
        public int Quantidade { get; protected set; }
        public bool MataFome { get; protected set; }
        public bool MataSede { get; protected set; }

        public Item(string nome, bool empilhavel, string descricao = "", int quantidade = 1, bool mataSede = false, bool mataFome = false)
        {
            Nome = nome;
            Descricao = descricao;
            Quantidade = quantidade;
            Empilhavel = empilhavel;
            MataFome = mataFome;
            MataSede = mataSede;
        }
        public virtual void Usar(Formiga f)
        {
            if (Empilhavel)
            {
                Quantidade--;
                if (Quantidade <= 0)
                { f.RemoverItem(this); }
            }
        }
        public void Adicionar(int quantidade)
        {
            Quantidade += quantidade;
        }
    }
    public class Folha : Item, IComidaBebida
    {
        public int QtdSatisfacao { get; set; }
        public Folha(int qtdSatisfacao) : base("Folha", false, $"Uma folha que satifaz {qtdSatisfacao} de fome", mataFome: true)
        {
            QtdSatisfacao = qtdSatisfacao;
        }
        public override void Usar(Formiga f)
        {
            base.Usar(f);
            f.Saciedade += QtdSatisfacao;
        }
    }
    public class Agua : Item, IComidaBebida
    {
        public int QtdSatisfacao { get; set; }
        public Agua(int qtdSatisfacao) : base("Folha", false, $"Uma quantia de agua que satifaz {qtdSatisfacao} de sede", mataSede: true)
        {
            QtdSatisfacao = qtdSatisfacao;
        }
        public override void Usar(Formiga f)
        {
            base.Usar(f);
            f.Hidratacao += QtdSatisfacao;
        }
    }


    public static class Pathfinding
    {
        public static List<(int y, int x)> BFS((int y, int x) inicio, (int y, int x) destino)
        {
            (int y, int x)[] direcoes = { (-1, 0), (1, 0), (0, -1), (0, 1) };

            Queue<(int y, int x)> fila = new();
            Dictionary<(int y, int x), (int y, int x)?> veioDe = new();

            fila.Enqueue(inicio);
            veioDe[inicio] = null;

            while (fila.Count > 0)
            {
                var atual = fila.Dequeue();

                if (atual == destino)
                    return ReconstruirCaminho(veioDe, destino);

                foreach (var dir in direcoes)
                {
                    int ny = atual.y + dir.y;
                    int nx = atual.x + dir.x;

                    // verifica limite e bloqueio  
                    if (!Mapa.PosicaoValida(ny, nx))
                        continue;

                    if (veioDe.ContainsKey((ny, nx)))
                        continue;

                    veioDe[(ny, nx)] = atual;
                    fila.Enqueue((ny, nx));
                }
            }
            return null;
        }

        private static List<(int y, int x)> ReconstruirCaminho(Dictionary<(int y, int x), (int y, int x)?> veioDe, (int y, int x) destino)
        {
            List<(int y, int x)> caminho = new();
            var atual = destino;

            while (veioDe.ContainsKey(atual))
            {
                caminho.Add(atual);
                var anterior = veioDe[atual];
                if (anterior == null)
                    break;
                atual = anterior.Value;
            }
            caminho.Reverse();
            return caminho;
        }
    }

    public static class Mapa
    {
        public static int Tamanho = 5;
        public static Tile[,] grid = new Tile[Tamanho, Tamanho];
        public static List<Formiga> Formigas = new List<Formiga>();

        public static void Inicializar()
        {
            for (int y = 0; y < Tamanho; y++)
            {
                for (int x = 0; x < Tamanho; x++)
                {
                    grid[y, x] = new Tile();
                }
            }
            new Formiga(3, 2, comFome: false).Iniciar();
            grid[3, 0] = new Bloqueio();
            grid[2, 2] = new Bloqueio();
            grid[3, 2] = new Bloqueio();
            grid[4, 2] = new Bloqueio();
            grid[1, 2] = new Bloqueio();
            grid[1, 1] = new Bloqueio();
            grid[4, 1] = new Deposito(recursos: new Item[] { new Folha(10), new Agua(20) });
        }

        public static void Mostrar()
        {
            for (int y = 0; y < Tamanho; y++)
            {
                for (int x = 0; x < Tamanho; x++)
                {
                    if (Formigas.Any(f => f.X == x && f.Y == y))
                    {
                        Console.Write(Formigas.FirstOrDefault(f => f.X == x && f.Y == y).Simbolo);
                    }
                    else { Console.Write(grid[y, x].Simbolo); }
                }
                Console.WriteLine();
            }
        }

        public static bool PosicaoValida(int Y, int X)
        {
            return X >= 0 && Y >= 0 && X < Tamanho && Y < Tamanho && !grid[Y, X].Bloqueia;
        }

        public static void Atualizar()
        {
            Program.Tick?.Invoke();
            Mostrar();
        }

        public static int MedirDistancia(int PosX1, int PosY1, int PosX2, int PosY2)
        {
            return Math.Abs(PosX1 - PosX2) + Math.Abs(PosY1 - PosY2);
        }
    }
    public class Formigueiro : Tile
    {
        public List<Formiga> FormigasDentro = new List<Formiga>();

    }


    public class Formiga
    {
        public string Simbolo;
        public int X = 0;
        public int Y = 0;
        public (int x, int y)? Objetivo;
        public int Saciedade = 100;
        public int Hidratacao = 100;
        public bool ComFome = false;
        public bool ComSede = false;
        public bool PodeAndar = true;
        public bool Viva = true;
        public int DelayPraAndar = 0;
        public int DelayCount = 0;
        public event Action<Formiga> ActFormigaMorreu;
        public int TempoMorta = 0;
        public List<Formiga> FormigasProximas = new List<Formiga>();
        public List<Item> Inventario = new List<Item> { new Folha(500) };
        public Estado EstadoAtual = Estado.Satisfeita;
        public List<string> Sentimentos = new List<string>();
        public enum dir
        {
            Cima,
            Baixo,
            Esquerda,
            Direita,
            Parada
        }
        public enum Estado
        {
            ProcurarComida,
            ProcurarBebida,
            AjudarFormiga,
            Satisfeita,
            ProcurarRecursos,
            Construir,
            PrecidaDeAjuda
        }
        public Formiga(int x, int y, string simbolo = "🐜", bool comSede = false, bool comFome = false)
        {
            X = x;
            Y = y;
            Simbolo = simbolo;
            if (comSede == true) { Hidratacao = 34; ComSede = true; }
            if (comFome == true) { Saciedade = 34; ComFome = true; }
        }
        public void Iniciar()
        {
            Mapa.Formigas.Add(this);
            Program.Tick += Viver;

        }
        public void Viver()
        {
            if (Viva)
            {
                VerFormigasProximas();
                FomeESede();
                if (EstadoAtual != Estado.AjudarFormiga)
                {
                    Comer();
                    foreach (string i in Sentimentos)
                    { Console.WriteLine(i); }
                    foreach ((int, int) i in Radar(EstadoAtual))
                    { Console.WriteLine(i); }
                }
                Mover(OndeIr().x, OndeIr().y);
            }
            else
            {
                Simbolo = "💀";
                ActFormigaMorreu?.Invoke(this);
                Morta();
            }
        }
        public void FomeESede()
        {
            Hidratacao--;
            Saciedade--;
            if (Saciedade < 35) { ComFome = true; } else { ComFome = false; }
            if (Hidratacao < 35) { ComSede = true; } else { ComSede = false; }
            if (Hidratacao == 0 && Saciedade == 0)
            {
                Viva = false;
            }
        }

        public void Comer()
        {
            if (ComFome && Inventario.Where(i => i.MataFome).OfType<IComidaBebida>().ToList().Count() > 0)
            {
                Inventario.OfType<IComidaBebida>().OrderBy(c => Math.Abs(c.QtdSatisfacao + Saciedade - 60)).FirstOrDefault().Usar();
                Sentimentos.Remove("Com fome e sem comida");
            }
            else if (ComFome && Inventario.Where(i => i.MataFome).OfType<IComidaBebida>().ToList().Count() == 0 && !Sentimentos.Contains("Com fome e sem comida"))
            {
                Sentimentos.Add("Com fome e sem comida");
            }

            if (ComSede && Inventario.Where(i => i.MataSede).OfType<IComidaBebida>().ToList().Count() > 0)
            {
                Inventario.OfType<IComidaBebida>().OrderBy(c => Math.Abs(c.QtdSatisfacao + Hidratacao - 60)).FirstOrDefault().Usar();
                Sentimentos.Remove("Com sede e sem bebida");
            }
            else if (ComSede && Inventario.Where(i => i.MataSede).OfType<IComidaBebida>().ToList().Count() == 0 && !Sentimentos.Contains("Com sede e sem bebida"))
            {
                Sentimentos.Add("Com sede e sem bebida");
            }
        }

        public void VerFormigasProximas()
        {
            FormigasProximas = Mapa.Formigas.OrderBy(f => { return DistanciaAte(f.X, f.Y); }).Where(f => f != this).Take(3).ToList();
        }

        public List<(int x, int y)> Radar(Estado e)
        {
            List<(int x, int y)> Coordenadas = new List<(int x, int y)>();

            for (int y = 0; y < Mapa.Tamanho; y++)
            {
                for (int x = 0; x < Mapa.Tamanho; x++)
                {
                    switch (e)
                    {
                        case Estado.ProcurarComida:
                            if (Mapa.grid[y, x] is Deposito dc && dc.Recursos.Any(i => i.MataFome && i is IComidaBebida))
                            {
                                Coordenadas.Add((x, y));
                            }
                            break;
                        case Estado.ProcurarBebida:
                            if (Mapa.grid[y, x] is Deposito db && db.Recursos.Any(i => i.MataSede && i is IComidaBebida))
                            {
                                Coordenadas.Add((x, y));
                            }
                            break;
                    }
                }
            }
            return Coordenadas;
        }

        public (int x, int y) OndeIr()
        {
            return (0, 4);
        }

        public void Mover(int x, int y)
        {
            if (Objetivo != null && X == Objetivo.Value.x && Y == Objetivo.Value.y)
            {
                Objetivo = null; // reset  
                Mapa.grid[Y, X].Interagir(this);
                return;
            }
            if (ComFome && ComSede) { DelayPraAndar = 2; }
            else if (ComFome || ComSede) { DelayPraAndar = 1; }
            else { DelayPraAndar = 0; DelayCount = 0; }
            if (DelayCount < DelayPraAndar) { DelayCount++; return; }

            var inicio = (Y, X);
            var destino = (y, x);
            var caminho = Pathfinding.BFS(inicio, destino);
            if (caminho != null && caminho.Count > 1)
            {
                var proximo = caminho[1];
                if (Mapa.PosicaoValida(proximo.y, proximo.x))
                {
                    Y = proximo.y;
                    X = proximo.x;
                }
            }
            else
            {

            }
        }
        private bool TemCaminho((int y, int x) inicio, (int y, int x) destino)
        {
            var caminho = Pathfinding.BFS(inicio, destino);
            return caminho != null && caminho.Count > 1;
        }

        public void Morta()
        {
            if (TempoMorta == 10)
            {
                Mapa.Formigas.Remove(this);
                Program.Tick -= Viver;
            }
            else
            {
                TempoMorta++;
            }
        }

        public int DistanciaAte(int PosX, int PosY)
        {
            return Math.Abs(X - PosX) + Math.Abs(Y - PosY);
        }

        public Estado ObjetivoAtual()
        {
            List<Estado> ObjetivosPossiveis = new List<Estado>();
            if (ComFome) { ObjetivosPossiveis.Add(Estado.ProcurarComida); }
            if (ComSede) { ObjetivosPossiveis.Add(Estado.ProcurarBebida); }
            if (ObjetivosPossiveis.Contains(Estado.ProcurarComida) || ObjetivosPossiveis.Contains(Estado.ProcurarBebida))
            {
                ObjetivosPossiveis = ObjetivosPossiveis.OrderByDescending(e =>
                {
                    int Necessidade;
                    switch (e)
                    {
                        case Estado.ProcurarComida:
                            Necessidade = Saciedade;
                            break;
                        case Estado.ProcurarBebida:
                            Necessidade = Hidratacao;
                            break;
                        default:
                            Necessidade = 0;
                            break;
                    }
                    return Necessidade;
                }).ToList();
            }
            else
            {

            }
            return ObjetivosPossiveis.First();
        }

        public void PegarItem(Item item)
        {
            if (item.Empilhavel)
            {
                var itemEncontrado = Inventario.FirstOrDefault(i => i.Nome.ToLower() == item.Nome.ToLower());
                if (itemEncontrado != null)
                {
                    itemEncontrado.Adicionar(item.Quantidade);
                }
                else
                {
                    Inventario.Add(item);
                }
            }
            else
            {
                Inventario.Add(item);
            }
        }
        public void RemoverItem(Item item)
        {
            Inventario.Remove(item);
        }


    }
    public class Program
    {
        public static CancellationTokenSource cts = new CancellationTokenSource();
        public static Action Tick;
        public static bool Pausado = true;

        public static void Main()
        {
            Thread t = new Thread(Segundos);
            t.Start();

            Mapa.Inicializar();

            while (true)
            {
                Pausado = true;
                Console.WriteLine("Quantos loops você quer rodar?");
                string escolha = Console.ReadLine();
                //Pausado = false; pra testes  

                if (escolha == "0") { break; }
                else
                {
                    for (int i = 0; i < int.Parse(escolha); i++)
                    {
                        Thread.Sleep(00);
                        Console.Clear();
                        Mapa.Atualizar();
                    }
                }
            }

            cts.Cancel();
        }//fim do Main  


        public static void Segundos()
        {
            while (!cts.IsCancellationRequested)
            {
                if (!Pausado)
                {
                    cts.Token.WaitHandle.WaitOne(1000);
                    Tick?.Invoke();
                }
            }
        }

    }//fim do program

}//fim do namespace