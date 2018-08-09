using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Tools;

namespace _24HourDay
{
    public class ModEntry : Mod
    {

        private float PreCollapseStamina;

        private int PreCollapseHealth;

        private bool StartToPassOut;

        private bool HasPassOut;
        
        public override void Entry(IModHelper helper)
        {
            if (!Game1.IsMultiplayer)
            {
                SaveEvents.AfterLoad += this.SaveEvents_AfterLoad;
                TimeEvents.AfterDayStarted += this.TimeEvents_AfterDayStarted;
                GameEvents.UpdateTick += this.GameEvents_UpdateTick;
            }
        }

        public void SaveEvents_AfterLoad(object sender, EventArgs e)
        {
            this.Monitor.Log($"SaveEvents_AfterLoad called");

            IReflectedField<NetEvent0> passOutEventField = this.Helper.Reflection.GetField<NetEvent0>(Game1.player, "passOutEvent");
            NetEvent0 passOutEvent = new NetEvent0();
            passOutEvent.onEvent += new NetEvent0.Event(this.performPassOut);
            passOutEventField.SetValue(passOutEvent);

            //TODO: inject new version of performTenMinuteClockUpdate()

            this.Monitor.Log($"SaveEvents_AfterLoad ended");
        }

        private void performPassOut()
        {
            Farmer farmer = Game1.player;
            if ((double)farmer.stamina <= -15.0)
            {
                farmer.faceDirection(2);
                farmer.completelyStopAnimatingOrDoingAction();
                farmer.animateOnce(293);
            }
            else
            {
                HasPassOut = true;
            }
        }

        public void TimeEvents_AfterDayStarted(object sender, EventArgs e)
        {
            StartToPassOut = false;
            HasPassOut = false;
            //Reinstate stamina and health if it was higher when passed out at night
            if (Game1.player.stamina < this.PreCollapseStamina)
                Game1.player.stamina = this.PreCollapseStamina;
            if (Game1.player.health < this.PreCollapseHealth)
                Game1.player.health = this.PreCollapseHealth;
        }
        
        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {
            this.PreCollapseStamina = Game1.player.stamina;
            this.PreCollapseHealth = Game1.player.health;

            if (!Context.IsWorldReady || Game1.timeOfDay <= 2550)
                return;
            if (!Game1.newDay && StartToPassOut == false)
            {
                Farmer farmer = Game1.player;

                if (HasPassOut)
                {
                    this.Monitor.Log($"GameEvents_UpdateTick called. set freezePause to 0");
                    farmer.freezePause = 0;
                    HasPassOut = false;
                }
                
                //Harvest Moon style day cycle
                //Passed out at 6 AM and return home
                //No money lost
                if (Game1.timeOfDay >= 3000)
                {
                    this.Monitor.Log($"FourthUpdateTick called. Attempt to restart the day");
                    StartToPassOut = true;
                    Game1.dayTimeMoneyBox.timeShakeTimer = 2000;
                    farmer.freezePause = 7000;
                    startPassOut();
                    this.Monitor.Log($"FourthUpdateTick ended.");
                }
            }
        }

