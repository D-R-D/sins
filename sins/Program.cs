using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace sins
{
    class Program
    {
        static Process proc = new Process();
        static StreamWriter cmdsw;
        static UdpClient udp, udp2, udp3, udp4, udpClient, udpClient2, udpClient3, udpClient4;
        static List<List<string>> container = new List<List<string>>();

        static void Main(string[] args)
        {
            Console.WriteLine("プロセスが実行されました。");


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
        //
        static void ReceiveCallback2(IAsyncResult ar)
        {
            udp2 = (UdpClient)ar.AsyncState;
            IPEndPoint remoteEP = null;
            byte[] rcvBytes = null;

            try { rcvBytes = udp2.EndReceive(ar, ref remoteEP); }
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
            udp2.BeginReceive(ReceiveCallback2, udp2);
        }
        //
        /*plugin用*/
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

            if (rcvcmd == "reboot")
            {
                for (int i = 0; i < container.Count; i++)
                {
                    if (container[i][0] == "mcp_h")
                    {
                        sender(rcvcmd, container[i][1]);
                        cmdsw.WriteLine("alert sys_Message : サーバーのアップデートを開始します。これにはサーバーの再起動を伴うため一旦作業を中断し、10分のちに再ログインして下さい。");
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
        //
        static void ReceiveCallback4(IAsyncResult ar)
        {
            udp4 = (UdpClient)ar.AsyncState;
            IPEndPoint remoteEP = null;
            byte[] rcvBytes = null;

            try { rcvBytes = udp4.EndReceive(ar, ref remoteEP); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            string rcvcmd = Encoding.UTF8.GetString(rcvBytes);
            string[] cmd_sped = rcvcmd.Split(":");

            ilis(cmd_sped, remoteEP.Address);

            udp4.BeginReceive(ReceiveCallback4, udp4);
        }
        //
        /*データ用*/
        //


        //
        /*鯖に送信するやつ*/
        //
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
            ProcessStartInfo p = new ProcessStartInfo("java", "-jar Bungeecordのフルパス");
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
                    for (int i = 0; i <= container.Count; i++)
                    {
                        if (container[i].Contains(cmd_sped[1]))
                        {
                            bl = true;
                        }
                    }


                    if (bl)
                    {
                        ProcessStartInfo start = new ProcessStartInfo("/usr/bin/bash", "コンテナ起動用のシェルのフルパス " + cmd_sped[2]);
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


                    for (int i = 0; i <= container.Count; i++)
                    {


                        if (container[i].Contains(cmd_sped[2]))
                        {
                            ProcessStartInfo stop = new ProcessStartInfo("/usr/bin/bash", "コンテナ停止用のシェルのフルパス " + cmd_sped[2]);
                            Process.Start(stop);

                            container.Remove(container[i]);

                            cmdsw.WriteLine("alert sins_Message : 指定されたコンテナ [ " + cmd_sped[2] + " ] が終了しました。");

                            break;
                        }
                    }


                    Console.WriteLine("Process Err : 指定されたコンテナ [ " + cmd_sped[2] + "] は起動していません。");
                    cmdsw.WriteLine("alert Process Err : 指定されたコンテナ [ " + cmd_sped[2] + "] は起動していません");
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


        static void ilis(string[] spd, IPAddress ip)
        {
            bool ctn = false;
            for (int i = 0; i <= container.Count; i++)
            {
                Console.WriteLine(spd[0] + ":" + spd[1]);
                if (container[i].Contains(spd[0]))
                {
                    if (spd[1] == "stopped")
                    {
                        container.Remove(container[i]);
                        Console.WriteLine(spd[0] + " removed");
                    }
                    else if (spd[1] == "started")
                    {
                        container[i].Add(ip.ToString());
                        Console.WriteLine(container[i][0] +" started " + container[i][1]);
                    }
                    else { }
                    break;
                }
                else
                {
                    if (spd[1] == "starting")
                    {
                        container.Add(new List<string>());
                        container[container.Count].Add(spd[0]);
                        Console.WriteLine(container[container.Count -1][0] + " starting");
                    }
                    else if (spd[1] == "started")
                    {
                        ctn = true;
                    }
                }

                if (ctn)
                {
                    container.Add(new List<string>());
                    container[container.Count].Add(spd[0]);
                    container[container.Count].Add(ip.ToString());
                    Console.WriteLine(container[container.Count - 1][0] + " started " + container[container.Count -1][1]);

                    break;
                }
            }
        }
        //サーバー起動のためのプロセス――――――――――――――――――――――――――――――――――――――――
    }
}