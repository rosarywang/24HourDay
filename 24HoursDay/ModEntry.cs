using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
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

        private bool StartToPassOut;

        private bool HasPassOut;

        private int facingDirection = 0;

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
            StartToPassOut = false;
            HasPassOut = false;
            //Reinstate stamina and health if it was higher when passed out at night
            if (Game1.player.stamina < this.PreCollapseStamina)
                Game1.player.stamina = this.PreCollapseStamina;
            if (Game1.player.health < this.PreCollapseHealth)
                Game1.player.health = this.PreCollapseHealth;
        }


        private void TimeEvents_TimeOfDayChanged(object sender, EventArgs e)
        {
            //Add clock shake animation at every hour between 2 AM and 6 AM
            if (Game1.timeOfDay > 2550 && Game1.timeOfDay < 3000 && Game1.timeOfDay % 100 == 0)
            {
                Game1.dayTimeMoneyBox.timeShakeTimer = 2000;
                Game1.player.doEmote(24);
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

                if (!Game1.newDay && StartToPassOut == false)
                {
                    Farmer farmer = Game1.player;

                    //Running out of stamina will still passed out
                    if ((double)Game1.player.stamina > -15.0)
                    {
                        //Remove StartToPassOut from animation 
                        FarmerSprite sprite = (FarmerSprite)farmer.Sprite;
                        if (sprite.CurrentAnimation != null && 
                            sprite.currentAnimation.Any(x => x.frameBehavior == Farmer.passOutFromTired))
                        {
                            sprite.StopAnimation();
                            this.Monitor.Log($"sprite stop animation.");
                            sprite.PauseForSingleAnimation = false;
                            farmer.freezePause = 0;
                            farmer.CanMove = true;
                            farmer.forceCanMove();
                            this.Monitor.Log($"player forceCanMove");
                            HasPassOut = true;
                            
                        }

                        //TODO: make sure time pass

                        //Fix facingDirection caused by startToPassOut animation
                        if (HasPassOut && !farmer.FarmerSprite.PauseForSingleAnimation && !farmer.UsingTool && farmer.CanMove || (Game1.eventUp && farmer.isRidingHorse()))
                        {
                            this.Monitor.Log($"Set facingDirection when button pressed");
                            if (this.Helper.Input.IsDown(SButton.Right) || this.Helper.Input.IsDown(SButton.LeftThumbstickRight) || this.Helper.Input.IsDown(SButton.DPadRight) || this.Helper.Input.IsDown(SButton.D))
                            {
                                if (this.facingDirection != 1)
                                {
                                    this.Monitor.Log($"change FacingDirection 1");
                                    this.facingDirection = 1;
                                }
                            }
                            else if (this.Helper.Input.IsDown(SButton.Left) || this.Helper.Input.IsDown(SButton.LeftThumbstickLeft) || this.Helper.Input.IsDown(SButton.DPadLeft) || this.Helper.Input.IsDown(SButton.A))
                            {
                                if (this.facingDirection != 3)
                                {
                                    this.Monitor.Log($"change FacingDirection 3");
                                    this.facingDirection = 3;
                                }
                            }
                            else if (this.Helper.Input.IsDown(SButton.Up) || this.Helper.Input.IsDown(SButton.LeftThumbstickUp) || this.Helper.Input.IsDown(SButton.DPadUp) || this.Helper.Input.IsDown(SButton.W))
                            {
                                if (this.facingDirection != 0)
                                {
                                    this.Monitor.Log($"change FacingDirection 0");
                                    this.facingDirection = 0;
                                }
                            }
                            else if (this.Helper.Input.IsDown(SButton.Down) || this.Helper.Input.IsDown(SButton.LeftThumbstickDown) || this.Helper.Input.IsDown(SButton.DPadDown) || this.Helper.Input.IsDown(SButton.S))
                            {
                                if (this.facingDirection != 2)
                                {
                                    this.Monitor.Log($"change FacingDirection 2");
                                    this.facingDirection = 2;
                                }
                            }
                            this.Monitor.Log($"facingDirection {this.facingDirection.ToString()}");
                            if (farmer.FacingDirection != this.facingDirection)
                            {
                                farmer.FacingDirection = this.facingDirection;
                                animateMovement(farmer, Game1.currentGameTime);
                                //TODO: Reinstate animation and movement
                            }
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
            }
        }

        //Based on StardewValley.Farmer.updateMovementAnimation()
        public void animateMovement(Farmer who, GameTime time)
        {
            bool flag = who.ActiveObject != null && !Game1.eventUp;
            if (who.isRidingHorse() && !who.mount.dismounting.Value)
                who.showRiding();
            else if (who.FacingDirection == 3 && who.running && !flag)
                who.FarmerSprite.animate(56, time);
            else if (who.FacingDirection == 1 && who.running && !flag)
                who.FarmerSprite.animate(40, time);
            else if (who.FacingDirection == 0 && who.running && !flag)
                who.FarmerSprite.animate(48, time);
            else if (who.FacingDirection == 2 && who.running && !flag)
                who.FarmerSprite.animate(32, time);
            else if (who.FacingDirection == 3 && who.running)
                who.FarmerSprite.animate(152, time);
            else if (who.FacingDirection == 1 && who.running)
                who.FarmerSprite.animate(136, time);
            else if (who.FacingDirection == 0 && who.running)
                who.FarmerSprite.animate(144, time);
            else if (who.FacingDirection == 2 && who.running)
                who.FarmerSprite.animate(128, time);
            else if (who.FacingDirection == 3 && !flag)
                who.FarmerSprite.animate(24, time);
            else if (who.FacingDirection == 1 && !flag)
                who.FarmerSprite.animate(8, time);
            else if (who.FacingDirection == 0 && !flag)
                who.FarmerSprite.animate(16, time);
            else if (who.FacingDirection == 2 && !flag)
                who.FarmerSprite.animate(0, time);
            else if (who.FacingDirection == 3)
                who.FarmerSprite.animate(120, time);
            else if (who.FacingDirection == 1)
                who.FarmerSprite.animate(104, time);
            else if (who.FacingDirection == 0)
                who.FarmerSprite.animate(112, time);
            else if (who.FacingDirection == 2)
                who.FarmerSprite.animate(96, time);
            else if (flag)
                who.showCarrying();
            else
                who.showNotCarrying();
        }

        //Based on StardewValley.Farmer.performPassOut()
        public void startPassOut()
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

        //Based on StardewValley.Farmer.passOutFromTired()
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