        //Based on StardewValley.Game1.performTenMinuteClockUpdate()
        private Action performTenMinuteClockUpdate()
        {
            return (Action)(() =>
            {
                int trulyDarkTime = Game1.getTrulyDarkTime();
                Game1.gameTimeInterval = 0;
                if (Game1.IsMasterGame)
                    Game1.timeOfDay += 10;
                if (Game1.timeOfDay % 100 >= 60)
                    Game1.timeOfDay = Game1.timeOfDay - Game1.timeOfDay % 100 + 100;
                Game1.timeOfDay = Math.Min(Game1.timeOfDay, 3000);
                if (Game1.isLightning && Game1.timeOfDay < 2400)
                    Utility.performLightningUpdate();
                if (Game1.timeOfDay == trulyDarkTime)
                    Game1.currentLocation.switchOutNightTiles();
                else if (Game1.timeOfDay == Game1.getModeratelyDarkTime())
                {
                    if (Game1.currentLocation.IsOutdoors && !Game1.isRaining)
                        Game1.ambientLight = Color.White;
                    if (!Game1.isRaining && !(Game1.currentLocation is MineShaft) && (Game1.currentSong != null && !Game1.currentSong.Name.Contains("ambient")) && Game1.currentLocation is Town)
                        Game1.changeMusicTrack("none");
                }
                if ((bool)((NetFieldBase<bool, NetBool>)Game1.currentLocation.isOutdoors) && !Game1.isRaining && (!Game1.eventUp && Game1.currentSong != null) && (Game1.currentSong.Name.Contains("day") && Game1.isDarkOut()))
                    Game1.changeMusicTrack("none");
                if (Game1.weatherIcon == 1)
                {
                    int int32 = Convert.ToInt32(Game1.temporaryContent.Load<Dictionary<string, string>>("Data\\Festivals\\" + Game1.currentSeason + (object)Game1.dayOfMonth)["conditions"].Split('/')[1].Split(' ')[0]);
                    if (Game1.whereIsTodaysFest == null)
                        Game1.whereIsTodaysFest = Game1.temporaryContent.Load<Dictionary<string, string>>("Data\\Festivals\\" + Game1.currentSeason + (object)Game1.dayOfMonth)["conditions"].Split('/')[0];
                    if (Game1.timeOfDay == int32)
                    {
                        string str = Game1.temporaryContent.Load<Dictionary<string, string>>("Data\\Festivals\\" + Game1.currentSeason + (object)Game1.dayOfMonth)["conditions"].Split('/')[0];
                        if (!(str == "Forest"))
                        {
                            if (!(str == "Town"))
                            {
                                if (str == "Beach")
                                    str = Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2639");
                            }
                            else
                                str = Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2637");
                        }
                        else
                            str = Game1.currentSeason.Equals("winter") ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2634") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2635");
                        Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2640", (object)Game1.temporaryContent.Load<Dictionary<string, string>>("Data\\Festivals\\" + Game1.currentSeason + (object)Game1.dayOfMonth)["name"]) + str);
                    }
                }
                Game1.player.performTenMinuteUpdate();
                switch (Game1.timeOfDay)
                {
                    case 1200:
                        if ((bool)((NetFieldBase<bool, NetBool>)Game1.currentLocation.isOutdoors) && !Game1.isRaining && (Game1.currentSong == null || Game1.currentSong.IsStopped || Game1.currentSong.Name.ToLower().Contains("ambient")))
                        {
                            Game1.playMorningSong();
                            break;
                        }
                        break;
                    case 2000:
                        if (!Game1.isRaining && Game1.currentLocation is Town)
                        {
                            Game1.changeMusicTrack("none");
                            break;
                        }
                        break;
                    case 2400:
                        Game1.dayTimeMoneyBox.timeShakeTimer = 2000;
                        Game1.player.doEmote(24);
                        Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2652"));
                        break;
                    case 2500:
                        Game1.dayTimeMoneyBox.timeShakeTimer = 2000;
                        Game1.player.doEmote(24);
                        break;
                    case 2600:
                        Game1.dayTimeMoneyBox.timeShakeTimer = 2000;
                        Game1.player.doEmote(24);
                        break;
                    case 2700:
                        Game1.dayTimeMoneyBox.timeShakeTimer = 2000;
                        Game1.player.doEmote(24);
                        break;
                    case 2800:
                        Game1.dayTimeMoneyBox.timeShakeTimer = 2000;
                        Game1.player.doEmote(24);
                        break;
                    case 2900:
                        Game1.dayTimeMoneyBox.timeShakeTimer = 2000;
                        Game1.player.doEmote(24);
                        break;
                }
                foreach (GameLocation location in (IEnumerable<GameLocation>)Game1.locations)
                {
                    location.performTenMinuteUpdate(Game1.timeOfDay);
                    if (location is Farm)
                        ((BuildableGameLocation)location).timeUpdate(10);
                }
                MineShaft.UpdateMines10Minutes(Game1.timeOfDay);
                if (!Game1.IsMasterGame || Game1.farmEvent != null)
                    return;
                Game1.netWorldState.Value.UpdateFromGame1();
            });
        }

