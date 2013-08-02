﻿namespace Mercury.ParticleEngine
{
    using System;
    using System.Collections.Generic;
    using Mercury.ParticleEngine.Modifiers;
    using Mercury.ParticleEngine.Profiles;

    public unsafe class Emitter : IDisposable
    {
        private readonly float _term;
        private float _totalSeconds;

        public Emitter(int capacity, float term, Profile profile)
        {
            _term = term;

            Buffer = new ParticleBuffer(capacity);
            Profile = profile;
            Modifiers = new List<Modifier>();
        }

        internal ParticleBuffer Buffer { get; private set; }

        public int ActiveParticles
        {
            get { return Buffer.Count; }
        }

        public IList<Modifier> Modifiers { get; private set; }
        public Profile Profile { get; private set; }
        public ReleaseParameters Parameters { get; set; }

        public void Update(float elapsedSeconds)
        {
            _totalSeconds += elapsedSeconds;

            if (Buffer.Count == 0)
                return;

            var iterator = Buffer.GetIterator();
            var particle = iterator.First;

            do
            {
                particle->Age = (_totalSeconds - particle->Inception) / _term;

                if (particle->Age > 1f)
                    break;

                particle->Position[0] += particle->Velocity[0];
                particle->Position[1] += particle->Velocity[1];
            }
            while (iterator.MoveNext(&particle));

            if (iterator.Remaining > 0)
                Buffer.Reclaim(iterator.Remaining);

            if (Buffer.Count > 0)
            {
                iterator = Buffer.GetIterator();

                foreach (var modifier in Modifiers)
                {
                    modifier.Update(ref iterator);
                    iterator.Reset();
                }
            }
        }

        public void Trigger(float x, float y)
        {
            var iterator = Buffer.Release(Randu.NextInteger(Parameters.Quantity));
            var particle = iterator.First;

            do
            {
                Profile.GetOffsetAndHeading(particle->Position, particle->Velocity);

                particle->Age = 0f;
                particle->Inception = _totalSeconds;
                
                particle->Position[0] += x;
                particle->Position[1] += y;

                var speed = Randu.NextSingle(Parameters.Speed);

                particle->Velocity[0] *= speed;
                particle->Velocity[1] *= speed;
            }
            while (iterator.MoveNext(&particle));
        }

        public void Dispose()
        {
            Buffer.Dispose();
            GC.SuppressFinalize(this);
        }

        ~Emitter()
        {
            Dispose();
        }
    }
}