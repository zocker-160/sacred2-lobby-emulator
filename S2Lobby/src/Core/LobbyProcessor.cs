﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using S2Library.Protocol;

namespace S2Lobby
{
    public class LobbyProcessor : ServerProcessor
    {
        //private readonly byte[] _userData = Crypto.BytesFromHexString("00000000040000000000000000000000000000000000000006000000");
        private readonly byte[] _nicknameData = Crypto.BytesFromHexString("000000003900000000000000000000000000000000000000a2000000785edbc9c8800cd880b8842195a1184c1631e000ff819891094508668e438300886265604788beb8ce644b0c8d0d1c05004a9b0ff3");

        private Server _server;
        private static readonly ConcurrentDictionary<uint, uint> ServerUpdateReceivers = new ConcurrentDictionary<uint, uint>();

        public LobbyProcessor(Program program, uint connection) : base(program, connection)
        {
        }

        public override void Close()
        {
            base.Close();

            if (_server != null)
            {
                Program.Servers.Remove(_server.Id);
                NotifyUnlistServer(_server.Id, _server.Running);
                _server = null;
            }
        }

        protected sealed override bool HandlePayloadType(Payloads.Types payloadType, PayloadPrefix payload, PayloadWriter writer)
        {
            if (base.HandlePayloadType(payloadType, payload, writer))
            {
                return true;
            }

            switch (payloadType)
            {
                case Payloads.Types.RegisterNickname:
                    HandleRegisterNickname((RegisterNickname)payload, writer);
                    return true;
                case Payloads.Types.ConfirmNickname:
                    HandleConfirmNickname((ConfirmNickname)payload, writer);
                    return true;
                case Payloads.Types.GetCDKeys:
                    HandleGetCdKeys((GetCDKeys)payload, writer);
                    return true;
                case Payloads.Types.RequestMOTD:
                    HandleGetMOTD((RequestMOTD)payload, writer);
                    return true;
                case Payloads.Types.GetUserInfo:
                    HandleGetUserInfo((GetUserInfo)payload, writer);
                    return true;
                case Payloads.Types.GetPlayerInfo:
                    HandleGetCharacterInfo((GetPlayerInfo)payload, writer);
                    return true;
                case Payloads.Types.GetChatServer:
                    HandleGetChatServer((GetChatServer)payload, writer);
                    return true;
                case Payloads.Types.SelectNickname:
                    HandleSelectNickname((SelectNickname)payload, writer);
                    return true;
                case Payloads.Types.RegisterServer:
                    HandleRegisterServer((RegisterServer)payload, writer);
                    return true;
                case Payloads.Types.GetServers:
                    HandleGetServers((GetServers)payload, writer);
                    return true;
                case Payloads.Types.StopServerUpdates:
                    HandleStopServerUpdates((StopServerUpdates)payload, writer);
                    return true;
                case Payloads.Types.UnknownType056:
                    HandlePayload056((Payload56)payload, writer);
                    return true;
                case Payloads.Types.RegObserverUserLogin:
                    HandleRegObserverUserLogin((RegObserverUserLogin)payload, writer);
                    return true;
                case Payloads.Types.DeregObserverUserLogin:
                    HandleDeregObserverUserLogin((DeregObserverUserLogin)payload, writer);
                    return true;
                case Payloads.Types.UnknownType157:
                    HandlePayload157((Payload157)payload, writer);
                    return true;
                case Payloads.Types.ConnectToServer:
                    HandleConnectToServer((ConnectToServer)payload, writer);
                    return true;
                case Payloads.Types.UpdateServerInfo:
                    HandleUpdateServerInfo((UpdateServerInfo)payload, writer);
                    return true;
                case Payloads.Types.UnlistServer:
                    HandleUnlistServer((UnlistServer) payload, writer);
                    return true;
                case Payloads.Types.PlayerJoinedServer:
                    HandlePlayerJoinedServer((PlayerJoinedServer)payload, writer);
                    return true;
                case Payloads.Types.PlayerLeftServer:
                    HandlePlayerLeftServer((PlayerLeftServer)payload, writer);
                    return true;
                
                // Chat related packages
                case Payloads.Types.RegObserverGlobalChat:
                    HandleRegObserverGlobalChat((RegObserverGlobalChat)payload, writer);
                    return true;
                case Payloads.Types.DeregObserverGlobalChat:
                    HandleDeregObserverGlobalChat((DeregObserverGlobalChat)payload, writer);
                    return true;
                case Payloads.Types.ChatPayload:
                    HandleChatPayload((ChatPayload) payload, writer);
                    return true;
                
                
                default:
                    Logger.LogError($"{payloadType} not implemented!");
                    return false;
            }
        }