        //Based on StardewValley.Farmer.performPassOut()
        private void startPassOut()
        {
            this.Monitor.Log($"startPassOut called.");
            Farmer farmer = Game1.player;
            farmer.faceDirection(2);
            farmer.completelyStopAnimatingOrDoingAction();
            farmer.CanMove = false;
            FarmerSprite sprite = (FarmerSprite)farmer.Sprite;
            sprite.PauseForSingleAnimation = true;
            sprite.currentSingleAnimationInterval = 100f;
            sprite.CurrentAnimation = ((IEnumerable<FarmerSprite.AnimationFrame>)getPassOutAnimation()).ToList<FarmerSprite.AnimationFrame>();
            this.Monitor.Log($"startPassOut currentAnimation set.");
            sprite.currentAnimationIndex = 0;
            sprite.CurrentFrame = sprite.CurrentAnimation[0].frame;
            sprite.interval = (float)sprite.CurrentAnimationFrame.milliseconds;
            sprite.timer = 0.0f;
            this.Monitor.Log($"startPassOut ended.");
        }

        //Based on StardewValley.FarmerSprite animation# 293
        private FarmerSprite.AnimationFrame[] getPassOutAnimation()
        {
            this.Monitor.Log($"getPassOutAnimation called.");
            Game1.player.FarmerSprite.loopThisAnimation = false;
            return new FarmerSprite.AnimationFrame[7]
            {
            new FarmerSprite.AnimationFrame(16, 1000),
            new FarmerSprite.AnimationFrame(0, 500),
            new FarmerSprite.AnimationFrame(16, 1000),
            new FarmerSprite.AnimationFrame(4, 200),
            new FarmerSprite.AnimationFrame(5, 2000, false, false, new AnimatedSprite.endOfAnimationBehavior(Farmer.doSleepEmote), false),
            new FarmerSprite.AnimationFrame(5, 2000, false, false, new AnimatedSprite.endOfAnimationBehavior(passOut), false),
            new FarmerSprite.AnimationFrame(5, 2000)
            };

        }

        //Based on StardewValley.Farmer.passOutFromTired()
        private void passOut(Farmer who)
        {
            this.Monitor.Log($"passOut called.");
            if (who.isRidingHorse())
                who.mount.dismount();
            if (Game1.activeClickableMenu != null)
            {
                Game1.activeClickableMenu.emergencyShutDown();
                Game1.exitActiveMenu();
            }
            who.completelyStopAnimatingOrDoingAction();
            if (who.bathingClothes.Value)
                who.changeOutOfSwimSuit();
            who.swimming.Value = false;
            who.CanMove = false;
            Vector2 bed = Utility.PointToVector2(Utility.getHomeOfFarmer(who).getBedSpot()) * 64f;
            bed.X -= 64f;
            LocationRequest.Callback callback = (LocationRequest.Callback)(() =>
            {
                this.Monitor.Log($"callback called.");
                who.Position = bed;
                who.currentLocation.lastTouchActionLocation = bed;
                if (!Game1.IsMultiplayer || Game1.timeOfDay >= 2600)
                    Game1.PassOutNewDay();
                Game1.changeMusicTrack("none");
                this.Monitor.Log($"callback ended.");
            });
            if (!(bool)((NetFieldBase<bool, NetBool>)who.isInBed))
            {
                this.Monitor.Log($"Game1.player.isInBed called.");
                LocationRequest locationRequest = Game1.getLocationRequest(who.homeLocation.Value, false);
                Game1.warpFarmer(locationRequest, (int)bed.X / 64, (int)bed.Y / 64, 2);
                locationRequest.OnWarp += callback;
                who.FarmerSprite.setCurrentSingleFrame(5, (short)3000, false, false);
                who.FarmerSprite.PauseForSingleAnimation = true;
            }
            else
                callback();
            this.Monitor.Log($"passOut ended.");
        }
    }
}
