using TerrariaApi.Server;
using Terraria;
using TShockAPI;
using Terraria.Net;
using Terraria.GameContent.NetModules;
using Microsoft.Xna.Framework;

namespace PingWarp
{
    [ApiVersion(2,1)]
    public class PingWarpPlugin : TerrariaPlugin
    {
        public override string Name => "Ping Warper";
        public override string Author => "";
        public override Version Version => new(1, 0, 0, 0);
        public override string Description => "";


        private PWState[] States;

        public PingWarpPlugin(Main game) : base(game)
        {
            States = new PWState[Main.maxPlayers];
        }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("pingwarp.canpw", ToggleState, "pwp"));
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
            ServerApi.Hooks.NetGetData.Register(this, GetDataHandler);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
                ServerApi.Hooks.NetGetData.Deregister(this, GetDataHandler);
            }
            base.Dispose(disposing);
        }

        private void GetDataHandler(GetDataEventArgs args)
        {
            if (args.MsgID != PacketTypes.LoadNetModule || !States[args.Msg.whoAmI].canWarp)
                return;

            using (var stream = new MemoryStream(args.Msg.readBuffer))
            {
                stream.Position = args.Index;

                using (var reader = new BinaryReader(stream))
                {
                    ushort id = reader.ReadUInt16();
                    
                    if (id == NetManager.Instance.GetId<NetPingModule>())
                    {
                        Vector2 pos = reader.ReadVector2();
                        States[args.Msg.whoAmI].player.Teleport(pos.X * 16f, pos.Y * 16f);
                    }
                }
            }

            args.Handled = true;
        }

        private void OnGreet(GreetPlayerEventArgs args)
        {
            ResetState(args.Who);
        }

        private void OnLeave(LeaveEventArgs args)
        {
            ResetState(args.Who);
        }

        private void ResetState(int index)
        {
            States[index] = new(index);
        }

        public void ToggleState(CommandArgs args)
        {
            PWState state = States[args.Player.Index];
            state.canWarp = !state.canWarp;
        }

        class PWState
        {
            public int index;
            public bool canWarp;
            public TSPlayer player { get { return TShock.Players[index]; } }

            public PWState(int index)
            {
                this.index = index;
                canWarp = false;
            }
        }
    }
}
