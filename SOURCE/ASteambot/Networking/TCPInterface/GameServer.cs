﻿
using ASteambot.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ASteambot
{
    public class GameServer
    {
        public string Name { get; set; }
        public IPAddress IP { get; private set; }
        public int Port { get; private set; }
        public int ServerID { get; private set; }
        public long SteamID { get; private set; }
        public bool Alive { get; private set; }

        private Socket socket;
        private string tcppasswd;
        private DataQueue dataQueue;

        private bool socketError = false;

        public GameServer(Socket socket, string tcppaswd, int serverid, string ipportname)
        {
            Alive = true;
            string[] srvinfos = ipportname.Split(new[] { '|' }, 4);
            Name = srvinfos[3];
            tcppasswd = tcppaswd;
            SteamID = long.Parse(srvinfos[0]);
            IP = IPAddress.Parse(srvinfos[1]);
            Port = Int32.Parse(srvinfos[2]);
            this.socket = socket;
            ServerID = serverid;
            dataQueue = new DataQueue();
            dataQueue.OnAdd += DataQueue_OnAdd;
            
            FirstSend(ServerID);
        }

        private void DataQueue_OnAdd(object sender, EventArgData e)
        {
            byte[] bytes = e.GetData;
            string str = Encoding.Default.GetString(bytes);

            try
            {
                if (!socket.Connected)
                    socketError = true;
                else
                    socket.BeginSend(bytes, 0, bytes.Length, 0, new AsyncCallback(SendCallback), socket);
            }
            catch(Exception ex)
            {
                socketError = true;
            }

            dataQueue.Remove(bytes);
            dataQueue.Clear();
        }

        public void Update(GameServer gs)
        {
            this.socket = gs.socket;
            this.IP = gs.IP;
            this.Port = gs.Port;
            this.ServerID = gs.ServerID;
        }

        public bool SocketConnected()
        {
            string finaldata = tcppasswd + -1 + ")" + ((int)NetworkCode.ASteambotCode.Simple).ToString() + "|" + "PING" + "<EOF>";
           
            //socket.BeginSend(bytes, 0, bytes.Length, 0, new AsyncCallback(SendCallback), socket);
            int size = Encoding.ASCII.GetByteCount(finaldata);

            if (size > NetworkCode.MAX_CHUNK_SIZE)
            {
                List<string> chunks = ChunksUpto(finaldata, NetworkCode.MAX_CHUNK_SIZE).ToList();
                foreach (string chunk in chunks)
                {
                    if (socketError)
                        break;

                    byte[] data = Encoding.UTF8.GetBytes(chunk);
                    socketError = !QuickSend(data);
                }
            }
            else
            {
                byte[] data = Encoding.UTF8.GetBytes(finaldata);
                socketError = !QuickSend(data);
            }

            return !socketError;
        }

        private bool QuickSend(byte[] data)
        {
            try
            {
                socket.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), socket);
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }

        public void FirstSend(int serverID)
        {
            string finaldata = tcppasswd + "-1)SRVID| " + serverID + "<EOF>";

            //Console.WriteLine(finaldata);
            /*byte[] byteData = Encoding.UTF8.GetBytes(finaldata);
            socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), socket);*/

            int size = Encoding.ASCII.GetByteCount(finaldata);

            if (size > NetworkCode.MAX_CHUNK_SIZE)
            {
                List<string> chunks = ChunksUpto(finaldata, NetworkCode.MAX_CHUNK_SIZE).ToList();
                foreach (string chunk in chunks)
                    dataQueue.Add(Encoding.UTF8.GetBytes(chunk));
            }
            else
            {
                dataQueue.Add(Encoding.UTF8.GetBytes(finaldata));
            }
        }

        private IEnumerable<string> ChunksUpto(string str, int maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }

        public bool Send(int moduleID, NetworkCode.ASteambotCode netcode, string data)
        {
            string finaldata = tcppasswd + moduleID + ")" + ((int)netcode).ToString() + "|" + data + "<EOF>";
            int size = Encoding.ASCII.GetByteCount(finaldata);

            if (size > NetworkCode.MAX_CHUNK_SIZE && !socketError)
            {
                List<string> chunks = ChunksUpto(finaldata, NetworkCode.MAX_CHUNK_SIZE).ToList();
                foreach (string chunk in chunks)
                {
                    if (socketError)
                        break;

                    dataQueue.Add(Encoding.UTF8.GetBytes(chunk));
                    Thread.Sleep(100);
                }
            }
            else
            {
                dataQueue.Add(Encoding.UTF8.GetBytes(finaldata));
            }

            dataQueue.Clear();

            Console.WriteLine("Done, status -> Sucess ? " + !socketError);

            return !socketError;
            /*try
            {
                byte[] byteData = Encoding.UTF8.GetBytes(finaldata);
                if(byteData.Length > 900)
                {
                    for(int i = 0; i < byteData.Length; i+=900)
                    {
                        if (i > byteData.Length)
                            i = byteData.Length;

                        ArrayView<byte> p1 = new ArrayView<byte>(byteData, i, i+900);
                        byte[] bytes = p1.ToArray();
                        dataQueue.Add(bytes);
                        socket.BeginSend(bytes, 0, bytes.Length, 0, new AsyncCallback(SendCallback), socket);
                    }
                }
                else
                {
                    dataQueue.Add(byteData);
                    socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), socket);
                }
            }
            catch (Exception e)
            {
                /*Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e);
                Console.ForegroundColor = ConsoleColor.White;
                PrintSocketError(data);///
                Alive = false;
                return false;
            }*/
        }

        private void SendCallback(IAsyncResult ar)
        {
            //try
            //{
            Socket handler = (Socket)ar.AsyncState;

            int bytesSent = handler.EndSend(ar);
            /*}
            catch(SocketException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Socket crash: ");
                Console.WriteLine(e);
                Console.ForegroundColor = ConsoleColor.White;

                socketError = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }*/
        }

        private void PrintSocketError(string msg)
        {
            Program.PrintErrorMessage("Could not send data : ");
            Program.PrintErrorMessage(msg);
            Program.PrintErrorMessage("to " + Name + " (" + ServerID + ") because the socket is not connected (" + IP + ":" + Port + ") !");
        }

        class ArrayView<T> : IEnumerable<T>
        {
            private readonly T[] array;
            private readonly int offset, count;

            public ArrayView(T[] array, int offset, int count)
            {
                this.array = array;
                this.offset = offset;
                this.count = count;
            }

            public int Length
            {
                get { return count; }
            }

            public T this[int index]
            {
                get
                {
                    if (index < 0 || index >= this.count)
                        throw new IndexOutOfRangeException();
                    else
                        return this.array[offset + index];
                }
                set
                {
                    if (index < 0 || index >= this.count)
                        throw new IndexOutOfRangeException();
                    else
                        this.array[offset + index] = value;
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                for (int i = offset; i < offset + count; i++)
                    yield return array[i];
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                IEnumerator<T> enumerator = this.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }
        }

    }
}
