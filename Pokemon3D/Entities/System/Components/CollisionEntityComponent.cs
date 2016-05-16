﻿using System;
using Microsoft.Xna.Framework;
using Pokemon3D.Collisions;
using static Pokemon3D.GameCore.GameProvider;

namespace Pokemon3D.Entities.System.Components
{
    [JsonComponentId("collision")]
    class CollisionEntityComponent : EntityComponent
    {
        public Collider Collider { get; }
        public bool ResolvesPosition { get; set; }

        public CollisionEntityComponent(EntityComponentDataCreationStruct structData) : base(structData)
        {
            Collider = new Collider(GetData<Vector3>("CollisionSize"), GetData<Vector3>("CollisionOffset"));
        }

        public CollisionEntityComponent(Entity parent, Vector3 collisionSize, Vector3 collisionOffset) : base(parent)
        {
            Collider = new Collider(collisionSize, collisionOffset);
        }

        public override void OnComponentAdded()
        {
            base.OnComponentAdded();
            GameInstance.CollisionManager.Add(Collider);
        }

        public override void OnComponentRemove()
        {
            throw new NotImplementedException("Needs to be handled properly.");
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            Collider.SetPosition(Parent.Position);

            if (!ResolvesPosition) return;
            var collisionResult = GameInstance.CollisionManager.CheckCollision(Collider);
            if (collisionResult != null)
            {
                for (var i = 0; i < collisionResult.Length; i++)
                {
                    if (collisionResult[i].Collides) Parent.Position = Parent.Position + collisionResult[i].Axis;
                }
            }
        }
    }
}