        private void HandleRegisterNickname(RegisterNickname payload, PayloadWriter writer)
        {
            uint accountId = payload.OwnerId;
            string nickname = payload.Name;

            if (accountId != 0)
            {
                StatusWithId resultPayload1 = Payloads.CreatePayload<StatusWithId>();
                resultPayload1.Errorcode = 1;
                resultPayload1.Errormsg = "Incorrect account";
                resultPayload1.Id = payload.OwnerId;
                resultPayload1.TicketId = payload.TicketId;
                SendReply(writer, resultPayload1);
                return;
            }

            Program.Accounts.SetNickname(Database.Connection, Account.Id, nickname);
            Account.PlayerName = nickname;

            StatusWithId resultPayload2 = Payloads.CreatePayload<StatusWithId>();
            resultPayload2.Errorcode = 0;
            resultPayload2.Errormsg = null;
            resultPayload2.Id = Account.Id;
            resultPayload2.TicketId = payload.TicketId;
            SendReply(writer, resultPayload2);
        }

        private void HandleConfirmNickname(ConfirmNickname payload, PayloadWriter writer)
        {
            uint accountId = payload.UserId;
            if (accountId != Account.Id)
            {
                ResultStatusMsg resultPayload1 = Payloads.CreatePayload<ResultStatusMsg>();
                resultPayload1.Errorcode = 1;
                resultPayload1.Errormsg = "Incorrect account";
                resultPayload1.TicketId = payload.TicketId;
                SendReply(writer, resultPayload1);
                return;
            }

            string mail = payload.Mail;
            if (mail != null)
            {
                Program.Accounts.SetEmail(Database.Connection, accountId, mail);
                Account.Email = mail;
            }

            byte[] nicknameData = payload.Data;
            if (nicknameData != null)
            {
                Program.Accounts.SetUserData(Database.Connection, accountId, nicknameData);
                Account.UserData = nicknameData;
            }

            ResultStatusMsg resultPayload2 = Payloads.CreatePayload<ResultStatusMsg>();
            resultPayload2.Errorcode = 0;
            resultPayload2.Errormsg = null;
            resultPayload2.TicketId = payload.TicketId;
            SendReply(writer, resultPayload2);
        }

        private void HandleGetCdKeys(GetCDKeys payload, PayloadWriter writer)
        {
            uint accountId = payload.UserId;
            if (accountId != Account.Id)
            {
                ResultStatusMsg resultPayload1 = Payloads.CreatePayload<ResultStatusMsg>();
                resultPayload1.Errorcode = 1;
                resultPayload1.Errormsg = "Incorrect account";
                resultPayload1.TicketId = payload.TicketId;
                SendReply(writer, resultPayload1);
                return;
            }

            SendCDKey resultPayload2 = Payloads.CreatePayload<SendCDKey>();
            resultPayload2.UserId = accountId;
            resultPayload2.CdKey = "0000000000000000";
            resultPayload2.Keypool = 1;
            resultPayload2.TicketId = payload.TicketId;
            SendReply(writer, resultPayload2);

            ResultStatusMsg resultPayload3 = Payloads.CreatePayload<ResultStatusMsg>();
            resultPayload3.Errorcode = 0;
            resultPayload3.Errormsg = null;
            resultPayload3.TicketId = payload.TicketId;
            SendReply(writer, resultPayload3);
        }

