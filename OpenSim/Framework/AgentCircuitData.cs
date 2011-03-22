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
using System.Reflection;
using System.Collections.Generic;
using log4net;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Framework
{
    /// <summary>
    /// Circuit data for an agent.  Connection information shared between
    /// regions that accept UDP connections from a client
    /// </summary>
    public class AgentCircuitData
    {
        // DEBUG ON
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);
        // DEBUG OFF

        /// <summary>
        /// Avatar Unique Agent Identifier
        /// </summary>
        public UUID AgentID;

        /// <summary>
        /// Avatar's Appearance
        /// </summary>
        public AvatarAppearance Appearance;

        /// <summary>
        /// Base Caps path for user
        /// </summary>
        public string CapsPath = String.Empty;

        /// <summary>
        /// Root agent, or Child agent
        /// </summary>
        public bool child;

        /// <summary>
        /// Number given to the client when they log-in that they provide 
        /// as credentials to the UDP server
        /// </summary>
        public uint circuitcode;

        /// <summary>
        /// How this agent got here
        /// </summary>
        public uint teleportFlags;

        /// <summary>
        /// Random Unique GUID for this session.  Client gets this at login and it's
        /// only supposed to be disclosed over secure channels
        /// </summary>
        public UUID SecureSessionID;

        /// <summary>
        /// Non secure Session ID
        /// </summary>
        public UUID SessionID;

        /// <summary>
        /// Other unknown info
        /// </summary>
        public OSDMap OtherInformation = new OSDMap();

        /// <summary>
        /// Hypergrid service token; generated by the user domain, consumed by the receiving grid.
        /// There is one such unique token for each grid visited.
        /// </summary>
        public string ServiceSessionID = string.Empty;

        /// <summary>
        /// The client's IP address, as captured by the login service
        /// </summary>
        public string IPAddress;

        /// <summary>
        /// Position the Agent's Avatar starts in the region
        /// </summary>
        public Vector3 startpos;

        public AgentCircuitData()
        {
        }

         /// <summary>
        /// Pack AgentCircuitData into an OSDMap for transmission over LLSD XML or LLSD json
        /// </summary>
        /// <returns>map of the agent circuit data</returns>
        public OSDMap PackAgentCircuitData()
        {
            OSDMap args = new OSDMap();
            args["agent_id"] = OSD.FromUUID(AgentID);
            args["caps_path"] = OSD.FromString(CapsPath);

            args["child"] = OSD.FromBoolean(child);
            args["circuit_code"] = OSD.FromString(circuitcode.ToString());
            args["secure_session_id"] = OSD.FromUUID(SecureSessionID);
            args["session_id"] = OSD.FromUUID(SessionID);

            args["service_session_id"] = OSD.FromString(ServiceSessionID);
            args["start_pos"] = OSD.FromString(startpos.ToString());
            args["client_ip"] = OSD.FromString(IPAddress);
            args["otherInfo"] = OSDParser.SerializeLLSDXmlString(OtherInformation);
            args["teleport_flags"] = OSD.FromUInteger(teleportFlags);
            
            if (Appearance != null)
            {
                args["appearance_serial"] = OSD.FromInteger(Appearance.Serial);

                OSDMap appmap = Appearance.Pack();
                args["packed_appearance"] = appmap;
            }

            return args;
        }

        public AgentCircuitData Copy()
        {
            AgentCircuitData Copy = new AgentCircuitData();

            Copy.AgentID = AgentID;
            Copy.Appearance = Appearance;
            Copy.CapsPath = CapsPath;
            Copy.child = child;
            Copy.circuitcode = circuitcode;
            Copy.IPAddress = IPAddress;
            Copy.SecureSessionID = SecureSessionID;
            Copy.SessionID = SessionID;
            Copy.startpos = startpos;
            Copy.teleportFlags = teleportFlags;
            Copy.OtherInformation = OtherInformation;

            return Copy;
        }

        /// <summary>
        /// Unpack agent circuit data map into an AgentCiruitData object
        /// </summary>
        /// <param name="args"></param>
        public void UnpackAgentCircuitData(OSDMap args)
        {
            if (args["agent_id"] != null)
                AgentID = args["agent_id"].AsUUID();
            if (args["caps_path"] != null)
                CapsPath = args["caps_path"].AsString();

            if (args["child"] != null)
                child = args["child"].AsBoolean();
            if (args["circuit_code"] != null)
                UInt32.TryParse(args["circuit_code"].AsString(), out circuitcode);
            if (args["secure_session_id"] != null)
                SecureSessionID = args["secure_session_id"].AsUUID();
            if (args["session_id"] != null)
                SessionID = args["session_id"].AsUUID();
            if (args["service_session_id"] != null)
                ServiceSessionID = args["service_session_id"].AsString();
            if (args["client_ip"] != null)
                IPAddress = args["client_ip"].AsString();

            if (args["start_pos"] != null)
                Vector3.TryParse(args["start_pos"].AsString(), out startpos);

            if (args["teleport_flags"] != null)
                teleportFlags = args["teleport_flags"].AsUInteger();

            // DEBUG ON
            //m_log.WarnFormat("[AGENTCIRCUITDATA] agentid={0}, child={1}, startpos={2}", AgentID, child, startpos.ToString());
            // DEBUG OFF

            try
            {
                // Unpack various appearance elements
                Appearance = new AvatarAppearance(AgentID);

                // Eventually this code should be deprecated, use full appearance
                // packing in packed_appearance
                if (args["appearance_serial"] != null)
                    Appearance.Serial = args["appearance_serial"].AsInteger();

                if (args.ContainsKey("packed_appearance") && (args["packed_appearance"].Type == OSDType.Map))
                {
                    Appearance.Unpack((OSDMap)args["packed_appearance"]);
                    // DEBUG ON
                    //m_log.WarnFormat("[AGENTCIRCUITDATA] unpacked appearance");
                    // DEBUG OFF
                }
                // DEBUG ON
                else
                    m_log.Warn("[AGENTCIRCUITDATA] failed to find a valid packed_appearance");
                // DEBUG OFF
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[AGENTCIRCUITDATA] failed to unpack appearance; {0}", e.ToString());
            }

            if (args.ContainsKey("otherInfo"))
                OtherInformation = (OSDMap)OSDParser.DeserializeLLSDXml(args["otherInfo"].AsString());
        }
    }
}
