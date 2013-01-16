using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using SpaceGame.utility;

namespace SpaceGame.graphics
{
    public class ParticleEffect
    {
        #region static
        //stores all data for particle effects
        public static Dictionary<string, ParticleEffectData> Data;

        static Texture2D particleTexture;
        //base texture to draw all particles with. Hardcoded single pixel assigned in Game.LoadContent
        public static Texture2D ParticleTexture
        {
            get { return particleTexture; }
            set
            { 
                particleTexture = value;
                textureCenter = new Vector2(value.Width / 2.0f, value.Height / 2.0f);
            }
        }
        //center of particle texture with Scale = 1.0f
        static Vector2 textureCenter;

        static Random rand = new Random();
        #endregion

        #region fields
        //range of angles through which particles can be spawned, in degrees
        float _arc;
        //speed with which particles are spawned, and random variance factor
        float _particleSpeed, _speedVariance;
        //fraction of speed that is reduced each second
        float _particleDecelerationFactor;
        //time particle exists
        TimeSpan _particleLife;
        //percent variance in life of particles
        float _particleLifeVariance;
        //how many particles to spawn per second 
        int _spawnRate;
        //time till spawning another particle
        TimeSpan _tillNextParticleSpawn;
        //starting scale of particles, percent variance in starting scale, and increase in scale per second
        float _particleScale, _scaleVariance, _scaleRate;
        //rotation, in radians per second
        float _particleRotationSpeed;
        //starting color, andchange in color per second, represented as 4-vectors
        Color _startColor, _endColor;
        List<Particle> _particles;
        #endregion

        #region properties
        public bool Reversed { get; set; }
        #endregion

        class Particle
        {
            public Vector2 Position, Velocity;
            public float Scale, Angle;     //size and rotation(radians)
            public TimeSpan LifeTime, TimeAlive;        //How many seconds the particle should exist and has existed
        }

        /// <summary>
        /// Create a new particle effect, based on parameters stored in ParticleEffectData.xml
        /// </summary>
        /// <param name="effectKey">string identifier used to fetch parameters. Must match Name attribute in XML</param>
        public ParticleEffect(string effectKey)
        {
            ParticleEffectData data = Data[effectKey];
            _particleSpeed = data.Speed;
            _speedVariance = data.SpeedVariance;
            _particleDecelerationFactor = data.DecelerationFactor;
            _particleScale = data.StartScale;
            _scaleVariance = data.ScaleVariance;
            _scaleRate = (data.EndScale - data.StartScale) / ((float)data.ParticleLife.TotalSeconds);
            _arc = data.SpawnArc;
            _particleLife = data.ParticleLife;
            _particleLifeVariance = data.ParticleLifeVariance;
            _particleRotationSpeed = MathHelper.ToRadians(data.ParticleRotation / (float)data.ParticleLife.TotalSeconds);
            if (data.Reversed)
            {
                _startColor = data.EndColor;
                _endColor = data.StartColor;
                Reversed = true;
            }
            else
            {
                _startColor = data.StartColor;
                _endColor = data.EndColor;
                Reversed = false;
            }
            _spawnRate = data.SpawnRate;
            _tillNextParticleSpawn = TimeSpan.FromSeconds(1.0f / (float)_spawnRate);
            _particles = new List<Particle>();
        }

        public void Update(GameTime gameTime)
        {
            for (int i = _particles.Count - 1 ; i >= 0 ; i--)
            {
                Particle particle = _particles[i];
                if (particle.LifeTime < particle.TimeAlive)
                    _particles.RemoveAt(i);
                else
                {
                    //reduce life
                    particle.TimeAlive += gameTime.ElapsedGameTime;
                    //move particle
                    particle.Position += particle.Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    //scale down velocity
                    particle.Velocity -= particle.Velocity * _particleDecelerationFactor * (float)gameTime.ElapsedGameTime.TotalSeconds;

                    if (Reversed)
                    {
                        //rotate particle
                        particle.Angle -= _particleRotationSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
                        //adjust scale
                        particle.Scale -= _scaleRate * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    }
                    else
                    {
                        //rotate particle
                        particle.Angle += _particleRotationSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
                        //adjust scale
                        particle.Scale += _scaleRate * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    }
                }
            }
        }

        private Particle newParticle(Vector2 pos, float angle, Vector2 sourceVelocity)
        {
            Particle particle = new Particle();
            particle.Position = pos;
            float directionAngle = (float)MathHelper.ToRadians((XnaHelper.RandomAngle(angle, _arc)));
            float speed = applyVariance(_particleSpeed, _speedVariance);
            particle.Velocity = speed * XnaHelper.VectorFromAngle(directionAngle) + sourceVelocity;
            particle.Scale = applyVariance(_particleScale, _scaleVariance);
            particle.Angle = 0.0f;
            particle.LifeTime = TimeSpan.FromSeconds(applyVariance((float)_particleLife.TotalSeconds, _particleLifeVariance));

            if (Reversed)
            {
                float secondsAlive = (float)particle.LifeTime.TotalSeconds;
                //start at the end
                particle.Position = particle.Position + particle.Velocity * secondsAlive; 
                //comment above and uncomment below for a cool effect (unintentional side effect while working on particles.
                //not sure why it looks so awesome, but it does)
                //particle.Position = particle.Position + particle.Velocity * secondsAlive * (1 - _particleDecelerationFactor);

                //movce in reverse
                particle.Velocity = Vector2.Negate(particle.Velocity);
                //start at end scale
                particle.Scale = _particleScale + _scaleRate * secondsAlive;
                //start at end rotation
                particle.Angle = _particleRotationSpeed * secondsAlive;
            }
            return particle;
        }

        /// <summary>
        /// Spawn new particles
        /// </summary>
        /// <param name="position">Location at which to spawn particles</param>
        /// <param name="angle">direction at which to spawn particles (degrees)</param>
        /// <param name="sourceVeloctiy">Velocity of particle source, added to all particles</param>
        /// <param name="time"></param>
        public void Spawn(Vector2 position, float angle, TimeSpan time, Vector2 sourceVelocity)
        {
            //fractional number of particles to spawn
            float particlesToSpawn = (float)(_spawnRate * (float)time.TotalSeconds);            
            //spawn integer number of particles
            for(int i = 0 ; i < (int)particlesToSpawn ; i++)
            {
                _particles.Add(newParticle(position, angle, sourceVelocity));
            }
            //now deal with fractional part
            _tillNextParticleSpawn -= TimeSpan.FromSeconds((double)(particlesToSpawn - (int)particlesToSpawn));
            if (_tillNextParticleSpawn < TimeSpan.Zero)
            {
                _particles.Add(newParticle(position, angle, sourceVelocity));
                _tillNextParticleSpawn = TimeSpan.FromSeconds(1.0f / (float)_spawnRate);
            }
        }

        private float applyVariance(float baseFloat, float variance)
        {
            return baseFloat + baseFloat * variance * (1.0f - 2 * (float)rand.NextDouble());
        }


        public void Draw(SpriteBatch sb)
        {
            foreach (Particle p in _particles)
            {
                Color drawColor;
                if (Reversed)
                    drawColor = Color.Lerp(_endColor, _startColor, (float)p.TimeAlive.TotalSeconds / (float)p.LifeTime.TotalSeconds);
                else
                    drawColor = Color.Lerp(_startColor, _endColor, (float)p.TimeAlive.TotalSeconds / (float)p.LifeTime.TotalSeconds);

                sb.Draw(ParticleTexture, p.Position, null, drawColor, p.Angle, textureCenter, p.Scale, SpriteEffects.None, 0 );
            }
        }

    }
}