        private void HandleGetMOTD(RequestMOTD payload, PayloadWriter writer)
        {
            SendMOTD resultPayload = Payloads.CreatePayload<SendMOTD>();
            
            // I need to get rid of the last char, since that breaks the MOTD for some reason
            string name = Account.UserName.Substring(0, Account.UserName.Length - 1);
            resultPayload.Txt = Constants.MOTD.Replace("%name%", name);
            resultPayload.TicketId = payload.TicketId;
            SendReply(writer, resultPayload);
        }

        private void HandleGetUserInfo(GetUserInfo payload, PayloadWriter writer)
        {
            uint accountId = payload.UserId;
            if (accountId != Account.Id)
            {
                ResultStatusMsg resultPayload1 = Payloads.CreatePayload<ResultStatusMsg>();
                resultPayload1.Errorcode = 1;
                resultPayload1.Errormsg = "Incorrect account";
                resultPayload1.TicketId = payload.TicketId;
                SendReply(writer, resultPayload1);
                return;
            }

            SendUserInfo resultPayload2 = Payloads.CreatePayload<SendUserInfo>();
            resultPayload2.UserId = accountId;
            resultPayload2.Name = Account.UserName;
            resultPayload2.Password = null;
            resultPayload2.Mail = Account.Email;
            resultPayload2.Banned = false;
            resultPayload2.Active = true;
            resultPayload2.Status = 2;
            resultPayload2.Data = Account.UserData;
            resultPayload2.Created = "2012-04-02 00:00:00+0:00";
            resultPayload2.LastLogin = "2012-04-02 00:00:00+0:00";
            resultPayload2.TotalLogins = 1;
            resultPayload2.TicketId = payload.TicketId;
            SendReply(writer, resultPayload2);

            ResultStatusMsg resultPayload3 = Payloads.CreatePayload<ResultStatusMsg>();
            resultPayload3.Errorcode = 0;
            resultPayload3.Errormsg = null;
            resultPayload3.TicketId = payload.TicketId;
            SendReply(writer, resultPayload3);
        }

        private void HandleGetCharacterInfo(GetPlayerInfo payload, PayloadWriter writer)
        {
            uint accountId = payload.UserId;
            if (accountId != Account.Id)
            {
                ResultStatusMsg resultPayload1 = Payloads.CreatePayload<ResultStatusMsg>();
                resultPayload1.Errorcode = 1;
                resultPayload1.Errormsg = "Incorrect account";
                resultPayload1.TicketId = payload.TicketId;
                SendReply(writer, resultPayload1);
                return;
            }

            SendPlayerInfo resultPayload2 = Payloads.CreatePayload<SendPlayerInfo>();
            resultPayload2.CharId = Account.Id;
            resultPayload2.Name = Account.PlayerName;
            resultPayload2.OwnerId = Account.Id;
            resultPayload2.OwnerName = Account.UserName;
            resultPayload2.GuildId = 0;
            resultPayload2.GuildName = null;
            resultPayload2.GuildRole = 0;
            resultPayload2.Status = 1;
            resultPayload2.ServerId = 0;
            resultPayload2.ServerName = null;
            resultPayload2.Data = _nicknameData;
            resultPayload2.TicketId = payload.TicketId;
            SendReply(writer, resultPayload2);

            ResultStatusMsg resultPayload3 = Payloads.CreatePayload<ResultStatusMsg>();
            resultPayload3.Errorcode = 0;
            resultPayload3.Errormsg = null;
            resultPayload3.TicketId = payload.TicketId;
            SendReply(writer, resultPayload3);
        }

