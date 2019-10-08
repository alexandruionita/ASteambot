﻿using ASteambot.SteamMarketUtility;
using SteamKit2;
using SteamTrade;
using SteamTrade.TradeOffer;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ASteambot.SteamProfile;

namespace ASteambot.Networking
{
    public class HandleMessage
    {
        public HandleMessage() { }

        private int SINGLE_SERVER_ID;

        public void Execute(Bot bot, GameServerRequest gsr)
        {
            try
            {
                switch ((NetworkCode.ASteambotCode)gsr.NetworkCode)
                {
                    case NetworkCode.ASteambotCode.Core:
                        RegisterBot(bot, gsr);
                        break;
                    case NetworkCode.ASteambotCode.Disconnect:
                        Disconnect(bot, gsr);
                        break;
                    case NetworkCode.ASteambotCode.HookChat:
                        HookChat(bot, gsr);
                        break;
                    case NetworkCode.ASteambotCode.ScanInventory:
                        ScanInventory(bot, gsr, false);
                        break;
                    case NetworkCode.ASteambotCode.ScanInventoryIMG:
                        ScanInventory(bot, gsr, true);
                        break;
                    case NetworkCode.ASteambotCode.CreateTradeOffer:
                        CreateTradeOffer(bot, gsr);
                        break;
                    case NetworkCode.ASteambotCode.FriendInvite:
                        SendFriendInvitation(bot, gsr);
                        break;
                    case NetworkCode.ASteambotCode.ReportPlayer:
                        ReportPlayer(bot, gsr);
                        break;
                    case NetworkCode.ASteambotCode.InviteSteamGroup:
                        InviteToSteamGroup(bot, gsr);
                        break;
                    case NetworkCode.ASteambotCode.Unhookchat:
                        UnhookChat(bot, gsr);
                        break;
                    case NetworkCode.ASteambotCode.SGAnnoucement:
                        PostSteamGroupAnnoucement(bot, gsr);
                        break;
                    case NetworkCode.ASteambotCode.SteamID:
                        SendSteamID(bot, gsr);
                        break;
                    case NetworkCode.ASteambotCode.Simple:
                        SendChatMessage(bot, gsr);
                        break;
                }
                /*if (!bot.Friends.Contains(steamID))
                {
                    bot.Manager.Send(serverID, gsr.ModuleID, NetworkCode.ASteambotCode.NotFriends, gsr.Arguments);
                    return;
                }*/
            }
            catch(Exception e)
            {
                var st = new StackTrace(e, true);
                var frame = st.GetFrame(0);
                var line = frame.GetFileLineNumber();

                Program.PrintErrorMessage(e.ToString());
                Program.PrintErrorMessage("Crashing while executing net code " + gsr.NetworkCode + " !");
                Program.PrintErrorMessage("Full detail message [MAY CONTAIN SENSITIVE INFOS] :");
                Program.PrintErrorMessage("SRV ID : " + gsr.ServerID + " MDL ID: " + gsr.ModuleID);
                Program.PrintErrorMessage(gsr.Arguments);
                Program.PrintErrorMessage("Line number : " + line);
            }
        }
        
        private SteamID GetSteamIDFromString(string ssID)
        {
            if (ssID.Equals("BOT"))
                return new SteamID();

            if (ssID.StartsWith("STEAM_"))
                return new SteamID(ssID);
            else if (ssID.Trim('[').Trim(']').StartsWith("U:"))
                return new SteamID(ssID);
            else
                return new SteamID(ulong.Parse(ssID));
        }

        private void RegisterBot(Bot bot, GameServerRequest gsr)
        {
            IPEndPoint ipendpoint = ((IPEndPoint)gsr.Socket.RemoteEndPoint);
            
            int index = bot.Manager.Servers.FindIndex(f => f.IP == ipendpoint.Address);

            if (index >= 0)
                return;

            SINGLE_SERVER_ID++;

            GameServer gameserver = new GameServer(gsr.Socket, bot.Manager.Config.TCPPassword, SINGLE_SERVER_ID, gsr.Arguments);
            GameServer gs = bot.Manager.Servers.Find(srv => srv.SteamID == gameserver.SteamID);

            if (gs == null)
                bot.Manager.Servers.Add(gameserver);
            else
                gs.Update(gameserver);
        }

