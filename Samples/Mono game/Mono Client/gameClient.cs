﻿using System;
using Artemis;
using Asgard.Client;
using Asgard.Core.Network.Packets;
using Asgard.Core.System;
using Asgard.EntitySystems.Components;
using Farseer.Framework;
using Microsoft.Xna.Framework.Content;
using Shared;
using Asgard;
using Asgard.Core.Network;
using System.Diagnostics;
using System.Collections.Generic;
using Asgard.Core.Physics;

namespace Mono_Client
{
    public class gameClient : AsgardClient<ClientStatePacket>
    {
        public ContentManager Content;

        private Entity _thisPlayer;
        List<PlayerStateData> StateList;

        public PlayerStateData CurrentState { get; set; }

        Asgard.Core.Collections.LinkedList<PlayerStateData> _movebuffer =
            new Asgard.Core.Collections.LinkedList<PlayerStateData>();


        public gameClient()
        {
            CurrentState = new PlayerStateData();
            StateList = new List<PlayerStateData>();

            PacketFactory.AddCallback<LoginResponsePacket>(onLogin);

            var bifrost = LookupSystem<BifrostClient>();
            bifrost.OnConnection += Bifrost_OnConnection;
            bifrost.Start();

        }

        public RenderData GetPlayerData()
        {
            if (_thisPlayer == null) return null;
            return _thisPlayer.GetComponent<RenderData>();
        }

        private void Bifrost_OnConnection(Asgard.Core.Network.NetNode connection)
        {
            var bifrost = LookupSystem<BifrostClient>();
            MonoLoginPacket packet = new MonoLoginPacket();
            bifrost.Send(packet);
        }

        private void onLogin(LoginResponsePacket packet)
        {
            var id = packet.OwnerEntityId;
            var entity = EntityManager.GetEntityByUniqueId(id);
            if (entity == null)
            {
                entity = EntityManager.Create(id);
            }
            PlayerComponent pComp = new PlayerComponent(null);
            entity.AddComponent(pComp);
            _thisPlayer = entity;
        }

        protected override void Tick(double delta)
        {
            base.Tick(delta);

            if (_thisPlayer == null) return;
            var playerComp = _thisPlayer.GetComponent<PlayerComponent>();
            var phyComp = _thisPlayer.GetComponent<Physics2dComponent>();
            if (phyComp == null || phyComp.Body == null) return;


            var d = (phyComp.Body.Position - playerComp.OldPosition).LengthSquared();
            if (playerComp.LerpToReal && d < 5f)
            {
                float t = (float)((NetTime.RealTime - playerComp.LerpStart)
                    / (playerComp.LerpEnd - playerComp.LerpStart));
                t = Math.Min(t, 1.0f);
                t = Math.Max(t, 0.0f);

                playerComp.RenderPosition = Vector2.Lerp(playerComp.OldPosition, phyComp.Body.Position, t);
                if ((playerComp.OldPosition - phyComp.Body.Position).LengthSquared() <= 0.0001f)
                {
                    playerComp.LerpToReal = false;
                }
            }
            else
            {
                playerComp.LerpToReal = false;
                playerComp.OldPosition = phyComp.Body.Position;
                playerComp.RenderPosition = phyComp.Body.Position;
            }
        }

        protected override void BeforePhysics(float delta)
        {
            base.BeforePhysics(delta);

            var renderData = GetPlayerData();
            if (renderData == null) return;
            if (CurrentState.LeftMouseDown)
            {
                renderData.MovingToPosition = true;
            }

            if (renderData.MovingToPosition)
            {
                var phyComp = _thisPlayer.GetComponent<Physics2dComponent>();
                var curPos = phyComp.Body.Position;

                var diff = CurrentState.MousePositionInWorld - curPos;
                if (diff.LengthSquared() <= 0.001)
                {
                    renderData.MovingToPosition = false;
                    phyComp.Body.LinearVelocity = Vector2.Zero;
                }
                else
                {
                    diff = new Vector2(
                        diff.X / Math.Abs(diff.X),
                         diff.Y / Math.Abs(diff.Y)
                        );

                    var renderComp = _thisPlayer.GetComponent<RenderData>();
                    renderComp.SetSpeed(_thisPlayer, diff);
                }
            }

            ApplyLagComp();
        }

        protected override void AfterPhysics(float delta)
        {
            base.AfterPhysics(delta);

            if (_thisPlayer == null) return;

            var phyComp = _thisPlayer.GetComponent<Physics2dComponent>();
            if (phyComp == null || phyComp.Body == null) return;

            PlayerStateData newState = new PlayerStateData()
            {
                LeftMouseDown = CurrentState.LeftMouseDown,
                MousePositionInWorld = CurrentState.MousePositionInWorld,
                SimTick = NetTime.SimTick,
                Position = phyComp.Body.Position
            };

            StateList.Add(newState);
            _movebuffer.AddToTail(newState);

           

        }

        protected override ClientStatePacket GetClientState()
        {
            ClientStatePacket packet = new ClientStatePacket();
            packet.State = new List<PlayerStateData>(StateList);
            StateList.Clear();
            packet.SnapId = (int)NetTime.SimTick;
            return packet;
        }

        private void ApplyLagComp()
        {
            if (_thisPlayer == null) return;

            var netSync = _thisPlayer.GetComponent<NetPhysicsObject>();
            if (netSync == null) return;

            Asgard.Core.Collections.LinkedListNode<PlayerStateData> found_node = null;
            foreach (var node in _movebuffer)
            {
                if (node.Value.SimTick == netSync.SimTick)
                {
                    found_node = node;
                    break;
                }
            }

            if (found_node != null)
            {
                var moveData = found_node.Value;

                var diff = netSync.Position - moveData.Position;

                if (diff.LengthSquared() > 0)
                {
                    var node = found_node;
                    Vector2 pos = netSync.Position;

                    Vector2 prev_pos = node.Value.Position;
                    while (node != null)
                    {
                        var move = node.Value;
                        Vector2 velStep = move.Position - prev_pos;


                        pos += velStep;
                        prev_pos = move.Position;
                        node = node.Next;
                        move.Position = pos;
                    }

                    var pComp = _thisPlayer.GetComponent<Physics2dComponent>();
                    if (pComp == null || pComp.Body == null) return;

                    var playerComponent = _thisPlayer.GetComponent<PlayerComponent>();
                    playerComponent.LerpToReal = true;
                    playerComponent.OldPosition = pComp.Body.Position;
                    playerComponent.LerpStart = NetTime.RealTime;
                    playerComponent.LerpEnd = playerComponent.LerpStart + (0.1f);
                    pComp.Body.Position = pos;
                }


                _movebuffer.TruncateTo(found_node.Next);
            }


        }

    }
}