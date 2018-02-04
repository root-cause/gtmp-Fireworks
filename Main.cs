using System.Linq;
using System.Timers;
using System.Collections.Generic;
using GrandTheftMultiplayer.Server.API;
using GrandTheftMultiplayer.Server.Constant;
using GrandTheftMultiplayer.Server.Elements;
using GrandTheftMultiplayer.Shared;
using GrandTheftMultiplayer.Shared.Math;

namespace Fireworks
{
    #region FireworkData Class
    public class FireworkData
    {
        public int PropHash { get; set; }
        public string ParticleName { get; set; }
        public string AnimName { get; set; }
        public int EffectTime { get; set; }

        public FireworkData(string propName, string particleName, string animName, int effectTime)
        {
            PropHash = API.shared.getHashKey(propName);
            ParticleName = particleName;
            AnimName = animName;
            EffectTime = effectTime;
        }
    }
    #endregion

    #region Firework Class
    public class Firework
    {
        public NetHandle Owner { get; set; }
        public int Type { get; set; }
        public int Color { get; set; }
        public int Delay { get; set; }
        public Vector3 Position { get; set; }
        public Object Prop { get; set; }
        public ParticleEffect PTFX { get; set; }
        private Timer _timer;

        public Firework(NetHandle owner, int type, int color, int delay, Vector3 position)
        {
            Owner = owner;
            Type = type;
            Color = color;
            Delay = delay;
            Position = position;

            Prop = API.shared.createObject(Main.FireworkTypeData[type].PropHash, position, new Vector3());
            PTFX = null;
        }

        public void Launch()
        {
            if (_timer != null) return;

            if (Delay > 0)
            {
                _timer = API.shared.startTimer(Delay * 1000, true, () =>
                {
                    Prop.delete();
                    Prop = null;

                    PTFX = API.shared.createLoopedParticleEffectOnPosition("scr_indep_fireworks", Main.FireworkTypeData[Type].ParticleName, Position, new Vector3(), 1.0f);
                    PTFX.setSyncedData("FireworkColor", API.shared.toJson(Main.FireworkColors[Color]));

                    _timer = API.shared.startTimer(Main.FireworkTypeData[Type].EffectTime, true, () =>
                    {
                        DeleteEntities();
                        Main.AllFireworks.Remove(this);
                    });
                });
            }
            else
            {
                Prop.delete();
                Prop = null;

                PTFX = API.shared.createLoopedParticleEffectOnPosition("scr_indep_fireworks", Main.FireworkTypeData[Type].ParticleName, Position, new Vector3(), 1.0f);
                PTFX.setSyncedData("FireworkColor", API.shared.toJson(Main.FireworkColors[Color]));

                _timer = API.shared.startTimer(Main.FireworkTypeData[Type].EffectTime, true, () =>
                {
                    DeleteEntities();
                    Main.AllFireworks.Remove(this);
                });
            }
        }

        public void DeleteEntities()
        {
            if (Prop != null) Prop.delete();
            if (PTFX != null) PTFX.delete();
            if (_timer != null) API.shared.stopTimer(_timer);
        }
    }
    #endregion

    public class Main : Script
    {
        public static int FireworksPerPlayer = 10;

        #region Firework Data
        public static FireworkData[] FireworkTypeData =
        {
            new FireworkData("ind_prop_firework_04", "scr_indep_firework_fountain", "PLACE_FIREWORK_4_CONE", 7500),
            new FireworkData("ind_prop_firework_03", "scr_indep_firework_shotburst", "PLACE_FIREWORK_3_BOX", 5000),
            new FireworkData("ind_prop_firework_02", "scr_indep_firework_starburst", "PLACE_FIREWORK_2_CYLINDER", 5500),
            new FireworkData("ind_prop_firework_01", "scr_indep_firework_trailburst", "PLACE_FIREWORK_1_ROCKET", 3200)
        };

        public static Color[] FireworkColors =
        {
            new Color(255, 25, 25), // red
            new Color(255, 255, 255), // white
            new Color(25, 25, 255) // blue
        };
        #endregion

        public static List<Firework> AllFireworks = new List<Firework>();

        #region Methods
        public void RemoveFireworkProp(Client player)
        {
            if (player.hasData("Firework_Prop"))
            {
                API.deleteEntity(player.getData("Firework_Prop"));
                player.resetData("Firework_Prop");
            }
        }
        #endregion

        public Main()
        {
            API.onResourceStart += Fireworks_Init;
            API.onClientEventTrigger += Fireworks_EventTrigger;
            API.onPlayerDisconnected += Fireworks_PlayerLeave;
            API.onResourceStop += Fireworks_Exit;
        }

        #region Events
        public void Fireworks_Init()
        {
            if (API.hasSetting("fireworksPerPlayer")) FireworksPerPlayer = API.getSetting<int>("fireworksPerPlayer");
        }

        public void Fireworks_EventTrigger(Client player, string eventName, params object[] args)
        {
            switch (eventName)
            {
                case "Firework_BeginPlace":
                {
                    if (args.Length < 3 || player.hasData("Firework_Prop")) return;
                    if (AllFireworks.Count(f => f.Owner == player.handle) >= FireworksPerPlayer)
                    {
                        player.sendChatMessage("~r~ERROR: ~w~You can't place any more fireworks.");
                        return;
                    }

                    int type = (int)args[0];

                    Object tempFirework = API.createObject(FireworkTypeData[type].PropHash, player.position, new Vector3());
                    tempFirework.attachTo(player.handle, "PH_R_Hand", new Vector3(), new Vector3());

                    player.setData("Firework_Prop", tempFirework.handle);
                    player.setData("Firework_PlacingType", type);
                    player.setData("Firework_PlacingColor", (int)args[1]);
                    player.setData("Firework_PlacingDelay", (int)args[2]);

                    player.playAnimation("anim@mp_fireworks", FireworkTypeData[type].AnimName, 1048576);
                    player.triggerEvent("Firework_AnimReporter", FireworkTypeData[type].PropHash, FireworkTypeData[type].AnimName);
                    break;
                }

                case "Firework_CompletePlace":
                {
                    if (args.Length < 1 || !player.hasData("Firework_Prop")) return;
                    RemoveFireworkProp(player);

                    Vector3 pos = (Vector3)args[0];
                    AllFireworks.Add(new Firework(player.handle, (int)player.getData("Firework_PlacingType"), (int)player.getData("Firework_PlacingColor"), (int)player.getData("Firework_PlacingDelay"), pos));
                    break;
                }

                case "Firework_Launch":
                {
                    foreach (Firework fwItem in AllFireworks.Where(f => f.Owner == player.handle)) fwItem.Launch();
                    break;
                }

                case "Firework_Cancel":
                {
                    RemoveFireworkProp(player);
                    break;
                }
            }
        }

        public void Fireworks_PlayerLeave(Client player, string reason)
        {
            RemoveFireworkProp(player);

            foreach (Firework fwItem in AllFireworks.Where(f => f.Owner == player.handle)) fwItem.DeleteEntities();
            AllFireworks.RemoveAll(f => f.Owner == player.handle);
        }

        public void Fireworks_Exit()
        {
            foreach (Client player in API.getAllPlayers()) RemoveFireworkProp(player);
            foreach (Firework fwItem in AllFireworks) fwItem.DeleteEntities();
            AllFireworks.Clear();
        }
        #endregion
    }
}
