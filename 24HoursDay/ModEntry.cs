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

        private bool HasPassed2600;

        private bool HasPassOut;

        private int TickCount;

        public override void Entry(IModHelper helper)
        {
            if (!Game1.IsMultiplayer)
            {
                SaveEvents.AfterLoad += this.SaveEvents_AfterLoad;
                SaveEvents.BeforeSave += this.SaveEvents_BeforeSave;
                TimeEvents.AfterDayStarted += this.TimeEvents_AfterDayStarted;
                GameEvents.UpdateTick += this.GameEvents_UpdateTick;
            }
        }

        #region SaveEvents_AfterLoad

        public void SaveEvents_AfterLoad(object sender, EventArgs e)
        {
            //this.Monitor.Log($"SaveEvents_AfterLoad called");

            IReflectedField<NetEvent0> passOutEventField = this.Helper.Reflection.GetField<NetEvent0>(Game1.player, "passOutEvent");
            NetEvent0 passOutEvent = new NetEvent0();
            passOutEvent.onEvent += new NetEvent0.Event(this.performPassOut);
            passOutEventField.SetValue(passOutEvent);

            //this.Monitor.Log($"SaveEvents_AfterLoad ended");
        }

        private void performPassOut()
        {
            if ((double)Game1.player.stamina <= -15.0)
            {
                Game1.player.faceDirection(2);
                Game1.player.completelyStopAnimatingOrDoingAction();
                Game1.player.animateOnce(293);
            }
            else
            {
                HasPassOut = true;
            }
        }

        #endregion

        #region SaveEvents_BeforeSave

        /*
         * Reinstate stamina and health if it was higher when passed out at night
         */
        public void SaveEvents_BeforeSave(object sender, EventArgs e)
        {
            if (Game1.player.stamina < this.PreCollapseStamina)
                Game1.player.stamina = this.PreCollapseStamina;
            if (Game1.player.health < this.PreCollapseHealth)
                Game1.player.health = this.PreCollapseHealth;
        }

        #endregion

        #region TimeEvents_AfterDayStarted

        public void TimeEvents_AfterDayStarted(object sender, EventArgs e)
        {
            HasPassOut = false;
            TickCount = 0;
            HasPassed2600 = false;
        }

        #endregion

        #region GameEvents_UpdateTick

        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {
            this.PreCollapseStamina = Game1.player.stamina;
            this.PreCollapseHealth = Game1.player.health;

            if (!Context.IsWorldReady || Game1.timeOfDay <= 2550 || Game1.timeOfDay >= 3000)
                return;
            if (!Game1.newDay)
            {
                if (Game1.timeOfDay == 2600 && Game1.dayTimeMoneyBox.timeShakeTimer == 2000)
                {
                    if (!HasPassed2600)
                        HasPassed2600 = true;
                    else
                        performTenMinuteClockUpdate();

                    if (Game1.timeOfDay % 100 != 0 && Game1.dayTimeMoneyBox.timeShakeTimer > 0)
                        Game1.dayTimeMoneyBox.timeShakeTimer = 0;

                    TickCount++;
                }
                
                if (HasPassOut)
                {
                    //this.Monitor.Log($"GameEvents_UpdateTick called. set freezePause to 0");
                    Game1.player.freezePause = 0;
                    HasPassOut = false;
                }
            }
        }

        /*
         * Based on StardewValley.Game1.performTenMinuteClockUpdate()
         */
        private void performTenMinuteClockUpdate()
        {
            Game1.timeOfDay += (TickCount * 10);
            if (Game1.timeOfDay % 100 >= 60)
            {
                Game1.timeOfDay = Game1.timeOfDay - Game1.timeOfDay % 100 + 100;
                TickCount += 4;
            }
            Game1.timeOfDay = Math.Min(Game1.timeOfDay, 3000);
            /*
             * TODO: Reinstate horse riding if IsRidingHorse()
             */
            switch (Game1.timeOfDay)
            {
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
                case 3000:
                    /*
                     * Harvest Moon style day cycle
                     * Passed out at 6 AM and return home
                     * No money lost
                     */
                    this.Monitor.Log($"Attempt to restart the day");
                    Game1.dayTimeMoneyBox.timeShakeTimer = 2000;
                    Game1.player.freezePause = 7000;
                    Game1.player.faceDirection(2);
                    Game1.player.completelyStopAnimatingOrDoingAction();
                    startPassOut();
                    break;
            }
        }

        /*
         * Based on StardewValley.Farmer.performPassOut()
         */
        private void startPassOut()
        {
            this.Monitor.Log($"startPassOut called.");
            Game1.player.FarmerSprite.pauseForSingleAnimation = false;
            Game1.player.FarmerSprite.animateOnce(getPassOutAnimation());
            this.Monitor.Log($"startPassOut ended.");
        }

        /*
         * Based on StardewValley.FarmerSprite animation# 293
         */
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

        /*
         * Based on StardewValley.Farmer.passOutFromTired()
         */
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
                if (!Game1.IsMultiplayer || Game1.timeOfDay >= 3000)
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

        #endregion
    }
}
