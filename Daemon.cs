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
        readonly PlatformID _platform;
        Assembly _posixAsm;
        Type _unixSignalType, _signumType;
        MethodInfo _unixSignalWaitAny;

        Array _signals;

        static Daemon _instance;
        public static Daemon Instance
        {
            get { return _instance ?? (_instance = new Daemon()); }
        }

        private Daemon()
        {
            _platform = Environment.OSVersion.Platform;
            Setup();
        }

        private void Setup()
        {
            if (_platform != PlatformID.Unix && _platform != PlatformID.MacOSX)
                throw new InvalidOperationException("not unix platform");
            //Unixでデーモン化するための機構
            //静的にアセンブリを読み込むとコンパイラの設定が面倒なので動的に

            _posixAsm = Assembly.Load("Mono.Posix, Version=4.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756");//MonoのPosixライブラリを読み込む
            _unixSignalType = _posixAsm.GetType("Mono.Unix.UnixSignal");//UnixSignal型の作成
            _unixSignalWaitAny = _unixSignalType.GetMethod("WaitAny", new[] { _unixSignalType.MakeArrayType() });//UnixSignal.WaitAny関数
            _signumType = _posixAsm.GetType("Mono.Unix.Native.Signum");//Signum型の作成
            //Signalクラスを立てる

            _signals = Array.CreateInstance(_unixSignalType, 2);
            _signals.SetValue(Activator.CreateInstance(_unixSignalType, _signumType.GetField("SIGINT").GetValue(null)), 0);
            _signals.SetValue(Activator.CreateInstance(_unixSignalType, _signumType.GetField("SIGTERM").GetValue(null)), 1);
        }
        
        /// <summary>
        /// UnixSignalが来るまで待つ
        /// </summary>
        public void WaitForUnixSignal()
        {
            if (_platform != PlatformID.Unix && _platform != PlatformID.MacOSX)
            {
                throw new InvalidOperationException("not unix platform");
            }
            // Wait for a unix signal
            for (bool exit = false; !exit; )
            {
                var id = (int)_unixSignalWaitAny.Invoke(null, new object[] { _signals });

                if (id >= 0 && id < _signals.Length)
                {
                    dynamic val = _signals.GetValue(id);
                    if (val.IsSet) exit = true;
                }
            }
        }
    }
}
