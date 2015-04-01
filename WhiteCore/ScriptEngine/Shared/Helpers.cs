/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Runtime.Serialization;
using OpenMetaverse;
using WhiteCore.Framework.ClientInterfaces;
using WhiteCore.Framework.PresenceInfo;
using WhiteCore.Framework.SceneInfo;
using WhiteCore.Region;

namespace WhiteCore.ScriptEngine.Shared
{
    [Serializable]
    public class EventAbortException : Exception
    {
        public EventAbortException()
        {
        }

        protected EventAbortException(
                SerializationInfo info, 
                StreamingContext context)
        {
        }
    }

    [Serializable]
    public class SelfDeleteException : Exception
    {
        public SelfDeleteException()
        {
        }

        protected SelfDeleteException(
                SerializationInfo info, 
                StreamingContext context)
        {
        }
    }

    [Serializable]
    public class ScriptDeleteException : Exception
    {
        public ScriptDeleteException()
        {
        }

        protected ScriptDeleteException(
                SerializationInfo info, 
                StreamingContext context)
        {
        }
    }

    /// <summary>
    /// Used to signal when the script is stopping in co-operation with the script engine 
    /// (instead of through Thread.Abort()).
    /// </summary>
    [Serializable]
    public class ScriptCoopStopException : Exception
    {
        public ScriptCoopStopException()
        {
        }

        protected ScriptCoopStopException(
                SerializationInfo info, 
                StreamingContext context)
        {
        }
    }

    public class DetectParams
    {
        public const int AGENT = 1;
        public const int ACTIVE = 2;
        public const int PASSIVE = 4;
        public const int SCRIPTED = 8;

        public DetectParams()
        {
            Key = UUID.Zero;
            OffsetPos = new LSL_Types.Vector3();
            LinkNum = 0;
            Group = UUID.Zero;
            Name = String.Empty;
            Owner = UUID.Zero;
            Position = new LSL_Types.Vector3();
            Rotation = new LSL_Types.Quaternion();
            Type = 0;
            Velocity = new LSL_Types.Vector3();
            initializeSurfaceTouch();
        }

        public UUID Key;
        public LSL_Types.Vector3 OffsetPos;
        public int LinkNum;
        public UUID Group;
        public string Name;
        public UUID Owner;
        public LSL_Types.Vector3 Position;
        public LSL_Types.Quaternion Rotation;
        public int Type;
        public LSL_Types.Vector3 Velocity;

        LSL_Types.Vector3 touchST;
        public LSL_Types.Vector3 TouchST { get { return touchST; } }

        LSL_Types.Vector3 touchNormal;
        public LSL_Types.Vector3 TouchNormal { get { return touchNormal; } }

        LSL_Types.Vector3 touchBinormal;
        public LSL_Types.Vector3 TouchBinormal { get { return touchBinormal; } }

        LSL_Types.Vector3 touchPos;
        public LSL_Types.Vector3 TouchPos { get { return touchPos; } }

        LSL_Types.Vector3 touchUV;
        public LSL_Types.Vector3 TouchUV { get { return touchUV; } }

        int touchFace;
        public int TouchFace { get { return touchFace; } }

        // This can be done in two places including the constructor
        // so be carefull what gets added here
        void initializeSurfaceTouch()
        {
            touchST = new LSL_Types.Vector3(-1.0, -1.0, 0.0);
            touchNormal = new LSL_Types.Vector3();
            touchBinormal = new LSL_Types.Vector3();
            touchPos = new LSL_Types.Vector3();
            touchUV = new LSL_Types.Vector3(-1.0, -1.0, 0.0);
            touchFace = -1;
        }

        /*
         * Set up the surface touch detected values
         */
        public SurfaceTouchEventArgs SurfaceTouchArgs
        {
            set
            {
                if (value == null)
                {
                    // Initialise to defaults if no value
                    initializeSurfaceTouch();
                }
                else
                {
                    // Set the values from the touch data provided by the client
                    touchST = new LSL_Types.Vector3(value.STCoord);
                    touchUV = new LSL_Types.Vector3(value.UVCoord);
                    touchNormal = new LSL_Types.Vector3(value.Normal);
                    touchBinormal = new LSL_Types.Vector3(value.Binormal);
                    touchPos = new LSL_Types.Vector3(value.Position);
                    touchFace = value.FaceIndex;
                }
            }
        }

