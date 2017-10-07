﻿using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASteambot
{
    public class HandleSteamChat
    {
        private Bot bot;

        private Dictionary<SteamID, int> ChatListener;

        public HandleSteamChat(Bot bot)
        {
            this.bot = bot;
            ChatListener = new Dictionary<SteamID, int>();
        }

        public void HandleMessage(SteamID partenar, string message)
        {
            string command = message.Split(' ')[0];

            if (!command.Equals("STOPHOOK") && ChatListener.ContainsKey(partenar))
            {
                SendMessageToGameServer(0, partenar, message);
                return;
            }

            switch(command)
            {
                case "help":
                    PrintHelp(partenar);
                break;

                case "SERVER":
                    PrintServer(partenar);
                break;

                case "HOOKCHAT":
                    HookGameServerChat(-2, partenar, message);
                break;

                case "STOPHOOK":
                    StopHook(-2, partenar);
                break;

                default:
                    bot.SteamFriends.SendChatMessage(partenar, EChatEntryType.ChatMsg, "Sorry I don't understand you. Yet.");
                break;
            }            
        }

        private void SendMessageToGameServer(int moduleID, SteamID partenar, string message)
        {
            int serverID = ChatListener[partenar];
            GameServer gs = bot.botManager.Servers[serverID - 1];

            string name = bot.SteamFriends.GetFriendPersonaName(partenar);
            string data = string.Format("{0}|{1} : {2}", (int)Networking.NetworkCode.ASteambotCode.Simple, name, message);
            gs.Send(moduleID, data);
        }

        public void StopHook(int moduleID, SteamID partenar)
        {
            int serverID = ChatListener[partenar];
            ChatListener.Remove(partenar);
            SendChatMessage(partenar, "Disconnecting to server...");

            foreach (KeyValuePair<SteamID, int> value in ChatListener)
            {
                if (value.Value == serverID && value.Key != partenar)
                    return;
            }

            GameServer server = bot.botManager.Servers[serverID-1];
            server.Send(moduleID, ((int)Networking.NetworkCode.ASteambotCode.Unhookchat).ToString());
        }

        public void PrintHelp(SteamID partenar)
        {
            SendChatMessage(partenar, "help - Print this message.");
            SendChatMessage(partenar, "SERVER - Print all servers connected to me.");
            SendChatMessage(partenar, "HOOKCHAT - Listen to what's being sent in the game server.");
            SendChatMessage(partenar, "STOPHOOK - Stop listening to what's being sent in the game server.");
        }

        public void PrintServer(SteamID partenar)
        {
            if (bot.botManager.Servers.Count > 0)
            {
                SendChatMessage(partenar, "---------------------------");
                foreach (GameServer gs in bot.botManager.Servers)
                {
                    string serverLine = String.Format("{0} - {1}:{2}", gs.Name, gs.IP, gs.Port);
                    SendChatMessage(partenar, serverLine);
                }
                SendChatMessage(partenar, "Number of registred servers : " + bot.botManager.Servers.Count);
                SendChatMessage(partenar, "---------------------------");
            }
            else
            {
                SendChatMessage(partenar, "No servers connected to me :'( !");
            }
        }

        public void ServerMessage(int serverid, string message)
        {
            foreach(KeyValuePair<SteamID, int> value in ChatListener)
            {
                if (value.Value == serverid)
                    SendChatMessage(value.Key, message);
            }
        }

        public void HookGameServerChat(int moduleID, SteamID partenar, string message)
        {
            if(ChatListener.ContainsKey(partenar))
            {
                SendChatMessage(partenar, "You are already hooking a chat ! Use : STOPHOOK");
                return;
            }

            message = message.Replace("HOOKCHAT ", String.Empty);

            int serverID = -1;
            Int32.TryParse(message, out serverID);

            if(serverID > bot.botManager.Servers.Count || serverID <= 0)
            {
                SendChatMessage(partenar, "Invalid game server ID. Use SERVER to get game server ID.");
                return;
            }

            GameServer gs = bot.botManager.Servers[serverID - 1];

            ChatListener.Add(partenar, serverID);

            SendChatMessage(partenar, "Connecting to server...");

            gs.Send(moduleID, ((int)Networking.NetworkCode.ASteambotCode.HookChat).ToString());
        }

        private void SendChatMessage(SteamID partenar, string message)
        {
            bot.SteamFriends.SendChatMessage(partenar, EChatEntryType.ChatMsg, message);
        }
    }
}
