﻿SharpDaemon
===========

C#でDaemonを作るためのライブラリです。Windowsでもビルドは正しく通る(Daemonにはならないけど)ように作成

使い方
====

+ コンソールプロジェクトを作る
+ Mainで起動処理を行ったあと、Daemon.Instance.WaitForUnixSignalを呼び出してプロセス終了を待つ(WindowsだとInvalidOperationExceptionが飛ぶので、catchしてConsole.ReadKeyでもしてあげてください)
+ あとは/etc/init/下に.confファイルを作ってstart-stop-daemonを使ってdaemon化しましょう(詳細はググれ)
