using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;

namespace _24HourDay
{
    public class ModEntry : Mod
    {

        private float PreCollapseStamina;

        private int PreCollapseHealth;

        private bool StartPassOut;

        public override void Entry(IModHelper helper)
        {
            if (!Game1.IsMultiplayer)
            {
                TimeEvents.AfterDayStarted += this.TimeEvents_AfterDayStarted;
                TimeEvents.TimeOfDayChanged += this.TimeEvents_TimeOfDayChanged;
                GameEvents.UpdateTick += this.GameEvents_UpdateTick;
            }
        }

        public void TimeEvents_AfterDayStarted(object sender, EventArgs e)
        {
            StartPassOut = false;
            if (Game1.player.stamina < this.PreCollapseStamina)
                Game1.player.stamina = this.PreCollapseStamina;
            if (Game1.player.health < this.PreCollapseHealth)
                Game1.player.health = this.PreCollapseHealth;
        }


        private void TimeEvents_TimeOfDayChanged(object sender, EventArgs e)
        {
            if (Game1.timeOfDay > 2550 && Game1.timeOfDay < 3000 && Game1.timeOfDay % 100 == 0)
            {
                Game1.dayTimeMoneyBox.timeShakeTimer = 2000;
                Game1.player.doEmote(24);
                Game1.player.yJumpVelocity = -2f;
            }
        }

        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {
            this.PreCollapseStamina = Game1.player.stamina;
            this.PreCollapseHealth = Game1.player.health;

            if (!Context.IsWorldReady || Game1.timeOfDay <= 2550)
                return;
            else
            {
                this.Monitor.Log($"FourthUpdateTick called. Current time {Game1.timeOfDay.ToString()}");

                //TODO: Remove StartToPassOut from animation 
                //TODO: Reinstate animation and movement

                if (!Game1.newDay)
                {
                    if (Game1.timeOfDay >= 3000 && StartPassOut == false)
                    {
                        this.Monitor.Log($"FourthUpdateTick called. Attempt to restart the day");
                        StartPassOut = true;
                        Game1.dayTimeMoneyBox.timeShakeTimer = 2000;
                        Game1.player.freezePause = 7000;
                        startPassOut();
                        this.Monitor.Log($"FourthUpdateTick ended.");
                    }
                }
            }
        }

        public void completelyStopAnimatingOrDoingAction()
        {
            this.Monitor.Log($"completelyStopAnimatingOrDoingAction called.");
            Farmer farmer = Game1.player;
            farmer.CanMove = false;
            if (farmer.UsingTool)
                farmer.EndUsingTool();
            if (farmer.usingSlingshot && farmer.CurrentTool is Slingshot)
                (farmer.CurrentTool as Slingshot).finish();
            farmer.UsingTool = false;
            farmer.isEating = false;
            farmer.FarmerSprite.PauseForSingleAnimation = false;
            farmer.usingSlingshot = false;
            farmer.canReleaseTool = false;
            farmer.Halt();
            farmer.Sprite.StopAnimation();
            if (farmer.CurrentTool is MeleeWeapon)
                (farmer.CurrentTool as MeleeWeapon).isOnSpecial = false;
            farmer.stopJittering();
            this.Monitor.Log($"completelyStopAnimatingOrDoingAction ended.");
        }

        public void startPassOut()
        {
            this.Monitor.Log($"startPassOut called.");
            Farmer farmer = Game1.player;
            farmer.faceDirection(2);
            completelyStopAnimatingOrDoingAction();
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

        public FarmerSprite.AnimationFrame[] getPassOutAnimation()
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

        public void passOut(Farmer who)
        {
            this.Monitor.Log($"passOut called.");
            if (who.isRidingHorse())
                who.mount.dismount();
            if (Game1.activeClickableMenu != null)
            {
                Game1.activeClickableMenu.emergencyShutDown();
                Game1.exitActiveMenu();
            }
            completelyStopAnimatingOrDoingAction();
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
