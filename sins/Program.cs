using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sins
{
    class Program
    {
        static Process proc = new Process();
        static StreamWriter cmdsw;
        static UdpClient udp, udp2, udp3, udp4, udpClient, udpClient2, udpClient3, udpClient4;
        static List<List<string>> container = new List<List<string>>();
        readonly static string udsp = @"/home/nisinn/uds_sock/mc_server/";

        static void Main(string[] args)
        {
            Console.WriteLine("プロセスが実行されました。");
            container.Add(new List<string>());
            container[0].Add("name");
            container[0].Add("ip");


            //非同期udpでポート6001をlisten
            IPEndPoint IPE = new IPEndPoint(IPAddress.Any, 6001);
            udpClient = new UdpClient(IPE);
            udpClient.BeginReceive(ReceiveCallback, udpClient);

            //非同期udpでポート6011をlisten
            IPEndPoint IPE2 = new IPEndPoint(IPAddress.Any, 6011);
            udpClient2 = new UdpClient(IPE2);
            udpClient2.BeginReceive(ReceiveCallback2, udpClient2);

            //非同期udpでポート6021をlisten
            IPEndPoint IPE3 = new IPEndPoint(IPAddress.Any, 6021);
            udpClient3 = new UdpClient(IPE3);
            udpClient3.BeginReceive(ReceiveCallback3, udpClient3);

            //非同期udpでポート7011をlisten
            IPEndPoint IPE4 = new IPEndPoint(IPAddress.Any, 7011);
            udpClient4 = new UdpClient(IPE4);
            udpClient4.BeginReceive(ReceiveCallback4, udpClient4);

            //別スレッドでproxyを実行
            Thread thread = new Thread(new ThreadStart(() =>
            {
                backprocess();
            }));
            thread.Start();
            Thread thread0 = new Thread(new ThreadStart(() =>
            {
                ProcessStartInfo start = new ProcessStartInfo("/usr/bin/bash", "スクリプトのパス " + "mcp_h");
                Process.Start(start);
            }));
            thread0.Start();


            //コード到達時の待機・確認用
            using (ManualResetEvent manualResetEvent = new ManualResetEvent(false))
            {
                Console.WriteLine("コードを実行しました。");
                manualResetEvent.WaitOne();
            }
        }


        //プロセス間通信のためのudp接続―――――――――――――――――――――――――――――――――――――――

        //
        /*bot用ポート6001でlisten*/
        //
        static void ReceiveCallback(IAsyncResult ar)
        {
            udp = (UdpClient)ar.AsyncState;
            IPEndPoint remoteEP = null;
            byte[] rcvBytes = null;

            try { rcvBytes = udp.EndReceive(ar, ref remoteEP); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            string rcvcmd = Encoding.UTF8.GetString(rcvBytes);
            string[] cmd_sped = rcvcmd.Split(":");

            try
            {
                datas(cmd_sped, rcvcmd);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Process Err : 不明なエラーです。cmd = " + rcvcmd + "\nSystem_Message : " + ex.Message);
                cmdsw.WriteLine("alert Process Err : 不明なエラーです。cmd = " + rcvcmd + "   System_Message : " + ex.Message);
            }
            udp.BeginReceive(ReceiveCallback, udp);
        }
        /*bot用*/
        //



        //
        /*plugin用ポート6011でlisten*/
        //hubサーバー　コマンド
        static void ReceiveCallback2(IAsyncResult ar)
        {
            udp2 = (UdpClient)ar.AsyncState;
            IPEndPoint remoteEP = null;
            byte[] rcvBytes = null;

            try { rcvBytes = udp2.EndReceive(ar, ref remoteEP); }
            catch (Exception ex) { cmdsw.WriteLine("alert " + ex.Message); }
            string rcvcmd = Encoding.UTF8.GetString(rcvBytes);
            string[] cmd_sped = rcvcmd.Split(":");

            try
            {
                datas(cmd_sped, rcvcmd);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Process Err : 不明なエラーです。cmd = " + rcvcmd + "\nSystem_Message : " + ex.Message);
                cmdsw.WriteLine("alert Process Err : 不明なエラーです。cmd = " + rcvcmd + "   System_Message : " + ex.Message);
            }
            udp2.BeginReceive(ReceiveCallback2, udp2);
        }
        //
        /*pluginコマンド応答用*/
        //



        //
        /*netcat用ポート6021でlisten*/
        //
        static void ReceiveCallback3(IAsyncResult ar)
        {
            udp3 = (UdpClient)ar.AsyncState;
            IPEndPoint remoteEP = null;
            byte[] rcvBytes = null;

            try { rcvBytes = udp3.EndReceive(ar, ref remoteEP); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            string rcvcmd = Encoding.UTF8.GetString(rcvBytes);
            cmdsw.WriteLine("alert sys_Message : サーバーのアップデートを開始します。これにはサーバーの再起動を伴うため一旦作業を中断し、10分のちに再ログインして下さい。");

            if (rcvcmd == "reboot")
            {
                for (int i = 0; i < container.Count; i++)
                {
                    if (container[i][0] == "mcp_h")
                    {
                        sender(rcvcmd, container[i][1]);
                    }
                }
            }
            udp3.BeginReceive(ReceiveCallback3, udp3);
        }
        //
        /*netcat用*/
        //



        //
        /*データ用ポート7011でlisten*/
        //hubサーバー
        static void ReceiveCallback4(IAsyncResult ar)
        {
            udp4 = (UdpClient)ar.AsyncState;
            IPEndPoint remoteEP = null;
            byte[] rcvBytes = null;

            try { rcvBytes = udp4.EndReceive(ar, ref remoteEP); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            string rcvcmd = Encoding.UTF8.GetString(rcvBytes);
            string[] cmd_sped = rcvcmd.Split(":");

            ilis(cmd_sped, remoteEP.Address.ToString());

            udp4.BeginReceive(ReceiveCallback4, udp4);
        }
        //
        /*ステータス管理用*/
        //


        //
        /*鯖に送信するやつ*/
        //hubサーバー
        static void sender(string msg, string ip)
        {
            UdpClient sudp = new UdpClient();
            byte[] sendBytes = Encoding.UTF8.GetBytes(msg);

            sudp.Send(sendBytes, sendBytes.Length, ip, 7001);
        }
        //
        /*鯖に送信するやつ*/
        //

        //プロセス間通信のためのudp接続―――――――――――――――――――――――――――――――――――――――


        //サーバー起動のためのプロセス――――――――――――――――――――――――――――――――――――――――
        static void backprocess()
        {
            ProcessStartInfo p = new ProcessStartInfo("java", "-jar プロキシサーバーのフルパス");
            p.WorkingDirectory = Directory.GetCurrentDirectory();
            p.CreateNoWindow = true;
            p.UseShellExecute = false;
            p.RedirectStandardInput = true;
            p.RedirectStandardOutput = false;
            p.RedirectStandardError = false;

            proc.StartInfo = p;
            proc.Start();
            cmdsw = proc.StandardInput;

            proc.WaitForExit();
        }

        static void datas(string[] cmd_sped, string rcvcmd)
        {
            if (cmd_sped[0] == "container")
            {
                if (cmd_sped[1] == "start")
                {
                    bool bl = false;
                    try
                    {
                        for (int i = 0; i < container.Count; i++)
                        {
                            if (container[i].Contains(cmd_sped[2]))
                            {
                                bl = true;
                                break;
                            }
                        }
                    }
                    catch (Exception ex) { cmdsw.WriteLine("alert : ExMessage0 : " + ex.Message); }


                    if (!bl)
                    {
                        ProcessStartInfo start = new ProcessStartInfo("/usr/bin/bash", "スクリプトのフルパス " + cmd_sped[2]);
                        Process.Start(start);

                        cmdsw.WriteLine("alert sins_Message : 指定されたコンテナ [ " + cmd_sped[2] + " ] が起動しました。");
                    }
                    else
                    {
                        Console.WriteLine("Process Err : 指定されたコンテナ [ " + cmd_sped[2] + " ] は既に起動しています。");
                        cmdsw.WriteLine("alert Process Err : 指定されたコンテナ [ " + cmd_sped[2] + " ] は既に起動しています。");
                    }
                }


                else if (cmd_sped[1] == "stop")
                {
                    //stopコマンドは動作時にプロキシが再起動することがあるので削除、通知メッセージのみを送信する。
                    cmdsw.WriteLine("alert sins_Message : 指定されたコンテナ [ " + cmd_sped[2] + " ] は終了しています。");
                }


                else if (cmd_sped[1] == "dead")
                {
                    Console.WriteLine("Process message : コンテナ [ " + cmd_sped[2] + " ] は終了しました。");
                    cmdsw.WriteLine("alert Process message : コンテナ [ " + cmd_sped[2] + " ] は終了しました。");
                }


                else
                {
                    Console.WriteLine("UDP Err : 不明なコマンドを受信しました。\ncmd_content : " + rcvcmd);
                    cmdsw.WriteLine("alert UDP Err : 不明なコマンドを受信しました。\ncmd_content : " + rcvcmd);
                }
            }


            else if (cmd_sped[0] == "alert")
            {
                cmdsw.WriteLine("alert [from discord + " + cmd_sped[1] + " ] : " + cmd_sped[2]);
            }


            else
            {
                cmdsw.WriteLine(rcvcmd);
            }
        }

        //起動・終了等のステータスの記憶、そしてipアドレスをlistで管理する。
        static void ilis(string[] spd, string ip)
        {
            //spd content == [containername] : [serverstatus]

            int n = 0;
            if(spd[1] == "starting")
            {
                for(int i = 0 ; i < container.Count ; i++)
                {
                    if (container[i][0] == spd[0])
                    {
                        container[i][1] == spd[1];
                        container[i][2] == ip.ToString();
                    }
                    else
                    {
                        container.Add(new List<string>());
                        container[container.Count - 1].Add(spd);
                        container[container.Count - 1].Add(ip.ToString());
                    }
                }
            }
            else if(spd[1] == "started")
            {
                for(int i = 0 ; i < container.Count ; i++)
                {
                    if (container[i][0] == spd[0])
                    {
                        container[i][1] == spd[1];
                        container[i][2] == ip.ToString();
                    }
                    else
                    {
                        container.Add(new List<string>());
                        container[container.Count - 1].Add(spd);
                        container[container.Count - 1].Add(ip.ToString());
                    }
                }
            }
            else if(spd[1] == "stopped")
            {
                for (int i = 0 ; i < container.Count ; i++)
                {
                    if(container[i][0] == spd[0])
                    {
                        container.RemoveAt(i);
                    }
                    else { }
                    return;
                }
            }
        }

        static void unix_sock_server(string path)
        {
            string mine_path = udsp + path + "/receive";

            try
            {
                if(Directory.Exists(udsp + path))
                { Directory.CreateDirectory(udsp + path); }
                //ソケットファイルがあったら処す
                if (File.Exists(mine_path))
                { File.Delete(mine_path); }

                //ソケットは使ったらゴミ箱へ！！
                using (var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP))
                {
                    //エンドポイントを作ってソケットにBindする
                    var EP = new UnixDomainSocketEndPoint(mine_path);
                    socket.Bind(EP);

                    while (true)
                    {
                        //通信受け入れ開始
                        socket.Listen(1);
                        //要求があれば受け入れる
                        var s = socket.Accept();

                        //もらったbyte配列をUTF-8でエンコードしてConsoleにWriteLineする
                        byte[] buffer = new byte[1024];
                        var numberOfBytesReceived = s.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                        var sign = Encoding.UTF8.GetString(buffer, 0, numberOfBytesReceived);

                        var ssign = sign.Split(":");

                        if (ssign[1] != "stopped")
                        { datas(ssign, sign); }
                        else { break; }
                    }
                }
            }
            //エラー通知
            catch (Exception e) { Console.WriteLine(e.Message); }
        }
        //サーバー起動のためのプロセス――――――――――――――――――――――――――――――――――――――――
    }
}