        private void HandleGetChatServer(GetChatServer payload, PayloadWriter writer)
        {
            SendChatServerInfo resultPayload = Payloads.CreatePayload<SendChatServerInfo>();
            resultPayload.ServerId = 1;
            resultPayload.Ip = Config.Get("chat/ip");
            resultPayload.Port = Config.GetInt("chat/port");
            resultPayload.ServerType = payload.ServerType;
            resultPayload.Version = null;
            resultPayload.Data = null;
            resultPayload.TicketId = payload.TicketId;
            SendReply(writer, resultPayload);
        }

        private void HandleSelectNickname(SelectNickname payload, PayloadWriter writer)
        {
            uint playerId = payload.CharId;

            Account account = Program.Accounts.Get(Database.Connection, playerId);
            if (account == null)
            {
                ResultStatusMsg resultPayload1 = Payloads.CreatePayload<ResultStatusMsg>();
                resultPayload1.Errorcode = 1;
                resultPayload1.Errormsg = "Incorrect account";
                resultPayload1.TicketId = payload.TicketId;
                SendReply(writer, resultPayload1);
                return;
            }

            SelectNicknameReply resultPayload2 = Payloads.CreatePayload<SelectNicknameReply>();
            resultPayload2.CharId = account.Id;
            resultPayload2.Name = account.PlayerName;
            resultPayload2.OwnerId = account.Id;
            resultPayload2.OwnerName = account.UserName;
            resultPayload2.GuildId = 0;
            resultPayload2.GuildName = null;
            resultPayload2.GuildRole = 0;
            resultPayload2.Status = 1;
            resultPayload2.ServerId = 0;
            resultPayload2.ServerName = null;
            resultPayload2.Data = _nicknameData;
            resultPayload2.TicketId = payload.TicketId;
            SendReply(writer, resultPayload2);

            ResultStatusMsg resultPayload3 = Payloads.CreatePayload<ResultStatusMsg>();
            resultPayload3.Errorcode = 0;
            resultPayload3.Errormsg = null;
            resultPayload3.TicketId = payload.TicketId;
            SendReply(writer, resultPayload3);
        }

