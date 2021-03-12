using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core.Native;
using JetBrains.Annotations;
using Moonlight.Server.Identity;
using Moonlight.Server.Internal.Components;
using Moonlight.Server.Sessions;
using Moonlight.Shared.Internal;
using Moonlight.Shared.Internal.Events;

namespace Moonlight.Server.Internal.Events
{
    [PublicAPI]
    public class ClientId : ISource
    {
        public static readonly ClientId Global = new ClientId(-1);

        public Snowflake Id { get; set; }
        public int Handle { get; set; }
        public string[] Identifiers { get; set; }
        public Player Player => Component.GetInstance<SessionComponent>().Sessions.SingleOrDefault(self => self.Owner.Handle == Handle)?.Owner;

        public ClientId(Snowflake id)
        {
            var component = Component.GetInstance<SessionComponent>();
            var owner = component.Sessions.SingleOrDefault(self => self.Owner.Id == id)?.Owner;

            if (owner != null)
            {
                Id = id;
                Handle = owner.Handle;
                Identifiers = owner.Identifiers;
            }
            else
            {
                throw new Exception($"Could not find runtime client: {id}");
            }
        }

        public ClientId(int handle)
        {
            Handle = handle;

            var holder = new List<string>();

            for (var index = 0; index < API.GetNumPlayerIdentifiers(handle.ToString()); index++)
            {
                holder.Add(API.GetPlayerIdentifier(handle.ToString(), index));
            }

            var component = Component.GetInstance<SessionComponent>();

            Id = component.Sessions.SingleOrDefault(self => self.Owner.Handle == handle)?.Owner.Id ?? Snowflake.Empty;
            Identifiers = holder.ToArray();
        }

        public ClientId(Snowflake id, int handle, string[] identifiers)
        {
            Id = id;
            Handle = handle;
            Identifiers = identifiers;
        }

        public override string ToString()
        {
            return $"{(Id != Snowflake.Empty ? Id.ToString() : Handle.ToString())} ({API.GetPlayerName(Handle.ToString())})";
        }

        public bool Compare(string[] identifiers)
        {
            return identifiers.Any(self => Identifiers.Contains(self));
        }

        public bool Compare(Player player)
        {
            return Compare(player.Identifiers);
        }

        public bool Compare(ClientId client)
        {
            return client.Handle == Handle;
        }

        public static explicit operator ClientId(string netId)
        {
            if (int.TryParse(netId.Replace("net:", string.Empty), out var handle))
            {
                return new ClientId(handle);
            }

            throw new Exception($"Could not parse net id: {netId}");
        }

        public static explicit operator ClientId(int handle) => new ClientId(handle);
    }
}