        private void Disconnect(Bot bot, GameServerRequest gsr)
        {
            bot.Manager.DisconnectServer(gsr.ServerID);
        }

        private void ReportPlayer(Bot bot, GameServerRequest gsr)
        {
            GameServer gs = bot.Manager.GetServerByID(gsr.ServerID);

            string[] ids = gsr.Arguments.Split('/');
            SteamID steamID = GetSteamIDFromString(ids[0]);
            
            if (!steamID.IsValid)
                return;

            SteamID reportedDude = GetSteamIDFromString(ids[1]);

            if (!steamID.IsValid)
                return;

            Infos spGuy = bot.GetSteamProfileInfo(steamID);
            Infos spDude = bot.GetSteamProfileInfo(reportedDude);

            if (spDude != null && spGuy != null)
            {
                string firstMsg = String.Format("{0} ({1}) reported {2} ({3}) for \"{4}\" @ {5} ({6}) !", spGuy.Name, steamID.ToString(), spDude.Name, reportedDude.ToString(), ids[2], DateTime.Now.ToString("dd/MM/yyyy"), DateTime.Now.ToString("HH:mm"));
                /*string[] data = { spGuy.Name, steamID.ToString(), spDude.Name, reportedDude.ToString(), ids[2], DateTime.Now.ToString("dd/MM/yyyy"), DateTime.Now.ToString("HH:mm") };
                string firstMsg = String.Format("REPORT_MSG_1", data);*/
                string secondMsg = String.Format("Name of server : {0}", gs.Name);
                string thirdMsg = String.Format("Direct URL : steam://connect/{0}:{1}", gs.IP, gs.Port);

                foreach (SteamID steamid in bot.Friends)
                {
                    if (bot.Config.IsAdmin(steamid) || bot.Config.IsAdmin(steamid))
                    {
                        bot.SteamFriends.SendChatMessage(steamid, EChatEntryType.ChatMsg, firstMsg);
                        Thread.Sleep(100);
                        bot.SteamFriends.SendChatMessage(steamid, EChatEntryType.ChatMsg, secondMsg);
                        Thread.Sleep(100);
                        bot.SteamFriends.SendChatMessage(steamid, EChatEntryType.ChatMsg, thirdMsg);
                    }
                }
            }
            else
            {
                Program.PrintErrorMessage("One of the following steam ID is wrong !");
                Program.PrintErrorMessage("> " + ids[0]);
                Program.PrintErrorMessage("> " + ids[1]);
                Program.PrintErrorMessage("Report was denied !");
            }
        }

        private void HookChat(Bot bot, GameServerRequest gsr)
        {
            bot.SteamchatHandler.ServerMessage(gsr.ServerID, gsr.Arguments);
        }

        private void UnhookChat(Bot bot, GameServerRequest gsr)
        {
            List<SteamID> toRemove = new List<SteamID>();
            foreach (KeyValuePair<SteamID, int> value in bot.ChatListener)
            {
                if (value.Value == gsr.ServerID)
                {
                    SteamID partenar = value.Key;
                    toRemove.Add(partenar);

                    bot.SteamFriends.SendChatMessage(partenar, EChatEntryType.ChatMsg, "Server sent a unhook chat package, disconnecting...");
                }
            }

            foreach (SteamID cl in toRemove)
            {
                bot.ChatListener.Remove(cl);
                bot.SteamFriends.SendChatMessage(cl, EChatEntryType.ChatMsg, "Done !");
            }
        }

