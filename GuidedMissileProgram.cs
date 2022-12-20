using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        bool fired = false;
        float timeUnit = 0;
        readonly float timeLimit = 5;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10; // Run every 10 ticks

        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {

            // Thrusters
            string thrustForwardsName = "Forwards";
            string thrustDownName = "Down";
            string thrustUpName = "Up";
            string thrustLeftName = "Left";
            string thrustRightName = "Right";

            IMyThrust thrustForwards = GridTerminalSystem.GetBlockWithName(thrustForwardsName) as IMyThrust;
            if (thrustForwards == null) { Echo("THRUST_FORWARDS NOT FOUND");  }
            IMyThrust thrustDown = GridTerminalSystem.GetBlockWithName(thrustDownName) as IMyThrust;
            if (thrustDown == null) { Echo("THRUST_DOWN NOT FOUND"); }
            IMyThrust thrustUp = GridTerminalSystem.GetBlockWithName(thrustUpName) as IMyThrust;
            if (thrustUp == null) { Echo("THRUST_UP NOT FOUND"); }
            IMyThrust thrustLeft = GridTerminalSystem.GetBlockWithName(thrustLeftName) as IMyThrust;
            if (thrustLeft == null) { Echo("THRUST_LEFT NOT FOUND"); }
            IMyThrust thrustRight = GridTerminalSystem.GetBlockWithName(thrustRightName) as IMyThrust;
            if (thrustRight == null) { Echo("THRUST_RIGHT NOT FOUND"); }

            string warheadGroupName = "Payload";
            IMyBlockGroup warheadsGroup = GridTerminalSystem.GetBlockGroupWithName(warheadGroupName);
            if (warheadsGroup == null) { Echo("WARHEADS NOT FOUND"); }

            List<IMyTerminalBlock> warheads = new List<IMyTerminalBlock>();
            warheadsGroup.GetBlocks(warheads);



            // Internals
            string remoteName = "Remote Control";
            string turretName = "Custom Turret Controller";
            string rotor1Name = "Advanced Rotor";
            string rotor2Name = "Advanced Rotor 2";
            string cameraName = "Camera";
            string mergeName = "Merge";

            IMyTurretControlBlock myTurretControl = GridTerminalSystem.GetBlockWithName(turretName) as IMyTurretControlBlock;
            if (myTurretControl == null) { Echo("TURRET_CONTROL NOT FOUND"); }
            IMyRemoteControl myRemoteControl = GridTerminalSystem.GetBlockWithName(remoteName) as IMyRemoteControl;
            if (myRemoteControl == null) { Echo("REMOTE_CONTROL NOT FOUND"); }
            IMyShipMergeBlock merge = GridTerminalSystem.GetBlockWithName(mergeName) as IMyShipMergeBlock;
            if (merge == null) { Echo("MERGE NOT FOUND"); }

            // Setup turret control
            IMyCameraBlock cam = GridTerminalSystem.GetBlockWithName(cameraName) as IMyCameraBlock;
            if (merge == null) { Echo("CAMERA NOT FOUND"); }
            IMyMotorStator rot1 = GridTerminalSystem.GetBlockWithName(rotor1Name) as IMyMotorStator;
            if (merge == null) { Echo("ROTOR1 NOT FOUND"); }
            IMyMotorStator rot2 = GridTerminalSystem.GetBlockWithName(rotor2Name) as IMyMotorStator;
            if (merge == null) { Echo("ROTOR2 NOT FOUND"); }

            // Lock up rotors
            rot1.RotorLock = true;
            rot2.RotorLock = true;


            // Init all values
            myTurretControl.AzimuthRotor = rot1;
            myTurretControl.ElevationRotor = rot2;
            myTurretControl.Camera = cam;

            // Enable AI and get enemy
            myTurretControl.AIEnabled = true;
            MyDetectedEntityInfo enemy = myTurretControl.GetTargetedEntity();

            // Found pos we thrust up else continue

            // If not connected we fire the boost up and then we start the locking phase



            if (fired)
            {
                Echo("Fired loop");
                // Tracking

                //double sum = Math.Abs(enemy.Position.X) + Math.Abs(enemy.Position.Y) + Math.Abs(enemy.Position.Z);
                if (enemy.Position.Sum == 0) // Not found
                {
                    Echo("NO TARGET");

                    thrustForwards.ThrustOverridePercentage = 0.8f;
                }
                else // Found
                {
                    Echo("TARGET FOUND");

                    foreach (IMyWarhead warhead in warheads)
                    {
                        warhead.IsArmed = true;
                        Echo(warhead.Name.ToString() + " armed");
                        if (!warhead.IsCountingDown)
                        {
                            warhead.DetonationTime = 30.0f;
                            warhead.StartCountdown();
                        }
                    }

                    // Update waypoint position
                    thrustForwards.ThrustOverridePercentage = 1.0f;
                    myRemoteControl.ClearWaypoints();
                    myRemoteControl.AddWaypoint(enemy.Position, "target");
                    myRemoteControl.SetAutoPilotEnabled(true);
                    myRemoteControl.FlightMode = FlightMode.OneWay;
                }
            }
            else
            {
                // Not fired we check connector
                // If connector disconnected (i.e. fired)
                // We start fired procedure
                if (!merge.IsConnected)
                {
                    Echo("MERGE DISABLED");
                    // Toggle all thrusters on
                    thrustForwards.Enabled = true;
                    thrustUp.Enabled = true;
                    thrustDown.Enabled = true;
                    thrustLeft.Enabled = true;
                    thrustRight.Enabled = true;

                    merge.Enabled = false; // Disable the connector block and start fire
                    // Not connected and so we start the fire procedure
                    thrustUp.ThrustOverridePercentage = 1.0f;

                    // timed fire up
                    timeUnit++;
                    if (timeUnit == timeLimit)
                    {
                        thrustUp.ThrustOverridePercentage = 0.0f; // Turn off
                        fired = true;
                        Echo("Fired");
                    }
                }
                else
                {
                    Echo("MERGE ENABLED");
                    thrustForwards.Enabled = false;
                    thrustUp.Enabled = false;
                    thrustDown.Enabled = false;
                    thrustLeft.Enabled = false;
                    thrustRight.Enabled = false;
                }
            }
        }

    }
}