        private void HandleRegisterServer(RegisterServer payload, PayloadWriter writer)
        {
            // TODO
            uint serverId = Program.Servers.Register(payload.Name);
            if (serverId == 0)
            {
                SendReply(writer, Payloads.CreateStatusFailMsg("Failed to register server", payload.TicketId));
                return;
            }

            _server = Program.Servers.Get(serverId);
            if (_server == null)
            {
                Program.Servers.Remove(serverId);
                SendReply(writer, Payloads.CreateStatusFailMsg("Failed to register server", payload.TicketId));
                return;
            }
            
            _server.ConnectionId = Connection;
            _server.OwnerId = Account.Id;
            _server.Description = payload.Description;
            //_server.Ip = payload.Ip ?? Config.Get("lobby/ip");
            _server.Port = payload.Port;
            _server.PlayersTotal = 2;
            _server.PlayersJoined = 0;
            _server.Map = payload.Map;
            _server.Running = false;
            
            SendServerUpdates(payload.TicketId);

            var resultPayload = Payloads.CreatePayload<StatusWithId>();
            resultPayload.Errorcode = 0;
            resultPayload.Errormsg = null;
            resultPayload.Id = _server.Id;
            resultPayload.TicketId = payload.TicketId;
            SendReply(writer, resultPayload);
            
            Logger.Log($"User {Account.UserName} created a new lobby as {payload.Name}");
        }
        private void HandleRegisterServerOld(RegisterServer payload, PayloadWriter writer)
        {
            string name = payload.Name;

            uint serverId = Program.Servers.Register(name);
            if (serverId == 0)
            {
                StatusWithId resultPayload1 = Payloads.CreatePayload<StatusWithId>();
                resultPayload1.Errorcode = 3;
                resultPayload1.Errormsg = "Can not register server";
                resultPayload1.Id = 0;
                resultPayload1.TicketId = payload.TicketId;
                SendReply(writer, resultPayload1);
                return;
            }

            _server = Program.Servers.Get(serverId);
            if (_server == null)
            {
                Program.Servers.Remove(serverId);

                StatusWithId resultPayload1 = Payloads.CreatePayload<StatusWithId>();
                resultPayload1.Errorcode = 3;
                resultPayload1.Errormsg = "Can not register server";
                resultPayload1.Id = 0;
                resultPayload1.TicketId = payload.TicketId;
                SendReply(writer, resultPayload1);
                return;
            }

            /*
            _server.ConnectionId = Connection;
            _server.OwnerId = Account.Id;
            _server.Description = payload.Description;
            _server.Ip = payload.Ip ?? Config.Get("lobby/ip");
            _server.Port = payload.Port;
            _server.Type = payload.ServerType;
            _server.SubType = payload.ServerSubtype;
            _server.MaxPlayers = payload.MaxPlayers;
            _server.RoomId = payload.RoomId;
            _server.Level = payload.Level;
            _server.GameMode = payload.GameMode;
            _server.Hardcore = payload.Hardcore;
            _server.Running = payload.Running;
            _server.LockedConfig = payload.LockedConfig;
            _server.Data = payload.Data;
            if (payload.Cipher != null)
            {
                byte[] serverPassword = Crypto.HandleCipher(payload.Cipher, SessionKey);
                int length = System.Array.FindIndex(serverPassword, b => b == 0);
                string password = System.Text.Encoding.ASCII.GetString(serverPassword, 0, length);
                Logger.LogDebug($" Server password is: {password}");

                _server.NeedsPassword = true;
                _server.Password = password;
            }
            */
            SendServerUpdates();

            StatusWithId resultPayload2 = Payloads.CreatePayload<StatusWithId>();
            resultPayload2.Errorcode = 0;
            resultPayload2.Errormsg = null;
            resultPayload2.Id = _server.Id;
            resultPayload2.TicketId = payload.TicketId;
            SendReply(writer, resultPayload2);
        }

        private void HandleGetServers(GetServers payload, PayloadWriter writer)
        {
            if (!ServerUpdateReceivers.TryAdd(Connection, Account.Id))
            {
                ResultStatusMsg resultPayload1 = Payloads.CreatePayload<ResultStatusMsg>();
                resultPayload1.Errorcode = 3;
                resultPayload1.Errormsg = "Can not get server list";
                resultPayload1.TicketId = payload.TicketId;
                SendReply(writer, resultPayload1);
                return;
            }

            List<Server> servers = Program.Servers.GetServers();

            uint ticketId = payload.TicketId;
            foreach (Server server in servers)
            {
                ServerInfo resultPayload1 = CreateServerInfoPayload(server, ticketId);
                SendReply(writer, resultPayload1);
            }

            ResultStatusMsg resultPayload2 = Payloads.CreatePayload<ResultStatusMsg>();
            resultPayload2.Errorcode = 0;
            resultPayload2.Errormsg = null;
            resultPayload2.TicketId = payload.TicketId;
            SendReply(writer, resultPayload2);
        }

        private static ServerInfo CreateServerInfoPayload(Server server, uint ticketId)
        {
            ServerInfo resultPayload = Payloads.CreatePayload<ServerInfo>();
            resultPayload.ServerId = server.Id;
            resultPayload.Name = server.Name;
            //resultPayload1.OwnerId = server.OwnerId;
            resultPayload.Description = server.Description;
            resultPayload.PlayersTotal = server.PlayersTotal;
            resultPayload.PlayersJoined = server.PlayersJoined;
            resultPayload.Unknown1 = 0;
            resultPayload.Map = server.Map;
            resultPayload.Unknown2 = 0;
            resultPayload.Unknown21 = 0;
            resultPayload.Unknown3 = 10;
            resultPayload.TicketId = ticketId;
            return resultPayload;
        }