        private void ScanInventory(Bot bot, GameServerRequest gsr, bool withImg)
        {
            if (bot.ArkarrSteamMarket == null)
                bot.ArkarrSteamMarket = new SteamMarket(bot.Config.ArkarrAPIKey, bot.Config.DisableMarketScan, bot.SteamWeb);
            
            SteamID steamID = GetSteamIDFromString(gsr.Arguments);

            if (!steamID.IsValid)
                return;

            if (!bot.Friends.Contains(steamID.ConvertToUInt64()))
            {
                bot.Manager.Send(gsr.ServerID, gsr.ModuleID, NetworkCode.ASteambotCode.NotFriends, steamID.ConvertToUInt64().ToString());
                return;
            }

            Thread invScan = new Thread(() =>
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                Thread.CurrentThread.IsBackground = true;

                string items = gsr.Arguments + "/";

                items += AddInventoryItems(bot, Games.TF2, steamID, withImg) + "/";
                items += AddInventoryItems(bot, Games.CSGO, steamID, withImg) + "/";
                items += AddInventoryItems(bot, Games.Dota2, steamID, withImg);

                if (withImg)
                    bot.Manager.Send(gsr.ServerID, gsr.ModuleID, NetworkCode.ASteambotCode.ScanInventoryIMG, items);
                else
                    bot.Manager.Send(gsr.ServerID, gsr.ModuleID, NetworkCode.ASteambotCode.ScanInventory, items);
            });

            invScan.Start();
            invScan.Join();
        }
        
        private string AddInventoryItems(Bot bot, Games game, SteamID steamID, bool img)
        {
            string items = "";
            long[] contextID = new long[1];
            contextID[0] = 2;

            bot.OtherGenericInventory.load((int)game, contextID, steamID);

            if (bot.OtherGenericInventory.errors.Count > 0)
            {
                Program.PrintErrorMessage("Error while inventory scan :");
                foreach (string error in bot.OtherGenericInventory.errors)
                {
                    Program.PrintErrorMessage(error);
                }
                items = "EMPTY";
            }
            else
            {
                if (bot.OtherGenericInventory.items.Count == 0) //time out
                {
                    items = "TIME_OUT";
                }
                else
                {
                    bool allItemsFound = false;
                    while (!allItemsFound)
                    {
                        allItemsFound = true;

                        foreach (GenericInventory.Item item in bot.OtherGenericInventory.items.Values)
                        {
                            GenericInventory.ItemDescription description = bot.OtherGenericInventory.getDescription(item.assetid);

                            Item i = bot.ArkarrSteamMarket.GetItemByName(description.market_hash_name);
                            if (description.tradable)
                            {
                                if (i != null)// && i.Value != 0)
                                    items += item.assetid + "=" + description.market_hash_name.Replace("|", " - ") + "=" + (i.Value.ToString().Replace(',', '.')) + (img ? "=" + i.Image : "") + ",";
                                else
                                    items += item.assetid + "=" + description.market_hash_name.Replace("|", " - ") + "=" + "0" + (img ? "=" + "NOT_FOUND" : "") + ",";
                            }
                        }
                    }

                    if (items.Length != 0)
                        items = items.Remove(items.Length - 1);
                    else
                        items = "EMPTY";
                }
            }

            return items;
        }

