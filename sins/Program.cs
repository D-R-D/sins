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
        static UdpClient udp, udp2, udpClient,udpClient2;
        static List<string> container = new List<string>();

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

                //ここのコンテナ起動する部分は不要

                if (cmd_sped[0] == "container")
                {
                    if (cmd_sped[1] == "start")
                    {
                        if (!container.Contains(cmd_sped[2]))
                        {
                            container.Add(cmd_sped[2]);

                            ProcessStartInfo start = new ProcessStartInfo("/usr/bin/bash", "コンテナ起動用のシェルのフルパス "+cmd_sped[2]);
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
                        if (container.Contains(cmd_sped[2]))
                        {
                            ProcessStartInfo stop = new ProcessStartInfo("/usr/bin/bash", "コンテナ停止用のシェルのフルパス " + cmd_sped[2]);
                            Process.Start(stop);

                            container.Remove(cmd_sped[2]);

                            cmdsw.WriteLine("alert sins_Message : 指定されたコンテナ [ " + cmd_sped[2] + " ] が終了しました。");
                        }
                        else
                        {
                            Console.WriteLine("Process Err : 指定されたコンテナ [ " + cmd_sped[2] + "] は起動していません。");
                            cmdsw.WriteLine("alert Process Err : 指定されたコンテナ [ " + cmd_sped[2] + "] は起動していません");
                        }
                    }
                    else if (cmd_sped[1] == "dead")
                    {
                        Console.WriteLine("Process message : コンテナ [ " + cmd_sped[2] + " ] は終了しました。");
                        cmdsw.WriteLine("alert Process message : コンテナ [ " + cmd_sped[2] + " ] は終了しました。");
                        container.Remove(cmd_sped[2]);
                    }
                    else
                    {
                        Console.WriteLine("UDP Err : 不明なコマンドを受信しました。\ncmd_content : " + rcvcmd);
                        cmdsw.WriteLine("alert UDP Err : 不明なコマンドを受信しました。\ncmd_content : " + rcvcmd);
                    }
                }
                else
                {
                    cmdsw.WriteLine(rcvcmd);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Process Err : 不明なエラーです。cmd = " + rcvcmd +"\nSystem_Message : "+ex.Message);
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
                if (cmd_sped[0] == "container")
                {
                    if (cmd_sped[1] == "start")
                    {
                        if (!container.Contains(cmd_sped[2]))
                        {
                            container.Add(cmd_sped[2]);

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
                        if (container.Contains(cmd_sped[2]))
                        {
                            ProcessStartInfo stop = new ProcessStartInfo("/usr/bin/bash", "コンテナ停止用のシェルのフルパス " + cmd_sped[2]);
                            Process.Start(stop);

                            container.Remove(cmd_sped[2]);

                            cmdsw.WriteLine("alert sins_Message : 指定されたコンテナ [ " + cmd_sped[2] + " ] が終了しました。");
                        }
                        else
                        {
                            Console.WriteLine("Process Err : 指定されたコンテナ [ " + cmd_sped[2] + "] は起動していません。");
                            cmdsw.WriteLine("alert Process Err : 指定されたコンテナ [ " + cmd_sped[2] + "] は起動していません");
                        }
                    }
                    else if (cmd_sped[1] == "dead")
                    {
                        Console.WriteLine("Process message : コンテナ [ " + cmd_sped[2] + " ] は終了しました。");
                        cmdsw.WriteLine("alert Process message : コンテナ [ " + cmd_sped[2] + " ] は終了しました。");
                        container.Remove(cmd_sped[2]);
                    }
                    else
                    {
                        Console.WriteLine("UDP Err : 不明なコマンドを受信しました。\ncmd_content : " + rcvcmd);
                        cmdsw.WriteLine("alert UDP Err : 不明なコマンドを受信しました。\ncmd_content : " + rcvcmd);
                    }
                }
                else
                {
                    cmdsw.WriteLine(rcvcmd);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Process Err : 不明なエラーです。cmd = " + rcvcmd + "\nSystem_Message : " + ex.Message);
                cmdsw.WriteLine("alert Process Err : 不明なエラーです。cmd = " + rcvcmd + "   System_Message : " + ex.Message);
            }
            udp2.BeginReceive(ReceiveCallback2, udp2);
        }
        //
        /*plugin用*/
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
        //サーバー起動のためのプロセス――――――――――――――――――――――――――――――――――――――――
    }
}