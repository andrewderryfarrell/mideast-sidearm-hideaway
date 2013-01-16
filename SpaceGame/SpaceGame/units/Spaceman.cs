﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SpaceGame.graphics;

namespace SpaceGame.units
{
    /// <summary>
    /// Player character
    /// </summary>
    class Spaceman : PhysicalUnit
    {
        #region constants
        const string SPACEMAN_NAME = "Spaceman";
        const string THRUSTER_EFFECT_NAME = "SpacemanThruster";
        #endregion

        #region members
        ParticleEffect thrusterParticleEffect;
        #endregion

        public Spaceman(Vector2 startPosition)
            :base(SPACEMAN_NAME, startPosition)
        {
            thrusterParticleEffect = new ParticleEffect(THRUSTER_EFFECT_NAME);
        }

    }
}