        private void CreateTradeOffer(Bot bot, GameServerRequest gsr)
        {
            float tradeValue = -1;
            string message = gsr.Arguments;
            string[] assetIDs = null;
            string[] myAssetIDs = null;
            string[] steamIDitems = message.Split('/');
            SteamID steamid = GetSteamIDFromString(steamIDitems[0]);

            if (!steamid.IsValid)
                return;

            TradeOffer to = bot.TradeOfferManager.NewOffer(steamid);

            GameServer gameServer = bot.Manager.GetServerByID(gsr.ServerID);

            if (steamIDitems[1].Length > 1 && steamIDitems[1] != "NULL")
            {
                assetIDs = steamIDitems[1].Split(',');

                List<long> contextId = new List<long>();
                contextId.Add(2);

                bot.OtherGenericInventory.load((int)Games.CSGO, contextId, steamid);

                foreach (GenericInventory.Item item in bot.OtherGenericInventory.items.Values)
                {
                    if (Array.IndexOf(assetIDs, item.assetid.ToString()) > -1)
                    {
                        GenericInventory.ItemDescription description = bot.OtherGenericInventory.getDescription(item.assetid);
                        to.Items.AddTheirItem(item.appid, item.contextid, (long)item.assetid);
                    }
                }
                bot.OtherGenericInventory.load((int)Games.TF2, contextId, steamid);

                foreach (GenericInventory.Item item in bot.OtherGenericInventory.items.Values)
                {
                    if (Array.IndexOf(assetIDs, item.assetid.ToString()) > -1)
                    {
                        GenericInventory.ItemDescription description = bot.OtherGenericInventory.getDescription(item.assetid);
                        to.Items.AddTheirItem(item.appid, item.contextid, (long)item.assetid);
                    }
                }
                bot.OtherGenericInventory.load((int)Games.Dota2, contextId, steamid);

                foreach (GenericInventory.Item item in bot.OtherGenericInventory.items.Values)
                {
                    if (Array.IndexOf(assetIDs, item.assetid.ToString()) > -1)
                    {
                        GenericInventory.ItemDescription description = bot.OtherGenericInventory.getDescription(item.assetid);
                        to.Items.AddTheirItem(item.appid, item.contextid, (long)item.assetid);
                    }
                }
            }

            if(steamIDitems[2].Length > 1 && steamIDitems[2] != "NULL")
            {
                myAssetIDs = steamIDitems[2].Split(',');

                List<long> contextId = new List<long>();
                contextId.Add(2);

                bot.MyGenericInventory.load((int)Games.CSGO, contextId, steamid);

                foreach (GenericInventory.Item item in bot.OtherGenericInventory.items.Values)
                {
                    if (Array.IndexOf(myAssetIDs, item.assetid.ToString()) > -1)
                    {
                        GenericInventory.ItemDescription description = bot.OtherGenericInventory.getDescription(item.assetid);
                        to.Items.AddTheirItem(item.appid, item.contextid, (long)item.assetid);
                    }
                }

                bot.MyGenericInventory.load((int)Games.TF2, contextId, steamid);

                foreach (GenericInventory.Item item in bot.OtherGenericInventory.items.Values)
                {
                    if (Array.IndexOf(myAssetIDs, item.assetid.ToString()) > -1)
                    {
                        GenericInventory.ItemDescription description = bot.OtherGenericInventory.getDescription(item.assetid);
                        to.Items.AddTheirItem(item.appid, item.contextid, (long)item.assetid);
                    }
                }

                bot.MyGenericInventory.load((int)Games.Dota2, contextId, steamid);

                foreach (GenericInventory.Item item in bot.OtherGenericInventory.items.Values)
                {
                    if (Array.IndexOf(myAssetIDs, item.assetid.ToString()) > -1)
                    {
                        GenericInventory.ItemDescription description = bot.OtherGenericInventory.getDescription(item.assetid);
                        to.Items.AddTheirItem(item.appid, item.contextid, (long)item.assetid);
                    }
                }
            }
            
            if (steamIDitems[3] != "NULL")
                tradeValue = float.Parse(steamIDitems[3], CultureInfo.InvariantCulture);
            
            string offerId;
            to.Send(out offerId, String.Format("\"{0}\" the {1}@{2}", gameServer.Name, DateTime.Now.ToString("dd/MM/yyyy"), DateTime.Now.ToString("HH:mm")));

            if (offerId != "")
            {
                bot.Manager.Send(gsr.ServerID, gsr.ModuleID, NetworkCode.ASteambotCode.CreateTradeOffer, steamid.ConvertToUInt64()  +"/" + offerId);
                bot.TradeoffersGS.Add(offerId, gsr.ServerID+"|"+gsr.ModuleID+"|"+tradeValue);
                bot.TradeOfferValue.Add(offerId, tradeValue);

                bot.AcceptMobileTradeConfirmation(offerId);
            }
            else
            {
                bot.Manager.Send(gsr.ServerID, gsr.ModuleID, NetworkCode.ASteambotCode.CreateTradeOffer, steamid.ConvertToUInt64() + "/" + "-1");
            }
        }

        private void SendFriendInvitation(Bot bot, GameServerRequest gsr)
        {
            SteamID steamID = GetSteamIDFromString(gsr.Arguments);
            if (steamID.IsValid)
                bot.SteamFriends.AddFriend(steamID.ConvertToUInt64());
        }

        private void SendSteamID(Bot bot, GameServerRequest gsr)
        {
            bot.Manager.Send(gsr.ServerID, gsr.ModuleID, NetworkCode.ASteambotCode.SteamID, bot.getSteamID().ConvertToUInt64().ToString());
        }