        private void HandleStopServerUpdates(StopServerUpdates payload, PayloadWriter writer)
        {
            uint accountId;
            ServerUpdateReceivers.TryRemove(Connection, out accountId);

            SendReply(writer, Payloads.CreateStatusOkMsg(payload.TicketId));
        }

        private void HandlePayload056(Payload56 payload, PayloadWriter writer)
        {
            SendReply(writer, Payloads.CreateStatusOkMsg(payload.TicketId));
        }

        private void HandleRegObserverUserLogin(RegObserverUserLogin payload, PayloadWriter writer)
        {
            //TODO: do I need a proper implementation?
            bool bSendAll = payload.SendAll;
            SendReply(writer, Payloads.CreateStatusOkMsg(payload.TicketId));
        }

        private void HandlePayload157(Payload157 payload, PayloadWriter writer)
        {
            ResultStatusMsg resultPayload = Payloads.CreatePayload<ResultStatusMsg>();
            resultPayload.Errorcode = 0;
            resultPayload.Errormsg = null;
            resultPayload.TicketId = payload.TicketId;
            SendReply(writer, resultPayload);
        }

        private void HandleDeregObserverGlobalChat(DeregObserverGlobalChat payload, PayloadWriter writer)
        {
            // TODO: proper implementation?
            SendReply(writer, Payloads.CreateStatusOkMsg(payload.TicketId));
        }
        
        private void HandleConnectToServer(ConnectToServer payload, PayloadWriter writer)
        {
            uint serverId = payload.ServerId;

            Server server = Program.Servers.Get(serverId);
            if (server == null)
            {
                ResultStatusMsg resultPayload = Payloads.CreatePayload<ResultStatusMsg>();
                resultPayload.Errorcode = 1;
                resultPayload.Errormsg = "Unknown server";
                resultPayload.TicketId = payload.TicketId;
                SendReply(writer, resultPayload);
                return;
            }

            byte[] nonce = Crypto.CreateNonce();

            PlayerConnecting resultPayload2 = Payloads.CreatePayload<PlayerConnecting>();
            resultPayload2.Nonce = nonce;
            resultPayload2.CharId = Account.Id;
            resultPayload2.Name = Account.PlayerName;
            resultPayload2.OwnerId = Account.Id;
            resultPayload2.OwnerName = Account.UserName;
            resultPayload2.GuildId = 0;
            resultPayload2.GuildName = null;
            resultPayload2.GuildRole = 0;
            resultPayload2.Data = _nicknameData;
            SendToLobbyConnection(server.ConnectionId, resultPayload2);

            ConnectToServerReply resultPayload3 = Payloads.CreatePayload<ConnectToServerReply>();
            resultPayload3.PermId = Account.Id;
            resultPayload3.ServerId = serverId;
            resultPayload3.Ip = server.Ip;
            resultPayload3.Port = server.Port;
            resultPayload3.Nonce = nonce;
            resultPayload3.Errorcode = 0;
            resultPayload3.Errormsg = null;
            resultPayload3.TicketId = payload.TicketId;
            SendReply(writer, resultPayload3);
        }