        public void Populate(IScene scene)
        {
            SceneObjectPart part = (SceneObjectPart) scene.GetSceneObjectPart(Key);
            if (part == null) // Avatar, maybe?
            {
                IScenePresence presence = scene.GetScenePresence(Key);
                if (presence == null)
                    return;

                Name = presence.Name;
                Owner = Key;
                Position = new LSL_Types.Vector3(presence.AbsolutePosition);
                Rotation = new LSL_Types.Quaternion(
                        presence.Rotation.X,
                        presence.Rotation.Y,
                        presence.Rotation.Z,
                        presence.Rotation.W);
                Velocity = new LSL_Types.Vector3(presence.Velocity);
                Type = AGENT;

                if (presence.Velocity != Vector3.Zero)
                    Type |= ACTIVE;

                Group = presence.ControllingClient.ActiveGroupId;

                return;
            }

            if (!part.IsRoot)
                part = part.ParentGroup.RootPart; // We detect objects only

            LinkNum = 0; // Not relevant

            Group = part.GroupID;
            Name = part.Name;
            Owner = part.OwnerID;
            if (part.Velocity == Vector3.Zero)
                Type = PASSIVE;
            else
                Type = ACTIVE;

            foreach (ISceneChildEntity p in part.ParentGroup.Parts)
            {
                if (p.Inventory.ContainsScripts())
                {
                    Type |= SCRIPTED; // Scripted
                    break;
                }
            }

            Position = new LSL_Types.Vector3(part.AbsolutePosition);

            Quaternion wr = part.ParentGroup.GroupRotation;
            Rotation = new LSL_Types.Quaternion(wr.X, wr.Y, wr.Z, wr.W);

            Velocity = new LSL_Types.Vector3(part.Velocity);
        }
    }

    /// <summary>
    /// Holds all the data required to execute a scripting event.
    /// </summary>
    public class EventParams
    {
        public EventParams(string eventName, Object[] eventParams, DetectParams[] detectParams)
        {
            EventName = eventName;
            Params = eventParams;
            DetectParams = detectParams;
        }

        public string EventName;
        public Object[] Params;
        public DetectParams[] DetectParams;
    }

    /// <summary>
    ///     Threat Level for a scripting function
    /// </summary>
    public enum ThreatLevel
    {
        /// <summary>
        ///     Function is no threat at all. It doesn't constitute a threat to either users or the system and has no known side effects
        /// </summary>
        None = 0,

        /// <summary>
        ///     Abuse of this command can cause a nuisance to the region operator, such as log message spew
        /// </summary>
        Nuisance = 1,

        /// <summary>
        ///     Extreme levels of abuse of this function can cause impaired functioning of the region, or very gullible users can be tricked into experiencing harmless effects
        /// </summary>
        VeryLow = 2,

        /// <summary>
        ///     Intentional abuse can cause crashes or malfunction under certain circumstances, which can easily be rectified, or certain users can be tricked into certain situations in an avoidable manner.
        /// </summary>
        Low = 3,

        /// <summary>
        ///     Intentional abuse can cause denial of service and crashes with potential of data or state loss, or trusting users can be tricked into embarrassing or uncomfortable situations.
        /// </summary>
        Moderate = 4,

        /// <summary>
        ///     Casual abuse can cause impaired functionality or temporary denial of service conditions. Intentional abuse can easily cause crashes with potential data loss, or can be used to trick experienced and cautious users into unwanted situations, or changes global data permanently and without undo ability
        ///     Malicious scripting can allow theft of content
        /// </summary>
        High = 5,

        /// <summary>
        ///     Even normal use may, depending on the number of instances, or frequency of use, result in severe service impairment or crash with loss of data, or can be used to cause unwanted or harmful effects on users without giving the user a means to avoid it.
        /// </summary>
        VeryHigh = 6,

        /// <summary>
        ///     Even casual use is a danger to region stability, or function allows console or OS command execution, or function allows taking money without consent, or allows deletion or modification of user data, or allows the compromise of sensitive data by design.
        /// </summary>
        Severe = 7,

        NoAccess = 8
    }
}