        private void InviteToSteamGroup(Bot bot, GameServerRequest gsr)
        {
            GameServer gs = bot.Manager.GetServerByID(gsr.ServerID);

            string[] steamIDgroupID = gsr.Arguments.Split('/');

            if (steamIDgroupID.Length == 2)
            {
                SteamID steamID = GetSteamIDFromString(steamIDgroupID[0]);
                SteamID groupID = GetSteamIDFromString(steamIDgroupID[1]);
                if (steamID.IsValid)
                {
                    if (bot.Friends.Contains(steamID.ConvertToUInt64()))
                    {
                        if (groupID.IsValid)
                        {
                            bot.InviteUserToGroup(steamID, groupID);
                            if(gs != null)
                                bot.Manager.Send(gsr.ServerID, gsr.ModuleID, NetworkCode.ASteambotCode.InviteSteamGroup, steamID.ConvertToUInt64().ToString());
                            else
                                Console.WriteLine(">>>> COUDLN'T FIND SERVER; NO REPLY SENT !");
                        }
                    }
                    else
                    {
                        if (gs != null)
                            bot.Manager.Send(gsr.ServerID, gsr.ModuleID, NetworkCode.ASteambotCode.NotFriends, steamID.ConvertToUInt64().ToString());
                        else
                            Console.WriteLine(">>>> COUDLN'T FIND SERVER; NO REPLY SENT !");
                    }
                }
            }
        }
        
        private void PostSteamGroupAnnoucement(Bot bot, GameServerRequest gsr)
        {   
            GameServer gs = bot.Manager.GetServerByID(gsr.ServerID);

            string[] groupIDHeadlineBody = gsr.Arguments.Split(new char[] { '/' }, 3);

            var data = new NameValueCollection();
            data.Add("sessionID", bot.SteamWeb.SessionId);
            data.Add("action", "post");
            data.Add("headline", groupIDHeadlineBody[1]);
            data.Add("body", groupIDHeadlineBody[2]);
            data.Add("languages[0][headline]", groupIDHeadlineBody[1]);
            data.Add("languages[0][body]", groupIDHeadlineBody[2]);

            string link = "https://steamcommunity.com/gid/" + groupIDHeadlineBody[0] + "/announcements";
            
            bot.SteamWeb.Fetch(link, "POST", data);

            if (gs != null)
                bot.Manager.Send(gsr.ServerID, gsr.ModuleID, NetworkCode.ASteambotCode.SGAnnoucement, groupIDHeadlineBody[1]);
        }

        private void SendChatMessage(Bot bot, GameServerRequest gsr)
        {
            string[] steamID_msg = gsr.Arguments.Split(new char[] { '/' }, 2);
            SteamID steamID = GetSteamIDFromString(steamID_msg[0]);

            if (steamID == null || !steamID.IsValid)
            {
                bot.Manager.Send(gsr.ServerID, gsr.ModuleID, NetworkCode.ASteambotCode.Simple, "Invalid steam ID (STEAM_X:Y:ZZZZ) !");
            }
            else if (!bot.Friends.Contains(steamID.ConvertToUInt64()))
            {
                bot.Manager.Send(gsr.ServerID, gsr.ModuleID, NetworkCode.ASteambotCode.NotFriends, steamID.ConvertToUInt64().ToString());
            }
            else
            {
                if (steamID_msg[1].StartsWith("steam://connect/"))
                {
                    /*Random rnd = new Random();
                    string key = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 10).Select(s => s[rnd.Next(s.Length)]).ToArray());
                    while (!Program.httpsrv.AddToRedirectTable(key, steamID_msg[1], out key))
                        key = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 10).Select(s => s[rnd.Next(s.Length)]).ToArray());

                    bot.SteamFriends.SendChatMessage(steamID, EChatEntryType.ChatMsg, key);*/
                    bot.GSMH.SendGameInvite(bot.getSteamID(), steamID, steamID_msg[1].Replace("steam://connect/", ""));
                }
                else
                {
                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                        foreach (string line in steamID_msg[1].Split(new[] { "\n" }, StringSplitOptions.None))
                        {
                            bot.SteamFriends.SendChatMessage(steamID, EChatEntryType.ChatMsg, line);
                            Thread.Sleep(1300);
                        }
                    }).Start();
                }
            }
        }
    }
}