        private void HandleUpdateServerInfo(UpdateServerInfo payload, PayloadWriter writer)
        {
            // TODO
            SendReply(writer, Payloads.CreateStatusOkMsg(payload.TicketId));
            
            Logger.Log($"Server {payload.Name} got updated by {Account.UserName}");
        }
        private void HandleUpdateServerInfoOld(UpdateServerInfo payload, PayloadWriter writer)
        {
            if (_server == null)
            {
                ResultStatusMsg resultPayload1 = Payloads.CreatePayload<ResultStatusMsg>();
                resultPayload1.Errorcode = 3;
                resultPayload1.Errormsg = "No server";
                resultPayload1.TicketId = payload.TicketId;
                SendReply(writer, resultPayload1);
                return;
            }

            /*
            _server.Name = payload.Name;
            _server.Description = payload.Description;
            _server.MaxPlayers = payload.MaxPlayers;
            _server.RoomId = payload.RoomId;
            _server.Level = payload.Level;
            _server.GameMode = payload.GameMode;
            _server.Hardcore = payload.Hardcore;
            _server.Running = payload.Running;
            _server.LockedConfig = payload.LockedConfig;
            _server.Data = payload.Data;
            if (payload.Cipher == null)
            {
                _server.NeedsPassword = false;
                _server.Password = null;
            }
            else
            {
                byte[] serverPassword = Crypto.HandleCipher(payload.Cipher, SessionKey);
                int length = System.Array.FindIndex(serverPassword, b => b == 0);
                string password = System.Text.Encoding.ASCII.GetString(serverPassword, 0, length);
                Logger.LogDebug($" Server password is: {password}");

                _server.NeedsPassword = true;
                _server.Password = password;
            }
            */

            SendServerUpdates();

            ResultStatusMsg resultPayload2 = Payloads.CreatePayload<ResultStatusMsg>();
            resultPayload2.Errorcode = 0;
            resultPayload2.Errormsg = null;
            resultPayload2.TicketId = payload.TicketId;
            SendReply(writer, resultPayload2);
        }

        private void HandlePlayerJoinedServer(PlayerJoinedServer payload, PayloadWriter writer)
        {
            if (_server == null)
            {
                return;
            }

            _server.Players.TryAdd(payload.PermId, Connection);
            SendServerUpdates();
        }

        private void HandlePlayerLeftServer(PlayerLeftServer payload, PayloadWriter writer)
        {
            if (_server == null)
            {
                return;
            }

            uint dummy;
            _server.Players.TryRemove(payload.PermId, out dummy);
            SendServerUpdates();
        }

        private void HandleUnlistServer(UnlistServer payload, PayloadWriter writer)
        {
            // TODO: notify all clients(?) that the server is not listed anymore
            SendReply(writer, Payloads.CreateStatusOkMsg(payload.TicketId));
        }
        
        private void SendServerUpdates(uint ticketId = 0)
        {
            ServerInfo serverInfo = CreateServerInfoPayload(_server, ticketId);
            KeyValuePair<uint, uint>[] servers = ServerUpdateReceivers.ToArray();
            foreach (KeyValuePair<uint, uint> server in servers)
            {
                SendToLobbyConnection(server.Key, serverInfo);
            }
        }

        private void NotifyUnlistServer(uint serverId, bool running)
        {
            UnlistServer unlistInfo = Payloads.CreatePayload<UnlistServer>();
            unlistInfo.ServerId = serverId;
            unlistInfo.Running = running;
            KeyValuePair<uint, uint>[] servers = ServerUpdateReceivers.ToArray();
            foreach (KeyValuePair<uint, uint> server in servers)
            {
                SendToLobbyConnection(server.Key, unlistInfo);
            }
        }
        
        // Chat related payloads
        
        private void HandleRegObserverGlobalChat(RegObserverGlobalChat payload, PayloadWriter writer)
        {
            SendReply(writer, Payloads.CreateStatusOkMsg(payload.TicketId));
        }
        
        private void HandleDeregObserverUserLogin(DeregObserverUserLogin payload, PayloadWriter writer)
        {
            // TODO: proper implementation?
            SendReply(writer, Payloads.CreateStatusOkMsg(payload.TicketId));
        }

        private void HandleChatPayload(ChatPayload payload, PayloadWriter writer)
        {
            // TODO: send to all connected clients?
            // FIXME: fromID is broken, I need something different

            var testPayload = Payloads.CreatePayload<Chat>();
            testPayload.Txt = payload.Txt;
            testPayload.FromId = payload.FromId;

            SendReply(writer, testPayload);
        }
    }
}
