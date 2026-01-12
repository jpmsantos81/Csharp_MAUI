using System;
using System.Linq;
using System.Collections.Generic;

namespace jogo
{
    public interface IVida
    {
        int Vida { get; set; }
        int VidaMaxima { get; set; }
    }
}