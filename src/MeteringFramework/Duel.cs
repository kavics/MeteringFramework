﻿using System;

namespace MeteringFramework
{
    public class Duel : Race
    {
        public Duel(Action racer1, Action racer2, int turns, int count, Action<Progress> progress, bool parallelRacers = false)
            : base(new[] { racer1, racer2 }, turns, count, progress, parallelRacers) { }
    }
}
