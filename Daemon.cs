/*
The MIT License (MIT)

Copyright (c) 2013 Kazuki Yasufuku

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Daemonizer
{
    /// <summary>
    /// プロセスをLinuxDaemon化するクラス
    /// 
    /// </summary>
    public class Daemon
    {
        PlatformID platform;
        Assembly posixAsm;
        Type unixSignalType, signumType;
        MethodInfo unixSignalWaitAny;

        Array signals;

        static Daemon instance;
        public static Daemon Instance
        {
            get
            {
                if (instance == null)
                    instance = new Daemon();
                return instance;
            }
        }

        private Daemon()
        {
            platform = System.Environment.OSVersion.Platform;
            Setup();
        }

        private void Setup()
        {
            if (platform != PlatformID.Unix && platform != PlatformID.MacOSX)
                throw new InvalidOperationException("not unix platform");
            //Unixでデーモン化するための機構
            //静的にアセンブリを読み込むとコンパイラの設定が面倒なので動的に

            posixAsm = Assembly.Load("Mono.Posix, Version=4.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756");//MonoのPosixライブラリを読み込む
            unixSignalType = posixAsm.GetType("Mono.Unix.UnixSignal");//UnixSignal型の作成
            unixSignalWaitAny = unixSignalType.GetMethod("WaitAny", new Type[1] { unixSignalType.MakeArrayType() });//UnixSignal.WaitAny関数
            signumType = posixAsm.GetType("Mono.Unix.Native.Signum");//Signum型の作成
            //Signalクラスを立てる

            signals = Array.CreateInstance(unixSignalType, 2);
            signals.SetValue(Activator.CreateInstance(unixSignalType, signumType.GetField("SIGINT").GetValue(null)), 0);
            signals.SetValue(Activator.CreateInstance(unixSignalType, signumType.GetField("SIGTERM").GetValue(null)), 1);
        }
        
        /// <summary>
        /// UnixSignalが来るまで待つ
        /// </summary>
        public void WaitForUnixSignal()
        {
            if (platform != PlatformID.Unix && platform != PlatformID.MacOSX)
            {
                throw new InvalidOperationException("not unix platform");
            }
            // Wait for a unix signal
            for (bool exit = false; !exit; )
            {
                int id = (int)unixSignalWaitAny.Invoke(null, new object[1] { signals });

                if (id >= 0 && id < signals.Length)
                {
                    dynamic val = signals.GetValue(id);
                    if (val.IsSet) exit = true;
                }
            }
        }
    }
}
